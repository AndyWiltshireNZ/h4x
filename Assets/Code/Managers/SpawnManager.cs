using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

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
	[SerializeField] private List<GameObject> _spawnedObjects = new List<GameObject>();

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

		// Build runtime spawn list from selectedWaveDef.Waves
		StopAllSpawning();
		ReleaseAllSpawned();

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

				// Load the prefab via Addressables then instantiate using normal Instantiate
				AsyncOperationHandle<GameObject> loadHandle = spawn.Prefab.LoadAssetAsync<GameObject>();
				yield return loadHandle;

				if ( loadHandle.Status == AsyncOperationStatus.Succeeded && loadHandle.Result != null )
				{
					GameObject prefab = loadHandle.Result;

					Vector3 pos = pathway.Spline.positionMap[ 0 ];
					Quaternion rot = Quaternion.identity;
					Transform parent = pathway.Spline.transform;

					GameObject spawnObj = Instantiate( prefab, pos, rot, parent );

					Addressables.Release( loadHandle );

					_spawnedObjects.Add( spawnObj );

					SplineObject splineObject = spawnObj.GetComponent<SplineObject>();

					if ( splineObject != null )
					{
						// use spline-local properties so spline system places it correctly at start
						splineObject.localSplinePosition = new Vector3( 0f, 0.5f, 0f );
						splineObject.localSplineRotation = Quaternion.identity;
						splineObject.followAxels = Vector3Int.zero;
						splineObject.type = SplineObject.Type.FOLLOWER;
						splineObject.componentMode = ComponentMode.ACTIVE;
						//splineObject.snapMode = SplineObject.SnapMode.SPLINE_OBJECTS;
						//splineObject.Initalize();
						splineObject.gameObject.GetComponent<MoveAlongPathway>()?.Setup();

						EntityBase entity = spawnObj.GetComponent<EntityBase>();
						entity.Setup( this );
					}
				}
				else
				{
					Debug.LogError( $"SpawnManager: Failed to load addressable {spawn.Prefab.RuntimeKey}. Status: {loadHandle.Status}" );
					// ensure we release on failure
					if ( loadHandle.IsValid() ) Addressables.Release( loadHandle );
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
		for ( int i = 0; i < _spawnedObjects.Count; i++ )
		{
			GameObject go = _spawnedObjects[ i ];
			if ( go != null )
			{
				Destroy( go );
			}
		}
		_spawnedObjects.Clear();
	}

	public void RemovedSpawnedFromList( GameObject go )
	{
		if ( go == null )
		{
			return;
		}

		for ( int i = _spawnedObjects.Count - 1; i >= 0; i-- )
		{
			GameObject item = _spawnedObjects[ i ];
			if ( item == go )
			{
				_spawnedObjects.RemoveAt( i );
			}
		}
	}

}