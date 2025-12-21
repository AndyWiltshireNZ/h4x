using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu( fileName = "LevelManagerDefinition", menuName = "Definitions/LevelManagerDefinition" )]
public class LevelManagerDefinition : ScriptableObject
{
	public AssetReference TestLevelAssetReference;
	public LevelDefinition[] availableLevels;

}