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
    [SerializeField] private GameObject crosshair;
    [SerializeField] private TMP_Text promptText;

    [Tooltip("Optional. Shows '[F] hide the steering wheel' while sitting in the driver's seat.")]
    [SerializeField] private TMP_Text obstructionHint;
    [SerializeField] private PovObstructionToggle obstructionToggle;

    [Header("Phase banner")]
    [SerializeField] private GameObject phaseBanner;
    [SerializeField] private TMP_Text phaseTitle;
    [SerializeField] private TMP_Text phaseHint;

    [Header("Intervene HUD")]
    [SerializeField] private GameObject interveneGroup;
    [SerializeField] private TMP_Text countdownValue;
    [SerializeField] private TMP_Text countdownLabel;
    [SerializeField] private TMP_Text hazardCounter;
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

    // ---------------------------------------------------------------- state

    private DialogueSequence activeDialogue;
    private int dialogueIndex;

    // Seconds left before the hazard toast hides itself
    private float hazardToastRemaining;

    // What to run if the player says yes to the confirm box
    private System.Action pendingConfirmAction;

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

        if (retryButton != null) retryButton.onClick.AddListener(() => director.RetryFromStart());

        if (confirmYesButton != null) confirmYesButton.onClick.AddListener(AcceptConfirm);
        if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CancelConfirm);

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

        // ---- crosshair: only while aiming at the world, and never behind a panel ----
        bool showCrosshair = director.CanInteract && !IsModalOpen &&
                             Cursor.lockState == CursorLockMode.Locked;
        SetActive(crosshair, showCrosshair);

        // ---- interaction prompt ----
        IInteractable focused = (interactor != null && !IsModalOpen) ? interactor.Focused : null;
        if (promptText != null)
        {
            promptText.gameObject.SetActive(focused != null);
            if (focused != null) promptText.text = focused.Prompt;
        }

        // ---- phase banner ----
        // Hidden during the debrief: that panel carries its own headline, and the banner
        // would sit on top of it.
        SetActive(phaseBanner, !IsModalOpen && phase != GamePhase.Debrief);
        if (phaseTitle != null) phaseTitle.text = TitleFor(phase);
        if (phaseHint != null) phaseHint.text = HintFor(phase);

        // ---- observe buttons ----
        // Only once the crash has finished playing, so they don't interrupt the shot.
        bool observeDone = phase == GamePhase.Observe && !director.IsObservationPlaying;
        SetActive(observePanel, observeDone);

        // ---- "[F] hide the steering wheel", only while it would actually do something ----
        if (obstructionToggle != null && obstructionHint != null)
        {
            bool showHint = obstructionToggle.IsAvailable && !IsModalOpen;
            obstructionHint.gameObject.SetActive(showHint);
            if (showHint) obstructionHint.text = $"[F]  {obstructionToggle.PromptLabel}";
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

            if (hazardCounter != null && interventions != null)
            {
                hazardCounter.text =
                    $"HAZARDS FIXED   {interventions.CorrectCount} / {interventions.RequiredCount}";
            }
        }
    }

    private string TitleFor(GamePhase phase) => phase switch
    {
        GamePhase.Observe       => "WHAT HAPPENED",
        GamePhase.FreeRoam      => "INVESTIGATE",
        GamePhase.PovReplay     => "",
        GamePhase.PassengerSeat => "",
        GamePhase.Intervene     => "CHANGE IT",
        GamePhase.Resolve       => "",
        GamePhase.Debrief       => "DEBRIEF",
        _ => ""
    };

    private string HintFor(GamePhase phase) => phase switch
    {
        GamePhase.Observe       => "Watch the incident from above",
        GamePhase.FreeRoam      => "Examine the scene and talk to both people    ·    " +
                                   "[ENTER] when you are ready",
        GamePhase.PovReplay     => "",
        GamePhase.PassengerSeat => "Look around the car    ·    [Q] get out",
        GamePhase.Intervene     => "Approach either person and fix what you found",
        GamePhase.Resolve       => "",
        GamePhase.Debrief       => "[ENTER] try again",
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

        if (hazardFoundText != null)
        {
            hazardFoundText.text = found >= total
                ? $"That's all {total}. Let's see what happens."
                : $"Good catch — you dealt with {hazardName}.";
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
