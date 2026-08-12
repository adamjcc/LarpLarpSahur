/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * ScenarioPath.cs
 * A route of waypoints that can be queried by distance.
 */

using UnityEngine;

/// <summary>
/// A route made of waypoints, which you can ask questions about by DISTANCE.
///
/// This is the difference between this and normal waypoint code. Normal waypoint code only
/// knows "move toward the next point". This knows its own total length, so you can ask
/// "where am I after travelling 34.2 metres?" and get an answer instantly, without having
/// to simulate every step to get there. That is what makes rewind and replay possible.
///
/// Put this on an empty GameObject, then make empty children for the waypoints and drag
/// them into the Points array IN ORDER.
/// </summary>
public class ScenarioPath : MonoBehaviour
{
    [Tooltip("The waypoints, in travel order. Order matters — out of order gives a zigzag.")]
    [SerializeField] private Transform[] points;

    // cumulative[i] = how far along the path waypoint i sits.
    // e.g. cumulative[0] is always 0, cumulative[2] might be 45.7
    private float[] cumulative;

    /// <summary>
    /// Total length of the whole route, in metres.
    /// </summary>
    public float TotalLength { get; private set; }

    /// <summary>
    /// A path needs at least 2 points to be a line.
    /// </summary>
    public bool IsValid => points != null && points.Length >= 2;

    private void Awake()
    {
        Rebuild();
    }

    /// <summary>
    /// Measures the path. Called automatically on Awake. Call it again yourself only if you
    /// move waypoints while the game is running (you normally won't).
    /// </summary>
    public void Rebuild()
    {
        if (!IsValid)
        {
            TotalLength = 0f;
            Debug.LogError($"[ScenarioPath] '{name}' needs at least 2 waypoints assigned.", this);
            return;
        }

        cumulative = new float[points.Length];
        cumulative[0] = 0f;

        for (int i = 1; i < points.Length; i++)
        {
            if (points[i] == null || points[i - 1] == null)
            {
                Debug.LogError($"[ScenarioPath] '{name}' has an empty waypoint slot at index {i}.", this);
                return;
            }

            // add this segment's length to the running total
            float segment = Vector3.Distance(points[i - 1].position, points[i].position);
            cumulative[i] = cumulative[i - 1] + segment;
        }

        TotalLength = cumulative[points.Length - 1];
    }

    /// <summary>
    /// THE MAIN QUESTION: "I have travelled `distance` metres along this path. Where am I,
    /// and which way am I facing?"
    /// </summary>
    public Vector3 Evaluate(float distance, out Vector3 forward)
    {
        forward = transform.forward;

        if (!IsValid) return transform.position;
        if (cumulative == null || cumulative.Length != points.Length) Rebuild();

        // never let it run off either end of the path
        distance = Mathf.Clamp(distance, 0f, TotalLength);

        // walk forward through the segments until we find the one this distance falls inside
        int seg = 1;
        while (seg < points.Length - 1 && distance > cumulative[seg]) seg++;

        Vector3 a = points[seg - 1].position;
        Vector3 b = points[seg].position;

        // how far through THIS segment are we, as a 0-to-1 fraction?
        float segmentLength = cumulative[seg] - cumulative[seg - 1];
        float t = segmentLength > 0.0001f
            ? (distance - cumulative[seg - 1]) / segmentLength
            : 0f;
        t = Mathf.Clamp01(t);

        Vector3 direction = b - a;
        if (direction.sqrMagnitude > 0.000001f) forward = direction.normalized;

        // Lerp = "blend between a and b by fraction t"
        return Vector3.Lerp(a, b, t);
    }

    /// <summary>
    /// Convenience version when you don't care which way you're facing.
    /// </summary>
    public Vector3 Evaluate(float distance)
    {
        return Evaluate(distance, out _);
    }

    /// <summary>
    /// THE OTHER MAIN QUESTION: "here is a point in the world (the ImpactMarker). How far
    /// along my path is the closest I ever get to it?"
    ///
    /// This is what lets actors work out their own start positions instead of you placing
    /// them by hand.
    /// </summary>
    public float GetDistanceAtClosestPoint(Vector3 worldPosition)
    {
        if (!IsValid) return 0f;
        if (cumulative == null || cumulative.Length != points.Length) Rebuild();

        float bestDistanceAlongPath = 0f;
        float bestSquaredGap = float.MaxValue;

        // check every segment, keep whichever gets closest to the target point
        for (int i = 1; i < points.Length; i++)
        {
            Vector3 a = points[i - 1].position;
            Vector3 b = points[i].position;
            Vector3 ab = b - a;

            float abSquared = ab.sqrMagnitude;
            if (abSquared < 0.000001f) continue; // two waypoints stacked on top of each other

            // project the target point onto this segment, clamped to the segment's ends.
            // Dot() here answers "how far along ab does worldPosition sit?"
            float t = Mathf.Clamp01(Vector3.Dot(worldPosition - a, ab) / abSquared);
            Vector3 closestOnSegment = a + ab * t;

            float squaredGap = (worldPosition - closestOnSegment).sqrMagnitude;
            if (squaredGap < bestSquaredGap)
            {
                bestSquaredGap = squaredGap;
                bestDistanceAlongPath = cumulative[i - 1] + Vector3.Distance(a, closestOnSegment);
            }
        }

        return bestDistanceAlongPath;
    }

    // ---------------------------------------------------------------------
    // Editor drawing. Lets you see the route in the Scene view without pressing Play.
    // ---------------------------------------------------------------------
    private void OnDrawGizmos()
    {
        if (points == null || points.Length < 2) return;

        for (int i = 1; i < points.Length; i++)
        {
            if (points[i] == null || points[i - 1] == null) continue;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(points[i - 1].position, points[i].position);

            // a small arrow head so you can tell which way the path runs
            Vector3 mid = Vector3.Lerp(points[i - 1].position, points[i].position, 0.5f);
            Vector3 dir = (points[i].position - points[i - 1].position).normalized;
            Vector3 side = Vector3.Cross(dir, Vector3.up) * 0.4f;
            Gizmos.DrawLine(mid, mid - dir * 1f + side);
            Gizmos.DrawLine(mid, mid - dir * 1f - side);
        }

        // green sphere = start of the path, red sphere = end
        Gizmos.color = Color.green;
        if (points[0] != null) Gizmos.DrawWireSphere(points[0].position, 0.5f);

        Gizmos.color = Color.red;
        Transform last = points[points.Length - 1];
        if (last != null) Gizmos.DrawWireSphere(last.position, 0.5f);
    }
}
