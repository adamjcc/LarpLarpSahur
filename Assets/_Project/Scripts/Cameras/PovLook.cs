using UnityEngine;

/// Mouse-look for the POV cameras, with limits so the player can't spin round and see
/// through the back of someone's head.
///
/// Put this on the CinemachineCamera GameObject itself. The CameraDirector enables it only
/// while that camera is live — if two of these ran at once they'd both bank up rotation and
/// you'd get bizarre drift when you switched back.
///
/// It rotates its own LOCAL rotation, so it works correctly when the camera is a child of a
/// moving anchor (the car, or a POV_Free follower on the pedestrian).
public class PovLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float sensitivity = 2f;

    [Header("Limits, in degrees from the resting direction")]
    [SerializeField] private float minPitch = -70f;   // how far down you can look
    [SerializeField] private float maxPitch = 45f;    // how far up you can look

    [SerializeField] private bool clampYaw = true;
    [SerializeField] private float minYaw = -70f;     // left
    [SerializeField] private float maxYaw = 70f;      // right

    private float yaw;
    private float pitch;

    /// Snap back to looking straight ahead. Called when this camera becomes live, so you
    /// never inherit where you happened to be looking last time.
    public void ResetLook()
    {
        yaw = 0f;
        pitch = 0f;
        Apply();
    }

    private void OnEnable()
    {
        ResetLook();
    }

    private void Update()
    {
        // Mouse axes are already a per-frame delta, so they must NOT be multiplied
        // by Time.deltaTime. Doing that is the single most common mouse-look bug.
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        yaw += mouseX;
        pitch -= mouseY;   // inverted: pushing the mouse forward looks up

        if (clampYaw) yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Apply();
    }

    private void Apply()
    {
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
