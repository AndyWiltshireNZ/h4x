using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu( fileName = "GameModeDefinition", menuName = "Definitions/GameModeDefinition" )]
public class GameModeDefinition : ScriptableObject
{
	public bool DebugMode = false;

	public AssetReference UIManagerAssetReference;
	public AssetReference InputManagerAssetReference;
	public AssetReference LevelManagerAssetReference;
	public AssetReference UpgradeManagerAssetReference;
	public AssetReference AudioManagerAssetReference;

	public enum GameModeType
	{
		Default
	}
	public GameModeType GameMode;
}