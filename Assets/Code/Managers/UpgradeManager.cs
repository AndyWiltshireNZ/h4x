using UnityEngine;
using System;

public class UpgradeManager : MonoBehaviour
{
	[SerializeField] private UpgradeManagerDefinition upgradeManagerDef;

	private const int GroupSize = 4;

	// --- Current stat values ---
	private float currentHackTime;
	public float CurrentHackTime => currentHackTime;

	private float currentPacketValue;
	public float CurrentPacketValue => currentPacketValue;

	private float currentVirusTime;
	public float CurrentVirusTime => currentVirusTime;

	private float currentDataSpawnInterval;
	public float CurrentDataSpawnInterval => currentDataSpawnInterval;

	private float currentDataFlowSpeed;
	public float CurrentDataFlowSpeed => currentDataFlowSpeed;

	// --- Indices stored as arrays to reduce duplication ---
	private readonly int[] hackTimeIndices = new int[GroupSize];
	public int CurrentHackTimeIndex01 => hackTimeIndices[0];
	public int CurrentHackTimeIndex02 => hackTimeIndices[1];
	public int CurrentHackTimeIndex03 => hackTimeIndices[2];
	public int CurrentHackTimeIndex04 => hackTimeIndices[3];

	private readonly int[] packetValueIndices = new int[GroupSize];
	public int CurrentPacketValueIndex01 => packetValueIndices[0];
	public int CurrentPacketValueIndex02 => packetValueIndices[1];
	public int CurrentPacketValueIndex03 => packetValueIndices[2];
	public int CurrentPacketValueIndex04 => packetValueIndices[3];

	private readonly int[] virusTimeIndices = new int[GroupSize];
	public int CurrentVirusTimeIndex01 => virusTimeIndices[0];
	public int CurrentVirusTimeIndex02 => virusTimeIndices[1];
	public int CurrentVirusTimeIndex03 => virusTimeIndices[2];
	public int CurrentVirusTimeIndex04 => virusTimeIndices[3];

	private readonly int[] dataSpawnIntervalIndices = new int[GroupSize];
	public int CurrentDataSpawnIntervalIndex01 => dataSpawnIntervalIndices[0];
	public int CurrentDataSpawnIntervalIndex02 => dataSpawnIntervalIndices[1];
	public int CurrentDataSpawnIntervalIndex03 => dataSpawnIntervalIndices[2];
	public int CurrentDataSpawnIntervalIndex04 => dataSpawnIntervalIndices[3];

	private readonly int[] dataFlowSpeedIndices = new int[GroupSize];
	public int CurrentDataFlowSpeedIndex01 => dataFlowSpeedIndices[0];
	public int CurrentDataFlowSpeedIndex02 => dataFlowSpeedIndices[1];
	public int CurrentDataFlowSpeedIndex03 => dataFlowSpeedIndices[2];
	public int CurrentDataFlowSpeedIndex04 => dataFlowSpeedIndices[3];

	// Return the current stat value considering upgrades.
	private float GetCurrentStatValue(float baseValue, UpgradeDefinition[] defs, int[] indices)
	{
		float result = baseValue;

		if (defs == null || defs.Length == 0) return result;
		if (indices == null) indices = new int[defs.Length];

		for (int i = 0; i < defs.Length; i++)
		{
			UpgradeDefinition def = defs[i];
			if (def == null) continue;

			float[] levels = def.StatChangePerUpgradeLevel;
			if (levels == null || levels.Length == 0) continue;

			// Interpret indices[i] as the number of purchased levels for this definition.
			// Only sum the purchased entries; do not apply level[0] if nothing is purchased.
			int purchased = Mathf.Clamp(indices.Length > i ? indices[i] : 0, 0, levels.Length);

			for (int l = 0; l < purchased; l++)
			{
				result += levels[l];
			}
		}

		return result;
	}

