using UnityEngine;
using System;

public class Packet : EntityBase
{
	[SerializeField] private PacketDefinition PacketData;

	[NonSerialized] public int XPValue;

	private void Start()
    {
        XPValue = PacketData.XPValue;
	}
}
