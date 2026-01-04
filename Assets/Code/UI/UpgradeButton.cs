using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum UpgradeButtonState
{
	Uninitialized,
	Locked,
	Unaffordable,
	Unlocked,
	Maxed
}

public class UpgradeButton : MonoBehaviour
{
	[SerializeField] private UpgradeDefinition upgradeData;

	private UpgradeManager upgradeManager;
	private int currentUpgradeLevelIndex;
	private int maxUpgradeLevelIndex;

	[SerializeField] private CanvasGroup buttonCanvasGroup;
	public CanvasGroup ButtonCanvasGroup => buttonCanvasGroup;

	[SerializeField] private bool startUnlocked = false;
	private bool isUnlocked = false;

	[SerializeField] private FunkButton upgradeLockedButton;
	[SerializeField] private FunkButton upgradeUnaffordableButton;
	[SerializeField] private FunkButton upgradeUnlockedButton;
	[SerializeField] private FunkButton upgradeMaxedButton;

	[SerializeField] private Image[] iconImages;

	[SerializeField] private TextMeshProUGUI currentUpgradeLevelText;

	[SerializeField] private UpgradeButton[] connectedUpgradeButtons;

	private UpgradeButtonState currentUpgradeButtonState;
	public UpgradeButtonState CurrentUpgradeButtonState => currentUpgradeButtonState;

	// setup by upgrade popup controller
	public void Setup( UpgradeManager newUpgradeManager ) 
	{
		upgradeManager = newUpgradeManager;

		currentUpgradeLevelIndex = upgradeManager.GetCurrentUpgradeLevel( upgradeData ) - 1;
		maxUpgradeLevelIndex = upgradeData?.StatChangePerUpgradeLevel != null
			? upgradeData.StatChangePerUpgradeLevel.Length
			: 0;

		foreach ( Image iconImage in iconImages )
		{
			iconImage.sprite = upgradeData.Icon;
		}

		// set initial state
		SetState( UpgradeButtonState.Uninitialized );
		if ( startUnlocked == false )
		{
			SetState( UpgradeButtonState.Locked );
			buttonCanvasGroup.alpha = 0f;
		}
		else
		{
			isUnlocked = true;
			SetState( UpgradeButtonState.Unlocked );
			buttonCanvasGroup.alpha = 1f;
		}

		RefreshUpgradeButton();
	}

	// triggered by upgrade popup controller on fade in canvas
	public void RefreshUpgradeButton()
	{
		UpdateUpgradeButton();
	}

	private void UpdateUpgradeButton()
	{
		currentUpgradeLevelIndex = upgradeManager.GetCurrentUpgradeLevel( upgradeData );

		currentUpgradeLevelText.text = Mathf.Max(0, currentUpgradeLevelIndex).ToString() + "/" + maxUpgradeLevelIndex.ToString();

		Debug.Log("Stat Value: " + upgradeManager.CurrentHackTime.ToString() );

		// if we've reached the last valid index mark the button as maxed
		if ( currentUpgradeLevelIndex >= maxUpgradeLevelIndex )
		{
			SetState( UpgradeButtonState.Maxed );
			return;
		}

		// if not maxed, ensure unlocked state if appropriate
		if ( isUnlocked )
		{
			SetState( UpgradeButtonState.Unlocked );
		}
	}

	public void SetState( UpgradeButtonState newUpgradeButtonState )
	{
		if ( currentUpgradeButtonState == newUpgradeButtonState )
		{
			return;
		}

		currentUpgradeButtonState = newUpgradeButtonState;
		OnEnterState( currentUpgradeButtonState );
	}

	private void OnEnterState( UpgradeButtonState state )
	{
		switch ( state )
		{
			case UpgradeButtonState.Locked:
				upgradeLockedButton.gameObject.SetActive( true );
				upgradeUnaffordableButton.gameObject.SetActive( false );
				upgradeUnlockedButton.gameObject.SetActive( false );
				upgradeMaxedButton.gameObject.SetActive( false );
				break;
			case UpgradeButtonState.Unaffordable:
				upgradeLockedButton.gameObject.SetActive( false );
				upgradeUnaffordableButton.gameObject.SetActive( true );
				upgradeUnlockedButton.gameObject.SetActive( false );
				upgradeMaxedButton.gameObject.SetActive( false );
				break;
			case UpgradeButtonState.Unlocked:
				upgradeLockedButton.gameObject.SetActive( false );
				upgradeUnaffordableButton.gameObject.SetActive( false );
				upgradeUnlockedButton.gameObject.SetActive( true );
				upgradeMaxedButton.gameObject.SetActive( false );
				break;
			case UpgradeButtonState.Maxed:
				upgradeLockedButton.gameObject.SetActive( false );
				upgradeUnaffordableButton.gameObject.SetActive( false );
				upgradeUnlockedButton.gameObject.SetActive( false );
				upgradeMaxedButton.gameObject.SetActive( true );
				break;	
		}
	}

	public void ButtonEvent_PurchaseUpgrade()
	{
		if ( currentUpgradeButtonState == UpgradeButtonState.Unlocked )
		{
			// send upgrade def to upgrade manager so it knows what to upgrade
			upgradeManager?.PurchaseUpgrade( upgradeData );

			// refresh and re-evaluate maxed state
			RefreshUpgradeButton();
		}
	}
}
