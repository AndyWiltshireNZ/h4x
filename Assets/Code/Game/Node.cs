using UnityEngine;

public class Node : MonoBehaviour
{
	[SerializeField] private GameObject nodeClosedObject;
	[SerializeField] private GameObject nodeOpenObject;
	[SerializeField] private GameObject nodeHoverObject;
	[SerializeField] private BoxCollider triggerBoxCollider;
	public BoxCollider TriggerBoxCollider { get { return triggerBoxCollider; } }

	private bool isOpen = false;
	private bool isHovered = false;
	public bool IsHovered { get { return isHovered; } set { isHovered = value; } }

	public void Setup()
	{
		nodeOpenObject.SetActive( false );
		nodeClosedObject.SetActive( true );
		nodeHoverObject.SetActive( false );

		isOpen = false;
		isHovered = false;
	}

	public void ToggleObjects()
    {
		switch ( isOpen )
		{
			case true:
				nodeOpenObject.SetActive( false );
				nodeClosedObject.SetActive( true );
				isOpen = false;
				break;
			case false:
				nodeOpenObject.SetActive( true );
				nodeClosedObject.SetActive( false );
				isOpen = true;
				break;
		}

		//Debug.Log( "Node " + gameObject.name + " is now " + (isOpen ? "Open" : "Closed") );
	}

	public void SetHover( bool on )
	{
		isHovered = on;

		if ( nodeHoverObject != null )
		{
			nodeHoverObject.SetActive( on );
		}
	}
}
