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

    [Header("Steering — leave both at 0 for instant snapping")]
    [Tooltip("Metres to look ahead along the path when deciding which way to face.\n\n" +
             "0 = face the current segment exactly, which makes a vehicle pivot on the spot " +
             "at a corner. Try 4 on the car so it starts turning INTO the bend.\n" +
             "Leave at 0 for people — they walk in straight lines.")]
    [SerializeField] private float lookAheadDistance = 0f;

    [Tooltip("Degrees per second the object may rotate. 0 = snap instantly.\n" +
             "Try 90 on the car. Purely cosmetic — it never affects position, so the " +
             "collision timing is untouched.")]
    [SerializeField] private float maxTurnRate = 0f;

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

        // snap on reset — easing from wherever we happened to be facing would leave the
        // car pointing the wrong way at the start of every replay
        ApplyToTransform(0f, snapRotation: true);
    }

    /// Move forward along the path by speed x dt, then place the object there.
    protected void MoveAlongPath(float speed, float dt)
    {
        distanceTravelled += speed * dt;
        ApplyToTransform(dt, snapRotation: false);

        // Feeds the walk/idle blend tree once a real Animator Controller exists.
        // Safely ignored until then.
        SetAnimFloat("Speed", speed);
    }

    /// Ask the path where we are now and put the transform there.
    protected void ApplyToTransform(float dt, bool snapRotation)
    {
        if (path == null || !path.IsValid) return;

        Vector3 position = path.Evaluate(distanceTravelled, out Vector3 forward);
        transform.position = position;

        // Look a few metres up the path instead of at the current segment, so a vehicle
        // begins turning BEFORE it reaches the corner rather than pivoting on the spot.
        if (lookAheadDistance > 0.01f)
        {
            Vector3 ahead = path.Evaluate(distanceTravelled + lookAheadDistance);
            Vector3 toAhead = ahead - position;
            if (toAhead.sqrMagnitude > 0.0001f) forward = toAhead.normalized;
        }

        // LookRotation throws if handed a zero-length direction
        if (forward.sqrMagnitude <= 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(forward);

        if (snapRotation || maxTurnRate <= 0f)
        {
            transform.rotation = target;
        }
        else
        {
            // dt is the fixed simulation step, so this stays perfectly repeatable
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, maxTurnRate * dt);
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
