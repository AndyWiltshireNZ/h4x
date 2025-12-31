using UnityEngine;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

public class CPUManager : MonoBehaviour
{
	private Level currentLevel;
	private LevelDefinition currentLevelData;

	[SerializeField] private CPUManagerDefinition cpuManagerData;

	[SerializeField] private PathwayManager pathwayManager;
	public PathwayManager PathwayManager { get { return pathwayManager; } }

	private InputManager inputManager;

	[SerializeField] private CPUCore cpuCore;

	[SerializeField] private XPThresholdsDefinition xpThresholdsData;

	private AsyncOperationHandle<GameObject> asyncSpawnCPUCanvas;
	private CPUCanvasController cpuCanvasController;

	private int currentCPULevel;
	public int CurrentCPULevel
	{
		get { return currentCPULevel; }
		set
		{
			if ( currentLevelData != null )
			{
				currentCPULevel = Mathf.Clamp( value, currentLevelData.StartCPULevel, currentLevelData.EndCPULevel );
			}
			else
			{
				currentCPULevel = value;
			}

			UpdateHUDDebugText();
		}
	}

	private int currentXP;
	public int CurrentXP
	{
		get { return currentXP; }
		private set
		{
			currentXP = Mathf.Max( 0, value );
			UpdateHUDDebugText();
		}
	}

	private int nextXP;
	public int NextXP
	{
		get { return nextXP; }
		private set
		{
			nextXP = Mathf.Max( 0, value );
			UpdateHUDDebugText();
		}
	}

	private HUDController HUD => GameMode.Instance?.UIManager?.HUDController;

	public async void Setup ()
	{
		currentLevel = GameMode.Instance.LevelManager.CurrentLevel;
		currentLevelData = GameMode.Instance.LevelManager.CurrentLevel.LevelData;

		currentCPULevel = currentLevelData.StartCPULevel;
		CurrentXP = 0;

		ValidateXpThresholds();

		UpdateNextXP();

		UpdateHUDDebugText();

		inputManager = GameMode.Instance.InputManager;
		if ( GameMode.Instance.GameModeDefinition.DebugMode == true )
		{
			inputManager.InputActions.Game.DebugCpuUp.performed += DebugCPULevelUp_pressed;
			inputManager.InputActions.Game.DebugCpuDown.performed += DebugCPULevelDown_pressed;
			inputManager.InputActions.Game.DebugAddXP.performed += DebugCPUAddXP_pressed;
		}

		pathwayManager.SetupPathways( currentCPULevel );

		cpuCore.Setup( currentLevel, this );

		asyncSpawnCPUCanvas = cpuManagerData.CpuCanvasAssetReference.InstantiateAsync();
		await asyncSpawnCPUCanvas.Task;
		if ( asyncSpawnCPUCanvas.Status == AsyncOperationStatus.Succeeded )
		{
			GameObject cpuCanvasObj = asyncSpawnCPUCanvas.Result;
			cpuCanvasObj.transform.SetParent( this.transform );
			cpuCanvasObj.transform.localPosition = new Vector3( 0f, 2f, 0f );
			cpuCanvasController = cpuCanvasObj.GetComponent<CPUCanvasController>();
			cpuCanvasController.Setup( xpThresholdsData );
			cpuCanvasController.UpdateCPULevelText( currentCPULevel );
			cpuCanvasController.UpdateCPUXPText( currentXP );
		}
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
			if ( GameMode.Instance.GameModeDefinition.DebugMode == true )
			{
				inputManager.InputActions.Game.DebugCpuUp.performed -= DebugCPULevelUp_pressed;
				inputManager.InputActions.Game.DebugCpuDown.performed -= DebugCPULevelDown_pressed;
				inputManager.InputActions.Game.DebugAddXP.performed -= DebugCPUAddXP_pressed;
			}
		}

