using UnityEngine;
using UnityEngine.InputSystem;

public class CPUManager : MonoBehaviour
{
	private Level currentLevel;
	private LevelDefinition currentLevelData;

	[SerializeField] private PathwayManager pathwayManager;
	public PathwayManager PathwayManager { get { return pathwayManager; } }

	private InputManager inputManager;

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

	public void Setup ()
	{
		currentLevel = GameMode.Instance.LevelManager.CurrentLevel;
		currentLevelData = GameMode.Instance.LevelManager.CurrentLevel.LevelData;

		currentCPULevel = currentLevelData.StartCPULevel;
		GameMode.Instance.UIManager.HUDController.UpdateDebugText();

		inputManager = GameMode.Instance.InputManager;
		inputManager.InputActions.Game.DebugCpuUp.performed += DebugCPULevelUp_pressed;
		inputManager.InputActions.Game.DebugCpuDown.performed += DebugCPULevelDown_pressed;

		pathwayManager.SetupPathways( currentCPULevel );
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
}
