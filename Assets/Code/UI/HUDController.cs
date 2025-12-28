using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI debugText;

	public void UpdateDebugText()
	{
		debugText.text = 
			"Level State: " + GameMode.Instance.LevelManager.CurrentLevel.CurrentLevelState.ToString() +
			"   |   " +		
			"CPU Level: " + GameMode.Instance.LevelManager.CurrentLevel.CPUManager.CurrentCPULevel.ToString();
	}
}
