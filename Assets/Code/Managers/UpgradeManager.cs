using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
	[SerializeField] private UpgradeManagerDefinition upgradeManagerData;
	public UpgradeManagerDefinition UpgradeManagerData => upgradeManagerData;

	private float currentHackTime;
	public float CurrentHackTime => currentHackTime;
	public void SetCurrentHackTime( int index )	{ currentHackTime = upgradeManagerData.HackTimerUpgrades[ index ]; }

	private int currentPacketValue;
	public int CurrentPacketValue => currentPacketValue;
	public void SetCurrentPacketValue( int index )	{ currentPacketValue = upgradeManagerData.PacketXPValueUpgrades[ index ]; }

	private float currentVirusTime;
	public float CurrentVirusTime => currentVirusTime;
	public void SetCurrentVirusTime( int index )	{ currentVirusTime = upgradeManagerData.VirusTimeReductionUpgrades[ index ]; }

	// this will have to load saved data in the future
	public void Setup()
	{
		SetCurrentHackTime( 0 );
		SetCurrentPacketValue( 0 );
		SetCurrentVirusTime( 0 );
	}
}
