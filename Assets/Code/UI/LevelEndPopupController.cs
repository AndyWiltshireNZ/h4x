using MoreMountains.Feedbacks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndPopupController : MonoBehaviour
{
	[SerializeField] private CanvasGroup popupCanvasGroup;
	[SerializeField] private MMF_Player showPopupFeedback;
	[SerializeField] private MMF_Player hidePopupFeedback;
	[SerializeField] private GameObject levelWonObject;
	[SerializeField] private GameObject levelLostObject;

	private bool isLevelWon;
	public bool IsLevelWon { get { return isLevelWon; } set { isLevelWon = value; } }

	public static event Action Event_ReplayCurrentLevel;

	private void Start()
    {
		popupCanvasGroup.alpha = 0f;
		popupCanvasGroup.gameObject.SetActive( false );
		levelWonObject.gameObject.SetActive( false );
		levelLostObject.gameObject.SetActive( true );
	}

	public void FadePopupCanvasGroup( bool fadeIn, bool isLevelWon )
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

		TogglePopupContent( isLevelWon );
	}

	private void TogglePopupContent( bool isLevelWon )
	{
		if ( isLevelWon )
		{
			levelWonObject.gameObject.SetActive( true );
			levelLostObject.gameObject.SetActive( false );
		}
		else
		{
			levelWonObject.gameObject.SetActive( false );
			levelLostObject.gameObject.SetActive( true );
		}
	}

	public void ButtonEvent_Replay()
	{
		// broadcast - reload level
		Event_ReplayCurrentLevel?.Invoke();

		if ( levelWonObject.gameObject.activeInHierarchy )
		{
			FadePopupCanvasGroup( false, true );
		}
		else if ( levelLostObject.gameObject.activeInHierarchy )
		{
			FadePopupCanvasGroup( false, false );
		}

	}

	public void ButtonEvent_Continue()
	{
		// reload current scene for now
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );
		Debug.Log( "Scene Reloaded" );
	}
}
