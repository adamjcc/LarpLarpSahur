using UnityEngine;

/// Shared behaviour for the two crash participants: follow a path, and work out your own
/// starting position so that you arrive at the ImpactMarker at exactly impactTime.
///
/// PedestrianVictim and IncidentVehicle both inherit from this, so the awkward maths only
/// exists once. They only have to supply their state machine and their speed.
public abstract class PathScenarioActor : ScenarioActor
{
    [Header("Route")]
    [SerializeField] protected ScenarioPath path;

    [Tooltip("Leave empty — it finds the one on SYSTEMS automatically.")]
    [SerializeField] protected ScenarioSettings settings;

    [Tooltip("Leave empty — it finds the one on SYSTEMS automatically.")]
    [SerializeField] protected InterventionState interventions;

    [Header("Read-only (fills in while playing)")]
    [SerializeField] protected float distanceTravelled;
    [SerializeField] protected float startDistance;
    [SerializeField] protected float impactDistanceOnPath;

    /// The speed this actor expects to travel at between t=0 and impact.
    ///
    /// The vehicle overrides this to return its BRAKED speed when the player has applied
    /// the brake intervention. That is what keeps the arrival time locked: a slower car
    /// simply starts closer, so it still reaches the marker at impactTime. Braking changes
    /// how hard the crash is, never when it happens.
    protected abstract float PlannedSpeedToImpact { get; }

    /// Metres still to travel before reaching the impact point. Goes negative once past it.
    /// State machines use this instead of hard-coded times, so the logic survives you
    /// moving waypoints around.
    public float DistanceToImpact => impactDistanceOnPath - distanceTravelled;

    /// True when everything this actor needs has actually been assigned. Every Tick starts
    /// by checking this, so a forgotten drag gives you one clear error in Awake instead of
    /// a NullReferenceException sixty times a second.
    protected bool IsConfigured => path != null && path.IsValid && settings != null;

    /// Null-safe shortcuts, so the state machines stay readable.
    protected bool HasIntervention(HazardId id) => interventions != null && interventions.Has(id);
    protected bool AllHazardsFixed => interventions != null && interventions.AllRequiredFixed;

    protected override void Awake()
    {
        base.Awake();

        // Auto-find, so a forgotten drag doesn't cost you twenty minutes
        if (settings == null) settings = FindFirstObjectByType<ScenarioSettings>();
        if (interventions == null) interventions = FindFirstObjectByType<InterventionState>();

        if (settings == null)
            Debug.LogError($"[{name}] No ScenarioSettings in the scene. Add one to SYSTEMS.", this);
        if (path == null)
            Debug.LogError($"[{name}] No ScenarioPath assigned.", this);
    }

    public override void ResetToStart()
    {
        RecalculateStartDistance();
        distanceTravelled = startDistance;
        ApplyToTransform();
    }

    /// Move forward along the path by speed x dt, then place the object there.
    protected void MoveAlongPath(float speed, float dt)
    {
        distanceTravelled += speed * dt;
        ApplyToTransform();

        // Feeds the walk/idle blend tree once a real Animator Controller exists.
        // Safely ignored until then.
        SetAnimFloat("Speed", speed);
    }

    /// Ask the path where we are now and put the transform there.
    protected void ApplyToTransform()
    {
        if (path == null || !path.IsValid) return;

        Vector3 position = path.Evaluate(distanceTravelled, out Vector3 forward);
        transform.position = position;

        // LookRotation throws if handed a zero-length direction
        if (forward.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    /// THE AUTO-POSITIONING. Works out how far along the path this actor must begin,
    /// so it lands on the marker at exactly impactTime.
    ///
    ///     start  =  (distance along path to the marker)  -  (speed x impactTime)
    private void RecalculateStartDistance()
    {
        if (path == null || !path.IsValid || settings == null || settings.impactMarker == null)
        {
            startDistance = 0f;
            return;
        }

        impactDistanceOnPath = path.GetDistanceAtClosestPoint(settings.impactMarker.position);

        float distanceCoveredBeforeImpact = PlannedSpeedToImpact * settings.impactTime;
        float result = impactDistanceOnPath - distanceCoveredBeforeImpact;

        if (result < 0f)
        {
            Debug.LogWarning(
                $"[{name}] '{path.name}' is {(-result):F1} m too short before the ImpactMarker.\n" +
                $"   Needs {distanceCoveredBeforeImpact:F1} m of path before the marker " +
                $"({PlannedSpeedToImpact:F1} m/s x {settings.impactTime:F1} s)\n" +
                $"   Has only {impactDistanceOnPath:F1} m.\n" +
                $"   FIX: extend the path backwards, lower the speed, or lower Impact Time.", this);

            startDistance = 0f;
            return;
        }

        startDistance = result;
    }

    // Draws a yellow line from this actor to the impact point, so you can see the
    // geometry in the Scene view while tuning.
    protected virtual void OnDrawGizmosSelected()
    {
        if (settings == null || settings.impactMarker == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, settings.impactMarker.position);
        Gizmos.DrawWireSphere(settings.impactMarker.position, 0.5f);
    }
}
