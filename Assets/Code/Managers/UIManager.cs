using UnityEngine;
using System.Collections;

public class UIManager : MonoBehaviour
{
	[SerializeField] private Canvas mainCanvas;
	[SerializeField] private MainMenuController mainMenuController;
	public MainMenuController MainMenuController => mainMenuController;
	[SerializeField] private CanvasGroup mainmenuCanvasGroup;

	[SerializeField] private HUDController hudController;
	public HUDController HUDController => hudController;
	[SerializeField] private CanvasGroup hudCanvasGroup;

	private Camera currentUICamera;

	private void Awake()
	{
		this.gameObject.SetActive( false );
	}

	public void Setup()
    {
        Debug.Log( "UIManager started." );

		this.gameObject.SetActive( true );

		currentUICamera = GameMode.Instance?.UICamera;
		mainCanvas.worldCamera = currentUICamera;

		mainmenuCanvasGroup.alpha = 0f;
		hudCanvasGroup.alpha = 1f;

		StartCoroutine( UISetupDelayRoutine() );
	}

	private IEnumerator UISetupDelayRoutine()
	{
		yield return new WaitForEndOfFrame();
		mainMenuController.Setup();
		hudController.Setup();
	}
}
