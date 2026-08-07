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
    [Tooltip("ON for the car's outer shell: clicking it climbs into the passenger seat.\n" +
             "OFF for a person you talk to standing in the street.")]
    [SerializeField] private bool talkFromPassengerSeat;

    [Tooltip("ON for the DRIVER sitting inside the car. Lets you click him once you are " +
             "already in the passenger seat, which plays his POV replay.\n\n" +
             "The car's outer volume must have this OFF, or you would re-enter the seat " +
             "you are already sitting in.")]
    [SerializeField] private bool availableInPassengerSeat;

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

            // Already sitting beside him — clicking him replays what he saw
            if (director.Phase == GamePhase.PassengerSeat)
                return $"Ask {displayName} what he saw";

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

            // Only the driver himself is clickable once you're sitting in the car.
            // The car's outer volume must NOT be, or you'd re-enter the seat you're in.
            if (director.Phase == GamePhase.PassengerSeat) return availableInPassengerSeat;

            // During Intervene you can step in from outside. You leave with Q,
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

        // Sitting beside him in the car: clicking him plays his POV replay, and the
        // director returns you to the passenger seat afterwards rather than the street.
        if (director.Phase == GamePhase.PassengerSeat)
        {
            director.PlayPovReplay(replayCamera);
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
