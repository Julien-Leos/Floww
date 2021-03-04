using UnityEngine;

public class WheelMovement : MonoBehaviour {
    public WheelCollider wheel;
    private Vector3 position = new Vector3();
    private Quaternion rotation = new Quaternion();

    void Update() {
        wheel.GetWorldPose(out position, out rotation);
        transform.position = position;
        transform.rotation = rotation;
    }
}
