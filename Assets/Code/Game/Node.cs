using UnityEngine;
using UnityEngine.InputSystem;

public class Node : MonoBehaviour
{
	[SerializeField] private GameObject nodeClosedObject;
	[SerializeField] private GameObject nodeOpenObject;
	[SerializeField] private GameObject nodeHoverObject;
	[SerializeField] private BoxCollider triggerBoxCollider;

	[SerializeField] private LayerMask clickableLayer;
    [SerializeField] private float maxDistance = 100f;

	private Camera mainCamera;

	private InputManager inputManager;

	private bool isOpen = false;
	private bool isHovered = false;

	public void Setup()
	{
		inputManager = GameMode.Instance.InputManager;
		inputManager.InputActions.UI.Click.performed += LeftClick_pressed;
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
			inputManager.InputActions.UI.Click.performed -= LeftClick_pressed;
		}
	}

	private void Start()
    {
        nodeOpenObject.SetActive( false );
		nodeClosedObject.SetActive( true );
		nodeHoverObject.SetActive( false );

		isOpen = nodeOpenObject != null && nodeOpenObject.activeSelf;

		mainCamera = GameMode.Instance.CurrentCamera;
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
			if ( triggerBoxCollider != null )
			{
				isHitThisNode = hit.collider == triggerBoxCollider;
			}
			if ( isHitThisNode )
			{
				ToggleObjects();
			}
		}
	}

	private void ToggleObjects()
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

		Debug.Log( "Node " + gameObject.name + " is now " + (isOpen ? "Open" : "Closed") );
	}

	private void Update()
	{
		if ( mainCamera == null )
		{
			mainCamera = GameMode.Instance?.CurrentCamera ?? Camera.main;
			if ( mainCamera == null ) { return; }
		}

		if ( Mouse.current == null ) { return; }

		Vector2 mousePos = Mouse.current.position.ReadValue();
		Ray ray = mainCamera.ScreenPointToRay( mousePos );

		bool hitThisNode = false;
		if ( Physics.Raycast( ray, out RaycastHit hit, maxDistance, clickableLayer ) )
		{
			if ( triggerBoxCollider != null )
			{
				hitThisNode = hit.collider == triggerBoxCollider;
			}
			else
			{
				hitThisNode = hit.collider.transform.IsChildOf( transform );
			}
		}

		if ( hitThisNode && !isHovered )
		{
			SetHover( true );
		}
		else if ( !hitThisNode && isHovered )
		{
			SetHover( false );
		}
	}

	private void SetHover( bool on )
	{
		isHovered = on;

		if ( nodeHoverObject != null )
		{
			nodeHoverObject.SetActive( on );
		}
	}
}
