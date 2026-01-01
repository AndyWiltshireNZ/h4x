using UnityEngine;

[CreateAssetMenu( fileName = "UpgradeManagerDefinition", menuName = "Definitions/UpgradeManagerDefinition" )]
public class UpgradeManagerDefinition : ScriptableObject
{
	// first 4 increase by 15s, next 3 by 30s, next 2 by 45s, last by 60s
	public float[] HackTimerUpgrades = new float[] { 60f, 75f, 90f, 105f, 135f, 165f, 195f, 240f, 285f, 345f };

	// first 4 increase by 5, next 3 by 10, next 2 by 15, last by 20
	public int[] PacketXPValueUpgrades = new int[] { 5, 10, 15, 20, 30, 40, 50, 65, 80, 100 };

	// first 4 decrease by 0.5s, next 3 by 1s, next 2 by 1.5s, last by 2s
	public float[] VirusTimeReductionUpgrades = new float[] { 10f, 9.5f, 9.0f, 8.5f, 7.5f, 6.5f, 5.5f, 4.0f, 2.5f, 0.5f };
}