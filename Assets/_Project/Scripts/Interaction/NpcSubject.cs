using UnityEngine;

/// "Talk to this person" in Free Roam, "get inside their head" during Intervene.
///
/// One component, two behaviours, picked by asking the director what phase we are in.
/// Goes on the big trigger volume around a person or a car — the one that ISN'T a hazard.
///
/// On the pedestrian this sits on Interact_Body, whose capsule stops at her NECK. Her head
/// has its own small collider for the headphones. Because Physics.Raycast returns the
/// NEAREST hit, aiming at her head gets the headphones and aiming anywhere else gets this.
public class NpcSubject : MonoBehaviour, IInteractable
{
    public enum Subject { Pedestrian, Driver }

    [Header("Who is this?")]
    [SerializeField] private Subject subject = Subject.Pedestrian;
    [SerializeField] private string displayName = "the pedestrian";

    [Header("Cameras")]
    [Tooltip("The locked camera used for the 'see what they saw' replay in Free Roam.")]
    [SerializeField] private CameraId replayCamera = CameraId.PedestrianPov;

    [Tooltip("The free-look camera used when you step into their view during Intervene.\n" +
             "For the driver this is normally the same one.")]
    [SerializeField] private CameraId interveneCamera = CameraId.PedestrianPov;

    [Header("Free Roam behaviour")]
    [Tooltip("The driver is spoken to from the passenger seat, so talking to him moves the " +
             "camera into the car. The pedestrian is spoken to standing in the street.")]
    [SerializeField] private bool talkFromPassengerSeat;

    [Header("Reach")]
    [SerializeField] private float maxDistance = 12f;

    [Header("Wiring — auto-found if left empty")]
    [SerializeField] private Highlighter highlighter;
    [SerializeField] private ScenarioDirector director;

    public Subject Who => subject;

    private void Awake()
    {
        if (highlighter == null) highlighter = GetComponent<Highlighter>();
        if (director == null) director = FindFirstObjectByType<ScenarioDirector>();

        if (gameObject.layer != LayerMask.NameToLayer("Interactable"))
        {
            Debug.LogWarning($"[NpcSubject] '{name}' is not on the Interactable layer, " +
                             "so the player's ray will never hit it.", this);
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
                return director.IsInNpcView
                    ? "Step back out"
                    : $"See through {displayName}'s eyes";
            }

            return talkFromPassengerSeat
                ? $"Get in and talk to {displayName}"
                : $"Talk to {displayName}";
        }
    }

    public bool IsAvailable
    {
        get
        {
            if (director == null) return true;

            if (director.Phase == GamePhase.FreeRoam) return true;

            // During Intervene you can step in from outside. You leave with Esc,
            // not by clicking, so this hides itself once you're inside.
            if (director.IsInterventionActive) return !director.IsInNpcView;

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
        if (director == null) return;

        if (director.IsInterventionActive)
        {
            // Straight into their eyes. The clock keeps running while you're in there.
            director.EnterNpcView(interveneCamera);
            return;
        }

        if (director.Phase == GamePhase.FreeRoam)
        {
            if (talkFromPassengerSeat)
            {
                // Talking to the driver means getting into the passenger seat beside him
                director.EnterPassengerSeat();
            }
            else
            {
                // Part 5 replaces this with the real dialogue panel, whose last page
                // carries the "See what she saw" button.
                Debug.Log($"<color=#ffd08e>[TALK]</color> {displayName} — " +
                          "(dialogue comes in Part 5). Press C to see her POV replay.");
            }
        }
    }

    /// Called by the dialogue panel's "See what they saw" button in Part 5.
    public void PlayTheirReplay()
    {
        if (director != null) director.PlayPovReplay(replayCamera);
    }
}
