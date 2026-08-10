using UnityEngine;
using UnityEngine.InputSystem;

/// The single place the game reads player input from.
///
/// HOW IT RECEIVES INPUT
/// Starter Assets' PlayerInput component uses Behaviour = "Send Messages", which calls
/// OnMove / OnLook / OnJump / OnSprint on every component sitting on the SAME GameObject.
/// Add an action called "Interact" to the asset and it starts calling OnInteract too.
/// So this component just has to live beside PlayerInput on the player.
///
/// HOW OTHER SCRIPTS READ IT
/// Through the static properties. ScenarioDirector and UIManager live on _SYSTEMS, not on
/// the player, so Send Messages can never reach them directly.
///
/// WHY FRAME STAMPS INSTEAD OF FLAGS
/// A "pressed this frame" bool has to be cleared by somebody, and if a consumer happens to
/// run after the clear it misses the press. Recording WHICH frame the press happened on
/// removes the clearing step entirely, so script execution order stops mattering.
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    private int interactFrame = -1;
    private int backFrame = -1;
    private int continueFrame = -1;

    private Vector2 lookThisFrame;

    // ---------------------------------------------------------------- public API

    /// Left mouse / E / gamepad A — examine, talk, change a hazard.
    ///
    /// Falls back to legacy input when this component isn't in the scene yet, so the game
    /// keeps working while you're partway through wiring it up.
    public static bool InteractPressed =>
        Instance != null
            ? Instance.interactFrame == Time.frameCount
            : Input.GetMouseButtonDown(0);

    /// Q / right mouse / gamepad B — leave a seat or a point of view.
    public static bool BackPressed =>
        Instance != null
            ? Instance.backFrame == Time.frameCount
            : Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1) ||
              Input.GetKeyDown(KeyCode.Escape);

    /// Enter / gamepad Start — advance to the next part.
    public static bool ContinuePressed =>
        Instance != null
            ? Instance.continueFrame == Time.frameCount
            : Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

    /// Mouse movement since last frame, in pixels.
    ///
    /// NOTE the scale difference: the old Input.GetAxis("Mouse X") returned a smoothed,
    /// pre-scaled value around 0.1 per pixel, whereas this is the raw pixel delta. Anything
    /// using it needs roughly a tenth of the old sensitivity.
    public static Vector2 LookDelta =>
        Instance != null
            ? Instance.lookThisFrame
            : new Vector2(Input.GetAxis("Mouse X") * 10f, Input.GetAxis("Mouse Y") * 10f);

    /// Where the mouse cursor is, for the ray used when the cursor is unlocked.
    public static Vector2 PointerPosition =>
        Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;

    // ---------------------------------------------------------------- plumbing

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        // The Input System does not always send a final zero when the mouse stops moving,
        // which would leave the camera drifting forever. Clearing after every frame's
        // consumers have read it guarantees a stop means a stop.
        lookThisFrame = Vector2.zero;
    }

    // ---- called by PlayerInput via Send Messages ----

    public void OnInteract(InputValue value)
    {
        if (value.isPressed) interactFrame = Time.frameCount;
    }

    public void OnBack(InputValue value)
    {
        if (value.isPressed) backFrame = Time.frameCount;
    }

    public void OnContinue(InputValue value)
    {
        if (value.isPressed) continueFrame = Time.frameCount;
    }

    public void OnLook(InputValue value)
    {
        lookThisFrame = value.Get<Vector2>();
    }
}
