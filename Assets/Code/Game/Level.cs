using UnityEngine;

public class Level : MonoBehaviour
{
	[SerializeField] private LevelDefinition levelData;

	private void Awake()
    {
        this.gameObject.SetActive( false );
    }

	public void Setup()
	{
		this.gameObject.SetActive( true );
	}
}
