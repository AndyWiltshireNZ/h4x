using UnityEngine;

[CreateAssetMenu( fileName = "UpgradeDefinition", menuName = "Definitions/UpgradeDefinition" )]
public class UpgradeDefinition : ScriptableObject
{
	public enum UpgradeType
	{
		HackTimerIncrease,
		PacketXPValueIncrease,
		VirusHackTimeReduction,
		DataSpawnIntervalDecrease,
		DataFlowSpeedIncrease
	}

	public UpgradeType ButtonUpgradeType;

	public Sprite Icon;

	public int LevelUnlockRequirement = 0;

	public int[] SilicaCostsPerUpgradeLevel = new int[] { 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };

	// first 4 increase by 15s, next 3 by 30s, next 2 by 45s, last by 60s
	public float[] StatChangePerUpgradeLevel = new float[] { 15f, 15f, 15f, 15f, 30f, 30f, 30f, 45f, 45f, 60f };
}