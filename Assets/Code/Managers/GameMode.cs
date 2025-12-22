using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

using static GameModeDefinition;

public class GameMode : MonoBehaviour
{
	public static GameMode Instance;
	
	[SerializeField] private GameModeDefinition GameModeData;

	public UIManager UIManager = null;
	public InputManager InputManager = null;
	public LevelManager LevelManager = null;
	
	private Camera currentCamera;
	public Camera CurrentCamera	{ get { return currentCamera; } private set { currentCamera = value; } }

	private GameModeType currentGameMode;
	public GameModeType CurrentGameMode	{ get { return currentGameMode; } private set { currentGameMode = value; } }

	private void Awake ()
    {
        if ( Instance != null && Instance != this )
		{
			Destroy( this );
		}
		else
		{
			Instance = this;
		}

		init();
    }

	private void OnDestroy()
	{
		Instance = null;
	}

	private async void init()
	{
		Debug.Log( "GameMode initialized." );

		currentCamera = Camera.main;

		AsyncOperationHandle<GameObject> asyncSpawnUIManager = GameModeData.UIManagerAssetReference.InstantiateAsync();
		await asyncSpawnUIManager.Task;
		if ( asyncSpawnUIManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject uimanagerObj = asyncSpawnUIManager.Result;
			UIManager = uimanagerObj.GetComponent<UIManager>();
			UIManager.Setup();
		}

		AsyncOperationHandle<GameObject> asyncSpawnInputManager = GameModeData.InputManagerAssetReference.InstantiateAsync();
		await asyncSpawnInputManager.Task;
		if ( asyncSpawnInputManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject inputmanagerObj = asyncSpawnInputManager.Result;
			InputManager = inputmanagerObj.GetComponent<InputManager>();
			InputManager.Setup();
		}

		AsyncOperationHandle<GameObject> asyncSpawnLevelManager = GameModeData.LevelManagerAssetReference.InstantiateAsync();
		await asyncSpawnLevelManager.Task;
		if ( asyncSpawnLevelManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject modemanagerObj = asyncSpawnLevelManager.Result;
			LevelManager = modemanagerObj.GetComponent<LevelManager>();
			LevelManager.Setup();
		}

		AsyncOperationHandle<GameObject> asyncSpawnAudioManager = GameModeData.AudioManagerAssetReference.InstantiateAsync();
		await asyncSpawnAudioManager.Task;
		if ( asyncSpawnAudioManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject audiomanagerObj = asyncSpawnAudioManager.Result;
		}
	}

    private void Update()
    {
        
    }
}
