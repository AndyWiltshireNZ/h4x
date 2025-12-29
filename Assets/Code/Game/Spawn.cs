using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class Spawn
{
	[Tooltip("Addressable prefab to spawn")]
	public AssetReference Prefab;

	[Tooltip("Number of instances to spawn each spawn event")]
	public int SpawnQuantity = 1;

	[Tooltip("Seconds between spawns (per spawned instance)")]
	public float SpawnInterval = 1f;

	[Tooltip("If true the spawn will start automatically when SpawnManager.Setup is called")]
	public bool AutoStart = true;
}