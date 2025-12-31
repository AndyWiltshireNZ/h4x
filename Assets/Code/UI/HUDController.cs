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
			"Current XP: " + GameMode.Instance.LevelManager.CurrentLevel.CPUManager.CurrentXP.ToString() + 
			" / " + GameMode.Instance.LevelManager.CurrentLevel.CPUManager.NextXP.ToString() +
			"   |   " + 
			"CPU Level: " + GameMode.Instance.LevelManager.CurrentLevel.CPUManager.CurrentCPULevel.ToString();
	}
}
