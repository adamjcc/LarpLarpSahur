using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// The single hub for every piece of on-screen text and every panel.
///
/// Nothing else in the game touches a Text component. Scripts call methods here
/// (ShowExamine, ShowDialogue, ...) and this decides what appears. That means changing the
/// look of the game never means editing gameplay code.
///
/// It is a singleton because there is exactly one of it and half the project needs to reach
/// it. Singletons are frowned on in large codebases; in a four-week student project used for
/// a handful of true globals they save real time.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ---------------------------------------------------------------- wiring

    [Header("Always-on HUD")]
    [SerializeField] private GameObject crosshair;
    [SerializeField] private TMP_Text promptText;

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

    [Header("References")]
    [SerializeField] private ScenarioDirector director;
    [SerializeField] private InterventionState interventions;
    [SerializeField] private PlayerInteractor interactor;

    // ---------------------------------------------------------------- state

    private DialogueSequence activeDialogue;
    private int dialogueIndex;

    /// True while a panel is up that the player must dismiss. Movement and the interaction
    /// ray are both suspended while this is true, so clicking "Next" cannot also re-trigger
    /// whatever is behind the panel.
    public bool IsModalOpen { get; private set; }

    /// Safe to call before Awake has run anywhere.
    public static bool ModalOpen => Instance != null && Instance.IsModalOpen;

    // ---------------------------------------------------------------- lifecycle

    private void Awake()
    {
        Instance = this;

        if (director == null) director = FindFirstObjectByType<ScenarioDirector>();
        if (interventions == null) interventions = FindFirstObjectByType<InterventionState>();
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();

        if (examineCloseButton != null) examineCloseButton.onClick.AddListener(CloseExamine);
        if (dialogueNextButton != null) dialogueNextButton.onClick.AddListener(AdvanceDialogue);
        if (dialoguePovButton != null) dialoguePovButton.onClick.AddListener(PlayDialoguePov);

        if (replayButton != null) replayButton.onClick.AddListener(() => director.ReplayObservation());
        if (continueButton != null) continueButton.onClick.AddListener(() => director.FinishObserving());

        SetActive(examinePanel, false);
        SetActive(dialoguePanel, false);
        SetActive(observePanel, false);
    }

    private void Update()
    {
        RefreshHud();

        // Any of these dismisses an open panel, so the player never has to hunt for the button
        if (IsModalOpen &&
            (Input.GetKeyDown(KeyCode.Escape) ||
             Input.GetKeyDown(KeyCode.Q) ||
             Input.GetMouseButtonDown(1)))
        {
            if (activeDialogue != null) CloseDialogue();
            else CloseExamine();
        }
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
        SetActive(phaseBanner, !IsModalOpen);
        if (phaseTitle != null) phaseTitle.text = TitleFor(phase);
        if (phaseHint != null) phaseHint.text = HintFor(phase);

        // ---- observe buttons ----
        // Only once the crash has finished playing, so they don't interrupt the shot.
        bool observeDone = phase == GamePhase.Observe && !director.IsObservationPlaying;
        SetActive(observePanel, observeDone);

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

    /// Called when the player clicks a hazard during the investigation.
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

    /// Shown when the player changes something that was never going to help.
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

        // Hand control back to whatever the current phase wants — the director owns that
        // decision, so this never has to know whether we're walking or sitting in a car.
        if (director != null) director.SetModalControl(false);
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }
}
