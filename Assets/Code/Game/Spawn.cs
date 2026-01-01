using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class Spawn
{
	[Tooltip("Addressable prefab to spawn")]
	public AssetReference Prefab;
}