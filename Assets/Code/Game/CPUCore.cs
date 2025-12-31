using UnityEngine;

public class CPUCore : MonoBehaviour
{
	[SerializeField] private LayerMask entityLayerMask;
	public LayerMask EntityLayerMask => entityLayerMask;

	[SerializeField] private BoxCollider packetBoxCollider;
	[SerializeField] private BoxCollider virusBoxCollider;

	// update hack timer
	private Level currentLevel;

	// update xp level
	private CPUManager cpuManager;

	public void Setup( Level level, CPUManager cpuManager )
	{
		this.currentLevel = level;
		this.cpuManager = cpuManager;
	}

	// Collision-based handling: only process collisions where the other object's layer
	// is included in the serialized `entityLayerMask` (e.g. "packet" or "virus").
	// Additionally only handle Packet collisions that hit `packetBoxCollider` and
	// Virus collisions that hit `virusBoxCollider`.
	private void OnCollisionEnter( Collision collision )
	{
		if ( packetBoxCollider == null || virusBoxCollider == null ) { return; }
		if ( collision == null ) { return; }

		Collider otherCollider = collision.collider;
		if ( otherCollider == null ) { return; }

		int otherLayer = otherCollider.gameObject.layer;

		// Check LayerMask membership
		if ( (entityLayerMask.value & (1 << otherLayer)) == 0 )
		{
			return;
		}

		EntityBase otherEntity = otherCollider.GetComponentInParent<EntityBase>();
		if ( otherEntity == null ) { return; }

		int packetLayer = LayerMask.NameToLayer( "Packet" );
		int virusLayer = LayerMask.NameToLayer( "Virus" );

		bool collidedWithPacketBox = false;
		bool collidedWithVirusBox = false;

		foreach ( ContactPoint contact in collision.contacts )
		{
			Collider thisCollider = contact.thisCollider;
			if ( thisCollider == packetBoxCollider )
			{
				collidedWithPacketBox = true;
			}
			else if ( thisCollider == virusBoxCollider )
			{
				collidedWithVirusBox = true;
			}

			if ( collidedWithPacketBox && collidedWithVirusBox )
			{
				break;
			}
		}

		bool handled = false;

		if ( otherLayer == packetLayer && collidedWithPacketBox )
		{
			UpdateCPUManagerForPacket( otherEntity );
			handled = true;
		}
		else if ( otherLayer == virusLayer && collidedWithVirusBox )
		{
			UpdateLevelForVirus( otherEntity );
			handled = true;
		}

		if ( handled )
		{
			otherEntity.CPUDestroyEntity();
		}
	}

	private void UpdateCPUManagerForPacket( EntityBase otherEntity )
	{
		switch ( otherEntity.GetEntityType() )
		{
			case EntityBase.EntityType.Packet:
				// add xp to cpu manager
				int xpValue = otherEntity.gameObject.GetComponent<Packet>()?.XPValue ?? 0;
				cpuManager.AddXP( xpValue );
				//Debug.Log( $"Packet collected: {xpValue} +XP!" );
				break;
			case EntityBase.EntityType.Silica:
				// add silica to ...

				Debug.Log( "Silica collected: +Silica!" );
				break;
			default:
				break;
		}
	}

	private void UpdateLevelForVirus( EntityBase otherEntity )
	{
		switch ( otherEntity.GetEntityType() )
		{
			case EntityBase.EntityType.Virus:
				// reduce hack timer

				//Debug.Log( "Virus hit: -Hack Time!" );
				break;
			case EntityBase.EntityType.Malware:
				// reduce hack timer + randomize nodes

				Debug.Log( "Malware hit: -Hack Time + Randomize Nodes!" );
				break;
			default:
				break;
		}
	}
}
