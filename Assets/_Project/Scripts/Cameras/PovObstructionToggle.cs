/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * PovObstructionToggle.cs
 * Hides whatever blocks the view inside a point-of-view camera.
 */

using UnityEngine;

/// <summary>
/// Lets the player hide whatever is physically in the way while they are inside a POV.
///
/// The driver's seat has a real problem: he sits close to the wheel, so the steering wheel
/// and the signal stalk sit directly between the camera and the pedals. Moving the camera
/// back would fix the raycast and break the framing. Hiding the obstruction on a keypress
/// fixes it without compromising either, and reads as a deliberate feature — an analyst
/// stripping away the parts of the reconstruction that are in the way.
///
/// Put this on VEHICLE_INCIDENT and drag the steering wheel and signal lever into the array.
/// </summary>
public class PovObstructionToggle : MonoBehaviour
{
    [Tooltip("Objects to hide. Their colliders go too, so anything behind them becomes " +
             "clickable while they are hidden.")]
    [SerializeField] private GameObject[] obstructions;

    [Tooltip("Only usable while THIS camera is the live one.")]
    [SerializeField] private CameraId activeForCamera = CameraId.DriverPov;

    [Tooltip("Shown in the HUD while the toggle is available.")]
    [SerializeField] private string showLabel = "hide the steering wheel";
    [SerializeField] private string hideLabel = "show the steering wheel";

    [Header("Wiring — auto-found if left empty")]
    [SerializeField] private CameraDirector cameras;
    [SerializeField] private ScenarioDirector director;

    private bool obstructionsHidden;

    /// <summary>
    /// True when the player is actually in the right view to use this. The HUD reads it.
    /// </summary>
    public bool IsAvailable =>
        director != null && cameras != null &&
        director.IsInNpcView &&
        director.CanInteract &&
        cameras.Current == activeForCamera;

    /// <summary>
    /// Text for the HUD hint, e.g. "[F] hide the steering wheel".
    /// </summary>
    public string PromptLabel => obstructionsHidden ? hideLabel : showLabel;

    private void Awake()
    {
        if (cameras == null) cameras = FindFirstObjectByType<CameraDirector>();
        if (director == null) director = FindFirstObjectByType<ScenarioDirector>();
    }

    private void Update()
    {
        if (!IsAvailable)
        {
            // Always put them back on the way out, or you would climb out of the car and
            // find the steering wheel missing for the rest of the game.
            if (obstructionsHidden) SetHidden(false);
            return;
        }

        if (GameInput.ToggleViewPressed) SetHidden(!obstructionsHidden);
    }

    private void SetHidden(bool hidden)
    {
        obstructionsHidden = hidden;

        for (int i = 0; i < obstructions.Length; i++)
        {
            if (obstructions[i] != null) obstructions[i].SetActive(!hidden);
        }
    }
}
