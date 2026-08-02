using UnityEngine;

/// Decides when the car has reached the pedestrian.
///
/// WHY THIS IS NOT A TRIGGER COLLIDER
/// The obvious way is OnTriggerEnter on the bumper. It does not work here, because
/// ScenarioRunner.SeekTo() runs up to 600 simulation steps inside a SINGLE frame, and
/// Unity only runs physics on its own separate schedule. Physics would never get a look in,
/// so no trigger would ever fire during a seek — and you'd jump to the free-roam aftermath
/// to find her standing calmly inside the car.
///
/// A plain distance check runs wherever we tell it to, so it behaves identically during a
/// seek, during slow motion, and at normal speed. That is worth more here than using the
/// physics engine for its own sake.
///
/// TickOrder = 100 so this runs AFTER the car and the pedestrian have both moved this step.
public class ImpactDetector : ScenarioActor
{
    [SerializeField] private PedestrianVictim pedestrian;
    [SerializeField] private IncidentVehicle vehicle;

    [Tooltip("How close the bumper must get to her chest to count as contact. " +
             "Roughly half the car's width plus half her body.")]
    [SerializeField] private float impactRadius = 1.1f;

    [SerializeField] private bool logToConsole = true;

    [Header("Read-only")]
    [SerializeField] private bool hasImpacted;
    [SerializeField] private float impactedAtTime = -1f;

    /// Scenario time the collision happened, or -1 if it didn't. Read by the debrief.
    public float ImpactedAtTime => impactedAtTime;
    public bool HasImpacted => hasImpacted;

    // run last, after everything else has moved
    public override int TickOrder => 100;

    protected override void Awake()
    {
        base.Awake();
        if (pedestrian == null) pedestrian = FindFirstObjectByType<PedestrianVictim>();
        if (vehicle == null) vehicle = FindFirstObjectByType<IncidentVehicle>();
    }

    public override void Tick(float dt, float now)
    {
        if (hasImpacted) return;
        if (pedestrian == null || vehicle == null) return;

        // She can only be hit while actually out in the road
        if (!pedestrian.IsVulnerable) return;
        if (!vehicle.IsMoving) return;

        float gap = Vector3.Distance(vehicle.FrontPoint, pedestrian.ChestPoint);
        if (gap > impactRadius) return;

        hasImpacted = true;
        impactedAtTime = now;

        pedestrian.NotifyStruck();
        vehicle.NotifyImpact();

        if (logToConsole)
        {
            Debug.Log($"<color=red><b>IMPACT</b></color> at scenario time " +
                      $"<b>{now:F2}s</b>   (car speed {vehicle.SpeedKmh:F0} km/h)");
        }
    }

    public override void ResetToStart()
    {
        hasImpacted = false;
        impactedAtTime = -1f;
        // deliberately does not call base — this object never moves
    }

    private void OnDrawGizmos()
    {
        if (vehicle == null || pedestrian == null) return;

        Gizmos.color = hasImpacted ? Color.red : Color.green;
        Gizmos.DrawWireSphere(vehicle.FrontPoint, impactRadius);
        Gizmos.DrawLine(vehicle.FrontPoint, pedestrian.ChestPoint);
    }
}
