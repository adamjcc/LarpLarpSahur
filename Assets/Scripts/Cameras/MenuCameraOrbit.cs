/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * MenuCameraOrbit.cs
 * Flies the main menu camera in a slow circle around the level.
 */

using UnityEngine;

/// <summary>
/// Moves the start menu camera in a slow circle around the level, so the title screen has a
/// living background instead of a still image.
///
/// Put this on CAM_StartMenu. The circle it flies is drawn in the Scene view as a yellow
/// ring, so you can see and adjust exactly where it goes without pressing Play.
/// </summary>
public class MenuCameraOrbit : MonoBehaviour
{
    [Header("Where it flies")]
    /// <summary>
    /// The middle of the circle. Drop an empty in the centre of the level and drag it here.
    /// Leave empty and it orbits the world origin.
    /// </summary>
    [Tooltip("Centre of the orbit. An empty GameObject in the middle of your level.")]
    [SerializeField] private Transform orbitCentre;

    /// <summary>How far out from the centre, in metres. This is your "how big is the map".</summary>
    [Tooltip("Radius of the circle. Watch the yellow ring in the Scene view.")]
    [SerializeField] private float radius = 60f;

    /// <summary>Height above the centre.</summary>
    [SerializeField] private float height = 25f;

    [Header("How it moves")]
    /// <summary>Degrees per second. 3 takes two minutes to go all the way round.</summary>
    [Tooltip("Degrees per second. Keep it slow — 2 to 5 reads as calm rather than dizzy.")]
    [SerializeField] private float degreesPerSecond = 3f;

    /// <summary>Where on the circle it starts, in degrees.</summary>
    [SerializeField] private float startAngle;

    /// <summary>
    /// How much the camera drifts up and down as it circles, in metres.
    /// A little makes the movement feel less mechanical. 0 keeps it perfectly level.
    /// </summary>
    [SerializeField] private float bobHeight = 2f;

    /// <summary>How quickly the up-and-down drift cycles.</summary>
    [SerializeField] private float bobSpeed = 0.2f;

    private float angle;

    private void OnEnable()
    {
        // Always start from the same place, so the menu looks identical every time you
        // come back to it from the debrief
        angle = startAngle;
        Apply();
    }

    private void Update()
    {
        // unscaledDeltaTime, because the menu should keep moving no matter what the
        // scenario clock happens to be doing behind it
        angle += degreesPerSecond * Time.unscaledDeltaTime;
        Apply();
    }

    /// <summary>Places the camera on the circle and points it at the centre.</summary>
    private void Apply()
    {
        Vector3 centre = orbitCentre != null ? orbitCentre.position : Vector3.zero;

        // Convert the angle to a point on the circle
        float radians = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(radians) * radius,
                                     height + Mathf.Sin(angle * bobSpeed) * bobHeight,
                                     Mathf.Cos(radians) * radius);

        transform.position = centre + offset;

        // Always look back at the middle of the level
        transform.rotation = Quaternion.LookRotation(centre - transform.position);
    }

    /// <summary>
    /// Draws the flight path in the Scene view so you can size it without guessing.
    /// Yellow ring = where the camera flies. Cyan line = what it is looking at.
    /// </summary>
    private void OnDrawGizmos()
    {
        Vector3 centre = orbitCentre != null ? orbitCentre.position : Vector3.zero;

        Gizmos.color = Color.yellow;

        // Draw the circle as 48 short lines
        Vector3 previous = Vector3.zero;
        for (int i = 0; i <= 48; i++)
        {
            float radians = (i / 48f) * Mathf.PI * 2f;
            Vector3 point = centre + new Vector3(Mathf.Sin(radians) * radius,
                                                 height,
                                                 Mathf.Cos(radians) * radius);

            if (i > 0) Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, centre);
        Gizmos.DrawWireSphere(centre, 1.5f);
    }
}
