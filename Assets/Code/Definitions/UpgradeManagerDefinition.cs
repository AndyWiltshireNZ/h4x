using UnityEngine;

[CreateAssetMenu( fileName = "UpgradeManagerDefinition", menuName = "Definitions/UpgradeManagerDefinition" )]
public class UpgradeManagerDefinition : ScriptableObject
{
	// first 4 increase by 15s, next 3 by 30s, next 2 by 45s, last by 60s
	//private float[] HackTimerUpgrades = new float[] { 60f, 75f, 90f, 105f, 135f, 165f, 195f, 240f, 285f, 345f };
	public float BaseHackTimeStatValue = 60f;
	[SerializeField] private UpgradeDefinition upgradeDefIncreaseHackTime01;
	public UpgradeDefinition UpgradeDefIncreaseHackTime01 => upgradeDefIncreaseHackTime01;
	[SerializeField] private UpgradeDefinition upgradeDefIncreaseHackTime02;
	public UpgradeDefinition UpgradeDefIncreaseHackTime02 => upgradeDefIncreaseHackTime02;
	[SerializeField] private UpgradeDefinition upgradeDefIncreaseHackTime03;
	public UpgradeDefinition UpgradeDefIncreaseHackTime03 => upgradeDefIncreaseHackTime03;
	[SerializeField] private UpgradeDefinition upgradeDefIncreaseHackTime04;
	public UpgradeDefinition UpgradeDefIncreaseHackTime04 => upgradeDefIncreaseHackTime04;

	// first 4 increase by 5, next 3 by 10, next 2 by 15, last by 20
	//private float[] PacketXPValueUpgrades = new float[] { 5, 10, 15, 20, 30, 40, 50, 65, 80, 100 };
	public float BasePacketXPValueStat = 5;
	[SerializeField] private UpgradeDefinition upgradeDefIncreasePacketValue01;
	public UpgradeDefinition UpgradeDefIncreasePacketValue01 => upgradeDefIncreasePacketValue01;
	[SerializeField] private UpgradeDefinition upgradeDefIncreasePacketValue02;
	public UpgradeDefinition UpgradeDefIncreasePacketValue02 => upgradeDefIncreasePacketValue02;
	[SerializeField] private UpgradeDefinition upgradeDefIncreasePacketValue03;
	public UpgradeDefinition UpgradeDefIncreasePacketValue03 => upgradeDefIncreasePacketValue03;
	[SerializeField] private UpgradeDefinition upgradeDefIncreasePacketValue04;
	public UpgradeDefinition UpgradeDefIncreasePacketValue04 => upgradeDefIncreasePacketValue04;

	// first 4 decrease by 0.5s, next 3 by 1s, next 2 by 1.5s, last by 2s
	//private float[] VirusTimeReductionUpgrades = new float[] { 10f, 9.5f, 9.0f, 8.5f, 7.5f, 6.5f, 5.5f, 4.0f, 2.5f, 0.5f };
	public float BaseVirusTimeStatValue = 10f;
	[SerializeField] private UpgradeDefinition upgradeDefDecreaseVirusTime01;
	public UpgradeDefinition UpgradeDefDecreaseVirusTime01 => upgradeDefDecreaseVirusTime01;
	[SerializeField] private UpgradeDefinition upgradeDefDecreaseVirusTime02;
	public UpgradeDefinition UpgradeDefDecreaseVirusTime02 => upgradeDefDecreaseVirusTime02;
	[SerializeField] private UpgradeDefinition upgradeDefDecreaseVirusTime03;
	public UpgradeDefinition UpgradeDefDecreaseVirusTime03 => upgradeDefDecreaseVirusTime03;
	[SerializeField] private UpgradeDefinition upgradeDefDecreaseVirusTime04;
	public UpgradeDefinition UpgradeDefDecreaseVirusTime04 => upgradeDefDecreaseVirusTime04;

	// first 4 decrease by 0.125s, next 3 by 0.25s, next 2 by 0.5s, last by 1.0s
	//private float[] DataSpawnIntervalUpgrades = new float[] { 3.625f, 3.5f, 3.375f, 3.25f, 3.0f, 2.75f, 2.5f, 2.0f, 1.5f, 0.5f };
	public float BaseDataSpawnIntervalStatValue = 3.625f;
	[SerializeField] private UpgradeDefinition upgradeDefDecreaseDataSpawnInterval01;
	public UpgradeDefinition UpgradeDefDecreaseDataSpawnInterval01 => upgradeDefDecreaseDataSpawnInterval01;
	[SerializeField] private UpgradeDefinition upgradeDefDecreaseDataSpawnInterval02;
	public UpgradeDefinition UpgradeDefDecreaseDataSpawnInterval02 => upgradeDefDecreaseDataSpawnInterval02;
	[SerializeField] private UpgradeDefinition upgradeDefDecreaseDataSpawnInterval03;
	public UpgradeDefinition UpgradeDefDecreaseDataSpawnInterval03 => upgradeDefDecreaseDataSpawnInterval03;
	[SerializeField] private UpgradeDefinition upgradeDefDecreaseDataSpawnInterval04;
	public UpgradeDefinition UpgradeDefDecreaseDataSpawnInterval04 => upgradeDefDecreaseDataSpawnInterval04;

	// first 4 increase by 0.125, next 3 by 0.25, next 2 by 0.5, last by 1.0
	//private float[] DataFlowSpeedUpgrades = new float[] { 5.5f, 5.625f, 5.75f, 5.875f, 6.0f, 6.25f, 6.5f, 7.5f, 8.0f, 9.0f };
	public float BaseDataFlowSpeedStatValue = 5.5f;
	[SerializeField] private UpgradeDefinition upgradeDefIncreaseDataFlowSpeed01;
	public UpgradeDefinition UpgradeDefIncreaseDataFlowSpeed01 => upgradeDefIncreaseDataFlowSpeed01;
	[SerializeField] private UpgradeDefinition upgradeDefIncreaseDataFlowSpeed02;
	public UpgradeDefinition UpgradeDefIncreaseDataFlowSpeed02 => upgradeDefIncreaseDataFlowSpeed02;
	[SerializeField] private UpgradeDefinition upgradeDefIncreaseDataFlowSpeed03;
	public UpgradeDefinition UpgradeDefIncreaseDataFlowSpeed03 => upgradeDefIncreaseDataFlowSpeed03;
	[SerializeField] private UpgradeDefinition upgradeDefIncreaseDataFlowSpeed04;
	public UpgradeDefinition UpgradeDefIncreaseDataFlowSpeed04 => upgradeDefIncreaseDataFlowSpeed04;
}