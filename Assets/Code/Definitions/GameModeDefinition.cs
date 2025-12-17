using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu( fileName = "GameModeDefinition", menuName = "Definitions/GameModeDefinition" )]
public class GameModeDefinition : ScriptableObject
{
	public AssetReference UIManagerAssetReference;
	public AssetReference InputManagerAssetReference;
	public AssetReference LevelManagerAssetReference;

	public enum GameModeType
	{
		Default
	}
	public GameModeType GameMode;
}