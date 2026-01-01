using UnityEngine;

[CreateAssetMenu( fileName = "WaveDefinition", menuName = "Definitions/WaveDefinition" )]

public class WaveDefinition : ScriptableObject
{
	public Entity[] Entities;
}