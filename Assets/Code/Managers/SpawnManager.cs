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
	/// Setup spawn manager with the currently available pathways.
	/// This will build runtime spawn entries from the LevelDefinition -> WaveDefinitions -> Wave data
	/// using the current CPU level to pick the active WaveDefinition.
	/// </summary>
	public void Setup( Pathway[] pathways )
	{
		currentLevelData = GameMode.Instance.LevelManager.CurrentLevel.LevelData;
		availablePathways = pathways;

		if ( currentLevelData == null )
		{
			Debug.LogWarning( "SpawnManager.Setup: currentLevelData is null" );
			return;
		}

		// determine CPU level and select correct WaveDefinition (wave 1 -> cpu level 1)
		int cpuLevel = 1;
		if ( GameMode.Instance != null && GameMode.Instance.LevelManager.CurrentLevel.CPUManager != null )
		{
			cpuLevel = Mathf.Clamp( GameMode.Instance.LevelManager.CurrentLevel.CPUManager.CurrentCPULevel, 1, int.MaxValue );
		}

		if ( currentLevelData.WaveDefinitions == null || currentLevelData.WaveDefinitions.Length == 0 )
		{
			Debug.LogWarning( "SpawnManager.Setup: No WaveDefinitions defined in LevelDefinition" );
			return;
		}

		int waveIndex = Mathf.Clamp( cpuLevel - 1, 0, currentLevelData.WaveDefinitions.Length - 1 );
		WaveDefinition selectedWaveDef = currentLevelData.WaveDefinitions[ waveIndex ];

		if ( selectedWaveDef == null || selectedWaveDef.Waves == null || selectedWaveDef.Waves.Length == 0 )
		{
			Debug.LogWarning( $"SpawnManager.Setup: WaveDefinition for CPU level {cpuLevel} is empty or null" );
			return;
		}

		// Clear any existing handles / coroutines
		StopAllSpawning();

		// Build runtime spawn list from selectedWaveDef.Waves
		spawns = new Spawn[ selectedWaveDef.Waves.Length ];
		for ( int i = 0; i < selectedWaveDef.Waves.Length; i++ )
		{
			Wave w = selectedWaveDef.Waves[ i ];
			Spawn s = new Spawn
			{
				Prefab = w.EntityAssetReference,
				SpawnQuantity = Mathf.Max( 1, w.SpawnQuantity ),
				SpawnInterval = Mathf.Max( 0.01f, w.SpawnInterval ),
				AutoStart = w.AutoStart
			};
			spawns[i] = s;
		}

		// Start waves sequentially, using TimeBetweenWaves from the selected WaveDefinition
		Coroutine waveSequence = StartCoroutine( WavesSequenceRoutine( selectedWaveDef ) );
		_runningCoroutines.Add( waveSequence );
	}

	private IEnumerator SpawnRoutine( Spawn spawn )
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

		while ( true )
		{
			// collect enabled pathways to pick from
			List<Pathway> candidates = GetEnabledPathwaysForSpawn( spawn );
			if ( candidates.Count == 0 )
			{
				// no enabled pathways right now - wait and retry
				yield return new WaitForSeconds( spawn.SpawnInterval );
				continue;
			}

			// spawn SpawnQuantity instances, spacing by SpawnInterval
			for ( int q = 0; q < spawn.SpawnQuantity; q++ )
			{
				Pathway pathway = candidates[ Random.Range( 0, candidates.Count ) ];
				if ( pathway == null || pathway.Spline == null || !pathway.gameObject.activeInHierarchy )
				{
					continue;
				}

				// Use AssetReference.InstantiateAsync rather than LoadAssetAsync + Instantiate
				Vector3 pos = pathway.Spline.GetPositionFastLocal(0);
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
					pathway.Spline.CreateFollower( go, new Vector3( 0f, 0.5f, 0f ), rot, false, parent );

					// Setup movement/entity as before
					go.GetComponent<MoveAlongPathway>()?.Setup();

					EntityBase entity = go.GetComponent<EntityBase>();
					entity?.Setup( this );
				}
				else
				{
					Debug.LogError( $"SpawnManager: Failed to instantiate addressable {spawn.Prefab.RuntimeKey}. Status: {instantiateHandle.Status}" );
					// ensure we release a failed handle
					if ( instantiateHandle.IsValid() ) Addressables.Release( instantiateHandle );
				}

				yield return new WaitForSeconds( spawn.SpawnInterval );
			}

			// small yield before next batch to prevent tight loop
			yield return null;
		}
	}

	/// <summary>
	/// Sequence through waves in order. For each wave, if the wave is marked AutoStart start its SpawnRoutine.
	/// Waits the WaveDefinition.TimeBetweenWaves value between each wave.
	/// </summary>
	private IEnumerator WavesSequenceRoutine( WaveDefinition waveDef )
	{
		if ( waveDef == null || waveDef.Waves == null )
		{
			yield break;
		}

		float delay = Mathf.Max( 0f, waveDef.TimeBetweenWaves );

		for ( int i = 0; i < waveDef.Waves.Length; i++ )
		{
			// guard against array mismatches
			if ( spawns == null || i < 0 || i >= spawns.Length )
			{
				yield break;
			}

			Spawn s = spawns[ i ];
			Wave w = waveDef.Waves[ i ];

			if ( s != null && ( w != null && w.AutoStart ) )
			{
				Coroutine co = StartCoroutine( SpawnRoutine( s ) );
				_runningCoroutines.Add( co );
			}

			// wait between waves unless this was the last wave
			if ( i < waveDef.Waves.Length - 1 && delay > 0f )
			{
				yield return new WaitForSeconds( delay );
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
}