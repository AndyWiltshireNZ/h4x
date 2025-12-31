using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu( fileName = "CPUManagerDefinition", menuName = "Definitions/CPUManagerDefinition" )]
public class CPUManagerDefinition : ScriptableObject
{
	public AssetReference CpuCanvasAssetReference;
	public int DebugXPGainAmount = 25;
}