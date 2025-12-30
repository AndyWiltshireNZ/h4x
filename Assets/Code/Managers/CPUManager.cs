using UnityEngine;
using UnityEngine.InputSystem;

public class CPUManager : MonoBehaviour
{
	private Level currentLevel;
	private LevelDefinition currentLevelData;

	[SerializeField] private PathwayManager pathwayManager;
	public PathwayManager PathwayManager { get { return pathwayManager; } }

	private InputManager inputManager;

	[SerializeField] private CPUCore cpuCore;

	[SerializeField] private XPThresholdsDefinition xpThresholdsData;

	private int currentCPULevel;
	public int CurrentCPULevel
	{
		get { return currentCPULevel; }
		set
		{
			currentCPULevel = Mathf.Clamp( value, currentLevelData.StartCPULevel, currentLevelData.EndCPULevel );
			GameMode.Instance.UIManager.HUDController.UpdateDebugText();
		}
	}

	private int currentXP;
	public int CurrentXP
	{
		get { return currentXP; }
		private set
		{
			currentXP = Mathf.Max( 0, value );
			GameMode.Instance.UIManager.HUDController.UpdateDebugText();
		}
	}

	public void Setup ()
	{
		currentLevel = GameMode.Instance.LevelManager.CurrentLevel;
		currentLevelData = GameMode.Instance.LevelManager.CurrentLevel.LevelData;

		currentCPULevel = currentLevelData.StartCPULevel;
		CurrentXP = 0;

		ValidateXpThresholds();

		GameMode.Instance.UIManager.HUDController.UpdateDebugText();

		inputManager = GameMode.Instance.InputManager;
		inputManager.InputActions.Game.DebugCpuUp.performed += DebugCPULevelUp_pressed;
		inputManager.InputActions.Game.DebugCpuDown.performed += DebugCPULevelDown_pressed;
		inputManager.InputActions.Game.DebugAddXP.performed += DebugCPUAddXP_pressed;

		pathwayManager.SetupPathways( currentCPULevel );

		cpuCore.Setup( currentLevel, this );
	}

	private void OnDisable()
	{
		Unsubscribe();
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	private void Unsubscribe()
	{
		if ( inputManager != null )
		{
			inputManager.InputActions.Game.DebugCpuUp.performed -= DebugCPULevelUp_pressed;
			inputManager.InputActions.Game.DebugCpuDown.performed -= DebugCPULevelDown_pressed;
			inputManager.InputActions.Game.DebugAddXP.performed -= DebugCPUAddXP_pressed;
		}
	}

	private void DebugCPULevelUp_pressed( InputAction.CallbackContext ctx )
	{
		if ( ctx.phase != InputActionPhase.Performed ) return;
		if ( CurrentCPULevel < currentLevelData.EndCPULevel )
			UpdateCPULevelUp();
	}

	private void DebugCPULevelDown_pressed( InputAction.CallbackContext ctx )
	{
		if ( ctx.phase != InputActionPhase.Performed ) return;
		if ( CurrentCPULevel > currentLevelData.StartCPULevel )
			UpdateCPULevelDown();
	}

	private void DebugCPUAddXP_pressed( InputAction.CallbackContext ctx )
	{
		if ( ctx.phase != InputActionPhase.Performed ) return;
		AddXP( 50 ); // add 50 XP for debug
	}

	private void UpdateCPULevelUp()
	{
		CurrentCPULevel += 1;

		pathwayManager.UpdatePathwaysAdd( CurrentCPULevel );
	}

	private void UpdateCPULevelDown()
	{
		CurrentCPULevel -= 1;

		pathwayManager.UpdatePathwaysRemove( CurrentCPULevel );
	}

	// Public API to add XP. When threshold is reached the CPU levels up and XP carries over.
	public void AddXP( int amount )
	{
		if ( amount <= 0 ) return;

		CurrentXP += amount;

		TryLevelUp();
	}

	private void TryLevelUp()
	{
		if ( currentLevelData == null ) return;
		if ( xpThresholdsData == null ) return;
		if ( xpThresholdsData.Thresholds == null ) return;

		int start = currentLevelData.StartCPULevel;
		int end = currentLevelData.EndCPULevel;

		// Already at max level
		if ( CurrentCPULevel >= end )
		{
			Debug.Log( "CPUManager: Reached max CPU level." );
			return;
		}

		// Index into thresholds based on current CPU level
		int index = CurrentCPULevel - start;
		if ( index < 0 || index >= xpThresholdsData.Thresholds.Length ) return;

		int threshold = xpThresholdsData.Thresholds[index];

		if ( CurrentXP >= threshold )
		{
			// Level up
			CurrentCPULevel += 1;

			// Apply pathway changes for new level
			pathwayManager.UpdatePathwaysAdd( CurrentCPULevel );

			// Carryover XP: subtract threshold instead of resetting to zero
			CurrentXP -= threshold;

			// If still enough XP to level again, attempt another level up
			TryLevelUp();
		}
	}

	private void ValidateXpThresholds()
	{
		if ( currentLevelData == null || xpThresholdsData == null || xpThresholdsData.Thresholds == null ) return;

		int required = Mathf.Max( 0, currentLevelData.EndCPULevel - currentLevelData.StartCPULevel );
		if ( xpThresholdsData.Thresholds.Length < required )
		{
			Debug.LogWarning( "CPUManager: xpThresholdsDefinition.Thresholds length is less than required. Some levels will not have thresholds." );
		}
	}
}
