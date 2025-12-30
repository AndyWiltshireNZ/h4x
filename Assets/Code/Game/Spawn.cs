using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class Spawn
{
	[Tooltip("Addressable prefab to spawn")]
	public AssetReference Prefab;

	[Tooltip("Movement speed applied to spawned entities (z axis)")]
	public float EntitySpeed = 1f;
}