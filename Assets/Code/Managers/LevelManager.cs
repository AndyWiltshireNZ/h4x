using UnityEngine;

public class LevelManager : MonoBehaviour
{
	[SerializeField] private LevelDefinition LevelDefinition;

	//Load level based on LevelDefinition

	public void Setup()
	{
		Debug.Log( "LevelManager started." );
	}

	private void Update()
	{

	}
}
