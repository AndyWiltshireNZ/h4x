using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LevelManager : MonoBehaviour
{
	[SerializeField] private LevelManagerDefinition LevelManagerData;

	private int currentLevelIndex = 0;
	public int CurrentLevelIndex { get { return currentLevelIndex; } set { currentLevelIndex = value; }	}

	private AssetReference levelToLoadAssetReference;
	private Level currentLevel;
	public Level CurrentLevel { get { return currentLevel; } }

	private AsyncOperationHandle<GameObject> instantiatedHandle;
	private bool hasInstantiatedHandle = false;

	public void Setup()
	{
		Debug.Log( "LevelManager started." );

		// Load the test level if true
		if ( LevelManagerData.TestLevelAssetReference != null && LevelManagerData.TestLevelAssetReference.RuntimeKeyIsValid() )
		{
			levelToLoadAssetReference = LevelManagerData.TestLevelAssetReference;
			currentLevelIndex = -1;
			//Debug.Log( "Test Level to load: " + (levelToLoadAssetReference.RuntimeKey != null ? levelToLoadAssetReference.RuntimeKey.ToString() : "(null)") );
			
			_ = LoadLevelAsync();
			return;
		}

		// Load current level index - select from available LevelData in LevelManagerData
		if ( currentLevelIndex >= 0 )
		{
			levelToLoadAssetReference = LevelManagerData.availableLevels[ currentLevelIndex ].LevelAssetReference;

			if ( levelToLoadAssetReference == null || !levelToLoadAssetReference.RuntimeKeyIsValid() )
			{
				Debug.LogError( "Level to load AssetReference is null or invalid." );
				return;
			}

			var def = LevelManagerData.availableLevels[ currentLevelIndex ];
			if ( def == null )
			{
				Debug.LogError( $"LevelDefinition at index {currentLevelIndex} is null." );
				return;
			}

			levelToLoadAssetReference = def.LevelAssetReference;
			//Debug.Log( "Level to load: " + (levelToLoadAssetReference?.RuntimeKey != null ? levelToLoadAssetReference.RuntimeKey.ToString() : "(null)") );

			_ = LoadLevelAsync();
		}
	}

	private async Task LoadLevelAsync()
	{
		if ( levelToLoadAssetReference == null )
			return;

		AsyncOperationHandle<GameObject> asyncLoadLevel = levelToLoadAssetReference.InstantiateAsync();
		await asyncLoadLevel.Task;
		if ( asyncLoadLevel.Status == AsyncOperationStatus.Succeeded )
		{
			// Store the handle so we can release the instance later
			instantiatedHandle = asyncLoadLevel;
			hasInstantiatedHandle = true;

			GameObject levelObj = asyncLoadLevel.Result;
			currentLevel = levelObj.GetComponent<Level>();
			if ( currentLevel != null )
			{
				Debug.Log( "Level loaded: " + levelObj.name );
				currentLevel.Setup();
			}
			else
			{
				Debug.LogError( "Loaded level does not have a Level component." );
			}
		}
		else
		{
			Debug.LogError( "Failed to load level." );
		}
	}

	public void UnloadLevel()
	{
		if ( hasInstantiatedHandle && instantiatedHandle.IsValid() )
		{
			Addressables.ReleaseInstance( instantiatedHandle );
			hasInstantiatedHandle = false;
			Debug.Log( "Addressables level instance released." );
			return;
		}

		// Fallback: find any active Level instance and destroy it
		Level level = GameObject.FindFirstObjectByType<Level>();
		if ( level != null )
		{
			Destroy( level.gameObject );
			Debug.Log( "Fallback: Level GameObject destroyed." );
		}
		else
		{
			Debug.LogWarning( "No loaded Level instance found to unload." );
		}
	}

	private void OnDestroy()
	{
		// Ensure any instantiated addressable is released when this manager is destroyed
		if ( hasInstantiatedHandle && instantiatedHandle.IsValid() )
		{
			Addressables.ReleaseInstance( instantiatedHandle );
			hasInstantiatedHandle = false;
		}
	}
}
