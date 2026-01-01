using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class Entity
{
	// The addressable prefab to spawn
	public AssetReference EntityAssetReference;

	public Entity() { }

	public Entity( AssetReference entityAssetRef )
	{
		EntityAssetReference = entityAssetRef;
	}
}