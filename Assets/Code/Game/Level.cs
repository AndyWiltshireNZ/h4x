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

		cpuManager.Setup( levelData, this );

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
				// perform any start initialization then transition to run
				StartCoroutine ( EnterStartThenRun() );
				break;

			case LevelState.LevelRun:
				Debug.Log( "Level: Entering LevelRun" );
				// gameplay begins
				break;

			case LevelState.LevelEnd:
				Debug.Log( "Level: Entering LevelEnd" );
				// cleanup / show results
				break;
		}
	}

	private IEnumerator EnterStartThenRun()
	{
		yield return new WaitForSeconds( 1f );

		SetState( LevelState.LevelRun );
	}

	private void OnExitState( LevelState state )
	{
		switch ( state )
		{
			case LevelState.LevelStart:
				// cleanup start-specific resources if needed
				break;
			case LevelState.LevelRun:
				// pause gameplay or stop timers on ending level run state
				break;
			case LevelState.LevelEnd:
				// final cleanup
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
		// When level completion condition is met, call EndLevel().
	}

	public void EndLevel()
	{
		SetState( LevelState.LevelEnd );
	}

	// Expose current state for other systems
	public LevelState CurrentLevelState => currentLevelState;
}
