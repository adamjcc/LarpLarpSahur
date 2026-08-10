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
    [Tooltip("Raw pixel delta from the Input System, so this is roughly a TENTH of what " +
             "the old Input.GetAxis value needed. 0.2 is a good starting point.")]
    [SerializeField] private float sensitivity = 0.2f;

    [Header("Limits, in degrees from the resting direction")]
    // NOTE: named by what they DO, not by min/max. In Unity a positive X-rotation tilts
    // the nose DOWN, so "minPitch = -70" actually meant "70 degrees UP" — which is how the
    // brake pedal at 57.7 degrees below horizontal ended up out of reach behind a 45 cap.
    [Tooltip("How far below the resting direction you can look. The brake pedal is about " +
             "58 degrees below the driver's eyeline, so this needs headroom.")]
    [SerializeField] private float maxLookDownAngle = 80f;

    [Tooltip("How far above the resting direction you can look.")]
    [SerializeField] private float maxLookUpAngle = 60f;

    [SerializeField] private bool clampYaw = true;
    [SerializeField] private float maxLookLeftAngle = 80f;
    [SerializeField] private float maxLookRightAngle = 80f;

    private float yaw;
    private float pitch;

    // The rotation you set in the Inspector. Mouse-look is applied ON TOP of this, so the
    // passenger-seat camera keeps its 25-degree turn toward the driver and the limits are
    // measured from THAT direction, not from straight ahead.
    private Quaternion baseRotation;
    private bool captured;

    private void Awake()
    {
        CaptureBase();
    }

    private void CaptureBase()
    {
        if (captured) return;
        baseRotation = transform.localRotation;
        captured = true;
    }

    /// Snap back to the resting direction. Called when this camera becomes live, so you
    /// never inherit where you happened to be looking last time.
    public void ResetLook()
    {
        CaptureBase();
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
        // A free cursor means the player is pointing at UI, not looking around — a dialogue
        // panel, the debrief, anything modal. Without this the camera keeps turning while
        // you drag the mouse toward a button.
        //
        // This lives here rather than in PlayerRig because PovLook sits on the CAMERA, not
        // on the player, so disabling the player's controller never reached it.
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // Already a per-frame delta, so it must NOT be multiplied by Time.deltaTime.
        // Doing that is the single most common mouse-look bug.
        Vector2 look = GameInput.LookDelta;

        float mouseX = look.x * sensitivity;
        float mouseY = look.y * sensitivity;

        yaw += mouseX;
        pitch -= mouseY;   // mouse forward = look up

        // Positive pitch = looking DOWN, negative = looking UP.
        pitch = Mathf.Clamp(pitch, -maxLookUpAngle, maxLookDownAngle);

        if (clampYaw) yaw = Mathf.Clamp(yaw, -maxLookLeftAngle, maxLookRightAngle);

        Apply();
    }

    private void Apply()
    {
        // base first, then the player's offset from it
        transform.localRotation = baseRotation * Quaternion.Euler(pitch, yaw, 0f);
    }
}
