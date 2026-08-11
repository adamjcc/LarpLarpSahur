/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * PedestrianVictim.cs
 * State machine for the student who is hit.
 */

using UnityEngine;

/// <summary>
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
/// </summary>
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

    [Header("Phone placement")]
    [Tooltip("Where the phone sits while she is walking and typing. Local to whatever the " +
             "phone is parented to, usually her hand bone.")]
    [SerializeField] private Vector3 phoneWalkingPosition = new Vector3(0.0608f, -0.0066f, 0.0705f);
    [SerializeField] private Vector3 phoneWalkingRotation = new Vector3(8.522f, -64.605f, 72.478f);

    [Tooltip("Where it sits while she is standing still. The idle animation holds her arm " +
             "differently, so a single position looks wrong in one pose or the other.")]
    [SerializeField] private Vector3 phoneIdlePosition = new Vector3(-0.06606f, 0.169283f, 0.1135409f);
    [SerializeField] private Vector3 phoneIdleRotation = new Vector3(27.208f, -102.513f, 99.772f);

    // remembers which pose we last pushed, so we only move it when it actually changes
    private bool lastPhoneWalking;

    [Header("Her POV camera")]
    [Tooltip("The PovLook on CAM_PedestrianPov. Its resting pitch follows her head: angled " +
             "down at the phone while she's reading, level once she puts it away.")]
    [SerializeField] private PovLook povLook;

    [Tooltip("While she's reading. Pitch is positive DOWN, yaw is positive RIGHT — so " +
             "32 / 15 means head down and turned slightly to her right.")]
    [SerializeField] private float povPitchOnPhone = 32f;
    [SerializeField] private float povYawOnPhone = 15f;

    [Tooltip("Once the phone is away. 0 / 0 is level and straight ahead.")]
    [SerializeField] private float povPitchLookingUp = 0f;
    [SerializeField] private float povYawLookingUp = 0f;

    // remembers the last pose we pushed, so we only touch the camera when it changes
    private bool lastPhoneStowed;

    [Header("Free Roam")]
    [Tooltip("Where she stands as a witness during the investigation. Leave empty and she " +
             "stays wherever the collision left her — which is usually the middle of the road.")]
    [SerializeField] private Transform freeRoamStandPoint;

    // True only during Free Roam. Suppresses the hit animation so she is found standing
    // and talking rather than face-down in the road.
    private bool witnessMode;

    [Header("Read-only")]
    [SerializeField] private State state = State.Walking;
    [SerializeField] private float timeInState;

    // She only travels at one speed, so the back-solve is simple.
    protected override float PlannedSpeedToImpact => walkSpeed;

    /// <summary>
    /// True only while she is actually exposed in the road. The ImpactDetector checks this
    /// so she can never be "hit" while standing safely on the pavement.
    /// </summary>
    public bool IsVulnerable => state == State.Crossing;

    /// <summary>
    /// Roughly chest height, which is what the car's bumper would strike.
    /// </summary>
    public Vector3 ChestPoint => transform.position + Vector3.up * 1.0f;

    public State CurrentState => state;

    public override void Tick(float dt, float now)
    {
        if (!IsConfigured) return;

        UpdatePovPitch();
        UpdatePhonePose();
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

    /// <summary>
    /// Free Roam wants a witness standing up and able to talk. Every other phase is a
    /// playback, where she should go down on impact and stay there.
    ///
    /// MUST be called BEFORE the director seeks. The seek re-runs the whole incident inside
    /// one frame, so by the time it finishes the hit animation has already been triggered.
    /// </summary>
    public void SetWitnessMode(bool witness)
    {
        witnessMode = witness;
    }

    /// <summary>
    /// Moves her to the spot she should be standing on during the investigation.
    ///
    /// MUST be called AFTER the seek, because the seek puts her back wherever the collision
    /// left her. Safe to leave applying itself: the clock is paused in Free Roam, so Tick
    /// never runs and nothing moves her back.
    /// </summary>
    public void ApplyWitnessPlacement()
    {
        if (!witnessMode || freeRoamStandPoint == null) return;

        transform.SetPositionAndRotation(freeRoamStandPoint.position, freeRoamStandPoint.rotation);
        SetAnimFloat("Speed", 0f);

        // She is standing here, not walking — and Tick won't run again while Free Roam
        // has the clock paused, so this is the only chance to get the phone right.
        UpdatePhonePose(force: true);
    }

    /// <summary>
    /// Called by the ImpactDetector the moment the car reaches her.
    /// </summary>
    public void NotifyStruck()
    {
        if (state == State.Struck || state == State.Aftermath) return;
        GoTo(State.Struck);
    }

    /// <summary>
    /// Change state, reset the state timer, and run the "on entering" behaviour.
    /// </summary>
    private void GoTo(State next)
    {
        state = next;
        timeInState = 0f;

        switch (next)
        {
            // These use the safe setters from ScenarioActor, which quietly skip any
            // parameter the current Animator Controller doesn't have. That keeps the
            // Console clean while the characters are still on the stock Hodaart
            // controller, and starts working by itself once the real one is built.
            case State.Distracted:
                SetPhoneVisible(true);
                SetAnimBool("OnPhone", true);
                break;

            case State.Alerted:
                SetPhoneVisible(false);
                SetAnimBool("OnPhone", false);
                SetAnimFloat("Speed", 0f);
                break;

            case State.Struck:
                // Skipped in witness mode. Free Roam re-simulates straight through the
                // collision to reach the aftermath, so without this she would replay the
                // fall every single time you cut back to the investigation.
                if (!witnessMode) SetAnimTrigger("Hit");
                break;

            case State.Safe:
            case State.Aftermath:
                SetAnimFloat("Speed", 0f);
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

        // force, because a reset must re-apply these even if the flags haven't changed
        UpdatePovPitch(force: true);
        UpdatePhonePose(force: true);

        // Rebind wipes the Animator back to its default state and clears every parameter.
        // Without this the second replay inherits the first replay's pose. Almost nobody
        // knows about this method and it causes hours of confusion.
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    /// <summary>
    /// Points her POV camera down at the phone, or level once she's put it away.
    /// Only touches the camera when the state actually changes.
    /// </summary>
    private void UpdatePovPitch(bool force = false)
    {
        if (povLook == null) return;

        bool phoneStowed = HasIntervention(HazardId.PedestrianPhone);
        if (!force && phoneStowed == lastPhoneStowed) return;

        lastPhoneStowed = phoneStowed;

        povLook.SetBaseRotation(
            phoneStowed ? povPitchLookingUp : povPitchOnPhone,
            phoneStowed ? povYawLookingUp   : povYawOnPhone);
    }

    /// <summary>
    /// True in the states where she is actually moving along the path.
    /// </summary>
    private bool IsWalking =>
        state == State.Walking || state == State.Distracted || state == State.Crossing;

    /// <summary>
    /// Moves the phone between its walking pose and its standing pose.
    ///
    /// The texting animation and the idle animation hold her arm in completely different
    /// places, so one fixed offset can only ever look right in one of them. Only touches
    /// the transform when the pose actually changes.
    /// </summary>
    private void UpdatePhonePose(bool force = false)
    {
        if (phoneObject == null) return;

        bool walking = IsWalking;
        if (!force && walking == lastPhoneWalking) return;

        lastPhoneWalking = walking;

        Transform t = phoneObject.transform;
        t.localPosition = walking ? phoneWalkingPosition : phoneIdlePosition;
        t.localRotation = Quaternion.Euler(walking ? phoneWalkingRotation : phoneIdleRotation);
        // scale is left alone — set it once on the prefab
    }

    private void SetPhoneVisible(bool visible)
    {
        // Never show the phone if the player has stowed it, whatever the state wants
        if (HasIntervention(HazardId.PedestrianPhone)) visible = false;
        if (phoneObject != null) phoneObject.SetActive(visible);
    }
}
