using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
	public static InputSystem inputActions;
	public static InputSystem InputActions { get { if ( inputActions == null ) { inputActions = new InputSystem(); } return inputActions; } }

	private bool isLeftClickHeld = false;

	private void Awake()
	{
		if ( inputActions == null )
		{
			inputActions = new InputSystem();
		}
		else
		{
			Destroy( this.gameObject );
		}
	}

	private void OnEnable()
	{
		inputActions.UI.LeftClick.Enable();
		inputActions.UI.LeftClick.performed += LeftClick_pressed;

		Debug.Log( "InputManager enabled" );
	}

	private void OnDestroy()
	{
		inputActions.UI.LeftClick.Disable();
	}

	private void LeftClick_pressed( InputAction.CallbackContext ctx )
	{
		bool isPressed = ctx.ReadValue<float>() > 0.5f;

		if ( isPressed )
		{
			leftClickPressed();
		}
		else
		{
			leftClickReleased();
		}
	}

	private void leftClickPressed()
	{
		Debug.Log( "Left Click Pressed" );
		isLeftClickHeld = true;
		StartCoroutine( leftClickHeldRoutine() );
	}

	private IEnumerator leftClickHeldRoutine()
	{
		while ( isLeftClickHeld )
		{
			LeftClick_held();
			yield return null;
		}
	}

	private void LeftClick_held()
	{
		//Debug.Log( "Left Click Held" );
	}

	private void leftClickReleased()
	{
		isLeftClickHeld = false;
		//Debug.Log( "Left Click Released" );
	}
}
