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

    /// True whenever the player is allowed to walk around. Part 4's interaction system
    /// asks this to decide whether to raycast at all.
    public bool IsPlayerActive => phase == GamePhase.FreeRoam || phase == GamePhase.Intervene;

    /// True while hazards can actually be changed, as opposed to merely examined.
    public bool IsInterventionActive => phase == GamePhase.Intervene;

    /// Scenario seconds left before the collision. Drives the countdown on screen.
    public float TimeToImpact => settings != null ? settings.impactTime - runner.ScenarioTime : 0f;

    private void Awake()
    {
        if (runner == null) runner = FindFirstObjectByType<ScenarioRunner>();
        if (settings == null) settings = FindFirstObjectByType<ScenarioSettings>();
        if (interventions == null) interventions = FindFirstObjectByType<InterventionState>();
        if (cameras == null) cameras = FindFirstObjectByType<CameraDirector>();
        if (player == null) player = FindFirstObjectByType<PlayerRig>();
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
                cameras.Activate(activeReplayCamera);
                SetPlayerActive(false);
                runner.Play();
                break;

            // -------------------------------------------------------------
            case GamePhase.PassengerSeat:
                runner.Pause();
                cameras.Activate(CameraId.PassengerSeat);
                SetPlayerActive(false);
                // Cursor free here, so you can click things on the dashboard
                player.SetCursorLocked(false);
                break;

            // -------------------------------------------------------------
            case GamePhase.Intervene:
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
                // Two ways out: fixed everything, or ran out of time.
                if (interventions != null && interventions.AllRequiredFixed)
                {
                    EnterPhase(GamePhase.Resolve);
                }
                else if (runner.ScenarioTime >= settings.impactTime)
                {
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

        // Escape leaves any sub-mode
        if (Input.GetKeyDown(KeyCode.Escape) && phase == GamePhase.PassengerSeat)
        {
            LeavePassengerSeat();
        }
    }
}
