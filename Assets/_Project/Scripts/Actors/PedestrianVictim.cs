using UnityEngine;

/// FSM for the student who gets hit. ADAM OWNS THIS ONE.
///
/// STATE DIAGRAM (draw this for the ReadMe):
///
///     Walking ──(phone still out)──> Distracted
///        │                               │
///        └──────────┬────────────────────┘
///                   ▼
///                 AtKerb ──(all 4 hazards fixed?)──┬── yes ──> Alerted ──> Safe
///                                                  └── no  ──> Crossing ──┬──> Struck
///                                                                         └──> Safe
///                                                                              │
///                                                          everything ──> Aftermath
///
/// Two things to notice, because they are the whole design:
///   1. She reaches AtKerb based on DISTANCE, not a hard-coded time. Move the waypoints and
///      the logic still works.
///   2. The branch at AtKerb reads InterventionState. That single "if" is the game mechanic.
public class PedestrianVictim : PathScenarioActor
{
    public enum State
    {
        Walking,     // approaching, head up
        Distracted,  // approaching, head down on the phone
        AtKerb,      // one-frame decision point at the edge of the road
        Crossing,    // stepped into the road
        Alerted,     // noticed the car and stopped at the kerb — the good ending
        Struck,      // hit
        Safe,        // made it across, or stopped in time
        Aftermath    // standing as a witness for the free-roam investigation
    }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.2f;

    [Tooltip("Scenario time she takes her phone out. Ignored if the player stows it.")]
    [SerializeField] private float looksAtPhoneTime = 1.0f;

    [Tooltip("How far PAST the impact point counts as safely across.")]
    [SerializeField] private float clearedRoadDistance = 4.0f;

    [Header("Visuals — leave empty for the grey-box test")]
    [SerializeField] private GameObject phoneObject;
    [SerializeField] private GameObject headphonesObject;

    [Header("Read-only")]
    [SerializeField] private State state = State.Walking;
    [SerializeField] private float timeInState;

    // She only travels at one speed, so the back-solve is simple.
    protected override float PlannedSpeedToImpact => walkSpeed;

    /// True only while she is actually exposed in the road. The ImpactDetector checks this
    /// so she can never be "hit" while standing safely on the pavement.
    public bool IsVulnerable => state == State.Crossing;

    /// Roughly chest height, which is what the car's bumper would strike.
    public Vector3 ChestPoint => transform.position + Vector3.up * 1.0f;

    public State CurrentState => state;

    public override void Tick(float dt, float now)
    {
        if (!IsConfigured) return;

        timeInState += dt;

        switch (state)
        {
            // -------------------------------------------------------------
            case State.Walking:
                MoveAlongPath(walkSpeed, dt);

                // Only gets distracted if the player hasn't already stowed her phone
                if (now >= looksAtPhoneTime && !HasIntervention(HazardId.PedestrianPhone))
                    GoTo(State.Distracted);

                if (DistanceToImpact <= settings.kerbDistanceBeforeImpact)
                    GoTo(State.AtKerb);
                break;

            // -------------------------------------------------------------
            case State.Distracted:
                MoveAlongPath(walkSpeed, dt);

                if (DistanceToImpact <= settings.kerbDistanceBeforeImpact)
                    GoTo(State.AtKerb);
                break;

            // -------------------------------------------------------------
            // THE DECISION POINT. This is where the player's work pays off.
            case State.AtKerb:
                if (AllHazardsFixed)
                {
                    // Phone away AND headphones off means she looks up and hears the car.
                    // Headlights on means she can actually see it. She stops.
                    GoTo(State.Alerted);
                }
                else
                {
                    // Any missing hazard and she walks into the road exactly as before.
                    GoTo(State.Crossing);
                }
                break;

            // -------------------------------------------------------------
            case State.Crossing:
                MoveAlongPath(walkSpeed, dt);

                // The ImpactDetector may call NotifyStruck() during this state.
                // If it doesn't, she simply reaches the far side.
                if (DistanceToImpact <= -clearedRoadDistance)
                    GoTo(State.Safe);
                break;

            // -------------------------------------------------------------
            case State.Alerted:
                // Stopped dead at the kerb. Turn to watch the car go past.
                if (timeInState > 2.0f) GoTo(State.Safe);
                break;

            // -------------------------------------------------------------
            case State.Struck:
                if (timeInState > 2.0f) GoTo(State.Aftermath);
                break;

            // -------------------------------------------------------------
            case State.Safe:
                if (timeInState > 1.0f) GoTo(State.Aftermath);
                break;

            // -------------------------------------------------------------
            case State.Aftermath:
                // Stands still. In Free Roam a separate WitnessLook script (Part 5)
                // turns her to face the player.
                break;
        }
    }

    /// Called by the ImpactDetector the moment the car reaches her.
    public void NotifyStruck()
    {
        if (state == State.Struck || state == State.Aftermath) return;
        GoTo(State.Struck);
    }

    /// Change state, reset the state timer, and run the "on entering" behaviour.
    private void GoTo(State next)
    {
        state = next;
        timeInState = 0f;

        switch (next)
        {
            case State.Distracted:
                SetPhoneVisible(true);
                if (animator != null) animator.SetBool("OnPhone", true);
                break;

            case State.Alerted:
                SetPhoneVisible(false);
                if (animator != null)
                {
                    animator.SetBool("OnPhone", false);
                    animator.SetFloat("Speed", 0f);
                }
                break;

            case State.Struck:
                if (animator != null) animator.SetTrigger("Hit");
                break;

            case State.Safe:
            case State.Aftermath:
                if (animator != null) animator.SetFloat("Speed", 0f);
                break;
        }
    }

    public override void ResetToStart()
    {
        base.ResetToStart();   // recalculates start distance and repositions

        state = State.Walking;
        timeInState = 0f;

        // Apply the player's interventions from scenario time zero.
        // This is what makes the Resolve replay show "she never had her phone out".
        bool phoneStowed = HasIntervention(HazardId.PedestrianPhone);
        bool headphonesOff = HasIntervention(HazardId.PedestrianHeadphones);

        SetPhoneVisible(!phoneStowed);
        if (headphonesObject != null) headphonesObject.SetActive(!headphonesOff);

        // Rebind wipes the Animator back to its default state and clears every parameter.
        // Without this the second replay inherits the first replay's pose. Almost nobody
        // knows about this method and it causes hours of confusion.
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void SetPhoneVisible(bool visible)
    {
        // Never show the phone if the player has stowed it, whatever the state wants
        if (HasIntervention(HazardId.PedestrianPhone)) visible = false;
        if (phoneObject != null) phoneObject.SetActive(visible);
    }
}
