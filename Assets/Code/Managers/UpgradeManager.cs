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
	public void SetCurrentPacketValue( int index ) { currentPacketValue = upgradeManagerData.PacketXPValueUpgrades[ index ]; }

	private float currentVirusTime;
	public float CurrentVirusTime => currentVirusTime;
	public void SetCurrentVirusTime( int index ) { currentVirusTime = upgradeManagerData.VirusTimeReductionUpgrades[ index ]; }

	private float currentDataSpawnInterval;
	public float CurrentDataSpawnInterval => currentDataSpawnInterval;
	public void SetCurrentDataSpawnInterval( int index ) { currentDataSpawnInterval = upgradeManagerData.DataSpawnIntervalUpgrades[ index ]; }

	private float currentDataFlowSpeed;
	public float CurrentDataFlowSpeed => currentDataFlowSpeed;
	public void SetCurrentDataFlowSpeed( int index ) { currentDataFlowSpeed = upgradeManagerData.DataFlowSpeedUpgrades[ index ]; }

	// this will have to load saved upgrades data in the future
	public void Setup()
	{
		SetCurrentHackTime( 0 );
		SetCurrentPacketValue( 0 );
		SetCurrentVirusTime( 0 );
		SetCurrentDataSpawnInterval( 0 );
		SetCurrentDataFlowSpeed( 0 );
	}
}
