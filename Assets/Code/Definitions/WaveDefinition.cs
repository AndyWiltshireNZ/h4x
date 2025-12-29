using UnityEngine;

[CreateAssetMenu( fileName = "WaveDefinition", menuName = "Definitions/WaveDefinition" )]

public class WaveDefinition : ScriptableObject
{
	public float TimeBetweenWaves = 6.0f;
	public Wave[] Waves;
}