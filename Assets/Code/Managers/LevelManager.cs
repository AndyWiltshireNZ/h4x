using UnityEngine;

public class LevelManager : MonoBehaviour
{
	[SerializeField] private LevelDefinition LevelDefinition;

	//Load level based on LevelDefinition

	private void Start()
	{
		Debug.Log( "LevelManager started" );
	}

	private void Update()
	{

	}
}