	// Public helper for UI: get the computed stat value for a specific UpgradeDefinition.
	public float GetStat( UpgradeDefinition def )
	{
		if (def == null) return 0f;

		switch (def.ButtonUpgradeType)
		{
			case UpgradeDefinition.UpgradeType.HackTimerIncrease:
				return GetCurrentStatValue(upgradeManagerDef.BaseHackTimeStatValue, GetHackTimeDefs(), hackTimeIndices);
			case UpgradeDefinition.UpgradeType.PacketXPValueIncrease:
				return GetCurrentStatValue(upgradeManagerDef.BasePacketXPValueStat, GetPacketValueDefs(), packetValueIndices);
			case UpgradeDefinition.UpgradeType.VirusHackTimeReduction:
				return GetCurrentStatValue(upgradeManagerDef.BaseVirusTimeStatValue, GetVirusTimeDefs(), virusTimeIndices);
			case UpgradeDefinition.UpgradeType.DataSpawnIntervalDecrease:
				return GetCurrentStatValue(upgradeManagerDef.BaseDataSpawnIntervalStatValue, GetDataSpawnIntervalDefs(), dataSpawnIntervalIndices);
			case UpgradeDefinition.UpgradeType.DataFlowSpeedIncrease:
				return GetCurrentStatValue(upgradeManagerDef.BaseDataFlowSpeedStatValue, GetDataFlowSpeedDefs(), dataFlowSpeedIndices);
			default:
				return 0f;
		}
	}

	// Return the current level index for the provided UpgradeDefinition asset.
	public int GetCurrentUpgradeLevel(UpgradeDefinition def)
	{
		if (def == null) return 0;

		UpgradeDefinition[][] groups = new UpgradeDefinition[][]
		{
			GetHackTimeDefs(),
			GetPacketValueDefs(),
			GetVirusTimeDefs(),
			GetDataSpawnIntervalDefs(),
			GetDataFlowSpeedDefs()
		};

		int[][] groupIndices = new int[][]
		{
			hackTimeIndices,
			packetValueIndices,
			virusTimeIndices,
			dataSpawnIntervalIndices,
			dataFlowSpeedIndices
		};

		for (int g = 0; g < groups.Length; g++)
		{
			UpgradeDefinition[] group = groups[g];
			int[] indices = groupIndices[g];
			if (group == null) continue;

			for (int i = 0; i < group.Length; i++)
			{
				if (group[i] == def)
				{
					int clamped = 0;
					float[] levels = def.StatChangePerUpgradeLevel;
					if (levels != null && levels.Length > 0)
					{
						// Return the number of purchased levels (0..levels.Length)
						clamped = Mathf.Clamp(indices.Length > i ? indices[i] : 0, 0, levels.Length);
					}

					return clamped;
				}
			}
		}

		// Not found: default to 0
		return 0;
	}

	// Initialize indices and compute current stat values
	public void Setup()
	{
		Array.Clear(hackTimeIndices, 0, GroupSize);
		Array.Clear(packetValueIndices, 0, GroupSize);
		Array.Clear(virusTimeIndices, 0, GroupSize);
		Array.Clear(dataSpawnIntervalIndices, 0, GroupSize);
		Array.Clear(dataFlowSpeedIndices, 0, GroupSize);

		currentHackTime = GetCurrentStatValue(
			upgradeManagerDef.BaseHackTimeStatValue,
			GetHackTimeDefs(),
			hackTimeIndices
		);

		currentPacketValue = GetCurrentStatValue(
			upgradeManagerDef.BasePacketXPValueStat,
			GetPacketValueDefs(),
			packetValueIndices
		);

		currentVirusTime = GetCurrentStatValue(
			upgradeManagerDef.BaseVirusTimeStatValue,
			GetVirusTimeDefs(),
			virusTimeIndices
		);

		currentDataSpawnInterval = GetCurrentStatValue(
			upgradeManagerDef.BaseDataSpawnIntervalStatValue,
			GetDataSpawnIntervalDefs(),
			dataSpawnIntervalIndices
		);

		currentDataFlowSpeed = GetCurrentStatValue(
			upgradeManagerDef.BaseDataFlowSpeedStatValue,
			GetDataFlowSpeedDefs(),
			dataFlowSpeedIndices
		);
	}

	// --- Helper methods to build groups from the serialized definition asset ---
	private UpgradeDefinition[] GetHackTimeDefs()
	{
		return new UpgradeDefinition[]
		{
			upgradeManagerDef.UpgradeDefIncreaseHackTime01,
			upgradeManagerDef.UpgradeDefIncreaseHackTime02,
			upgradeManagerDef.UpgradeDefIncreaseHackTime03,
			upgradeManagerDef.UpgradeDefIncreaseHackTime04
		};
	}

