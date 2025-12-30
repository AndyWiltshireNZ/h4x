using UnityEngine;

[CreateAssetMenu( fileName = "WaveDefinition", menuName = "Definitions/WaveDefinition" )]

public class WaveDefinition : ScriptableObject
{
	public float entitySpeed = 1.0f;
	public float SpawnInterval = 3.0f;
	public Wave[] Entities;
}