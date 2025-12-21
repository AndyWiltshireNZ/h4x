using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu( fileName = "LevelDefinition", menuName = "Definitions/LevelDefinition" )]
public class LevelDefinition : ScriptableObject
{
	public AssetReference LevelAssetReference;
}