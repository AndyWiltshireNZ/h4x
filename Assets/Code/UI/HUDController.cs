using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI debugText;
	[SerializeField] private TextMeshProUGUI hackTimerText;
	[SerializeField] private TextMeshProUGUI hackTimerValueText;

	[SerializeField] private LevelEndPopupController levelEndPopupController;
	public LevelEndPopupController LevelEndPopupController => levelEndPopupController;

	[SerializeField] private UpgradesPopupController upgradesPopupController;
	public UpgradesPopupController UpgradesPopupController => upgradesPopupController;

	// setup ui manager
	public void Setup()
	{
		debugText.text = "Loading Debug Text...";
		hackTimerText.text = "Get Ready...";
		hackTimerValueText.text = "";

		upgradesPopupController.Setup();
	}

	public void UpdateDebugText()
	{
		if ( GameMode.Instance == null )
			return;
		{
			
		}
		debugText.text = 
			"<color=#F8B1FF>" +
			GameMode.Instance.LevelManager.CurrentLevel.CurrentLevelState.ToString() +
			"</color>" +
			"\n" +
			"Hack Time: " + GameMode.Instance.UpgradeManager.CurrentHackTime.ToString() + " sec" +
			"   |   " +
			"Packet Value: " + GameMode.Instance.UpgradeManager.CurrentPacketValue.ToString() + " xp" +
			"   |   " +
			"Virus Time Reduction: " + GameMode.Instance.UpgradeManager.CurrentVirusTime.ToString() + " sec" +
			"   |   " +
			"Data Spawn Interval: " + GameMode.Instance.UpgradeManager.CurrentDataSpawnInterval.ToString() + " sec" +
			"   |   " +
			"Data Flow Speed: " + GameMode.Instance.UpgradeManager.CurrentDataFlowSpeed.ToString() +
			"";
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
