/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * PovLook.cs
 * Mouse look with limits, used by the point-of-view cameras.
 */

using UnityEngine;

/// <summary>
/// Mouse-look for the POV cameras, with limits so the player can't spin round and see
/// through the back of someone's head.
///
/// Put this on the CinemachineCamera GameObject itself. The CameraDirector enables it only
/// while that camera is live — if two of these ran at once they'd both bank up rotation and
/// you'd get bizarre drift when you switched back.
///
/// It rotates its own LOCAL rotation, so it works correctly when the camera is a child of a
/// moving anchor (the car, or a POV_Free follower on the pedestrian).
/// </summary>
public class PovLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [Tooltip("The Look binding pre-scales by 0.05, so this wants roughly DOUBLE the old " +
             "Input.GetAxis sensitivity. 4 is a good starting point.")]
    [SerializeField] private float sensitivity = 4f;

    [Tooltip("Tick if pushing the mouse forward should look DOWN.")]
    [SerializeField] private bool invertY = false;

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

    /// <summary>
    /// Snap back to the resting direction. Called when this camera becomes live, so you
    /// never inherit where you happened to be looking last time.
    /// </summary>
    public void ResetLook()
    {
        CaptureBase();
        yaw = 0f;
        pitch = 0f;
        Apply();
    }

    /// <summary>
    /// Change where "straight ahead" is, at runtime.
    ///
    /// Used when the pedestrian puts her phone away: her head is angled down AND slightly
    /// off to one side while she's reading, and both should return to level once she
    /// looks up.
    ///
    /// pitchDegrees: positive looks DOWN.
    /// yawDegrees:   positive looks RIGHT.
    /// </summary>
    public void SetBaseRotation(float pitchDegrees, float yawDegrees)
    {
        captured = true;
        baseRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        yaw = 0f;
        pitch = 0f;
        Apply();
    }

    public void SetBasePitch(float pitchDegrees) => SetBaseRotation(pitchDegrees, 0f);

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

        // PLUS, not minus. The Look binding already carries InvertVector2, so look.y is
        // negative when the mouse moves up. Subtracting it inverted the axis a second time,
        // which is why pushing the mouse up sent the camera down. Starter Assets adds it
        // for exactly the same reason.
        pitch += invertY ? -mouseY : mouseY;

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
