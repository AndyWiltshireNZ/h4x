using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

using static GameModeDefinition;

public enum GameState
{
	Default,
	GameStart,
	GameMenu,
	GameRun,
	GamePause,
	GameEnd
}

public class GameMode : MonoBehaviour
{
	public static GameMode Instance;
	
	[SerializeField] private GameModeDefinition GameModeData;
	public GameModeDefinition GameModeDefinition { get { return GameModeData; } }

	private GameState currentGameState;

	public UIManager UIManager = null;
	public InputManager InputManager = null;
	public UpgradeManager UpgradeManager = null;
	public LevelManager LevelManager = null;
	
	private Camera currentCamera;
	public Camera CurrentCamera	=> currentCamera;

	private Camera uiCamera;
	public Camera UICamera => uiCamera;

	private GameModeType currentGameMode;
	public GameModeType CurrentGameMode	{ get { return currentGameMode; } private set { currentGameMode = value; } }

	private AsyncOperationHandle<GameObject> asyncSpawnUIManager;
	private AsyncOperationHandle<GameObject> asyncSpawnInputManager;
	private AsyncOperationHandle<GameObject> asyncSpawnUpgradeManager;
	private AsyncOperationHandle<GameObject> asyncSpawnLevelManager;
	private AsyncOperationHandle<GameObject> asyncSpawnAudioManager;

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
		if (asyncSpawnUIManager.IsValid())
			Addressables.Release( asyncSpawnUIManager );
		if (asyncSpawnInputManager.IsValid() )
			Addressables.Release( asyncSpawnInputManager );
		if (asyncSpawnUpgradeManager.IsValid() )
			Addressables.Release( asyncSpawnUpgradeManager );
		if (asyncSpawnLevelManager.IsValid() )
			Addressables.Release( asyncSpawnLevelManager );
		if (asyncSpawnAudioManager.IsValid() )
			Addressables.Release( asyncSpawnAudioManager );

		Instance = null;
		UIManager = null;
		InputManager = null;
		UpgradeManager = null;
		LevelManager = null;
	}

	private async void init()
	{
		Debug.Log( "GameMode initialized." );

		currentCamera = Camera.main;
		uiCamera = currentCamera.gameObject.GetComponent<CameraController>().UICamera;

		asyncSpawnInputManager = GameModeData.InputManagerAssetReference.InstantiateAsync();
		await asyncSpawnInputManager.Task;
		if ( asyncSpawnInputManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject inputmanagerObj = asyncSpawnInputManager.Result;
			InputManager = inputmanagerObj.GetComponent<InputManager>();
			InputManager.Setup();
		}

		asyncSpawnUpgradeManager = GameModeData.UpgradeManagerAssetReference.InstantiateAsync();
		await asyncSpawnUpgradeManager.Task;
		if ( asyncSpawnUpgradeManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject upgrademanagerObj = asyncSpawnUpgradeManager.Result;

			// Run Initial Setup - then Level.cs re-reruns Setup on level reloads
			UpgradeManager = upgrademanagerObj.GetComponent<UpgradeManager>();
			UpgradeManager.Setup();
		}

		asyncSpawnLevelManager = GameModeData.LevelManagerAssetReference.InstantiateAsync();
		await asyncSpawnLevelManager.Task;
		if ( asyncSpawnLevelManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject levelmanagerObj = asyncSpawnLevelManager.Result;
			LevelManager = levelmanagerObj.GetComponent<LevelManager>();
			LevelManager.Setup();
		}

		asyncSpawnUIManager = GameModeData.UIManagerAssetReference.InstantiateAsync();
		await asyncSpawnUIManager.Task;
		if ( asyncSpawnUIManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject uimanagerObj = asyncSpawnUIManager.Result;
			UIManager = uimanagerObj.GetComponent<UIManager>();
			UIManager.Setup();
		}

		asyncSpawnAudioManager = GameModeData.AudioManagerAssetReference.InstantiateAsync();
		await asyncSpawnAudioManager.Task;
		if ( asyncSpawnAudioManager.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject audiomanagerObj = asyncSpawnAudioManager.Result;
		}

		InitializeStateMachine();
	}

    private void InitializeStateMachine()
	{
		SetState( GameState.GameStart );
	}

	private void SetState( GameState newGameState )
	{
		if ( currentGameState == newGameState )
		{
			return;
		}

		OnExitState( currentGameState );
		currentGameState = newGameState;
		OnEnterState( currentGameState );
	}
	
	private void OnEnterState( GameState state )
	{
		switch ( state )
		{
			case GameState.GameStart:
				Debug.Log( "Game: Entering GameStart" );
				// perform any start initialization then transition to run / menu
				StartCoroutine ( EnterStartThenRun() );
				break;
			case GameState.GameMenu:
				Debug.Log( "Game: Entering GameMenu" );
				// main menu
				break;
			case GameState.GameRun:
				Debug.Log( "Game: Entering GameRun" );
				// gameplay begins
				break;
			case GameState.GamePause:
				Debug.Log( "Game: Entering GamePause" );
				// gameplay paused
				break;
			case GameState.GameEnd:
				Debug.Log( "Game: Entering GameEnd" );
				// cleanup
				break;
		}
	}

	private IEnumerator EnterStartThenRun()
	{
		yield return null; // wait a frame
		//yield return new WaitForSeconds( 1f );

		SetState( GameState.GameRun );
	}

	private void OnExitState( GameState state )
	{
		// any clean up required when exiting a state
		switch ( state )
		{
			case GameState.GameStart:
				break;
			case GameState.GameMenu:
				break;
			case GameState.GameRun:
				break;
			case GameState.GamePause:
				break;
			case GameState.GameEnd:
				break;
		}
	}

	private void Update()
	{
		if ( currentGameState == GameState.GameRun )
		{
			State_UpdateRun();
		}
	}

	private void State_UpdateRun()
	{
		// Game runtime logic goes here.
	}

	public void EndGame()
	{
		SetState( GameState.GameEnd );
	}

	// Expose current state for other systems
	public GameState CurrentGameState => currentGameState;
}
