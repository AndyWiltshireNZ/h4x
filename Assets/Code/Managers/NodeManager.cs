using UnityEngine;
using UnityEngine.InputSystem;

public class NodeManager : MonoBehaviour
{
	[SerializeField] private Node[] nodes;
	public Node[] Nodes { get { return nodes; } }

	[SerializeField] private LayerMask clickableLayer;
    private float maxDistance = 120f;

	private InputManager inputManager;

	private Camera mainCamera;

	private void Start()
	{
		inputManager = GameMode.Instance.InputManager;
		inputManager.InputActions.Game.Click.performed += LeftClick_pressed;

		mainCamera = GameMode.Instance.CurrentCamera;
	}

		private void OnDisable()
	{
		Unsubscribe();
	}

	private void OnDestroy()
	{
		Unsubscribe();
	}

	private void Unsubscribe()
	{
		if ( inputManager != null )
		{
			inputManager.InputActions.Game.Click.performed -= LeftClick_pressed;
		}
	}

	private void LeftClick_pressed( InputAction.CallbackContext ctx )
	{
		if ( ctx.phase != InputActionPhase.Performed ) return;

		bool isPressed = ctx.ReadValue<float>() > 0.5f;

		if ( isPressed )
		{
			LeftClickPressed();
		}
	}

	private void LeftClickPressed()
	{
		Ray ray = mainCamera.ScreenPointToRay( Mouse.current.position.ReadValue() );

		if ( Physics.Raycast( ray, out RaycastHit hit, maxDistance, clickableLayer ) )
		{
			bool isHitThisNode = false;

			for ( int i = 0; i < nodes.Length; i++ )
			{
				if ( nodes[i].TriggerBoxCollider != null )
				{
					isHitThisNode = hit.collider == nodes[i].TriggerBoxCollider;
				}

				if ( isHitThisNode )
				{
					nodes[i].ToggleObjects();
				}
			}
		}
	}

	private Node activeNode = null;

	private void Update()
	{
		if ( mainCamera == null )
		{
			mainCamera = GameMode.Instance?.CurrentCamera ?? Camera.main;
			if ( mainCamera == null ) { return; }
		}

		if ( Mouse.current == null ) { return; }

		if ( nodes == null || nodes.Length == 0 )
		{
			// nothing to do
			if ( activeNode != null )
			{
				activeNode.SetHover( false );
				activeNode = null;
			}
			return;
		}

		Vector2 mousePos = Mouse.current.position.ReadValue();
		Ray ray = mainCamera.ScreenPointToRay( mousePos );

		// raycast once and find the matching node (if any)
		Node hitNode = null;
		if ( Physics.Raycast( ray, out RaycastHit hit, maxDistance, clickableLayer ) )
		{
			for ( int i = 0; i < nodes.Length; i++ )
			{
				Node node = nodes[i];
				if ( node == null ) { continue; }

				BoxCollider collider = node.TriggerBoxCollider;
				// if collider is null, accept child colliders as match
				if ( collider != null )
				{
					if ( hit.collider == collider )
					{
						hitNode = node;
						break;
					}
				}
				else
				{
					if ( hit.collider.transform.IsChildOf( node.transform ) )
					{
						hitNode = node;
						break;
					}
				}
			}
		}

		// if the hovered node changed, update hover states
		if ( hitNode != activeNode )
		{
			if ( activeNode != null && activeNode.IsHovered )
			{
				activeNode.SetHover( false );
			}

			if ( hitNode != null && !hitNode.IsHovered )
			{
				hitNode.SetHover( true );
			}

			activeNode = hitNode;
		}
	}

	public void ResetAllNodes()
	{
		foreach ( var node in nodes )
		{
			node?.StopAllNodes();
		}
	}
}