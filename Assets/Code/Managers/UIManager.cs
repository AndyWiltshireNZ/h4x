using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI debugText;
	public TextMeshProUGUI DebugText { get { return debugText; } set { debugText = value; } }

	[SerializeField] private CanvasGroup mainmenuCanvasGroup;
	[SerializeField] private CanvasGroup hudCanvasGroup;

	public void Setup()
    {
        Debug.Log("UIManager started.");

		debugText.text = "Debug Text Initialized";

		mainmenuCanvasGroup.alpha = 0f;
		hudCanvasGroup.alpha = 1f;
	}

    private void Update()
    {
        
    }
}
