using UnityEngine;

public class Node : MonoBehaviour
{
	[SerializeField] private GameObject nodeClosedObject;
	[SerializeField] private GameObject nodeOpenObject;
	[SerializeField] private GameObject nodeHoverObject;
	[SerializeField] private BoxCollider triggerBoxCollider;
	public BoxCollider TriggerBoxCollider { get { return triggerBoxCollider; } }

	[SerializeField] private LayerMask entityLayerMask;
	public LayerMask EntityLayerMask { get { return entityLayerMask; } }

	private bool isOpen = false;
	private bool isHovered = false;
	public bool IsHovered { get { return isHovered; } set { isHovered = value; } }

	private bool canHover = true;

	private bool firstTimeLoad = false;
	public bool FirstTimeLoad { get { return firstTimeLoad; } set { firstTimeLoad = value; } }

	public void Setup()
	{
		if ( firstTimeLoad == false )
		{
			nodeOpenObject.SetActive( false );
			nodeClosedObject.SetActive( true );
			nodeHoverObject.SetActive( false );

			isOpen = false;
			isHovered = false;
			canHover = true;

			firstTimeLoad = true;
		}
	}

	public void ResetNode()
	{
		firstTimeLoad = false;
		Setup();
	}

	public void StopAllNodes()
	{
		nodeOpenObject.SetActive( false );
		nodeClosedObject.SetActive( true );
		nodeHoverObject.SetActive( false );
		isOpen = false;
		isHovered = false;
		canHover = false;
		firstTimeLoad = false;
	}

	public void ToggleObjects()
    {
		if ( canHover == false ) { return; }

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
		if ( canHover == false ) { return; }

		isHovered = on;

		if ( nodeHoverObject != null )
		{
			nodeHoverObject.SetActive( on );
		}
	}

	// Collision-based handling: only process collisions where the other object's layer
	// is included in the serialized `entityLayerMask` (e.g. "packet" or "virus").
	private void OnCollisionEnter( Collision collision )
	{
		if ( collision == null ) { return; }

		Collider otherCollider = collision.collider;
		if ( otherCollider == null ) { return; }

		int otherLayer = otherCollider.gameObject.layer;

		// Check LayerMask membership
		if ( (entityLayerMask.value & (1 << otherLayer)) == 0 )
		{
			// not in the configured mask -> ignore
			return;
		}

		// If the collided object (or its parent) is an EntityBase, destroy it so its cleanup runs.
		EntityBase otherEntity = otherCollider.GetComponentInParent<EntityBase>();
		if ( otherEntity == null ) { return; }

		otherEntity.DestroyEntity();
	}
}