	private UpgradeDefinition[] GetPacketValueDefs()
	{
		return new UpgradeDefinition[]
		{
			upgradeManagerDef.UpgradeDefIncreasePacketValue01,
			upgradeManagerDef.UpgradeDefIncreasePacketValue02,
			upgradeManagerDef.UpgradeDefIncreasePacketValue03,
			upgradeManagerDef.UpgradeDefIncreasePacketValue04
		};
	}

	private UpgradeDefinition[] GetVirusTimeDefs()
	{
		return new UpgradeDefinition[]
		{
			upgradeManagerDef.UpgradeDefDecreaseVirusTime01,
			upgradeManagerDef.UpgradeDefDecreaseVirusTime02,
			upgradeManagerDef.UpgradeDefDecreaseVirusTime03,
			upgradeManagerDef.UpgradeDefDecreaseVirusTime04
		};
	}

	private UpgradeDefinition[] GetDataSpawnIntervalDefs()
	{
		return new UpgradeDefinition[]
		{
			upgradeManagerDef.UpgradeDefDecreaseDataSpawnInterval01,
			upgradeManagerDef.UpgradeDefDecreaseDataSpawnInterval02,
			upgradeManagerDef.UpgradeDefDecreaseDataSpawnInterval03,
			upgradeManagerDef.UpgradeDefDecreaseDataSpawnInterval04
		};
	}

	private UpgradeDefinition[] GetDataFlowSpeedDefs()
	{
		return new UpgradeDefinition[]
		{
			upgradeManagerDef.UpgradeDefIncreaseDataFlowSpeed01,
			upgradeManagerDef.UpgradeDefIncreaseDataFlowSpeed02,
			upgradeManagerDef.UpgradeDefIncreaseDataFlowSpeed03,
			upgradeManagerDef.UpgradeDefIncreaseDataFlowSpeed04
		};
	}

	public void PurchaseUpgrade( UpgradeDefinition upgradeDefinition )
	{
		if (upgradeDefinition == null) return;

		UpgradeDefinition[][] groups = new UpgradeDefinition[][]
		{
			GetHackTimeDefs(),
			GetPacketValueDefs(),
			GetVirusTimeDefs(),
			GetDataSpawnIntervalDefs(),
			GetDataFlowSpeedDefs()
		};

		int[][] groupIndices = new int[][]
		{
			hackTimeIndices,
			packetValueIndices,
			virusTimeIndices,
			dataSpawnIntervalIndices,
			dataFlowSpeedIndices
		};

		for (int g = 0; g < groups.Length; g++)
		{
			UpgradeDefinition[] group = groups[g];
			int[] indices = groupIndices[g];
			if (group == null) continue;

			for (int i = 0; i < group.Length; i++)
			{
				if (group[i] != upgradeDefinition) continue;

				float[] levels = upgradeDefinition.StatChangePerUpgradeLevel;
				if (levels == null || levels.Length == 0) return;

				// allow indices to represent number of purchased levels (0..levels.Length)
				int maxIndex = levels.Length;
				int current = indices.Length > i ? indices[i] : 0;
				int next = Mathf.Clamp(current + 1, 0, maxIndex);

				if (indices.Length > i)
				{
					indices[i] = next;
				}

				switch (g)
				{
					case 0:
						currentHackTime = GetCurrentStatValue(upgradeManagerDef.BaseHackTimeStatValue, GetHackTimeDefs(), hackTimeIndices);
						break;
					case 1:
						currentPacketValue = GetCurrentStatValue(upgradeManagerDef.BasePacketXPValueStat, GetPacketValueDefs(), packetValueIndices);
						break;
					case 2:
						currentVirusTime = GetCurrentStatValue(upgradeManagerDef.BaseVirusTimeStatValue, GetVirusTimeDefs(), virusTimeIndices);
						break;
					case 3:
						currentDataSpawnInterval = GetCurrentStatValue(upgradeManagerDef.BaseDataSpawnIntervalStatValue, GetDataSpawnIntervalDefs(), dataSpawnIntervalIndices);
						break;
					case 4:
						currentDataFlowSpeed = GetCurrentStatValue(upgradeManagerDef.BaseDataFlowSpeedStatValue, GetDataFlowSpeedDefs(), dataFlowSpeedIndices);
						break;
				}

				return;
			}
		}
	}
}
