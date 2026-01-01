using UnityEditor;
using UnityEngine;

public class PathwayManager : MonoBehaviour
{
	[SerializeField] private NodeManager nodeManager;
	public NodeManager NodeManager => nodeManager;

	[SerializeField] private SpawnManager spawnManager;
	public SpawnManager SpawnManager => spawnManager;

	[SerializeField] private Pathway[] pathways;

	private void Start()
	{
#if UNITY_EDITOR
		// Only in the editor: select and ping this object in the Hierarchy when play starts.
		if ( Application.isPlaying )
		{
			Selection.activeGameObject = this.gameObject;
			EditorGUIUtility.PingObject( this.gameObject );
		}
#endif
	}

	public void SetupPathways( int currentCPULevel )
	{
		foreach ( var pathway in pathways )
		{
			pathway?.gameObject.SetActive( false );
		}

		UpdatePathwaysAdd( currentCPULevel );
	}

	public void UpdatePathwaysAdd( int currentCPULevel )
	{
		for ( int i = 0; i < currentCPULevel && i < pathways.Length; i++ )
		{
			pathways[ i ]?.gameObject.SetActive( true );
			nodeManager?.Nodes[ i ]?.Setup();
		}

		spawnManager.Setup( pathways );
	}

	public void UpdatePathwaysRemove( int currentCPULevel )
	{
		for ( int i = currentCPULevel; i < pathways.Length; i++ )
		{
			pathways[ i ]?.gameObject.SetActive( false );
			if ( nodeManager?.Nodes[ i ]?.FirstTimeLoad == true )
				nodeManager?.Nodes[ i ]?.ResetNode();
		}

		// Ensure SpawnManager refreshes its wave selection when CPU level is decreased.
		// This will rebuild spawns and restart spawn coroutines based on the current CPU level.
		if ( spawnManager != null )
		{
			spawnManager.Setup( pathways );
		}
	}
}