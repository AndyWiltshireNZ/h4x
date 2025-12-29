using UnityEngine;

public class EntityBase : MonoBehaviour
{
	private SpawnManager spawnManager;

    public void Setup( SpawnManager spawnManager )
    {
		this.spawnManager = spawnManager;
    }

	private void OnDestroy()
	{
		spawnManager?.RemovedSpawnedFromList( this.gameObject );
	}
}
