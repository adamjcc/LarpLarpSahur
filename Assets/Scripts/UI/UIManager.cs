/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * UIManager.cs
 * Every panel and every piece of on-screen text.
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The single hub for every piece of on-screen text and every panel.
///
/// Nothing else in the game touches a Text component. Scripts call methods here
/// (ShowExamine, ShowDialogue, ...) and this decides what appears. That means changing the
/// look of the game never means editing gameplay code.
///
/// It is a singleton because there is exactly one of it and half the project needs to reach
/// it. Singletons are frowned on in large codebases; in a four-week student project used for
/// a handful of true globals they save real time.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ---------------------------------------------------------------- wiring

    [Header("Always-on HUD")]
    /// <summary>Parent of both crosshair sprites. Hidden when aiming isn't possible.</summary>
    [SerializeField] private GameObject crosshair;

    /// <summary>Shown when the crosshair is on nothing in particular.</summary>
    [SerializeField] private GameObject crosshairDefault;

    /// <summary>Shown the instant the crosshair lands on something interactive.</summary>
    [SerializeField] private GameObject crosshairSelected;

    [SerializeField] private TMP_Text promptText;

    /// <summary>
    /// The line under the phase hint that lists which keys currently do something.
    /// Kept separate from the hint so the hint can stay a plain description.
    /// </summary>
    [Tooltip("Shows the controls for the current phase, including [F] inside the car.")]
    [SerializeField] private TMP_Text controlsHint;

    /// <summary>Used to work out whether the [F] control should be listed right now.</summary>
    [SerializeField] private PovObstructionToggle obstructionToggle;

    [Header("Phase banner lights")]
    /// <summary>
    /// The "_ON" versions of the three traffic lights on the banner.
    /// Leave the "_OFF" versions permanently visible underneath them.
    /// </summary>
    [SerializeField] private GameObject greenLightOn;
    [SerializeField] private GameObject yellowLightOn;
    [SerializeField] private GameObject redLightOn;

    [Header("Phase banner")]
    [SerializeField] private GameObject phaseBanner;
    [SerializeField] private TMP_Text phaseTitle;
    [SerializeField] private TMP_Text phaseHint;

    [Header("Intervene HUD")]
    [SerializeField] private GameObject interveneGroup;
    [SerializeField] private TMP_Text countdownValue;
    [SerializeField] private TMP_Text countdownLabel;
    /// <summary>Just the numbers, e.g. "2/4".</summary>
    [SerializeField] private TMP_Text hazardCounter;

    /// <summary>The words next to the numbers, e.g. "HAZARDS FOUND".</summary>
    [SerializeField] private TMP_Text hazardLabel;

    /// <summary>
    /// Thin bar under the countdown that drains as the window runs out.
    /// Its Image Type must be Filled, Fill Method Horizontal.
    /// </summary>
    [SerializeField] private Image countdownBar;

    /// <summary>
    /// One row of the hazard tracker: the four slots that fill in as the player works.
    ///
    /// Each slot has two backgrounds and two icons, and we swap between them rather than
    /// changing a sprite, so the "found" versions can be a different size or colour with no
    /// extra code.
    /// </summary>
    [System.Serializable]
    public class HazardIndicator
    {
        [Tooltip("Which hazard this slot represents. Pick from the dropdown.")]
        public HazardId hazard;

        [Tooltip("HazardItemN_Inner — the empty background. Shown until it is found.")]
        public GameObject innerDefault;

        [Tooltip("HazardItemN_Inner_ITEMGOT — the filled background. Shown once found.")]
        public GameObject innerFound;

        [Tooltip("IconN_NOTFOUND — the greyed icon.")]
        public GameObject iconNotFound;

        [Tooltip("IconN_FOUND — the lit icon.")]
        public GameObject iconFound;
    }

    [Header("Hazard tracker")]
    /// <summary>
    /// The four hazard slots. Set each row's Hazard dropdown to match the icon you dragged in.
    /// </summary>
    [SerializeField] private HazardIndicator[] hazardIndicators;

    [Header("Countdown pulse")]
    /// <summary>How long each kick lasts, in real seconds.</summary>
    [SerializeField] private float pulseDuration = 0.25f;

    /// <summary>How much bigger the number gets at the peak. 0.25 = 25% bigger.</summary>
    [SerializeField] private float pulseScale = 0.25f;

    /// <summary>How far it tilts at the peak, in degrees.</summary>
    [SerializeField] private float pulseTilt = 8f;
    [SerializeField] private Color countdownNormal = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color countdownUrgent = new Color(1f, 0.3f, 0.25f);

    [Header("Examine panel")]
    [SerializeField] private GameObject examinePanel;
    [SerializeField] private TMP_Text examineTitle;
    [SerializeField] private TMP_Text examineBody;
    [SerializeField] private Button examineCloseButton;

    [Header("Dialogue panel")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueSpeaker;
    [SerializeField] private TMP_Text dialogueLine;
    [SerializeField] private Button dialogueNextButton;
    [SerializeField] private TMP_Text dialogueNextLabel;
    [SerializeField] private Button dialoguePovButton;
    [SerializeField] private TMP_Text dialoguePovLabel;

    [Header("Observe controls")]
    [SerializeField] private GameObject observePanel;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button continueButton;

    [Header("Hazard found toast")]
    /// <summary>Small celebratory line that pops up when a real hazard is fixed.</summary>
    [SerializeField] private GameObject hazardFoundPanel;
    [SerializeField] private TMP_Text hazardFoundText;

    /// <summary>How long the toast stays on screen, in real seconds.</summary>
    [SerializeField] private float hazardFoundDuration = 2.5f;

    [Header("Confirm box")]
    /// <summary>Yes/no panel used before committing to the intervention.</summary>
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmTitle;
    [SerializeField] private TMP_Text confirmBody;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    [Header("Main menu")]
    /// <summary>The title screen. Shown first, and again from the debrief.</summary>
    [SerializeField] private GameObject mainMenuPanel;

    /// <summary>Instructions panel opened from the main menu.</summary>
    [SerializeField] private GameObject howToPlayPanel;

    [SerializeField] private Button startButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button howToPlayBackButton;
    [SerializeField] private Button quitButton;

    /// <summary>"Back to Main Menu" on the debrief screen.</summary>
    [SerializeField] private Button backToMenuButton;

    [Header("Debrief")]
    [SerializeField] private GameObject debriefPanel;
    [SerializeField] private TMP_Text debriefHeadline;
    [SerializeField] private TMP_Text debriefGrade;
    [SerializeField] private TMP_Text debriefBody;
    [SerializeField] private Button retryButton;
    [SerializeField] private Color preventedColour = new Color(0.49f, 0.88f, 0.49f);
    [SerializeField] private Color collidedColour = new Color(1f, 0.48f, 0.43f);

    [Header("References")]
    [SerializeField] private ScenarioDirector director;
    [SerializeField] private InterventionState interventions;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private ScoreManager scoreManager;

    /// <summary>Used to tell whether the bird's-eye recap is actually playing right now.</summary>
    [SerializeField] private ScenarioRunner scenarioRunner;

    // ---------------------------------------------------------------- state

    private DialogueSequence activeDialogue;
    private int dialogueIndex;

    // Seconds left before the hazard toast hides itself
    private float hazardToastRemaining;

    // What to run if the player says yes to the confirm box
    private System.Action pendingConfirmAction;

    // Countdown pulse bookkeeping
    private int lastWholeSecond = -1;
    private float pulseTimer;
    private float pulseDirection = 1f;

    /// <summary>
    /// True while a panel is up that the player must dismiss. Movement and the interaction
    /// ray are both suspended while this is true, so clicking "Next" cannot also re-trigger
    /// whatever is behind the panel.
    /// </summary>
    public bool IsModalOpen { get; private set; }

    /// <summary>
    /// Safe to call before Awake has run anywhere.
    /// </summary>
    public static bool ModalOpen => Instance != null && Instance.IsModalOpen;

    // The frame a panel last closed on.
    private int modalClosedOnFrame = -1;

    /// <summary>
    /// True while a panel is open AND for the rest of the frame it closes on.
    ///
    /// The extra frame matters. Script execution order between UIManager, the EventSystem
    /// and PlayerInteractor is undefined, and Input.GetKeyDown / GetMouseButtonDown stay
    /// true for the whole frame. Without this guard:
    ///   · pressing Q to close a dialogue also ejected you from the passenger seat
    ///   · clicking "Leave" also re-triggered the person behind the panel, reopening it
    /// Both bugs would appear or vanish depending on the order Unity happened to pick.
    /// </summary>
    public static bool ModalBlockingInput =>
        Instance != null &&
        (Instance.IsModalOpen || Instance.modalClosedOnFrame == Time.frameCount);

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        Instance = this;

        if (director == null) director = FindFirstObjectByType<ScenarioDirector>();
        if (interventions == null) interventions = FindFirstObjectByType<InterventionState>();
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (obstructionToggle == null) obstructionToggle = FindFirstObjectByType<PovObstructionToggle>();
        if (scenarioRunner == null) scenarioRunner = FindFirstObjectByType<ScenarioRunner>();

        if (retryButton != null) retryButton.onClick.AddListener(() => director.RetryFromStart());

        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(AcceptConfirm);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CancelConfirm);

        // ---- main menu ----
        if (startButton != null) startButton.onClick.AddListener(() => director.StartGame());
        if (quitButton != null) quitButton.onClick.AddListener(() => director.QuitGame());
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(() => director.ReturnToMainMenu());

        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(() => SetActive(howToPlayPanel, true));

        if (howToPlayBackButton != null)
            howToPlayBackButton.onClick.AddListener(() => SetActive(howToPlayPanel, false));

        if (examineCloseButton != null) examineCloseButton.onClick.AddListener(CloseExamine);
        if (dialogueNextButton != null) dialogueNextButton.onClick.AddListener(AdvanceDialogue);
        if (dialoguePovButton != null) dialoguePovButton.onClick.AddListener(PlayDialoguePov);

        if (replayButton != null) replayButton.onClick.AddListener(() => director.ReplayObservation());
        if (continueButton != null) continueButton.onClick.AddListener(() => director.FinishObserving());

        SetActive(examinePanel, false);
        SetActive(dialoguePanel, false);
        SetActive(observePanel, false);
        SetActive(debriefPanel, false);
        SetActive(confirmPanel, false);
        SetActive(hazardFoundPanel, false);
        SetActive(howToPlayPanel, false);

        WarnAboutUnassignedButtons();
    }

    /// <summary>
    /// Says which buttons have not been dragged into their slot.
    ///
    /// A button with an empty slot is completely silent when clicked, which looks exactly
    /// like a broken script. One warning at startup saves a lot of hunting.
    /// </summary>
    private void WarnAboutUnassignedButtons()
    {
        WarnIfMissing(startButton, "Start Button");
        WarnIfMissing(quitButton, "Quit Button");
        WarnIfMissing(backToMenuButton, "Back To Menu Button");
        WarnIfMissing(retryButton, "Retry Button");
        WarnIfMissing(mainMenuPanel, "Main Menu Panel");
    }

    private void WarnIfMissing(Object slot, string slotName)
    {
        if (slot == null)
        {
            Debug.LogWarning($"[UIManager] '{slotName}' is empty, so that button will do " +
                             "nothing when clicked. Drag it in on the Canvas.", this);
        }
    }

    private void Update()
    {
        RefreshHud();
        UpdateHazardToast();

        // "Back" dismisses an open panel, so the player never has to hunt for the button
        if (IsModalOpen && GameInput.BackPressed)
        {
            if (confirmPanel != null && confirmPanel.activeSelf) CancelConfirm();
            else if (activeDialogue != null) CloseDialogue();
            else CloseExamine();
        }
    }

    /// <summary>Counts down the hazard toast and hides it when its time is up.</summary>
    private void UpdateHazardToast()
    {
        if (hazardFoundPanel == null || !hazardFoundPanel.activeSelf) return;

        // unscaledDeltaTime, so the toast lasts the same on screen no matter how slowly
        // the incident happens to be running
        hazardToastRemaining -= Time.unscaledDeltaTime;

        if (hazardToastRemaining <= 0f) SetActive(hazardFoundPanel, false);
    }

    // ---------------------------------------------------------------- the HUD

    private void RefreshHud()
    {
        if (director == null) return;

        GamePhase phase = director.Phase;

        // ---- main menu ----
        // The menu owns the whole screen, so every other piece of HUD is suppressed while
        // it is up. Checked first, before anything else gets a chance to show itself.
        bool inMenu = phase == GamePhase.MainMenu;
        SetActive(mainMenuPanel, inMenu);

        if (inMenu)
        {
            SetActive(crosshair, false);
            SetActive(crosshairDefault, false);
            SetActive(crosshairSelected, false);
            SetActive(phaseBanner, false);
            SetActive(interveneGroup, false);
            SetActive(observePanel, false);
            SetActive(debriefPanel, false);
            if (promptText != null) promptText.gameObject.SetActive(false);
            if (controlsHint != null) controlsHint.gameObject.SetActive(false);
            UpdatePhaseLights(phase, false);
            return;
        }

        SetActive(howToPlayPanel, false);

        // ---- what the crosshair is currently on ----
        IInteractable focused = (interactor != null && !IsModalOpen) ? interactor.Focused : null;

        // ---- crosshair: only while aiming at the world, and never behind a panel ----
        bool showCrosshair = director.CanInteract && !IsModalOpen &&
                             Cursor.lockState == CursorLockMode.Locked;
        SetActive(crosshair, showCrosshair);

        // Two sprites, one swapped for the other. Doing it by enabling and disabling rather
        // than changing a sprite means the "selected" version can be a different size or
        // colour without any extra code.
        SetActive(crosshairDefault, showCrosshair && focused == null);
        SetActive(crosshairSelected, showCrosshair && focused != null);

        // ---- interaction prompt ----
        if (promptText != null)
        {
            promptText.gameObject.SetActive(focused != null);
            if (focused != null) promptText.text = focused.Prompt;
        }

        // ---- phase banner ----
        // Hidden completely during a POV replay so nothing sits over the shot, and during
        // the debrief because that panel carries its own headline. Everywhere else it stays
        // up, including inside the car and inside someone's point of view.
        bool showBanner = !IsModalOpen &&
                          phase != GamePhase.Debrief &&
                          phase != GamePhase.PovReplay;

        SetActive(phaseBanner, showBanner);
        if (phaseTitle != null) phaseTitle.text = TitleFor(phase);
        if (phaseHint != null) phaseHint.text = HintFor(phase);

        UpdatePhaseLights(phase, showBanner);

        // ---- observe buttons ----
        // Only once the crash has finished playing, so they don't interrupt the shot.
        bool observeDone = phase == GamePhase.Observe && !director.IsObservationPlaying;
        SetActive(observePanel, observeDone);

        // ---- controls line ----
        // Kept separate from the phase hint on purpose: the hint says what this part of the
        // game IS, this says which keys do something right now. Splitting them stops the
        // hint from turning into a wall of bracketed keys.
        if (controlsHint != null)
        {
            bool showControls = showBanner;
            controlsHint.gameObject.SetActive(showControls);
            if (showControls) controlsHint.text = ControlsFor(phase);
        }

        // ---- debrief ----
        // Driven from the phase, not from a modal flag: the debrief IS the whole screen
        // at that point, so there is nothing behind it to protect.
        SetActive(debriefPanel, phase == GamePhase.Debrief);

        // ---- intervene countdown ----
        bool intervening = phase == GamePhase.Intervene;
        SetActive(interveneGroup, intervening && !IsModalOpen);

        if (intervening)
        {
            float remaining = Mathf.Max(0f, director.TimeToImpact);

            if (countdownValue != null)
            {
                // TWO decimals on purpose. At 0.06x speed a single decimal only ticks
                // once every 1.7 seconds and reads as frozen.
                countdownValue.text = remaining.ToString("F2");
                countdownValue.color = remaining < 1f ? countdownUrgent : countdownNormal;
            }

            if (countdownLabel != null) countdownLabel.text = "SECONDS TO IMPACT";

            // Fill drains from 1 to 0 as the window runs out
            if (countdownBar != null) countdownBar.fillAmount = director.InterveneProgress01;

            PulseCountdown(remaining);

            if (hazardCounter != null && interventions != null)
            {
                hazardCounter.text =
                    $"{interventions.CorrectCount}/{interventions.RequiredCount}";
            }

            if (hazardLabel != null) hazardLabel.text = "HAZARDS FOUND";

            UpdateHazardIndicators();
        }
        else
        {
            // Leave the number sitting straight when the countdown isn't running,
            // otherwise it can be frozen mid-tilt the next time it appears.
            ResetCountdownPulse();
        }
    }

    /// <summary>
    /// Fills in each hazard slot as the player deals with it.
    ///
    /// For every slot: the empty background is swapped for the filled one, and the greyed
    /// icon for the lit one. Nothing is hardcoded to a slot number — each row carries its
    /// own Hazard dropdown, so reordering the icons in the Inspector cannot break it.
    /// </summary>
    private void UpdateHazardIndicators()
    {
        if (hazardIndicators == null || interventions == null) return;

        foreach (HazardIndicator slot in hazardIndicators)
        {
            if (slot == null) continue;

            bool found = interventions.Has(slot.hazard);

            SetActive(slot.innerDefault, !found);
            SetActive(slot.innerFound, found);
            SetActive(slot.iconNotFound, !found);
            SetActive(slot.iconFound, found);
        }
    }

    /// <summary>
    /// Gives the countdown a small kick each time it passes a whole second, so the last
    /// few seconds feel urgent rather than just being a number that changes.
    ///
    /// The kick is a quick scale-up plus a slight tilt that settles back to normal.
    /// </summary>
    private void PulseCountdown(float remaining)
    {
        if (countdownValue == null) return;

        // Mathf.CeilToInt so 2.9 and 2.1 are both "3 seconds left". The moment that number
        // drops, we have crossed a whole second.
        int wholeSecondsLeft = Mathf.CeilToInt(remaining);

        if (wholeSecondsLeft != lastWholeSecond)
        {
            lastWholeSecond = wholeSecondsLeft;
            pulseTimer = pulseDuration;

            // Alternate the tilt direction so it rocks side to side instead of always
            // flicking the same way
            pulseDirection = -pulseDirection;
        }

        if (pulseTimer <= 0f)
        {
            ResetCountdownPulse();
            return;
        }

        pulseTimer -= Time.unscaledDeltaTime;

        // 1 at the start of the kick, easing down to 0
        float strength = Mathf.Clamp01(pulseTimer / pulseDuration);

        countdownValue.transform.localScale = Vector3.one * (1f + pulseScale * strength);
        countdownValue.transform.localRotation =
            Quaternion.Euler(0f, 0f, pulseTilt * strength * pulseDirection);
    }

    /// <summary>Puts the countdown back to its normal size and angle.</summary>
    private void ResetCountdownPulse()
    {
        if (countdownValue == null) return;

        countdownValue.transform.localScale = Vector3.one;
        countdownValue.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Lights the traffic light on the banner that matches what the player is doing.
    ///
    ///   GREEN  - the incident is playing back on the bird's-eye camera
    ///   YELLOW - the player is investigating the frozen scene
    ///   RED    - the countdown is running and they are changing things
    ///
    /// Only one is ever on. The "_OFF" bulbs sit underneath and are never touched.
    /// </summary>
    private void UpdatePhaseLights(GamePhase phase, bool bannerVisible)
    {
        bool green = false, yellow = false, red = false;

        if (bannerVisible)
        {
            switch (phase)
            {
                case GamePhase.Observe:
                    // Green only while the recap is actually running. Once it stops and the
                    // Replay / Continue buttons appear, it drops back to yellow.
                    bool recapPlaying = scenarioRunner != null && scenarioRunner.IsPlaying;
                    green = recapPlaying;
                    yellow = !recapPlaying;
                    break;

                case GamePhase.Resolve:
                    green = true;
                    break;

                case GamePhase.FreeRoam:
                case GamePhase.PassengerSeat:
                    yellow = true;
                    break;

                case GamePhase.Intervene:
                    red = true;
                    break;
            }
        }

        SetActive(greenLightOn, green);
        SetActive(yellowLightOn, yellow);
        SetActive(redLightOn, red);
    }

    /// <summary>
    /// Builds the controls line for the current phase.
    /// The [F] entry only appears when the steering wheel is actually in the way.
    /// </summary>
    private string ControlsFor(GamePhase phase)
    {
        bool canHideObstruction = obstructionToggle != null && obstructionToggle.IsAvailable;

        switch (phase)
        {
            case GamePhase.Briefing:
                return "[WASD] Move     [LMB] Talk to your officer";

            case GamePhase.Observe:
                return director.IsObservationPlaying ? "" : "[ENTER] Continue";

            case GamePhase.FreeRoam:
                return "[WASD] Move     [LMB] Examine or talk     [ENTER] Begin intervention";

            case GamePhase.PassengerSeat:
                return canHideObstruction
                    ? "[LMB] Examine     [F] Hide the wheel     [ESC] Get out"
                    : "[LMB] Examine     [ESC] Get out";

            case GamePhase.Intervene:
                if (director.IsInNpcView)
                {
                    return canHideObstruction
                        ? "[LMB] Change     [F] Hide the wheel     [ESC] Step back out"
                        : "[LMB] Change     [ESC] Step back out";
                }
                return "[WASD] Move     [LMB] Step into their view";

            case GamePhase.Debrief:
                return "[ENTER] Try again";

            default:
                return "";
        }
    }

    private string TitleFor(GamePhase phase) => phase switch
    {
        GamePhase.Briefing      => "BRIEFING",
        GamePhase.Observe       => "WHAT HAPPENED",
        GamePhase.FreeRoam      => "INVESTIGATE",
        GamePhase.PovReplay     => "",
        GamePhase.PassengerSeat => "INVESTIGATE",
        GamePhase.Intervene     => "CHANGE IT",
        GamePhase.Resolve       => "WHAT YOU CHANGED",
        GamePhase.Debrief       => "DEBRIEF",
        _ => ""
    };

    private string HintFor(GamePhase phase) => phase switch
    {
        // Description only. The keys live on the controls line underneath.
        GamePhase.Briefing      => "Find your training officer",
        GamePhase.Observe       => "Watch the incident from above",
        GamePhase.FreeRoam      => "Examine the scene and talk to both people",
        GamePhase.PovReplay     => "",
        GamePhase.PassengerSeat => "Sitting beside the driver",
        GamePhase.Intervene     => "Approach either person and fix what you found",
        GamePhase.Resolve       => "The same seconds, with your changes",
        GamePhase.Debrief       => "How it went",
        _ => ""
    };

    // ---------------------------------------------------------------- examine

    /// <summary>
    /// Called when the player clicks a hazard during the investigation.
    /// </summary>
    public void ShowExamine(string title, string body)
    {
        if (examinePanel == null) return;

        if (examineTitle != null) examineTitle.text = title;
        if (examineBody != null) examineBody.text = body;

        SetActive(examinePanel, true);
        OpenModal();
    }

    public void CloseExamine()
    {
        SetActive(examinePanel, false);
        if (activeDialogue == null) CloseModal();
    }

    /// <summary>
    /// Shown when the player changes something that was never going to help.
    /// </summary>
    public void ShowNoEffect(string objectName)
    {
        ShowExamine("No effect",
                    $"You changed {objectName}, but it played no part in this collision. " +
                    "Something else caused it.");
    }

    // ---------------------------------------------------------------- dialogue

    public void ShowDialogue(DialogueSequence sequence)
    {
        if (sequence == null || dialoguePanel == null || sequence.LineCount == 0) return;

        activeDialogue = sequence;
        dialogueIndex = 0;

        SetActive(dialoguePanel, true);
        OpenModal();
        RefreshDialogue();
    }

    private void RefreshDialogue()
    {
        if (activeDialogue == null) return;

        if (dialogueSpeaker != null) dialogueSpeaker.text = activeDialogue.SpeakerName;
        if (dialogueLine != null) dialogueLine.text = activeDialogue.GetLine(dialogueIndex);

        bool onLastLine = dialogueIndex >= activeDialogue.LineCount - 1;

        if (dialogueNextLabel != null) dialogueNextLabel.text = onLastLine ? "Leave" : "Next";

        // The POV button only appears on the final page, so the player hears the whole
        // account before they get to see it.
        bool showPov = onLastLine && activeDialogue.OfferPovReplay;
        if (dialoguePovButton != null) dialoguePovButton.gameObject.SetActive(showPov);
        if (dialoguePovLabel != null) dialoguePovLabel.text = activeDialogue.PovButtonLabel;
    }

    public void AdvanceDialogue()
    {
        if (activeDialogue == null) return;

        dialogueIndex++;

        if (dialogueIndex >= activeDialogue.LineCount) CloseDialogue();
        else RefreshDialogue();
    }

    private void PlayDialoguePov()
    {
        DialogueSequence sequence = activeDialogue;
        CloseDialogue();

        // Closing first, so the panel isn't left hanging over the replay
        if (sequence != null) sequence.PlayPovReplay();
    }

    public void CloseDialogue()
    {
        activeDialogue = null;
        SetActive(dialoguePanel, false);
        CloseModal();
    }

    // ---------------------------------------------------------------- hazard toast

    /// <summary>
    /// Pops up a short line when the player fixes one of the four real hazards.
    ///
    /// Deliberately NOT a modal — it appears and fades on its own, because interrupting
    /// the countdown to congratulate someone would be a strange reward.
    /// </summary>
    /// <param name="hazardName">The display name of the hazard, e.g. "her headphones".</param>
    /// <param name="found">How many are fixed now.</param>
    /// <param name="total">How many there are in total.</param>
    public void ShowHazardFound(string hazardName, int found, int total)
    {
        if (hazardFoundPanel == null) return;

        // Nothing on the last one. The phase is about to cut to the replay anyway, and a
        // congratulation that appears for half a second then vanishes just reads as a glitch.
        if (found >= total) return;

        if (hazardFoundText != null)
        {
            // Short enough for a pill in the corner. 28 characters is about the limit before
            // it either wraps or starts shrinking.
            string label = $"Found: {hazardName}";
            if (label.Length > 28) label = label.Substring(0, 27) + "…";

            hazardFoundText.text = label;
        }

        SetActive(hazardFoundPanel, true);
        hazardToastRemaining = hazardFoundDuration;
    }

    // ---------------------------------------------------------------- confirm box

    /// <summary>
    /// Asks the player a yes/no question and runs onYes only if they agree.
    ///
    /// Used before the intervention, which is a one-way door — once the countdown starts
    /// there is no going back to the investigation.
    /// </summary>
    /// <param name="title">Heading, e.g. "Begin the intervention?"</param>
    /// <param name="body">A sentence or two explaining what is about to happen.</param>
    /// <param name="onYes">What to run if they confirm.</param>
    public void ShowConfirm(string title, string body, System.Action onYes)
    {
        if (confirmPanel == null)
        {
            // No panel wired up yet — just do the thing rather than blocking the player
            onYes?.Invoke();
            return;
        }

        pendingConfirmAction = onYes;

        if (confirmTitle != null) confirmTitle.text = title;
        if (confirmBody != null) confirmBody.text = body;

        SetActive(confirmPanel, true);
        OpenModal();
    }

    /// <summary>Runs the pending action and closes the box.</summary>
    private void AcceptConfirm()
    {
        System.Action action = pendingConfirmAction;

        // Clear and close BEFORE running it, because the action changes phase and would
        // otherwise be fighting a panel that is still open.
        CancelConfirm();
        action?.Invoke();
    }

    /// <summary>Closes the box and forgets the pending action.</summary>
    private void CancelConfirm()
    {
        pendingConfirmAction = null;
        SetActive(confirmPanel, false);
        CloseModal();
    }

    // ---------------------------------------------------------------- debrief

    /// <summary>
    /// Called by the director when the Debrief phase opens. Asks the ScoreManager to work
    /// out the result, then displays it.
    /// </summary>
    public void ShowDebrief()
    {
        if (scoreManager == null)
        {
            Debug.LogError("[UIManager] No ScoreManager in the scene, so the debrief will " +
                           "be blank. Add one to SYSTEMS.", this);
            return;
        }

        scoreManager.Compute();

        if (debriefHeadline != null)
        {
            debriefHeadline.text = scoreManager.Headline;
            debriefHeadline.color = scoreManager.CollisionPrevented ? preventedColour : collidedColour;
        }

        if (debriefGrade != null) debriefGrade.text = scoreManager.GradeLine;
        if (debriefBody != null) debriefBody.text = scoreManager.Body;
    }

    // ---------------------------------------------------------------- modal plumbing

    private void OpenModal()
    {
        IsModalOpen = true;

        // Free the cursor so the buttons are clickable, and stop the player walking off
        // while they read.
        if (director != null) director.SetModalControl(true);
        if (interactor != null) interactor.ClearFocus();
    }

    private void CloseModal()
    {
        IsModalOpen = false;

        // Keep blocking input for the rest of this frame — see ModalBlockingInput
        modalClosedOnFrame = Time.frameCount;

        // Hand control back to whatever the current phase wants — the director owns that
        // decision, so this never has to know whether we're walking or sitting in a car.
        if (director != null) director.SetModalControl(false);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
