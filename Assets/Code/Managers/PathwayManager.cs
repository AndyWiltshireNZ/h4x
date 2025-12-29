using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PathwayManager : MonoBehaviour
{
	[SerializeField] private NodeManager nodeManager;
	[SerializeField] private SpawnManager spawnManager;

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
			pathway.gameObject.SetActive( false );
		}

		UpdatePathwaysAdd( currentCPULevel );
	}

	public void UpdatePathwaysAdd( int currentCPULevel )
	{
		for ( int i = 0; i < currentCPULevel && i < pathways.Length; i++ )
		{
			pathways[ i ].gameObject.SetActive( true );
			nodeManager.Nodes[ currentCPULevel - 1 ].Setup();
		}

		spawnManager.Setup( pathways );
	}

	public void UpdatePathwaysRemove( int currentCPULevel )
	{
		for ( int i = currentCPULevel; i < pathways.Length; i++ )
		{
			pathways[ i ].gameObject.SetActive( false );
		}
	}
}