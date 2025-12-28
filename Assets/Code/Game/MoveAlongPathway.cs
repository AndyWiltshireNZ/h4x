using UnityEngine;
using SplineArchitect.Objects;

public class MoveAlongPathway : MonoBehaviour
{
    private SplineObject splineObject;
    [SerializeField] private Vector3 speed;
	public Vector3 Speed { get { return speed; } set { speed = value; } }
	[SerializeField] private bool loop = false;
	private float resetRange;
    private Vector3 startPos;

    void Start()
    {
		splineObject = GetComponent<SplineObject>();
		resetRange = splineObject.splineParent.length;
		startPos = splineObject.localSplinePosition;
    }

    void Update()
    {
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