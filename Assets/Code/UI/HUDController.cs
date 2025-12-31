using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI debugText;
	[SerializeField] private TextMeshProUGUI hackTimerText;
	[SerializeField] private TextMeshProUGUI hackTimerValueText;

	private void Start()
	{
		debugText.text = "Loading Debug Text...";
		hackTimerText.text = "Get Ready...";
		hackTimerValueText.text = "";
	}

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

	public void UpdateHackTimerText( int stateIndex )
	{
		switch ( stateIndex )
		{
			case 0:
				hackTimerText.text = "Get Ready...";
				break;
			case 1:
				hackTimerText.text = "Hack Time Remaining";
				break;
			default:
				break;
		}
	}

	public void UpdateHackTimer( float newHackTimerValue )
	{
		hackTimerValueText.text = newHackTimerValue.ToString("F1");
	}
}
