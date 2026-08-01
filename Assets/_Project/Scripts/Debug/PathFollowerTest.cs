using UnityEngine;

/// A throwaway actor for Part 1, so you can prove the clock works before any real content
/// exists. Put it on a Cube. It walks a ScenarioPath at a constant speed.
///
/// It also demonstrates the auto-start-position trick that the real car and pedestrian will
/// use: instead of you placing the object by hand, it works out where it must START in order
/// to ARRIVE at the ImpactMarker at exactly impactTime seconds.
///
/// Delete this script once Part 2 is done.
public class PathFollowerTest : ScenarioActor
{
    [Header("Route")]
    [SerializeField] private ScenarioPath path;
    [SerializeField] private float speed = 5f;

    [Header("Where do I start?")]
    [Tooltip("Leave EMPTY to just start at the beginning of the path (or at Manual Start Distance).\n\n" +
             "Drag in the ImpactMarker to have this object work out its own start position, so " +
             "that it arrives at the marker at exactly Impact Time.")]
    [SerializeField] private Transform impactMarker;

    [Tooltip("Scenario time at which I should be standing on the ImpactMarker.")]
    [SerializeField] private float impactTime = 7f;

    [Tooltip("Used only when no ImpactMarker is assigned.")]
    [SerializeField] private float manualStartDistance = 0f;

    [Header("Read-only (filled in at runtime)")]
    [SerializeField] private float distanceTravelled;
    [SerializeField] private float startDistance;

    public override void Tick(float dt, float now)
    {
        // Distance from time. Because dt is always exactly the same fixed number, this
        // adds up to precisely the same total on every single replay.
        distanceTravelled += speed * dt;
        ApplyToTransform();
    }

    public override void ResetToStart()
    {
        startDistance = CalculateStartDistance();
        distanceTravelled = startDistance;
        ApplyToTransform();
    }

    /// Works out how far along the path this object must begin.
    private float CalculateStartDistance()
    {
        if (path == null || !path.IsValid) return 0f;

        // No marker assigned? Just use whatever the designer typed in.
        if (impactMarker == null) return manualStartDistance;

        // How far along my path does the impact point sit?
        float distanceToImpactPoint = path.GetDistanceAtClosestPoint(impactMarker.position);

        // How far do I travel between t=0 and impact?
        float distanceCoveredBeforeImpact = speed * impactTime;

        // So I must begin this far along the path.
        float result = distanceToImpactPoint - distanceCoveredBeforeImpact;

        // Negative means the path does not have enough road before the impact point.
        if (result < 0f)
        {
            Debug.LogWarning(
                $"[{name}] '{path.name}' is {(-result):F1} m too short before the ImpactMarker.\n" +
                $"   Needed: {distanceCoveredBeforeImpact:F1} m of path before the marker " +
                $"(speed {speed} m/s x impactTime {impactTime} s)\n" +
                $"   Available: {distanceToImpactPoint:F1} m\n" +
                $"   FIX: extend the path backwards, lower the speed, or lower impactTime.", this);

            return 0f;
        }

        return result;
    }

    /// Ask the path where we are, and put the object there.
    private void ApplyToTransform()
    {
        if (path == null || !path.IsValid) return;

        Vector3 position = path.Evaluate(distanceTravelled, out Vector3 forward);
        transform.position = position;

        // LookRotation blows up if handed a zero-length direction, so check first
        if (forward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(forward);
        }
    }
}
