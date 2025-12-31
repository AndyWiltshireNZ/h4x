using UnityEngine;
using System.Collections;

public enum LevelState
{
	Default,
	LevelStart,
	LevelRun,
	LevelEndWon,
	LevelEndLost
}

public class Level : MonoBehaviour
{
	[SerializeField] private LevelDefinition levelData;
	public LevelDefinition LevelData { get { return levelData; } }

	[SerializeField] private CPUManager cpuManager;
	public CPUManager CPUManager { get { return cpuManager; } }

	private UpgradeManager upgradeManager;

	private float currentHackTimer;
	public float CurrentHackTimer { get { return currentHackTimer; } private set { currentHackTimer = value; } }
	private float hackTimerElapsed;

	private LevelState currentLevelState;
	public LevelState CurrentLevelState => currentLevelState;

	private void Awake()
    {
        this.gameObject.SetActive( false );
		cpuManager.gameObject.SetActive( false );
    }

	public void Setup()
	{
		this.gameObject.SetActive( true );
		cpuManager.gameObject.SetActive( true );

		upgradeManager = GameMode.Instance.UpgradeManager;

		InitializeStateMachine();
	}

	private void InitializeStateMachine()
	{
		SetState( LevelState.LevelStart );
	}

	public void SetState( LevelState newLevelState )
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

				// set initial hack timer value based on upgrade level
				hackTimerElapsed = upgradeManager.CurrentHackTime;
				GameMode.Instance.UIManager.HUDController.UpdateHackTimer( upgradeManager.CurrentHackTime );
				GameMode.Instance.UIManager.HUDController.UpdateHackTimerText( 0 ); // Get Ready text

				StartCoroutine ( EnterStartThenRun() );
				break;

			case LevelState.LevelRun:
				Debug.Log( "Level: Entering LevelRun" );

				GameMode.Instance.UIManager.HUDController.UpdateHackTimerText( 1 ); // Hack Time Remaining text

				break;

			case LevelState.LevelEndWon:
				Debug.Log( "Level: Entering LevelEndWon" );

				// triggered by cpu reaching max level

				break;
			case LevelState.LevelEndLost:
				Debug.Log( "Level: Entering LevelEndLost" );

				// triggered by hack timer reaching zero

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
			case LevelState.LevelEndWon:
				break;
			case LevelState.LevelEndLost:
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
		if ( hackTimerElapsed > 0f )
		{
			hackTimerElapsed -= Time.deltaTime;
			GameMode.Instance.UIManager.HUDController.UpdateHackTimer( hackTimerElapsed );
		}
		else
		{
			hackTimerElapsed = 0f;
			GameMode.Instance.UIManager.HUDController.UpdateHackTimer( hackTimerElapsed );
			EndLevelLost();
		}
	}

	public void EndLevelLost()
	{
		cpuManager?.PathwayManager?.SpawnManager?.StopEverything();
		SetState( LevelState.LevelEndLost );
	}
}
