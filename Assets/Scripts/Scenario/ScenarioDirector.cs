/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * ScenarioDirector.cs
 * The phase state machine. The spine of the whole game.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The phases of the game.
/// </summary>
public enum GamePhase
{
    Boot,           // nothing yet
    MainMenu,       // title screen, camera orbiting the level behind it
    Briefing,       // the police office: walk up to the trainer and hear why you are here
    Observe,        // bird's-eye, the crash plays at full speed
    FreeRoam,       // clock frozen after the crash, player walks the aftermath
    PovReplay,      // sub-mode of FreeRoam: locked replay through someone's eyes
    PassengerSeat,  // sub-mode of FreeRoam: sitting beside the driver
    Intervene,      // rewound to just before impact, slow motion, player fixes hazards
    Resolve,        // the "what if" replay at full speed with the fixes applied
    Debrief         // score and explanation
}

/// <summary>
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
/// </summary>
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
    [SerializeField] private PedestrianVictim victim;

    [Header("Player positioning")]
    [Tooltip("Empty GameObject on the pavement where the player starts investigating.")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Main menu")]
    /// <summary>
    /// A Global Volume holding the menu's post-processing look. Switched on only while the
    /// main menu is showing.
    ///
    /// It has to work this way because Cinemachine drives ONE real camera, so every angle
    /// shares the same post-processing. Toggling the Volume is how you give one "camera" its
    /// own look.
    /// </summary>
    [SerializeField] private GameObject menuPostProcessing;

    [Header("Briefing scene")]
    /// <summary>
    /// Name of the police office scene, exactly as it appears in Build Settings.
    /// Loaded ALONGSIDE this scene rather than replacing it, so nothing here is destroyed.
    /// </summary>
    [Tooltip("Must match the scene name in File > Build Settings exactly.")]
    [SerializeField] private string briefingSceneName = "Start Office";

    [Header("Options")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private bool debugKeys = true;

    [Header("Read-only")]
    [SerializeField] private GamePhase phase = GamePhase.Boot;

    // which phase a POV replay should return to when it finishes
    private GamePhase returnPhase = GamePhase.FreeRoam;

    // which POV a replay is currently showing
    private CameraId activeReplayCamera = CameraId.PedestrianPov;

    /// <summary>
    /// Which part of the game we are currently in.
    /// </summary>
    public GamePhase Phase => phase;

    /// <summary>
    /// True while hazards can actually be changed, as opposed to merely examined.
    /// </summary>
    public bool IsInterventionActive => phase == GamePhase.Intervene;

    /// <summary>
    /// True whenever the player is seeing the world through someone else's eyes —
    /// the passenger seat, a POV replay, or standing inside someone during Intervene.
    ///
    /// This is what decides where each hazard can be reached from. Her headphones are
    /// FromOutsideOnly, so they need this to be FALSE. Her phone and every dashboard
    /// control are FromPovOnly, so they need it TRUE.
    /// </summary>
    public bool IsInNpcView => npcViewActive || phase == GamePhase.PassengerSeat;

    /// <summary>
    /// Whether the raycast interaction system should run at all right now.
    /// A panel being open suspends it, so clicking a button cannot also re-trigger
    /// whatever object is sitting behind the panel.
    /// </summary>
    public bool CanInteract =>
        !UIManager.ModalBlockingInput &&
        (phase == GamePhase.Briefing ||
         phase == GamePhase.FreeRoam ||
         phase == GamePhase.PassengerSeat ||
         phase == GamePhase.Intervene);

    /// <summary>
    /// True while the opening bird's-eye shot is still running. The Replay and Continue
    /// buttons wait for this to go false so they don't cover the crash.
    /// </summary>
    public bool IsObservationPlaying =>
        phase == GamePhase.Observe && runner != null && runner.IsPlaying;

    // Set while standing inside someone's view during Intervene. Not a phase of its own,
    // because the clock and the countdown must keep running exactly as before.
    private bool npcViewActive;

    /// <summary>
    /// Scenario seconds left before the collision. Drives the countdown on screen.
    ///
    /// Uses the MEASURED impact time, not the authored one. The authored impactTime (7.0) is
    /// when the two centre points meet, but contact happens a little earlier because the
    /// bumper sticks out in front — around 6.78. Counting down to 7.0 left the countdown
    /// reading 0.22 at the moment of the crash and then running on for another few real
    /// seconds. Now it hits 0.00 exactly as they collide.
    /// </summary>
    public float TimeToImpact
    {
        get
        {
            if (settings == null || runner == null) return 0f;
            float target = predictedImpactTime > 0f ? predictedImpactTime : settings.impactTime;
            return target - runner.ScenarioTime;
        }
    }

    /// <summary>
    /// How much of the intervention window is left, from 1 down to 0.
    /// Drives the progress bar under the countdown.
    /// </summary>
    public float InterveneProgress01
    {
        get
        {
            if (settings == null || settings.interveneLeadTime <= 0f) return 0f;
            return Mathf.Clamp01(TimeToImpact / settings.interveneLeadTime);
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
        if (victim == null) victim = FindFirstObjectByType<PedestrianVictim>();
    }

    private void Start()
    {
        if (startAutomatically) EnterPhase(GamePhase.MainMenu);
    }

    // =====================================================================
    //  MAIN MENU ACTIONS — wired to the buttons in the Inspector
    // =====================================================================

    /// <summary>"Start" on the main menu. Clears any old progress and opens the briefing.</summary>
    public void StartGame()
    {
        ClearProgress();
        EnterPhase(GamePhase.Briefing);
    }

    // =====================================================================
    //  THE BRIEFING SCENE
    // =====================================================================

    // True while Start Office is loaded on top of this scene
    private bool briefingLoaded;

    /// <summary>
    /// Loads the police office alongside this scene, then puts the player inside it.
    ///
    /// This is a coroutine because loading takes more than one frame — we cannot move the
    /// player to the spawn marker until the scene carrying that marker actually exists.
    /// </summary>
    private IEnumerator LoadBriefingScene()
    {
        // Take control away while the scene streams in, so the player cannot walk off
        SetPlayerActive(false);

        if (!briefingLoaded)
        {
            if (string.IsNullOrEmpty(briefingSceneName))
            {
                Debug.LogError("[ScenarioDirector] No briefing scene name set. Skipping " +
                               "straight to the incident.", this);
                EnterPhase(GamePhase.Observe);
                yield break;
            }

            yield return SceneManager.LoadSceneAsync(briefingSceneName, LoadSceneMode.Additive);
            briefingLoaded = true;
        }

        // The marker lives in the office scene, so it can only be found once that scene
        // has finished loading. Cross-scene references are not allowed in the Inspector.
        BriefingSpawnPoint spawn = FindFirstObjectByType<BriefingSpawnPoint>();

        if (spawn != null)
        {
            player.Teleport(spawn.transform);
        }
        else
        {
            Debug.LogWarning($"[ScenarioDirector] '{briefingSceneName}' has no " +
                             "BriefingSpawnPoint, so the player was left where they were. " +
                             "Add one to an empty GameObject in that scene.", this);
        }

        SetPlayerActive(true);
    }

    /// <summary>
    /// Called by the last button of the trainer's dialogue. Closes the office and starts
    /// the incident.
    /// </summary>
    public void FinishBriefing()
    {
        EnterPhase(GamePhase.Observe);
    }

    /// <summary>
    /// Removes the office scene from memory. Called automatically whenever we leave the
    /// briefing, so the geometry is not sitting around for the rest of the game.
    /// </summary>
    private void UnloadBriefingScene()
    {
        if (!briefingLoaded) return;

        briefingLoaded = false;

        Scene scene = SceneManager.GetSceneByName(briefingSceneName);
        if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
    }

    /// <summary>Wipes what the player found and changed. Shared by Start and Retry.</summary>
    private void ClearProgress()
    {
        if (interventions != null) interventions.ClearAll();

        EvidenceLedger ledger = FindFirstObjectByType<EvidenceLedger>();
        if (ledger != null) ledger.ClearAll();

        // Bring back anything that hid itself when it was fixed
        HazardInteractable[] hazards = FindObjectsByType<HazardInteractable>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (HazardInteractable h in hazards) h.RestoreVisual();
    }

    /// <summary>"Back to Main Menu" on the debrief, and anywhere else we want it.</summary>
    public void ReturnToMainMenu()
    {
        ClearProgress();
        EnterPhase(GamePhase.MainMenu);
    }

    /// <summary>
    /// "Quit". Closes the built game. In the Editor this does nothing visible, which is
    /// normal — the #if block stops the Editor from trying to quit itself.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // =====================================================================
    //  THE ONE METHOD THAT CHANGES PHASE
    // =====================================================================
    public void EnterPhase(GamePhase next)
    {
        // Close the office the moment we leave the briefing, whichever way we leave it —
        // the dialogue button, a debug key, or going back to the menu.
        if (next != GamePhase.Briefing) UnloadBriefingScene();

        phase = next;

        // Any phase change drops you out of someone's eyes, and clears whatever
        // highlight was stuck on when the camera cut away.
        npcViewActive = false;
        if (interactor != null) interactor.ClearFocus();

        // Default to playback presentation: she goes down on impact and stays down.
        // Free Roam overrides this below, before its seek.
        if (victim != null) victim.SetWitnessMode(false);

        // The menu's post-processing look belongs to the menu only. Switched back on by
        // the MainMenu case below.
        if (menuPostProcessing != null) menuPostProcessing.SetActive(false);

        Debug.Log($"<color=cyan>[PHASE]</color> -> <b>{next}</b>");

        switch (next)
        {
            // -------------------------------------------------------------
            case GamePhase.MainMenu:
                // The incident is parked at its opening frame so the orbit camera has
                // something sensible to fly over.
                runner.ResetScenario();
                runner.SetTimeScale(1f);

                cameras.Activate(CameraId.StartMenu);
                SetPlayerActive(false);
                player.SetCursorLocked(false);   // menu buttons need a cursor

                if (menuPostProcessing != null) menuPostProcessing.SetActive(true);
                break;

            // -------------------------------------------------------------
            case GamePhase.Briefing:
                // The incident sits paused at its opening frame across the map while the
                // player is in the office. Nothing here is destroyed, so every reference
                // the director holds is still valid when they come back.
                runner.ResetScenario();
                runner.SetTimeScale(1f);

                cameras.Activate(CameraId.PlayerFirstPerson);
                StartCoroutine(LoadBriefingScene());
                break;

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
                // Order matters here.
                //   BEFORE the seek: witness mode on, so re-running the collision doesn't
                //                    trigger the fall animation again.
                //   AFTER  the seek: place her on her standing spot, since the seek leaves
                //                    her wherever the impact put her.
                if (victim != null) victim.SetWitnessMode(true);

                // The frozen aftermath IS just the scenario paused a couple of seconds
                // after impact. Same objects, different clock value. No separate setup.
                runner.SetTimeScale(1f);
                runner.SeekTo(settings.FreeRoamTime);
                runner.Pause();

                if (victim != null) victim.ApplyWitnessPlacement();

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

                // Work out the result and fill the panel. Done here rather than in Update
                // so it is computed exactly once, from the state the Resolve replay
                // finished in — including whether the collision actually happened.
                if (UIManager.Instance != null) UIManager.Instance.ShowDebrief();
                break;
        }
    }

    /// <summary>
    /// Runs the crash silently, in one frame, to find out exactly when contact happens.
    ///
    /// Needed because contact lands a fraction BEFORE the authored impactTime — the bumper
    /// sticks out ahead of the car's centre — and later still if the player has applied the
    /// brakes. Guessing left the countdown out of step with what you could see happening.
    /// </summary>
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

    /// <summary>
    /// One place that decides everything about player control, so no phase can
    /// half-enable it.
    /// </summary>
    private void SetPlayerActive(bool active)
    {
        if (player == null) return;
        player.SetControlEnabled(active);
    }

    /// <summary>
    /// Called by UIManager when a panel opens or closes.
    ///
    /// Opening: free the cursor so buttons are clickable, and stop the player walking off
    /// while they read. Closing: hand control back to whatever the CURRENT phase wants,
    /// rather than blindly re-enabling — otherwise closing a panel while sitting in the
    /// passenger seat would stand you up and let you walk through the car.
    /// </summary>
    public void SetModalControl(bool modalOpen)
    {
        if (player == null) return;

        if (modalOpen)
        {
            player.SetControlEnabled(false);
            player.SetCursorLocked(false);
            return;
        }

        ApplyPhaseControlState();
    }

    /// <summary>
    /// Re-applies only the player/cursor part of the current phase, without touching the
    /// clock or the cameras. Used when a panel closes.
    /// </summary>
    public void ApplyPhaseControlState()
    {
        if (player == null) return;

        switch (phase)
        {
            case GamePhase.Briefing:
            case GamePhase.FreeRoam:
                SetPlayerActive(true);
                break;

            case GamePhase.Intervene:
                // Inside someone's eyes the body stays parked but the view still turns
                if (npcViewActive)
                {
                    player.SetControlEnabled(false);
                    player.SetCursorLocked(true);
                }
                else
                {
                    SetPlayerActive(true);
                }
                break;

            case GamePhase.PassengerSeat:
                player.SetControlEnabled(false);
                player.SetCursorLocked(true);
                break;

            case GamePhase.Debrief:
                player.SetControlEnabled(false);
                player.SetCursorLocked(false);
                break;

            default:
                SetPlayerActive(false);
                break;
        }
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

    /// <summary>
    /// "Replay" on the Observe screen.
    /// </summary>
    public void ReplayObservation()
    {
        runner.ResetScenario();
        cameras.Activate(CameraId.BirdsEye);
        runner.Play();
    }

    /// <summary>
    /// "Next" on the Observe screen.
    /// </summary>
    public void FinishObserving() => EnterPhase(GamePhase.FreeRoam);

    /// <summary>
    /// Called from dialogue: "See what she saw" / "See what he saw".
    /// </summary>
    public void PlayPovReplay(CameraId povCamera)
    {
        activeReplayCamera = povCamera;
        returnPhase = (phase == GamePhase.PassengerSeat) ? GamePhase.PassengerSeat : GamePhase.FreeRoam;
        EnterPhase(GamePhase.PovReplay);
    }

    public void EnterPassengerSeat() => EnterPhase(GamePhase.PassengerSeat);

    public void LeavePassengerSeat() => EnterPhase(GamePhase.FreeRoam);

    /// <summary>
    /// Step INTO someone's view during Intervene.
    ///
    /// Note what this does NOT do: it doesn't touch the clock or the time scale. The
    /// countdown keeps running at 0.06x the whole time you're in there, which is what you
    /// asked for — at that speed her phone is drifting at about 7 cm a second, so it is
    /// still perfectly easy to click.
    /// </summary>
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

    /// <summary>
    /// Step back out to your own body.
    /// </summary>
    public void ExitNpcView()
    {
        if (!npcViewActive) return;

        npcViewActive = false;
        cameras.Activate(CameraId.PlayerFirstPerson);
        player.SetControlEnabled(true);
        if (interactor != null) interactor.ClearFocus();
    }

    /// <summary>
    /// Asks the player to confirm before starting the intervention.
    ///
    /// The intervention is a one-way door — once the countdown starts there is no going
    /// back to the investigation, so it is worth one click to be sure.
    /// </summary>
    public void RequestBeginIntervention()
    {
        if (UIManager.Instance == null)
        {
            BeginIntervention();
            return;
        }

        UIManager.Instance.ShowConfirm(
            "Ready to intervene?",
            "You will be taken back to the last few seconds before the collision, running " +
            "in slow motion. Walk up to either person and change what you found.\n\n" +
            "You cannot return to the investigation once this starts.",
            BeginIntervention);
    }

    /// <summary>Starts the intervention immediately, with no confirmation.</summary>
    public void BeginIntervention() => EnterPhase(GamePhase.Intervene);

    /// <summary>
    /// "Try again" on the debrief screen. This is the ONLY thing that wipes the
    /// player's fixes — ResetScenario deliberately does not.
    ///
    /// The evidence ledger is cleared too. Without that, a second attempt would still count
    /// everything you examined the first time as "found", and the debrief would praise you
    /// for spotting things you never looked at this run.
    /// </summary>
    public void RetryFromStart()
    {
        ClearProgress();

        // Straight back to the incident, not the office — nobody wants the briefing twice.
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
        // A panel owns Enter, Q, Escape and right-click while it is open, and for the rest
        // of the frame it closes on. Without the frame part, pressing Q to dismiss a
        // dialogue would ALSO eject you from the passenger seat, depending on which script
        // Unity happened to update first.
        if (UIManager.ModalBlockingInput) return;

        if (Input.GetKeyDown(KeyCode.G)) EnterPhase(GamePhase.Briefing);
        if (Input.GetKeyDown(KeyCode.Z)) EnterPhase(GamePhase.Observe);
        if (Input.GetKeyDown(KeyCode.X)) EnterPhase(GamePhase.FreeRoam);
        if (Input.GetKeyDown(KeyCode.C)) PlayPovReplay(CameraId.PedestrianPov);
        if (Input.GetKeyDown(KeyCode.V)) PlayPovReplay(CameraId.DriverPov);
        if (Input.GetKeyDown(KeyCode.B)) EnterPhase(GamePhase.PassengerSeat);
        if (Input.GetKeyDown(KeyCode.N)) EnterPhase(GamePhase.Intervene);
        if (Input.GetKeyDown(KeyCode.M)) EnterPhase(GamePhase.Resolve);
        if (Input.GetKeyDown(KeyCode.K)) EnterPhase(GamePhase.Debrief);

        // "Continue" = Enter or gamepad Start, so you can walk the whole loop with one input
        if (GameInput.ContinuePressed)
        {
            switch (phase)
            {
                case GamePhase.Observe:       FinishObserving();    break;
                case GamePhase.FreeRoam:      RequestBeginIntervention(); break;
                case GamePhase.PassengerSeat: LeavePassengerSeat(); break;
                case GamePhase.Debrief:       RetryFromStart();     break;
            }
        }

        // Back out of whatever you stepped into. Bound to Q, right mouse and gamepad B.
        //
        // Escape is deliberately NOT bound. The Unity Editor grabs Escape itself and
        // force-releases the cursor lock, so our code re-locks it and the Editor unlocks it
        // again — which is why it used to leave a stray cursor. Editor-only, but no reason
        // to fight it.
        if (GameInput.BackPressed)
        {
            if (npcViewActive) ExitNpcView();
            else if (phase == GamePhase.PassengerSeat) LeavePassengerSeat();
        }
    }
}
