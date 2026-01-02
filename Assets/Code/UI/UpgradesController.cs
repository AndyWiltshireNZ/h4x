using MoreMountains.Feedbacks;

using UnityEngine;

public class UpgradesController : MonoBehaviour
{
    [SerializeField] private CanvasGroup popupCanvasGroup;
	[SerializeField] private MMF_Player showPopupFeedback;
	[SerializeField] private MMF_Player hidePopupFeedback;

	private void Start()
	{
		//popupCanvasGroup.alpha = 0f;
		//popupCanvasGroup.gameObject.SetActive( false );
	}

	public void FadePopupCanvasGroup( bool fadeIn )
	{
		switch ( fadeIn )
		{
			case true:
				showPopupFeedback.PlayFeedbacks();
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
