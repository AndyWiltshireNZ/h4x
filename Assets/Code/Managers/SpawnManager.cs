using System.Collections;
using System.Collections.Generic;
using SplineArchitect.Objects;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SpawnManager : MonoBehaviour
{
	[SerializeField] private Spawn[] spawns;
	private Pathway[] availablePathways;
	private LevelDefinition currentLevelData;

	// minimum world-space distance between spawned entities (configurable in inspector)
	[SerializeField] private float _minSpawnDistance = 2f;

	// last global spawn time to enforce a minimum time between any two spawned entities
	private float _lastGlobalSpawnTime = -Mathf.Infinity;

	// flag to prevent the very first spawned entity from moving
	private bool _firstSpawnPrevented = false;

	// running coroutines and instantiated handles so we can stop / release on destroy
	private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();

	// Serialized list so Unity inspector can show spawned objects grouped by pathway.
	// Keep a runtime dictionary for fast lookup.
	[System.Serializable]
	private class SpawnedPerPathway
	{
		public Pathway Pathway;
		public List<GameObject> Instances = new List<GameObject>();
	}

	[SerializeField] private List<SpawnedPerPathway> _spawnedObjectsByPathwaySerialized = new List<SpawnedPerPathway>();
	private readonly Dictionary<Pathway, List<GameObject>> _spawnedObjectsByPathway = new Dictionary<Pathway, List<GameObject>>();

	// track active addressables instantiate handles so we can release/cancel them if coroutines are stopped
	private readonly List<AsyncOperationHandle<GameObject>> _activeInstantiateHandles = new List<AsyncOperationHandle<GameObject>>();

	// track claimed pathways to prevent multiple spawn routines instantiating on the same pathway at once
	private readonly HashSet<Pathway> _claimedPathways = new HashSet<Pathway>();

	private void Awake()
	{
		RebuildSpawnedDictionaryFromSerialized();
	}

	private void OnDisable()
	{
		StopAllSpawning();
		ReleaseAllSpawned();
	}

	private void OnDestroy()
	{
		StopAllSpawning();
		ReleaseAllSpawned();
	}

	/// <summary>
	/// Start a delayed setup to run after 1 second.
	/// </summary>
	public void Setup( Pathway[] pathways )
	{
		Coroutine co = StartCoroutine( DelayedSetupRoutine( pathways ) );
		_runningCoroutines.Add( co );
	}

	// Delayed setup coroutine — waits 1 second then runs the original setup logic.
	private IEnumerator DelayedSetupRoutine( Pathway[] pathways )
	{
		yield return new WaitForSeconds( 1f );

		currentLevelData = GameMode.Instance.LevelManager.CurrentLevel.LevelData;
		availablePathways = pathways;

		if ( currentLevelData == null )
		{
			Debug.LogWarning( "SpawnManager.Setup: currentLevelData is null" );
			yield break;
		}

		// determine CPU level and select correct WaveDefinition (wave 1 -> cpu level 1)
		int cpuLevel = 1;
		if ( GameMode.Instance != null && GameMode.Instance.LevelManager.CurrentLevel.CPUManager != null )
		{
			cpuLevel = Mathf.Clamp( GameMode.Instance.LevelManager.CurrentLevel.CPUManager.CurrentCPULevel, 1, int.MaxValue );
		}

		if ( currentLevelData.Waves == null || currentLevelData.Waves.Length == 0 )
		{
			Debug.LogWarning( "SpawnManager.Setup: No WaveDefinitions defined in LevelDefinition" );
			yield break;
		}

		int waveIndex = Mathf.Clamp( cpuLevel - 1, 0, currentLevelData.Waves.Length - 1 );
		WaveDefinition selectedWaveDef = currentLevelData.Waves[ waveIndex ];

		if ( selectedWaveDef == null || selectedWaveDef.Entities == null || selectedWaveDef.Entities.Length == 0 )
		{
			Debug.LogWarning( $"SpawnManager.Setup: WaveDefinition for CPU level {cpuLevel} is empty or null" );
			yield break;
		}

		// Clear any existing handles / coroutines
		StopAllSpawning();

		// Build runtime spawn list from selectedWaveDef.Entities
		// NOTE: Spawn interval will now come from WaveDefinition.SpawnInterval, not from individual Wave entries.
		spawns = new Spawn[ selectedWaveDef.Entities.Length ];
		for ( int i = 0; i < selectedWaveDef.Entities.Length; i++ )
		{
			Wave w = selectedWaveDef.Entities[ i ];
			Spawn s = new Spawn
			{
				Prefab = w.EntityAssetReference,
				EntitySpeed = selectedWaveDef.entitySpeed
			};
			spawns[i] = s;
		}

		// Start waves sequentially (no inter-wave delay configured here)
		Coroutine waveSequence = StartCoroutine( WavesSequenceRoutine( selectedWaveDef ) );
		_runningCoroutines.Add( waveSequence );
	}

	private IEnumerator SpawnRoutine( Spawn spawn, float waveSpawnInterval )
	{
		// wait until the spawn prefab has a valid runtime key (addressable assigned)
		float wait = 0f;
		const float timeout = 10f;
		while ( spawn.Prefab != null && !spawn.Prefab.RuntimeKeyIsValid() && wait < timeout )
		{
			yield return new WaitForSeconds( 0.1f );
			wait += 0.1f;
		}

		if ( spawn.Prefab == null || !spawn.Prefab.RuntimeKeyIsValid() )
		{
			Debug.LogError( $"SpawnManager: invalid AssetReference on spawn ({spawn?.Prefab}). Aborting spawn routine.", this );
			yield break;
		}

		// track last used pathway for this spawn routine so we avoid using the same pathway twice in a row
		Pathway lastPathway = null;

		while ( true )
		{
			// collect enabled pathways to pick from
			List<Pathway> candidates = GetEnabledPathwaysForSpawn( spawn );
			if ( candidates.Count == 0 )
			{
				// no enabled pathways right now - wait and retry
				yield return new WaitForSeconds( waveSpawnInterval );
				continue;
			}

			// Spawn a single instance per spawn event (SpawnQuantity feature removed).
			// Start with the list of currently enabled candidates
			List<Pathway> pickList = new List<Pathway>( candidates );

			// Filter out pathways whose spawn start point is too close to any existing spawned object.
			// If all candidates are filtered out, we fall back to the original candidate list to avoid blocking spawns.
			List<Pathway> distanceFiltered = new List<Pathway>();
			for ( int i = 0; i < pickList.Count; i++ )
			{
				Pathway p = pickList[ i ];
				if ( p == null || p.Spline == null || !p.gameObject.activeInHierarchy )
				{
					continue;
				}

				Vector3 localPos = p.Spline.GetPositionFastLocal(0);
				Vector3 worldPos = p.Spline.transform.TransformPoint( localPos );
				if ( !IsSpawnPositionTooCloseToAnySpawn( worldPos ) )
				{
					distanceFiltered.Add( p );
				}
			}

			if ( distanceFiltered.Count > 0 )
			{
				pickList = distanceFiltered;
			}

			// If more than one candidate is available avoid selecting the same pathway as last time.
			if ( pickList.Count > 1 && lastPathway != null )
			{
				pickList.Remove( lastPathway );
				// fallback in case removal emptied the list for some reason
				if ( pickList.Count == 0 )
				{
					pickList = new List<Pathway>( candidates );
				}
			}

			// choose a pathway
			Pathway pathway = pickList[ Random.Range( 0, pickList.Count ) ];
			if ( pathway == null || pathway.Spline == null || !pathway.gameObject.activeInHierarchy )
			{
				// do not update lastPathway if selection was invalid; try next iteration
				yield return null;
				continue;
			}

			// record selected pathway so next spawn does not use it if other pathways exist
			lastPathway = pathway;

			// try to claim the selected pathway so no other SpawnRoutine can instantiate on it concurrently
			if ( !TryClaimPathway( pathway ) )
			{
				// claimed by another routine; give it a short delay and retry
				yield return new WaitForSeconds( 0.05f );
				continue;
			}

			// get randomized interval for this spawn (now based on the wave-level interval)
			float randomizedInterval = GetRandomizedInterval( waveSpawnInterval );

			// enforce global minimum time between any two spawned entities (uses randomizedInterval as minimum)
			float timeSinceLast = Time.time - _lastGlobalSpawnTime;
			float requiredInterval = Mathf.Max( 0.01f, randomizedInterval );
			float waitTime = Mathf.Max( 0f, requiredInterval - timeSinceLast );
			if ( waitTime > 0f )
			{
				yield return new WaitForSeconds( waitTime );
			}
			// update the global last spawn time immediately before starting instantiate to prevent races
			_lastGlobalSpawnTime = Time.time;

			// Use AssetReference.InstantiateAsync rather than LoadAssetAsync + Instantiate
			Vector3 pos = new Vector3 ( 0f, 0f, 1000f );
			Quaternion rot = Quaternion.identity;
			Transform parent = pathway.Spline.transform;

			AsyncOperationHandle<GameObject> instantiateHandle = spawn.Prefab.InstantiateAsync( pos, rot, parent );

			// track the handle so we can cancel/release if StopAllSpawning is called mid-load
			_activeInstantiateHandles.Add( instantiateHandle );

			yield return instantiateHandle;

			// remove from tracking list once operation finished (success or fail)
			_activeInstantiateHandles.Remove( instantiateHandle );

			if ( instantiateHandle.Status == AsyncOperationStatus.Succeeded && instantiateHandle.Result != null )
			{
				GameObject go = instantiateHandle.Result;

				// register in spawned dictionary for future cleanup (grouped by pathway)
				AddSpawnedForPathway( pathway, go );

				// Tell spline to register follower (CreateFollower will add SplineObject and initialize)
				pathway.Spline.CreateFollower( go, new Vector3( 0f, 0f, 0.5f ), rot, false, parent );

				// Don't move first spawned entity
				if ( !_firstSpawnPrevented )
				{
					RemovedSpawnedFromList( go );
					go.SetActive( false );
					_firstSpawnPrevented = true;
				}
				else
				{
					// Apply configured wave entity speed to the mover before enabling movement.
					MoveAlongPathway mover = go.GetComponent<MoveAlongPathway>();
					if ( mover != null )
					{
						mover.Speed = new Vector3( 0f, 0f, spawn.EntitySpeed );
						mover.Setup();
					}
					else
					{
						// Still call Setup if component exists but null check handled above.
					}

					// Pass spawn manager and entity speed into EntityBase.Setup
					go.GetComponent<EntityBase>()?.Setup( this, spawn.EntitySpeed );
				}
			}
			else
			{
				Debug.LogError( $"SpawnManager: Failed to instantiate addressable {spawn.Prefab.RuntimeKey}. Status: {instantiateHandle.Status}" );
				// ensure we release a failed handle
				if ( instantiateHandle.IsValid() ) Addressables.Release( instantiateHandle );
			}

			// release pathway claim so other routines may use it
			ReleaseClaimedPathway( pathway );

			// randomized delay after spawn before next spawn
			yield return new WaitForSeconds( randomizedInterval );

			// small yield before next batch to prevent tight loop
			yield return null;
		}
	}

	// Returns spawnInterval randomized by +/- 2 second and clamped to a sensible minimum.
	private float GetRandomizedInterval( float baseInterval )
	{
		float randomOffset = Random.Range( -1f, 1f );
		float interval = baseInterval + randomOffset;
		return Mathf.Max( 0.01f, interval );
	}

	/// <summary>
	/// Sequence through waves in order and start its SpawnRoutine for each configured spawn.
	/// </summary>
	private IEnumerator WavesSequenceRoutine( WaveDefinition waveDef )
	{
		if ( waveDef == null || waveDef.Entities == null )
		{
			yield break;
		}

		for ( int i = 0; i < waveDef.Entities.Length; i++ )
		{
			// guard against array mismatches
			if ( spawns == null || i < 0 || i >= spawns.Length )
			{
				yield break;
			}

			Spawn s = spawns[ i ];
			Wave w = waveDef.Entities[ i ];

			if ( s != null )
			{
				// pass the wave-level SpawnInterval into the routine
				Coroutine co = StartCoroutine( SpawnRoutine( s, Mathf.Max( 0.01f, waveDef.SpawnInterval ) ) );
				_runningCoroutines.Add( co );
			}
		}
	}

	private List<Pathway> GetEnabledPathwaysForSpawn(Spawn spawn)
	{
		List<Pathway> result = new List<Pathway>();
		if ( availablePathways == null || availablePathways.Length == 0 )
		{
			return result;
		}

		for ( int i = 0; i < availablePathways.Length; i++ )
		{
			Pathway p = availablePathways[ i ];
			if ( p != null && p.gameObject.activeInHierarchy )
			{
				result.Add( p );
			}
		}

		return result;
	}

	// Returns true if worldPos is within _minSpawnDistance of any currently tracked spawned instance.
	private bool IsSpawnPositionTooCloseToAnySpawn( Vector3 worldPos )
	{
		if ( _spawnedObjectsByPathway == null || _spawnedObjectsByPathway.Count == 0 )
		{
			return false;
		}

		foreach ( KeyValuePair<Pathway, List<GameObject>> kv in _spawnedObjectsByPathway )
		{
			List<GameObject> list = kv.Value;
			if ( list == null )
			{
				continue;
			}

			for ( int i = 0; i < list.Count; i++ )
			{
				GameObject go = list[ i ];
				if ( go == null )
				{
					continue;
				}

				// Check distance only in xz plane, ignore y distance for spawn position checks.
				Vector3 flatGoPos = go.transform.position;
				flatGoPos.y = 0f;
				Vector3 flatWorldPos = worldPos;
				flatWorldPos.y = 0f;

				if ( Vector3.Distance( flatGoPos, flatWorldPos ) < _minSpawnDistance )
				{
					return true;
				}
			}
		}

		return false;
	}

	public void StopAllSpawning()
	{
		// cancel any in-progress instantiate operations (release their handles).
		// Completed instantiates will be cleaned up by ReleaseAllSpawned.
		for ( int i = _activeInstantiateHandles.Count - 1; i >= 0; i-- )
		{
			AsyncOperationHandle<GameObject> handle = _activeInstantiateHandles[ i ];
			if ( handle.IsValid() && !handle.IsDone )
			{
				Addressables.Release( handle );
			}
		}
		_activeInstantiateHandles.Clear();

		for ( int i = 0; i < _runningCoroutines.Count; i++ )
		{
			if ( _runningCoroutines[ i ] != null )
			{
				StopCoroutine( _runningCoroutines[ i ] );
			}
		}
		_runningCoroutines.Clear();
		// clear claimed pathways as coroutines have been stopped
		_claimedPathways.Clear();
	}

	private void ReleaseAllSpawned()
	{
		// Iterate serialized list so inspector state matches runtime
		for ( int e = 0; e < _spawnedObjectsByPathwaySerialized.Count; e++ )
		{
			SpawnedPerPathway entry = _spawnedObjectsByPathwaySerialized[ e ];
			if ( entry == null || entry.Instances == null )
			{
				continue;
			}

			for ( int i = 0; i < entry.Instances.Count; i++ )
			{
				GameObject go = entry.Instances[ i ];
				if ( go != null )
				{
					// use Addressables.ReleaseInstance for objects created with InstantiateAsync
					Addressables.ReleaseInstance( go );
				}
			}
		}

		_spawnedObjectsByPathwaySerialized.Clear();
		_spawnedObjectsByPathway.Clear();
	}

	public void RemovedSpawnedFromList( GameObject go )
	{
		if ( go == null )
		{
			return;
		}

		// find and remove the GameObject from whichever pathway list it exists in
		for ( int k = _spawnedObjectsByPathwaySerialized.Count - 1; k >= 0; k-- )
		{
			SpawnedPerPathway entry = _spawnedObjectsByPathwaySerialized[ k ];
			if ( entry == null || entry.Instances == null )
			{
				continue;
			}

			for ( int i = entry.Instances.Count - 1; i >= 0; i-- )
			{
				if ( entry.Instances[ i ] == go )
				{
					entry.Instances.RemoveAt( i );
				}
			}

			if ( entry.Instances.Count == 0 )
			{
				_spawnedObjectsByPathwaySerialized.RemoveAt( k );
				_spawnedObjectsByPathway.Remove( entry.Pathway );
			}
			else
			{
				// keep dictionary in sync
				if ( entry.Pathway != null )
				{
					_spawnedObjectsByPathway[ entry.Pathway ] = entry.Instances;
				}
			}
		}
	}

	private void AddSpawnedForPathway( Pathway pathway, GameObject go )
	{
		if ( pathway == null || go == null )
		{
			return;
		}

		// runtime dictionary - ensure runtime list is independent of serialized list to avoid double-add
		if ( !_spawnedObjectsByPathway.TryGetValue( pathway, out List<GameObject> list ) )
		{
			list = new List<GameObject>();
			_spawnedObjectsByPathway[ pathway ] = list;
		}

		if ( !list.Contains( go ) )
		{
			list.Add( go );
		}

		// serialized inspector list - keep in sync but avoid duplicate entries
		SpawnedPerPathway serializedEntry = null;
		for ( int i = 0; i < _spawnedObjectsByPathwaySerialized.Count; i++ )
		{
			SpawnedPerPathway s = _spawnedObjectsByPathwaySerialized[ i ];
			if ( s != null && s.Pathway == pathway )
			{
				serializedEntry = s;
				break;
			}
		}

		if ( serializedEntry == null )
		{
			serializedEntry = new SpawnedPerPathway { Pathway = pathway, Instances = new List<GameObject>() };
			_spawnedObjectsByPathwaySerialized.Add( serializedEntry );
		}

		if ( !serializedEntry.Instances.Contains( go ) )
		{
			serializedEntry.Instances.Add( go );
		}
	}

	private void RebuildSpawnedDictionaryFromSerialized()
	{
		_spawnedObjectsByPathway.Clear();

		for ( int i = 0; i < _spawnedObjectsByPathwaySerialized.Count; i++ )
		{
			SpawnedPerPathway entry = _spawnedObjectsByPathwaySerialized[ i ];
			if ( entry == null || entry.Pathway == null || entry.Instances == null )
			{
				continue;
			}

			// copy the serialized list into a new runtime list to avoid shared references
			_spawnedObjectsByPathway[ entry.Pathway ] = new List<GameObject>( entry.Instances );
		}
	}

	// Optional accessor for other systems
	public List<GameObject> GetSpawnedForPathway( Pathway pathway )
	{
		if ( pathway == null )
		{
			return null;
		}

		if ( _spawnedObjectsByPathway.TryGetValue( pathway, out List<GameObject> list ) )
		{
			return list;
		}

		return null;
	}

	// Try to claim a pathway for the current coroutine; returns true if claimed.
	private bool TryClaimPathway( Pathway pathway )
	{
		if ( pathway == null )
		{
			return false;
		}

		if ( _claimedPathways.Contains( pathway ) )
		{
			return false;
		}

		_claimedPathways.Add( pathway );
		return true;
	}

	// Release a previously claimed pathway.
	private void ReleaseClaimedPathway( Pathway pathway )
	{
		if ( pathway == null )
		{
			return;
		}

		_claimedPathways.Remove( pathway );
	}
}