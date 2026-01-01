using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
	[SerializeField] private MainMenuController mainMenuController;
	public MainMenuController MainMenuController => mainMenuController;
	[SerializeField] private CanvasGroup mainmenuCanvasGroup;

	[SerializeField] private HUDController hudController;
	public HUDController HUDController => hudController;
	[SerializeField] private CanvasGroup hudCanvasGroup;

	public void Setup()
    {
        Debug.Log( "UIManager started." );

		mainmenuCanvasGroup.alpha = 0f;
		hudCanvasGroup.alpha = 1f;

		mainMenuController.Setup();
		hudController.Setup();
	}
}
