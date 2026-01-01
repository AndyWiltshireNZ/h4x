using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu( fileName = "PacketDefinition", menuName = "Definitions/PacketDefinition" )]
public class PacketDefinition : ScriptableObject
{
	public AssetReference PacketAssetReference;
}