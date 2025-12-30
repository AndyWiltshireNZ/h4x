using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class Wave
{
	// The addressable prefab to spawn
	public AssetReference EntityAssetReference;

	public Wave() { }

	public Wave( AssetReference entityAssetRef )
	{
		EntityAssetReference = entityAssetRef;
	}
}