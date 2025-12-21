using UnityEngine;

public class NodeManager : MonoBehaviour
{
	[SerializeField] private Node[] nodes;
	public Node[] Nodes { get { return nodes; } }

	void Start()
    {
		foreach ( var node in nodes )
		{
			node.Setup();
		}

	}
}
