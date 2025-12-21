using UnityEngine;

public class Rotate : MonoBehaviour {

    public float speed = 1.0f;
    public Vector3 dir;

    public bool useSteppedRotation = false;
    public float stepAngle = 1f;

    private new Transform transform;
    private float accumulatedRotation;

    private void Start() {
        transform = GetComponent<Transform>();
    }

    private void Update()
    {
        float deltaRotation = speed * Time.deltaTime;
        
        if (!useSteppedRotation)
        {
            transform.Rotate(dir, deltaRotation);
        }
        else
        {
            // accumulate smooth rotation amount
            accumulatedRotation += deltaRotation;

            // if enough rotation accumulated to hit a step, snap it
            if (Mathf.Abs(accumulatedRotation) >= stepAngle)
            {
                float stepCount = Mathf.Floor(accumulatedRotation / stepAngle);
                float snappedAmount = stepCount * stepAngle;
                transform.Rotate(dir, snappedAmount);

                // remove applied rotation portion
                accumulatedRotation -= snappedAmount;
            }
        }
    }

    public void event_SetEnabled(bool enabled) {
        this.enabled = enabled;
    }

    public void event_SetSpeed(float newSpeed) {
        this.speed = newSpeed;
    }

    public void event_SetSpeedMult(float speedMult) {
        this.speed *= speedMult;
    }
}