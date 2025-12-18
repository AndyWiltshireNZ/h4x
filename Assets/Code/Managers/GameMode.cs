using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

using static GameModeDefinition;

public class GameMode : MonoBehaviour
{
	public static GameMode Instance;
	
	[SerializeField] private GameModeDefinition GameModeDefinition;

	public UIManager UIManager = null;
	public InputManager InputManager = null;
	public LevelManager LevelManager = null;
	
	private CameraController currentCamera;
	public CameraController CurrentCamera	{ get { return currentCamera; } private set { currentCamera = value; } }

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

		currentCamera = Camera.main.GetComponent<CameraController>();

		AsyncOperationHandle<GameObject> asyncSpawnUIManager = GameModeDefinition.UIManagerAssetReference.InstantiateAsync();
		await asyncSpawnUIManager.Task;
		if ( asyncSpawnUIManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject uimanagerObj = asyncSpawnUIManager.Result;
			UIManager = uimanagerObj.GetComponent<UIManager>();
			UIManager.Setup();
		}

		AsyncOperationHandle<GameObject> asyncSpawnInputManager = GameModeDefinition.InputManagerAssetReference.InstantiateAsync();
		await asyncSpawnInputManager.Task;
		if ( asyncSpawnInputManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject inputmanagerObj = asyncSpawnInputManager.Result;
			InputManager = inputmanagerObj.GetComponent<InputManager>();
			InputManager.Setup();
		}

		AsyncOperationHandle<GameObject> asyncSpawnLevelManager = GameModeDefinition.LevelManagerAssetReference.InstantiateAsync();
		await asyncSpawnLevelManager.Task;
		if ( asyncSpawnLevelManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject modemanagerObj = asyncSpawnLevelManager.Result;
			LevelManager = modemanagerObj.GetComponent<LevelManager>();
			LevelManager.Setup();
		}
	}

    private void Update()
    {
        
    }
}
