/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * HazardInteractable.cs
 * A hazard or red herring the player can examine and change.
 */

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Where the player must be standing to CHANGE this hazard.
/// (Examining it in Free Roam is always allowed if you can see it.)
/// </summary>
public enum HazardAccess
{
    /// <summary>
    /// Reachable from anywhere. Used for red herrings like the umbrella.
    /// </summary>
    Both,

    /// <summary>
    /// Only from inside that person's eyes. Her phone; every dashboard control.
    /// </summary>
    FromPovOnly,

    /// <summary>
    /// Only from outside, looking at them. Her headphones — nobody can see
    /// their own ears, so this is the one that has to work from the outside.
    /// </summary>
    FromOutsideOnly
}

/// <summary>
/// ONE component for all eight clickable hazards and red herrings.
///
/// Its behaviour changes with the phase, which is the whole trick:
///   FREE ROAM  -> clicking EXAMINES it. Shows a vague, no-spoiler description and records
///                 it in the EvidenceLedger. This is also how the player learns which
///                 objects are interactive, before it matters.
///   INTERVENE  -> clicking FIXES it. Records it in InterventionState and fires onApplied,
///                 which you wire up in the Inspector with no code at all.
/// </summary>
public class HazardInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField] private HazardId hazardId;
    [SerializeField] private string displayName = "Object";

    [Tooltip("Red herrings can be examined and clicked, but never count for anything.")]
    [SerializeField] private bool isRedHerring;

    [Header("Where it can be changed from")]
    [SerializeField] private HazardAccess access = HazardAccess.Both;
    [SerializeField] private float maxDistance = 3.5f;

    [Header("Text")]
    [Tooltip("Shown in Free Roam. Hint at it WITHOUT giving the answer away.\n\n" +
             "Good:  \"Her screen is still lit. Whatever she was reading, it had her full " +
             "attention.\"\n" +
             "Bad:   \"She was distracted by her phone, which caused the accident.\"")]
    [TextArea(2, 5)]
    [SerializeField] private string examineDescription = "";

    [Tooltip("Shown in the debrief at the end. This one CAN spell it out.")]
    [TextArea(2, 5)]
    [SerializeField] private string debriefExplanation = "";

    [Header("What happens when it is fixed")]
    [Tooltip("Drag in the thing to change. E.g. headlight button -> VehicleLights.TurnOn()")]
    [SerializeField] private UnityEvent onApplied;

    [Tooltip("Optional: hide this object once fixed, e.g. the phone disappearing.")]
    [SerializeField] private GameObject hideWhenApplied;

    [Header("Wiring — all auto-found if left empty")]
    [SerializeField] private Highlighter highlighter;
    [SerializeField] private ScenarioDirector director;
    [SerializeField] private InterventionState interventions;
    [SerializeField] private EvidenceLedger ledger;

    public HazardId Id => hazardId;
    public bool IsRedHerring => isRedHerring;
    public string DisplayName => displayName;
    public string DebriefExplanation => debriefExplanation;

    /// <summary>
    /// Warns in the Inspector if the examine text is too long for its panel.
    ///
    /// 110 characters is what fits the examine card without wrapping past the bottom.
    /// It warns rather than trimming, because silently cutting someone's writing in half
    /// is worse than telling them to shorten it.
    /// </summary>
    private void OnValidate()
    {
        const int maxExamineLength = 110;

        if (examineDescription != null && examineDescription.Length > maxExamineLength)
        {
            Debug.LogWarning($"[{name}] Examine Description is {examineDescription.Length} " +
                             $"characters. Keep it under {maxExamineLength} or it will " +
                             "overflow the panel.", this);
        }
    }

    /// <summary>
    /// Puts a hidden object back. Called on Retry.
    ///
    /// Needed because "Hide When Applied" switches the GameObject off permanently. Some
    /// props (her phone, her headphones) also get restored by PedestrianVictim.ResetToStart,
    /// but anything NOT owned by an actor — a dropped bag, a dashboard item — would stay
    /// invisible for the whole of the second attempt with nothing to bring it back.
    /// </summary>
    public void RestoreVisual()
    {
        if (hideWhenApplied != null) hideWhenApplied.SetActive(true);
    }

    private void Awake()
    {
        if (highlighter == null) highlighter = GetComponent<Highlighter>();
        if (director == null) director = FindFirstObjectByType<ScenarioDirector>();
        if (interventions == null) interventions = FindFirstObjectByType<InterventionState>();
        if (ledger == null) ledger = FindFirstObjectByType<EvidenceLedger>();

        if (gameObject.layer != LayerMask.NameToLayer("Interactable"))
        {
            Debug.LogWarning($"[HazardInteractable] '{name}' is not on the Interactable " +
                             "layer, so the player's ray will never hit it.", this);
        }
    }

    // ---------------------------------------------------------------- IInteractable

    public float MaxDistance => maxDistance;

    public string Prompt
    {
        get
        {
            if (director == null) return displayName;

            if (director.IsInterventionActive)
            {
                bool alreadyFixed = interventions != null && interventions.Has(hazardId);
                return alreadyFixed ? $"{displayName} — done" : $"Change: {displayName}";
            }

            return $"Examine: {displayName}";
        }
    }

    public bool IsAvailable
    {
        get
        {
            if (director == null) return true;

            // Investigating: anything you can see, you can read about.
            if (director.Phase == GamePhase.FreeRoam || director.Phase == GamePhase.PassengerSeat)
                return true;

            // Intervening: you must be in the right place to actually change it.
            if (director.IsInterventionActive)
            {
                return access switch
                {
                    HazardAccess.Both => true,
                    HazardAccess.FromPovOnly => director.IsInNpcView,
                    HazardAccess.FromOutsideOnly => !director.IsInNpcView,
                    _ => true
                };
            }

            // Every other phase is a cutscene. Nothing is clickable.
            return false;
        }
    }

    public void OnFocusEnter()
    {
        if (highlighter != null) highlighter.SetHighlight(true);
    }

    public void OnFocusExit()
    {
        if (highlighter != null) highlighter.SetHighlight(false);
    }

    public void OnInteract()
    {
        if (director != null && director.IsInterventionActive) Apply();
        else Examine();
    }

    // ---------------------------------------------------------------- behaviour

    /// <summary>
    /// Free Roam: read about it, and remember that we did.
    /// </summary>
    private void Examine()
    {
        if (ledger != null) ledger.Record(hazardId);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowExamine(displayName, examineDescription);
        }
        else
        {
            Debug.Log($"<color=#8ecfff>[EXAMINE]</color> <b>{displayName}</b>  —  {examineDescription}");
        }
    }

    /// <summary>
    /// Intervene: actually change it.
    /// </summary>
    private void Apply()
    {
        if (interventions == null) return;

        // HashSet.Add returns false if it was already in there, so this can never
        // fire the effect twice.
        bool isNew = interventions.Apply(hazardId);
        if (!isNew) return;

        onApplied?.Invoke();
        if (hideWhenApplied != null) hideWhenApplied.SetActive(false);

        string tag = isRedHerring ? "no effect" : "contributing factor";
        Debug.Log($"<color=lime>[FIXED]</color> <b>{displayName}</b> ({tag})   " +
                  $"{interventions.CorrectCount}/{interventions.RequiredCount}");

        if (UIManager.Instance == null) return;

        if (isRedHerring)
        {
            // Say plainly that this one changed nothing. Without it, a red herring feels
            // identical to a real fix and the counter silently refusing to move just
            // reads as a bug.
            UIManager.Instance.ShowNoEffect(displayName);
        }
        else if (interventions.IsRequired(hazardId))
        {
            // A quick well done for one of the four that actually mattered
            UIManager.Instance.ShowHazardFound(
                displayName, interventions.CorrectCount, interventions.RequiredCount);
        }
    }
}
