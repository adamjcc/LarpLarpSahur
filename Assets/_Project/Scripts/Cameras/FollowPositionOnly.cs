/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * FollowPositionOnly.cs
 * Follows a bone's position without inheriting its rotation.
 */

using UnityEngine;

/// <summary>
/// Copies a target's POSITION every frame but never its rotation.
///
/// WHY THIS EXISTS. The pedestrian's free-look POV camera needs to sit at her head, but if
/// you simply parent it to the head bone it inherits the walk animation's head rotation.
/// The animation and the player's mouse then fight each other every frame and the result is
/// unusable — the view lurches with every step.
///
/// So: an empty GameObject with this script follows her head's position only, and stays
/// world-aligned. The camera parents to THAT, and mouse-look on top of it is smooth.
///
/// LateUpdate, not Update, because animation is applied during Update — reading the bone
/// any earlier gives you last frame's pose.
/// </summary>
public class FollowPositionOnly : MonoBehaviour
{
    [Tooltip("Usually a head bone, e.g. mixamorig:Head.")]
    [SerializeField] private Transform target;

    [Tooltip("Nudge in world space. Push forward a little so the camera clears the face.")]
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    [Tooltip("0 = snap instantly. Higher smooths out animation jitter. Try 10.")]
    [SerializeField] private float smoothing = 0f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 wanted = target.position + worldOffset;

        if (smoothing <= 0f)
        {
            transform.position = wanted;
        }
        else
        {
            // unscaledDeltaTime, so this keeps up during slow motion
            transform.position = Vector3.Lerp(
                transform.position, wanted, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
        }
    }
}
