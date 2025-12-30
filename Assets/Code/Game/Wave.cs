using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class Wave
{
	// The addressable prefab to spawn
	public AssetReference EntityAssetReference;

	// How many to spawn for this wave
	public int SpawnQuantity = 10;

	// Interval between spawns in seconds
	public float SpawnInterval = 3.0f;

	public Wave() { }

	public Wave( AssetReference entityAssetRef, int quantity, float interval )
	{
		EntityAssetReference = entityAssetRef;
		SpawnQuantity = quantity;
		SpawnInterval = interval;
	}
}