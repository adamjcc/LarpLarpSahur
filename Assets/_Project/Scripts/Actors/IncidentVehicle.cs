/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * IncidentVehicle.cs
 * State machine for the speeding car involved in the collision.
 */

using UnityEngine;

/// <summary>
/// FSM for the speeding car. DARRYL OWNS THIS ONE.
///
/// STATE DIAGRAM (draw this for the ReadMe):
///
///     Cruising ──(reaches react distance)──> Reacting
///                                               │
///                    ┌──────── all 4 fixed? ────┴────────┐
///                   yes                                  no
///                    ▼                                    ▼
///                 Braking ──> Stopped                  Unaware ──> Impact
///                    │                                              │
///                    └──────────────&gt; Aftermath &lt;──────────────────┘
///
/// THE SPEED TRICK, and it matters:
/// PlannedSpeedToImpact returns the BRAKED speed when the player has applied that
/// intervention. Because the base class back-solves the start position from that speed,
/// a braked car simply starts closer and still arrives at the marker at exactly impactTime.
/// So "slow the car down" changes how hard the crash is, never whether it happens.
/// Only fixing all four hazards avoids it.
/// </summary>
public class IncidentVehicle : PathScenarioActor
{
    public enum State
    {
        Cruising,   // approaching at speed
        Reacting,   // one-frame decision point
        Unaware,    // never saw her — carries on
        Braking,    // saw her in time — slowing to a stop
        Impact,     // hit her
        Stopped,    // stopped safely short
        Aftermath   // parked, for the free-roam investigation
    }

    [Header("Speed")]
    [Tooltip("Speeding. 13.9 m/s = 50 km/h.")]
    [SerializeField] private float normalSpeed = 13.9f;

    [Tooltip("Speed once the player has used the brake pedal. Still fast enough to reach " +
             "her — the crash is slower, not avoided.")]
    [SerializeField] private float brakedSpeed = 9.0f;

    [Tooltip("How fast it can change speed, in m/s per second.")]
    [SerializeField] private float acceleration = 4f;

    [Tooltip("Deceleration once it is actually stopping.")]
    [SerializeField] private float emergencyDeceleration = 12f;

    [Header("Hookups — leave empty for the grey-box test")]
    [SerializeField] private VehicleLights lights;
    [SerializeField] private DamageSwapper damage;

    [Tooltip("An empty GameObject at the front bumper. The ImpactDetector measures from here.")]
    [SerializeField] private Transform frontBumper;

    [Header("Read-only")]
    [SerializeField] private State state = State.Cruising;
    [SerializeField] private float currentSpeed;
    [SerializeField] private float timeInState;

    /// <summary>
    /// The speed the car will hold on the approach, given what the player has fixed.
    /// </summary>
    private float TargetCruiseSpeed =>
        HasIntervention(HazardId.CarSpeed) ? brakedSpeed : normalSpeed;

    // The base class uses this to work out where the car must start.
    protected override float PlannedSpeedToImpact => TargetCruiseSpeed;

    /// <summary>Finds the lights and damage swapper on the car if they weren't dragged in.</summary>
    protected override void Awake()
    {
        base.Awake();

        if (lights == null) lights = GetComponentInChildren<VehicleLights>();
        if (damage == null) damage = GetComponentInChildren<DamageSwapper>();
    }

    /// <summary>
    /// Where the ImpactDetector measures the gap from. Falls back to the object's own
    /// position if you haven't made a bumper marker yet.
    /// </summary>
    public Vector3 FrontPoint => frontBumper != null ? frontBumper.position : transform.position;

    public bool IsMoving => currentSpeed > 0.1f;
    public State CurrentState => state;
    public float CurrentSpeed => currentSpeed;

    /// <summary>
    /// Speed in km/h, for the dashboard speedometer in Part 6.
    /// </summary>
    public float SpeedKmh => currentSpeed * 3.6f;

    public override void Tick(float dt, float now)
    {
        if (!IsConfigured) return;

        timeInState += dt;

        switch (state)
        {
            // -------------------------------------------------------------
            case State.Cruising:
                // Ease toward the target speed. If the player hits the brake pedal
                // mid-Intervene, this is what makes the car visibly slow down.
                currentSpeed = Mathf.MoveTowards(currentSpeed, TargetCruiseSpeed, acceleration * dt);
                MoveAlongPath(currentSpeed, dt);

                if (DistanceToImpact <= settings.driverReactDistanceBeforeImpact)
                    GoTo(State.Reacting);
                break;

            // -------------------------------------------------------------
            // THE DECISION POINT.
            case State.Reacting:
                if (AllHazardsFixed)
                {
                    // Headlights on means he can see her; reduced speed means he has the
                    // distance to stop. He brakes properly.
                    GoTo(State.Braking);
                }
                else
                {
                    // Anything missing and he never reacts in time.
                    GoTo(State.Unaware);
                }
                break;

            // -------------------------------------------------------------
            case State.Unaware:
                currentSpeed = Mathf.MoveTowards(currentSpeed, TargetCruiseSpeed, acceleration * dt);
                MoveAlongPath(currentSpeed, dt);
                // ImpactDetector will call NotifyImpact() if it reaches her.
                break;

            // -------------------------------------------------------------
            case State.Braking:
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, emergencyDeceleration * dt);
                MoveAlongPath(currentSpeed, dt);

                if (currentSpeed <= 0.01f) GoTo(State.Stopped);
                break;

            // -------------------------------------------------------------
            case State.Impact:
                // Hard stop. Slightly softer than emergency braking so it reads as a
                // collision rather than a handbrake turn.
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, emergencyDeceleration * 0.7f * dt);
                MoveAlongPath(currentSpeed, dt);

                if (currentSpeed <= 0.01f && timeInState > 1.5f) GoTo(State.Aftermath);
                break;

            // -------------------------------------------------------------
            case State.Stopped:
                if (timeInState > 1.0f) GoTo(State.Aftermath);
                break;

            // -------------------------------------------------------------
            case State.Aftermath:
                break;
        }
    }

    /// <summary>
    /// Called by the ImpactDetector.
    /// </summary>
    public void NotifyImpact()
    {
        if (state == State.Impact || state == State.Aftermath) return;
        GoTo(State.Impact);
    }

    private void GoTo(State next)
    {
        state = next;
        timeInState = 0f;

        if (next == State.Impact && damage != null) damage.SetDamaged(true);
    }

    public override void ResetToStart()
    {
        base.ResetToStart();   // recalculates start distance using TargetCruiseSpeed

        state = State.Cruising;
        timeInState = 0f;

        // Start at full cruise speed. Not zero — the car is already moving when the
        // scenario opens, it doesn't pull away from a standstill.
        currentSpeed = TargetCruiseSpeed;

        // Apply the player's interventions from scenario time zero, so the Resolve replay
        // shows the headlights on for the whole run.
        if (lights != null)
        {
            lights.SetOn(HasIntervention(HazardId.CarHeadlights));
        }

        if (damage != null) damage.SetDamaged(false);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // red sphere at the bumper, so you can see what the ImpactDetector measures from
        if (frontBumper != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(frontBumper.position, 0.3f);
        }
    }
}
