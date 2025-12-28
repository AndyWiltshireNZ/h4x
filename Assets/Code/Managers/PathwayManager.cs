using System.Collections.Generic;
using UnityEngine;

public class PathwayManager : MonoBehaviour
{
	[SerializeField] private NodeManager nodeManager;
	[SerializeField] private SpawnManager spawnManager;

	[SerializeField] private Pathway[] pathways;

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
	}

	public void UpdatePathwaysRemove( int currentCPULevel )
	{
		for ( int i = currentCPULevel; i < pathways.Length; i++ )
		{
			pathways[ i ].gameObject.SetActive( false );
		}
	}
}