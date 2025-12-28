using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
	public MainMenuController MainMenuController;
	[SerializeField] private CanvasGroup mainmenuCanvasGroup;

	public HUDController HUDController;
	[SerializeField] private CanvasGroup hudCanvasGroup;

	public void Setup()
    {
        Debug.Log( "UIManager started." );

		mainmenuCanvasGroup.alpha = 0f;
		hudCanvasGroup.alpha = 1f;
	}
}
