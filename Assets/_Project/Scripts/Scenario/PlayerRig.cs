using StarterAssets;
using UnityEngine;

/// A thin wrapper around the Starter Assets first-person controller.
///
/// The ScenarioDirector only ever says "give the player control" or "take it away". All the
/// fiddly Starter Assets details live in here, so if you ever swap the controller out, this
/// is the only file that changes.
public class PlayerRig : MonoBehaviour
{
    [SerializeField] private FirstPersonController controller;
    [SerializeField] private StarterAssetsInputs input;
    [SerializeField] private CharacterController characterController;

    [Tooltip("The empty the camera follows. Starter Assets calls it PlayerCameraRoot.")]
    [SerializeField] private Transform cameraRoot;

    public Transform CameraRoot => cameraRoot;
    public bool ControlEnabled { get; private set; } = true;

    private void Awake()
    {
        // Auto-find, so a forgotten drag doesn't cost you an evening
        if (controller == null) controller = GetComponent<FirstPersonController>();
        if (input == null) input = GetComponent<StarterAssetsInputs>();
        if (characterController == null) characterController = GetComponent<CharacterController>();

        if (controller == null)
            Debug.LogError("[PlayerRig] No FirstPersonController found on this object.", this);
    }

    /// Turn walking and looking on or off.
    public void SetControlEnabled(bool enable)
    {
        ControlEnabled = enable;

        if (controller != null) controller.enabled = enable;

        if (input != null)
        {
            // Zero the stored input, or the player keeps drifting in whatever direction
            // they were holding when control was taken away.
            input.MoveInput(Vector2.zero);
            input.LookInput(Vector2.zero);
            input.JumpInput(false);
            input.SprintInput(false);

            // Stop the mouse banking up rotation while we're in a POV camera
            input.cursorInputForLook = enable;
            input.cursorLocked = enable;
        }

        SetCursorLocked(enable);
    }

    public void SetCursorLocked(bool locked)
    {
        // BOTH of these must be set. Setting only lockState leaves a visible cursor
        // pinned to the middle of the screen, which is the classic version of this bug.
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        // StarterAssetsInputs re-applies its OWN cursorLocked value whenever the window
        // regains focus. Leaving the two out of step meant that alt-tabbing back into the
        // game while sitting in the passenger seat silently freed the cursor — and now
        // that PovLook requires a locked cursor, that would also stop the camera turning.
        if (input != null) input.cursorLocked = locked;
    }

    /// Move the player somewhere.
    ///
    /// A CharacterController overwrites transform.position every frame it is enabled, so
    /// setting the position directly silently does nothing. Disabling it for one frame is
    /// the only reliable way to teleport one, and almost nobody knows this.
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        bool wasEnabled = characterController != null && characterController.enabled;

        if (characterController != null) characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (characterController != null) characterController.enabled = wasEnabled;
    }

    public void Teleport(Transform target)
    {
        if (target == null) return;
        Teleport(target.position, target.rotation);
    }
}
