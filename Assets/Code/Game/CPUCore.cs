using System.Buffers.Text;

using UnityEngine;

public class CPUCore : MonoBehaviour
{
	[SerializeField] private LayerMask entityLayerMask;
	public LayerMask EntityLayerMask { get { return entityLayerMask; } }

    void Start()
    {
        
    }

    void Update()
    {
        
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

		otherEntity.CPUDestroyEntity();

		// handle packets adding xp



		// handle viruses reducing hack timer
	}
}
