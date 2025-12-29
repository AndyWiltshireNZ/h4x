using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu( fileName = "LevelDefinition", menuName = "Definitions/LevelDefinition" )]
public class LevelDefinition : ScriptableObject
{
	public AssetReference LevelAssetReference;
	public int StartCPULevel = 1;
	public int EndCPULevel = 4;
	public WaveDefinition[] WaveDefinitions;
}