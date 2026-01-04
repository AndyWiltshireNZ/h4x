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
	public bool IsUnlocked => isUnlocked;

	[SerializeField] private bool stayLocked = false;

	[SerializeField] private FunkButton upgradeLockedButton;
	[SerializeField] private FunkButton upgradeUnaffordableButton;
	[SerializeField] private FunkButton upgradeUnlockedButton;
	[SerializeField] private FunkButton upgradeMaxedButton;

	[SerializeField] private Image[] iconImages;

	[SerializeField] private TextMeshProUGUI currentUpgradeLevelText;

	[SerializeField] private UpgradeButton[] connectedUpgradeButtons;
	[SerializeField] private GameObject[] connectedLines;

	private UpgradeButtonState currentUpgradeButtonState;
	public UpgradeButtonState CurrentUpgradeButtonState => currentUpgradeButtonState;

	private void Awake()
	{
		buttonCanvasGroup.alpha = 0f;
	}

	// setup by upgrade popup controller
	public void Setup( UpgradeManager newUpgradeManager ) 
	{
		upgradeManager = newUpgradeManager;

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

		// ensure connected lines start inactive; they should only be activated after this upgrade has been purchased at least once
		if ( connectedLines != null )
		{
			foreach ( GameObject line in connectedLines )
			{
				if ( line != null )
				{
					line.SetActive( false );
				}
			}
		}

		currentUpgradeLevelIndex = upgradeManager.GetCurrentUpgradeLevel( upgradeData ) - 1;
		maxUpgradeLevelIndex = upgradeData?.StatChangePerUpgradeLevel != null
			? upgradeData.StatChangePerUpgradeLevel.Length
			: 0;

		RefreshUpgradeButton();
	}

	// triggered by upgrade popup controller on fade in canvas
	public void RefreshUpgradeButton()
	{
		if ( stayLocked ) return;

		UpdateUpgradeButton();
	}

	private void UpdateUpgradeButton()
	{
		currentUpgradeLevelIndex = upgradeManager.GetCurrentUpgradeLevel( upgradeData );

		currentUpgradeLevelText.text = Mathf.Max(0, currentUpgradeLevelIndex).ToString() + "/" + maxUpgradeLevelIndex.ToString();

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

				// connected lines must be inactive while this upgrade is locked
				if ( connectedLines != null )
				{
					foreach ( GameObject line in connectedLines )
					{
						if ( line != null )
						{
							line.SetActive( false );
						}
					}
				}
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
				
				// Reveal connected lines and buttons when the upgrade has been purchased at least once.
				RevealConnectedAfterPurchase();
				break;
			case UpgradeButtonState.Maxed:
				upgradeLockedButton.gameObject.SetActive( false );
				upgradeUnaffordableButton.gameObject.SetActive( false );
				upgradeUnlockedButton.gameObject.SetActive( false );
				upgradeMaxedButton.gameObject.SetActive( true );
				break;	
		}
	}

	// Ensure connected upgrades and connection lines are revealed when this upgrade has any purchased levels.
	// This is extracted so we can call it both on entering the Unlocked state and immediately after a purchase,
	// because the state may already be Unlocked and SetState would early-return.
	private void RevealConnectedAfterPurchase()
	{
		if ( upgradeManager == null || upgradeManager.GetCurrentUpgradeLevel( upgradeData ) <= 0 )
		{
			return;
		}

		if ( connectedUpgradeButtons != null )
		{
			foreach ( UpgradeButton connected in connectedUpgradeButtons )
			{
				if ( connected == null || connected.IsUnlocked )
				{
					continue;
				}

				if ( connected.ButtonCanvasGroup != null )
				{
					connected.ButtonCanvasGroup.alpha = 1f;
				}

				if ( !connected.stayLocked )
				{
					connected.isUnlocked = true;
					connected.SetState( UpgradeButtonState.Unlocked );
				}
			}
		}

		if ( connectedLines != null )
		{
			foreach ( GameObject line in connectedLines )
			{
				if ( line == null )
				{
					continue;
				}

				line.SetActive( true );
			}
		}
	}

	public void ButtonEvent_PurchaseUpgrade()
	{
		if ( currentUpgradeButtonState == UpgradeButtonState.Unlocked )
		{
			if ( upgradeManager == null ) return;

			// perform purchase
			upgradeManager.PurchaseUpgrade( upgradeData );

			// refresh and re-evaluate maxed state
			RefreshUpgradeButton();

			Debug.Log($"{upgradeData.ButtonUpgradeType.ToString()} Value: " + upgradeManager.GetStat(upgradeData).ToString() );

			RevealConnectedAfterPurchase();
		}
	}
}
