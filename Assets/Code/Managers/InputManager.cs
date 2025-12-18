using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class InputManager : MonoBehaviour
{
	private InputSystem inputActions;
	public InputSystem InputActions { get { if ( inputActions == null ) { inputActions = new InputSystem(); } return inputActions; } }

	private bool isLeftClickHeld = false;

	private void Awake()
	{
		if ( inputActions == null ) { inputActions = new InputSystem(); }
	}

	public void Setup()
	{
		inputActions.UI.Enable();
		inputActions.UI.Click.performed += LeftClick_pressed;
		inputActions.UI.Reload.performed += Reload_pressed;
		inputActions.UI.Quit.performed += Quit_pressed;

		Debug.Log( "InputManager enabled" );
	}

	private void OnDisable()
	{
		inputActions.UI.Click.performed -= LeftClick_pressed;
		inputActions.UI.Reload.performed -= Reload_pressed;
		inputActions.UI.Quit.performed -= Quit_pressed;
		inputActions.UI.Disable();
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

	private void Reload_pressed( InputAction.CallbackContext ctx )
	{
		SceneManager.LoadScene( SceneManager.GetActiveScene().name );
		Debug.Log( "Scene Reloaded" );
	}

	private void Quit_pressed( InputAction.CallbackContext ctx )
	{
		Application.Quit();
		Debug.Log( "Application Quit" );
	}
}
