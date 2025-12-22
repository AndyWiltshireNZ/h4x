using UnityEngine;
using SplineArchitect.Objects;

public class MoveAlongPathway : MonoBehaviour
{
    private SplineObject splineObject;
    [SerializeField] private Vector3 speed;
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
			//splineObject.localSplinePosition = startPos;
			Destroy( this.gameObject );
		}
    }
}