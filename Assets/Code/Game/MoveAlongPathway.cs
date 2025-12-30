using UnityEngine;
using SplineArchitect.Objects;

public class MoveAlongPathway : MonoBehaviour
{
    private SplineObject splineObject;
    private Vector3 speed;
	public Vector3 Speed { get { return speed; } set { speed = value; } }
	[SerializeField] private bool loop = false;
	private float resetRange;
    private Vector3 startPos;
	private bool canMove = false;

	public void Setup()
    {
		splineObject = GetComponent<SplineObject>();
		resetRange = splineObject.splineParent.length;
		startPos = splineObject.localSplinePosition;
		canMove = true;
    }

    void Update()
    {
		if ( !canMove )
			return;

		splineObject.localSplinePosition += speed * Time.deltaTime;

		if ( splineObject.localSplinePosition.z > resetRange )
		{
			if ( loop == true )
			{
				splineObject.localSplinePosition = startPos;
			}
			else
			{
				Destroy( this.gameObject );
			}
		}
    }
}