using System.Collections.Generic;

using MoreMountains.Feedbacks;

using UnityEngine;

public class UpgradesPopupController : MonoBehaviour
{
    [SerializeField] private CanvasGroup popupCanvasGroup;
	[SerializeField] private MMF_Player showPopupFeedback;
	[SerializeField] private MMF_Player hidePopupFeedback;

	[SerializeField] private GameObject upgradeButtonContainer;
	[SerializeField] private List<UpgradeButton> upgradeButtons = new List<UpgradeButton>();

	private void Start()
	{
		// fade out canvas at start
		//popupCanvasGroup.alpha = 0f;
	}

	// setup by hud controller
	public void Setup()
	{
		// find upgrade buttons under the container if none cached yet
		if ( upgradeButtonContainer != null && (upgradeButtons == null || upgradeButtons.Count == 0) )
		{
			UpgradeButton[] buttons = upgradeButtonContainer.GetComponentsInChildren<UpgradeButton>( true );
			upgradeButtons = new List<UpgradeButton>( buttons );
		}

		// setup each upgrade button
		foreach ( UpgradeButton upgradeButton in upgradeButtons )
		{
			upgradeButton.Setup( GameMode.Instance?.UpgradeManager );
		}

		// then disable canvas group
		//popupCanvasGroup.gameObject.SetActive( false );
	}

	public void FadePopupCanvasGroup( bool fadeIn )
	{
		switch ( fadeIn )
		{
			case true:
				showPopupFeedback.PlayFeedbacks();
				foreach ( UpgradeButton upgradeButton in upgradeButtons )
				{
					upgradeButton.RefreshUpgradeButton();
				}
				break;
			case false:
				hidePopupFeedback.PlayFeedbacks();
				break;
		}
	}

	public void ButtonEvent_Close()
	{
		FadePopupCanvasGroup( false );
	}
}
