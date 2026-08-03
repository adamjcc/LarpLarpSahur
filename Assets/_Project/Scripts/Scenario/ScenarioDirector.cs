using UnityEngine;

/// The phases of the game.
public enum GamePhase
{
    Boot,           // nothing yet
    Observe,        // bird's-eye, the crash plays at full speed
    FreeRoam,       // clock frozen after the crash, player walks the aftermath
    PovReplay,      // sub-mode of FreeRoam: locked replay through someone's eyes
    PassengerSeat,  // sub-mode of FreeRoam: sitting beside the driver
    Intervene,      // rewound to just before impact, slow motion, player fixes hazards
    Resolve,        // the "what if" replay at full speed with the fixes applied
    Debrief         // score and explanation
}

/// THE SPINE OF THE WHOLE GAME.
///
/// Everything funnels through one method, EnterPhase(), which sets all six of these on
/// every single change:
///     1. which camera is live
///     2. whether the player can walk and look
///     3. cursor locked or free
///     4. the scenario clock's speed
///     5. whether the clock is running
///     6. where the clock is positioned
///
/// The reason it is one method and not six scattered ones is that it is the only way to
/// avoid the classic bug where you end up with an invisible cursor, a frozen player, and
/// no idea which of six scripts turned what off.
public class ScenarioDirector : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private ScenarioRunner runner;
    [SerializeField] private ScenarioSettings settings;
    [SerializeField] private InterventionState interventions;
    [SerializeField] private CameraDirector cameras;
    [SerializeField] private PlayerRig player;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private ImpactDetector impactDetector;

    [Header("Player positioning")]
    [Tooltip("Empty GameObject on the pavement where the player starts investigating.")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Options")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private bool debugKeys = true;

    [Header("Read-only")]
    [SerializeField] private GamePhase phase = GamePhase.Boot;

    // which phase a POV replay should return to when it finishes
    private GamePhase returnPhase = GamePhase.FreeRoam;

    // which POV a replay is currently showing
    private CameraId activeReplayCamera = CameraId.PedestrianPov;

    public GamePhase Phase => phase;

    /// True whenever the player is allowed to walk around.
    public bool IsPlayerActive =>
        (phase == GamePhase.FreeRoam || phase == GamePhase.Intervene) && !npcViewActive;

    /// True while hazards can actually be changed, as opposed to merely examined.
    public bool IsInterventionActive => phase == GamePhase.Intervene;

    /// True whenever the player is seeing the world through someone else's eyes —
    /// the passenger seat, a POV replay, or standing inside someone during Intervene.
    ///
    /// This is what decides where each hazard can be reached from. Her headphones are
    /// FromOutsideOnly, so they need this to be FALSE. Her phone and every dashboard
    /// control are FromPovOnly, so they need it TRUE.
    public bool IsInNpcView => npcViewActive || phase == GamePhase.PassengerSeat;

    /// Whether the raycast interaction system should run at all right now.
    public bool CanInteract =>
        phase == GamePhase.FreeRoam ||
        phase == GamePhase.PassengerSeat ||
        phase == GamePhase.Intervene;

    // Set while standing inside someone's view during Intervene. Not a phase of its own,
    // because the clock and the countdown must keep running exactly as before.
    private bool npcViewActive;

    /// Scenario seconds left before the collision. Drives the countdown on screen.
    ///
    /// Uses the MEASURED impact time, not the authored one. The authored impactTime (7.0) is
    /// when the two centre points meet, but contact happens a little earlier because the
    /// bumper sticks out in front — around 6.78. Counting down to 7.0 left the countdown
    /// reading 0.22 at the moment of the crash and then running on for another few real
    /// seconds. Now it hits 0.00 exactly as they collide.
    public float TimeToImpact
    {
        get
        {
            if (settings == null || runner == null) return 0f;
            float target = predictedImpactTime > 0f ? predictedImpactTime : settings.impactTime;
            return target - runner.ScenarioTime;
        }
    }

    // Worked out by a silent dry-run when Intervene begins. -1 until then.
    private float predictedImpactTime = -1f;

    private void Awake()
    {
        if (runner == null) runner = FindFirstObjectByType<ScenarioRunner>();
        if (settings == null) settings = FindFirstObjectByType<ScenarioSettings>();
        if (interventions == null) interventions = FindFirstObjectByType<InterventionState>();
        if (cameras == null) cameras = FindFirstObjectByType<CameraDirector>();
        if (player == null) player = FindFirstObjectByType<PlayerRig>();
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        if (impactDetector == null) impactDetector = FindFirstObjectByType<ImpactDetector>();
    }

    private void Start()
    {
        if (startAutomatically) EnterPhase(GamePhase.Observe);
    }

    // =====================================================================
    //  THE ONE METHOD THAT CHANGES PHASE
    // =====================================================================
    public void EnterPhase(GamePhase next)
    {
        phase = next;

        // Any phase change drops you out of someone's eyes, and clears whatever
        // highlight was stuck on when the camera cut away.
        npcViewActive = false;
        if (interactor != null) interactor.ClearFocus();

        Debug.Log($"<color=cyan>[PHASE]</color> -> <b>{next}</b>");

        switch (next)
        {
            // -------------------------------------------------------------
            case GamePhase.Observe:
                runner.ResetScenario();
                runner.SetTimeScale(1f);
                cameras.Activate(CameraId.BirdsEye);
                SetPlayerActive(false);
                runner.Play();
                break;

            // -------------------------------------------------------------
            case GamePhase.FreeRoam:
                // The frozen aftermath IS just the scenario paused a couple of seconds
                // after impact. Same objects, different clock value. No separate setup.
                runner.SetTimeScale(1f);
                runner.SeekTo(settings.FreeRoamTime);
                runner.Pause();

                cameras.Activate(CameraId.PlayerFirstPerson);
                if (playerSpawnPoint != null) player.Teleport(playerSpawnPoint);
                SetPlayerActive(true);
                break;

            // -------------------------------------------------------------
            case GamePhase.PovReplay:
                runner.SetTimeScale(1f);
                runner.SeekTo(settings.PovReplayStartTime);

                // allowLook FALSE — the camera is welded to where she was actually looking.
                // You cannot turn your head to spot the car. That is the whole shot.
                cameras.Activate(activeReplayCamera, allowLook: false);
                SetPlayerActive(false);
                player.SetCursorLocked(true);
                runner.Play();
                break;

            // -------------------------------------------------------------
            case GamePhase.PassengerSeat:
                runner.Pause();
                cameras.Activate(CameraId.PassengerSeat, allowLook: true);
                SetPlayerActive(false);

                // Cursor LOCKED, so you look around with the mouse and aim with the
                // crosshair — exactly like walking. A free cursor here meant the mouse
                // was trying to do two jobs at once.
                player.SetCursorLocked(true);
                break;

            // -------------------------------------------------------------
            case GamePhase.Intervene:
                // Find out when the crash will actually land, so the countdown is honest.
                predictedImpactTime = PredictImpactTime();

                // THE FAKE REWIND, plus slow motion. Note the player is NOT slowed —
                // runner.TimeScale only affects the crash actors.
                runner.SeekTo(settings.InterveneStartTime);
                runner.SetTimeScale(settings.interveneTimeScale);

                cameras.Activate(CameraId.PlayerFirstPerson);
                SetPlayerActive(true);
                runner.Play();
                break;

            // -------------------------------------------------------------
            case GamePhase.Resolve:
                // Replay the same seconds at full speed with the player's fixes applied
                // from the very start. InterventionState survives ResetScenario, which is
                // exactly why this works.
                runner.SetTimeScale(1f);
                runner.SeekTo(settings.InterveneStartTime);

                cameras.Activate(CameraId.Resolve);
                SetPlayerActive(false);
                runner.Play();
                break;

            // -------------------------------------------------------------
            case GamePhase.Debrief:
                runner.Pause();
                SetPlayerActive(false);
                player.SetCursorLocked(false);   // so the Retry button is clickable
                break;
        }
    }

    /// Runs the crash silently, in one frame, to find out exactly when contact happens.
    ///
    /// Needed because contact lands a fraction BEFORE the authored impactTime — the bumper
    /// sticks out ahead of the car's centre — and later still if the player has applied the
    /// brakes. Guessing left the countdown out of step with what you could see happening.
    private float PredictImpactTime()
    {
        if (impactDetector == null || runner == null || settings == null)
            return settings != null ? settings.impactTime : -1f;

        impactDetector.Silent = true;
        float measured = runner.SimulateUntil(() => impactDetector.HasImpacted, settings.simEndTime);
        impactDetector.Silent = false;

        if (measured < 0f)
        {
            // No collision at all — the player has already fixed enough that it never happens.
            Debug.Log("<color=cyan>[PREDICT]</color> no collision on the current settings.");
            return settings.impactTime;
        }

        Debug.Log($"<color=cyan>[PREDICT]</color> collision will land at " +
                  $"<b>{measured:F2}s</b> — countdown set to match.");
        return measured;
    }

    /// One place that decides everything about player control, so no phase can
    /// half-enable it.
    private void SetPlayerActive(bool active)
    {
        if (player == null) return;
        player.SetControlEnabled(active);
    }

    // =====================================================================
    //  AUTOMATIC TRANSITIONS
    // =====================================================================
    private void Update()
    {
        switch (phase)
        {
            case GamePhase.Observe:
                // Stop once the aftermath has settled and wait for the player to press Next
                if (runner.IsPlaying && runner.ScenarioTime >= settings.FreeRoamTime)
                {
                    runner.Pause();
                }
                break;

            case GamePhase.PovReplay:
                if (runner.IsPlaying && runner.ScenarioTime >= settings.PovReplayEndTime)
                {
                    EnterPhase(returnPhase);
                }
                break;

            case GamePhase.Intervene:
                // Three ways out, checked in this order:
                if (interventions != null && interventions.AllRequiredFixed)
                {
                    // 1. Fixed everything — cut to the payoff immediately.
                    EnterPhase(GamePhase.Resolve);
                }
                else if (impactDetector != null && impactDetector.HasImpacted)
                {
                    // 2. The crash actually happened. Cutting on THIS rather than on a
                    //    predicted clock value is what removes the dead couple of seconds
                    //    you were seeing between the collision and the replay.
                    EnterPhase(GamePhase.Resolve);
                }
                else if (runner.ScenarioTime >= settings.simEndTime)
                {
                    // 3. Safety net, in case braking delayed the car past the marker.
                    EnterPhase(GamePhase.Resolve);
                }
                break;

            case GamePhase.Resolve:
                if (runner.IsPlaying && runner.ScenarioTime >= settings.simEndTime)
                {
                    runner.Pause();
                    EnterPhase(GamePhase.Debrief);
                }
                break;
        }

        if (debugKeys) HandleDebugKeys();
    }

    // =====================================================================
    //  PUBLIC ACTIONS — the UI buttons will call these in Part 5
    // =====================================================================

    /// "Replay" on the Observe screen.
    public void ReplayObservation()
    {
        runner.ResetScenario();
        cameras.Activate(CameraId.BirdsEye);
        runner.Play();
    }

    /// "Next" on the Observe screen.
    public void FinishObserving() => EnterPhase(GamePhase.FreeRoam);

    /// Called from dialogue: "See what she saw" / "See what he saw".
    public void PlayPovReplay(CameraId povCamera)
    {
        activeReplayCamera = povCamera;
        returnPhase = (phase == GamePhase.PassengerSeat) ? GamePhase.PassengerSeat : GamePhase.FreeRoam;
        EnterPhase(GamePhase.PovReplay);
    }

    public void EnterPassengerSeat() => EnterPhase(GamePhase.PassengerSeat);

    public void LeavePassengerSeat() => EnterPhase(GamePhase.FreeRoam);

    /// Step INTO someone's view during Intervene.
    ///
    /// Note what this does NOT do: it doesn't touch the clock or the time scale. The
    /// countdown keeps running at 0.06x the whole time you're in there, which is what you
    /// asked for — at that speed her phone is drifting at about 7 cm a second, so it is
    /// still perfectly easy to click.
    public void EnterNpcView(CameraId povCamera)
    {
        if (phase != GamePhase.Intervene) return;

        npcViewActive = true;

        // allowLook TRUE — you need to look DOWN at her phone, or down at the pedals.
        cameras.Activate(povCamera, allowLook: true);

        // Body stays where it was standing; we just move the eyes.
        player.SetControlEnabled(false);

        // Cursor LOCKED. Mouse turns the head, crosshair aims, left-click selects —
        // identical to walking around, so there is no mode to learn.
        player.SetCursorLocked(true);

        if (interactor != null) interactor.ClearFocus();
    }

    /// Step back out to your own body.
    public void ExitNpcView()
    {
        if (!npcViewActive) return;

        npcViewActive = false;
        cameras.Activate(CameraId.PlayerFirstPerson);
        player.SetControlEnabled(true);
        if (interactor != null) interactor.ClearFocus();
    }

    /// "Begin Intervention" once the player has finished investigating.
    public void BeginIntervention() => EnterPhase(GamePhase.Intervene);

    /// "Try again" on the debrief screen. This is the ONLY thing that wipes the
    /// player's fixes — ResetScenario deliberately does not.
    public void RetryFromStart()
    {
        if (interventions != null) interventions.ClearAll();
        EnterPhase(GamePhase.Observe);
    }

    // =====================================================================
    //  TEMPORARY KEYS FOR PART 3. Real UI buttons replace these in Part 5.
    //
    //  The bottom letter row, left to right, follows the order of the game:
    //      Z observe   X free roam   C her POV   V his POV
    //      B passenger seat   N intervene   M resolve   K debrief
    //
    //  Deliberately NOT the F-keys (Unity steals some) and NOT Space
    //  (Starter Assets uses it to jump).
    // =====================================================================
    private void HandleDebugKeys()
    {
        if (Input.GetKeyDown(KeyCode.Z)) EnterPhase(GamePhase.Observe);
        if (Input.GetKeyDown(KeyCode.X)) EnterPhase(GamePhase.FreeRoam);
        if (Input.GetKeyDown(KeyCode.C)) PlayPovReplay(CameraId.PedestrianPov);
        if (Input.GetKeyDown(KeyCode.V)) PlayPovReplay(CameraId.DriverPov);
        if (Input.GetKeyDown(KeyCode.B)) EnterPhase(GamePhase.PassengerSeat);
        if (Input.GetKeyDown(KeyCode.N)) EnterPhase(GamePhase.Intervene);
        if (Input.GetKeyDown(KeyCode.M)) EnterPhase(GamePhase.Resolve);
        if (Input.GetKeyDown(KeyCode.K)) EnterPhase(GamePhase.Debrief);

        // Enter = the "Next" button, so you can walk the whole loop with one key
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            switch (phase)
            {
                case GamePhase.Observe:       FinishObserving();    break;
                case GamePhase.FreeRoam:      BeginIntervention();  break;
                case GamePhase.PassengerSeat: LeavePassengerSeat(); break;
                case GamePhase.Debrief:       RetryFromStart();     break;
            }
        }

        // Back out of whatever you stepped into.
        //
        // Q, not Escape. The Unity EDITOR grabs Escape itself and force-releases the cursor
        // lock every time you press it, so our code re-locks the cursor and the Editor
        // immediately unlocks it again — which is why you had to click the Game view to get
        // the cursor to go away. It only misbehaves in the Editor, never in a real build,
        // but there is no reason to fight it. Escape still works as a backup.
        bool backOut = Input.GetKeyDown(KeyCode.Q) ||
                       Input.GetMouseButtonDown(1) ||
                       Input.GetKeyDown(KeyCode.Escape);

        if (backOut)
        {
            if (npcViewActive) ExitNpcView();
            else if (phase == GamePhase.PassengerSeat) LeavePassengerSeat();
        }
    }
}
