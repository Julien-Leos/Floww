using UnityEngine;

public class CarEngine : MonoBehaviour {
    public float maxSpeed = 100f;
    public float maxMotorTorque = 80f;
    public float maxSteerAngle = 45f;
    public float turnSpeed = 5f;
    public Vector3 centerOfMass = new Vector3();
    public WheelCollider LeftFrontWheel;
    public WheelCollider RightFrontWheel;
    public PathCreator creator;
    private int pointIndex = 0;

    void Start() {
        GetComponent<Rigidbody>().centerOfMass = centerOfMass;
    }

    void FixedUpdate() {
        ApplyMotor();
        ApplySteer();
        CheckNextWaypoint();
    }

    private void ApplyMotor() {
        float currentSpeed = 2 * Mathf.PI * LeftFrontWheel.radius * ((LeftFrontWheel.rpm + RightFrontWheel.rpm) / 2) * 60 / 1000;
        LeftFrontWheel.motorTorque = RightFrontWheel.motorTorque = currentSpeed <= maxSpeed ? maxMotorTorque : 0;
    }

    private void ApplySteer() {
        Vector3 relativeVector = transform.InverseTransformPoint(creator.path.segmentPoints[pointIndex]);
        float newSteerAngle = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;
        LeftFrontWheel.steerAngle = Mathf.Lerp(LeftFrontWheel.steerAngle, newSteerAngle, turnSpeed * Time.deltaTime);
        RightFrontWheel.steerAngle = Mathf.Lerp(RightFrontWheel.steerAngle, newSteerAngle, turnSpeed * Time.deltaTime);
    }

    private void CheckNextWaypoint() {
        if (Vector3.Distance(transform.position, creator.path.segmentPoints[pointIndex]) < 0.25f) {
            pointIndex = (pointIndex + 1) % creator.path.segmentPoints.Count;
        }
    }
}
