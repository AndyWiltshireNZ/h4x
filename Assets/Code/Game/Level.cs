using UnityEngine;
using System.Collections;

public enum LevelState
{
	Default,
	LevelStart,
	LevelRun,
	LevelEnd
}

public class Level : MonoBehaviour
{
	[SerializeField] private LevelDefinition levelData;
	public LevelDefinition LevelData { get { return levelData; } }

	[SerializeField] private CPUManager cpuManager;
	public CPUManager CPUManager { get { return cpuManager; } }

	private LevelState currentLevelState;

	private void Awake()
    {
        this.gameObject.SetActive( false );
		cpuManager.gameObject.SetActive( false );
    }

	public void Setup()
	{
		this.gameObject.SetActive( true );
		cpuManager.gameObject.SetActive( true );

		InitializeStateMachine();
	}

	private void InitializeStateMachine()
	{
		SetState( LevelState.LevelStart );
	}

	private void SetState( LevelState newLevelState )
	{
		if ( currentLevelState == newLevelState )
		{
			return;
		}

		OnExitState( currentLevelState );
		currentLevelState = newLevelState;
		OnEnterState( currentLevelState );

		GameMode.Instance.UIManager.HUDController.UpdateDebugText();
	}

	private void OnEnterState( LevelState state )
	{
		switch ( state )
		{
			case LevelState.LevelStart:
				Debug.Log( "Level: Entering LevelStart" );
				cpuManager.Setup();
				StartCoroutine ( EnterStartThenRun() );
				break;

			case LevelState.LevelRun:
				Debug.Log( "Level: Entering LevelRun" );
				break;

			case LevelState.LevelEnd:
				Debug.Log( "Level: Entering LevelEnd" );
				break;
		}
	}

	private IEnumerator EnterStartThenRun()
	{
		yield return new WaitForSeconds( 2f );

		SetState( LevelState.LevelRun );
	}

	private void OnExitState( LevelState state )
	{
		// any clean up required when exiting a state
		switch ( state )
		{
			case LevelState.LevelStart:
				break;
			case LevelState.LevelRun:
				break;
			case LevelState.LevelEnd:
				break;
		}
	}

	private void Update()
	{
		if ( currentLevelState == LevelState.LevelRun )
		{
			UpdateRun();
		}
	}

	private void UpdateRun()
	{
		// Level runtime logic goes here.
	}

	public void EndLevel()
	{
		SetState( LevelState.LevelEnd );
	}

	// Expose current state for other systems
	public LevelState CurrentLevelState => currentLevelState;
}