		if ( asyncSpawnCPUCanvas.IsValid() )
			Addressables.Release( asyncSpawnCPUCanvas );
	}

	private bool isDebugCPUAddXPHeld = false;
	private void DebugCPUAddXP_pressed( InputAction.CallbackContext ctx )
	{
		if ( ctx.phase != InputActionPhase.Performed ) return;
		bool isPressed = ctx.ReadValue<float>() > 0.5f;
		if ( isPressed )
		{
			DebugCPUAddXPPressed();
		}
		else
		{
			DebugCPUAddXPReleased();
		}
	}

	private void DebugCPUAddXPPressed()
	{
		isDebugCPUAddXPHeld = true;
		StartCoroutine( DebugCPUAddXPHeldRoutine() );
	}

	private IEnumerator DebugCPUAddXPHeldRoutine()
	{
		while ( isDebugCPUAddXPHeld )
		{
			AddXP( cpuManagerData.DebugXPGainAmount ); // add XP for debug
			yield return null;
		}
	}

	private void DebugCPUAddXPReleased()
	{
		isDebugCPUAddXPHeld = false;
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

		if ( CurrentCPULevel < currentLevelData.EndCPULevel )
			pathwayManager?.UpdatePathwaysAdd( CurrentCPULevel );

		cpuCanvasController?.UpdateCPULevelText( CurrentCPULevel );

		UpdateNextXP();

		// If we've reached the configured max level, set CurrentXP and NextXP to the max threshold value
		if ( currentLevelData != null && xpThresholdsData != null && CurrentCPULevel >= currentLevelData.EndCPULevel )
		{
			int maxThreshold = xpThresholdsData.GetXPThresholdForLevel( currentLevelData.EndCPULevel );
			CurrentXP = maxThreshold;
			NextXP = maxThreshold;
			cpuCanvasController?.UpdateCPUXPText( CurrentXP );
			return;
		}

		cpuCanvasController?.UpdateCPUXPText( CurrentXP );
	}

	private void UpdateCPULevelDown()
	{
		CurrentCPULevel -= 1;

		pathwayManager?.UpdatePathwaysRemove( CurrentCPULevel );
		cpuCanvasController?.UpdateCPULevelText( CurrentCPULevel );

		// When lowering via debug, reset current XP to 0 and update the next XP threshold for the new level.
		CurrentXP = 0;
		UpdateNextXP();

		cpuCanvasController?.UpdateCPUXPText( CurrentXP );
	}

	public void AddXP( int amount )
	{
		if ( amount <= 0 ) return;

		// Prevent adding XP if at max configured level
		if ( currentLevelData != null && xpThresholdsData != null && CurrentCPULevel >= currentLevelData.EndCPULevel )
		{
			int maxThreshold = xpThresholdsData.GetXPThresholdForLevel( currentLevelData.EndCPULevel );
			CurrentXP = maxThreshold;
			NextXP = maxThreshold;
			cpuCanvasController?.UpdateCPUXPText( CurrentXP );
			return;
		}

		CurrentXP += amount;

		cpuCanvasController?.UpdateCPUXPText( CurrentXP );

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
			//Debug.Log( "CPUManager: Reached max CPU level." );
			int maxThreshold = xpThresholdsData.GetXPThresholdForLevel( end );
			CurrentXP = maxThreshold;
			NextXP = maxThreshold;
			cpuCanvasController?.UpdateCPUXPText( CurrentXP );
			return;
		}

		// Index into thresholds based on current CPU level
		int index = CurrentCPULevel - start;
		if ( index < 0 || index >= xpThresholdsData.Thresholds.Length ) return;

		int threshold = xpThresholdsData.Thresholds[index];

		if ( CurrentXP >= threshold )
		{
			// Level up
			UpdateCPULevelUp();

			// If we've reached max level after the increment, UpdateCPULevelUp already set XP/NextXP to max threshold.
			if ( currentLevelData != null && CurrentCPULevel >= end )
			{
				cpuCanvasController?.UpdateCPULevelText( CurrentCPULevel );
				cpuCanvasController?.UpdateCPUXPText( CurrentXP );
				return;
			}

			// Carryover XP: subtract threshold instead of resetting to zero
			CurrentXP -= threshold;

			// saved nextXP already updated in UpdateCPULevelUp, but ensure it's up-to-date here as well
			UpdateNextXP();

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

	private void UpdateNextXP()
	{
		if ( xpThresholdsData == null || currentLevelData == null )
		{
			NextXP = 0;
			return;
		}

		int start = currentLevelData.StartCPULevel;
		int end = currentLevelData.EndCPULevel;
		int clampedLevel = Mathf.Clamp( currentCPULevel, start, end );

		int threshold = xpThresholdsData.GetXPThresholdForLevel( clampedLevel );
		NextXP = threshold;
	}

	private void UpdateHUDDebugText()
	{
		HUD?.UpdateDebugText();
	}
}
