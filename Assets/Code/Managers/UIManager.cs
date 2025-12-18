using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI debugText;
	public TextMeshProUGUI DebugText { get { return debugText; } set { debugText = value; } }

	public void Setup()
    {
        Debug.Log("UIManager started.");

		debugText.text = "Debug Text Initialized";
	}

    private void Update()
    {
        
    }
}
