using UnityEngine;

/// Casts one ray per frame and routes focus and click events to whatever IInteractable
/// it hits. This is the ONLY raycasting script in the game.
///
/// It deliberately raycasts from Camera.main rather than from its own transform, because
/// with Cinemachine the real camera moves around independently of the player's body — into
/// the passenger seat, into someone's eyes. Reading Camera.main means this keeps working in
/// every one of those views with no extra code.
public class PlayerInteractor : MonoBehaviour
{
    [Header("What the ray can hit")]
    [Tooltip("Set this to ONLY the Interactable layer. Without it the ray hits the road, " +
             "the walls and your own capsule, and you will fight it all week.")]
    [SerializeField] private LayerMask interactableMask;

    [Tooltip("The furthest anything can be. Each interactable also has its own shorter " +
             "limit — 12 m for a person, 3.5 m for a phone.")]
    [SerializeField] private float maxRayDistance = 15f;

    [Header("Wiring")]
    [SerializeField] private ScenarioDirector director;

    private IInteractable focused;
    private Camera cam;

    /// What the player is looking at right now, or null. The HUD reads this.
    public IInteractable Focused => focused;

    private void Awake()
    {
        if (director == null) director = FindFirstObjectByType<ScenarioDirector>();

        if (interactableMask == 0)
        {
            Debug.LogError("[PlayerInteractor] Interactable Mask is set to Nothing, so the " +
                           "ray can never hit anything. Set it to the Interactable layer.", this);
        }
    }

    private void Update()
    {
        // Camera.main is cached because it does a scene search internally
        if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
        if (cam == null) return;

        IInteractable found = null;

        if (director == null || director.CanInteract)
        {
            found = CastForInteractable();
        }

        // Only tell things about focus when it actually CHANGES, not every frame
        if (!ReferenceEquals(found, focused))
        {
            focused?.OnFocusExit();
            focused = found;
            focused?.OnFocusEnter();
        }

        if (focused != null && Input.GetMouseButtonDown(0))
        {
            focused.OnInteract();
        }
    }

    private IInteractable CastForInteractable()
    {
        // The aim point follows the cursor automatically:
        //   cursor locked   -> we're walking, so aim from the centre of the screen
        //   cursor free     -> we're seated or in a POV, so aim wherever the mouse is
        Ray ray = Cursor.lockState == CursorLockMode.Locked
            ? cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : cam.ScreenPointToRay(Input.mousePosition);

        // QueryTriggerInteraction.Collide matters: every interaction collider is a trigger,
        // and by default raycasts ignore triggers. Remove this and nothing is ever clickable.
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance,
                             interactableMask, QueryTriggerInteraction.Collide))
        {
            return null;
        }

        // GetComponentInParent, so the collider is allowed to be a CHILD of the object
        // that holds the script. Unity's GetComponent works with interfaces.
        IInteractable candidate = hit.collider.GetComponentInParent<IInteractable>();
        if (candidate == null) return null;

        if (!candidate.IsAvailable) return null;

        // Each interactable enforces its own reach
        if (hit.distance > candidate.MaxDistance) return null;

        return candidate;
    }

    /// Clears focus without firing anything. The director calls this on phase changes so a
    /// highlight can't be left stuck on when the camera cuts somewhere else.
    public void ClearFocus()
    {
        focused?.OnFocusExit();
        focused = null;
    }
}
