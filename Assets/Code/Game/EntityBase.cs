using UnityEngine;
using SplineArchitect.Objects;

public class EntityBase : MonoBehaviour
{
	private SpawnManager spawnManager;
	[SerializeField] private SphereCollider sphereCollider;
	[SerializeField] private SphereCollider sphereNodeCollider;

	private MoveAlongPathway mover;
	private SplineObject splineObject;

	public void Setup( SpawnManager spawnManager, float entitySpeed )
	{
		this.spawnManager = spawnManager;

		MoveAlongPathway localMover = GetComponent<MoveAlongPathway>();
		if ( localMover != null )
		{
			localMover.Speed = new Vector3( 0f, 0f, entitySpeed );
		}

		// cache common components for collision handling
		mover = localMover;
		splineObject = GetComponent<SplineObject>();
	}

	private void OnTriggerEnter(Collider other)
	{
		TryMatchSpeedWith( other );
	}

	private void TryMatchSpeedWith(Collider otherCollider)
	{
		if ( otherCollider == null )
			return;

		if ( otherCollider.gameObject == this.gameObject )
			return;

		EntityBase otherEntity = otherCollider.GetComponentInParent<EntityBase>();
		if ( otherEntity == null )
			return;

		MoveAlongPathway otherMover = otherEntity.GetComponent<MoveAlongPathway>();
		if ( otherMover == null )
			return;

		if ( splineObject == null )
			splineObject = GetComponent<SplineObject>();

		SplineObject otherSpline = otherEntity.GetComponent<SplineObject>();
		if ( otherSpline == null || splineObject == null )
			return;

		// Only match speed if the other entity is ahead on the spline (larger local z)
		if ( otherSpline.localSplinePosition.z <= splineObject.localSplinePosition.z )
			return;

		if ( mover == null )
			mover = GetComponent<MoveAlongPathway>();

		if ( mover != null )
		{
			mover.Speed = otherMover.Speed;
		}
	}

	public virtual void DestroyEntity()
	{
		// run destroy fx

		//Debug.Log( "EntityBase: Destroying entity " + this.gameObject.name );

		Destroy( this.gameObject );
	}

	public virtual void CPUDestroyEntity()
	{
		// run cpu destroy fx

		//Debug.Log( "EntityBase: CPU Destroying entity " + this.gameObject.name );

		Destroy( this.gameObject );
	}

	private void OnDestroy()
	{
		spawnManager?.RemovedSpawnedFromList( this.gameObject );
	}
}
