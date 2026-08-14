/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * ObstructionInteractable.cs
 * A clickable switch for hiding and showing the steering wheel.
 */

using UnityEngine;

/// <summary>
/// Lets the player click to hide or show whatever is blocking their view inside a
/// point-of-view camera. Used for the steering wheel, which sits between the driver's eyes
/// and the pedals.
///
/// WHERE TO PUT THIS, AND WHY IT MATTERS
/// Put it on a small collider of its own, NOT on the steering wheel. Hiding the wheel also
/// removes its collider — that is the whole point, so the pedals become clickable — and if
/// this lived on the wheel it would vanish along with it and there would be no way to bring
/// the wheel back.
///
/// A small volume on the steering column or the top of the dashboard works well: it stays
/// put, and it sits above the pedals rather than between them and the camera.
/// </summary>
public class ObstructionInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("The Pov Obstruction Toggle on the car. Found automatically if left empty.")]
    [SerializeField] private PovObstructionToggle toggle;

    [Header("Prompts")]
    /// <summary>Shown while the wheel is visible.</summary>
    [SerializeField] private string hidePrompt = "Hide the steering wheel";

    /// <summary>Shown while the wheel is hidden.</summary>
    [SerializeField] private string showPrompt = "Show the steering wheel";

    /// <summary>How close the player has to be. Small, because it is inside the car.</summary>
    [SerializeField] private float maxDistance = 3f;

    [Tooltip("Optional. Lights up when the player looks at it.")]
    [SerializeField] private Highlighter highlighter;

    private void Awake()
    {
        if (toggle == null) toggle = GetComponentInParent<PovObstructionToggle>();
        if (highlighter == null) highlighter = GetComponent<Highlighter>();

        if (toggle == null)
        {
            Debug.LogError($"[ObstructionInteractable] '{name}' cannot find a " +
                           "PovObstructionToggle. Put this object under the car, or drag " +
                           "the toggle in.", this);
        }

        if (gameObject.layer != LayerMask.NameToLayer("Interactable"))
        {
            Debug.LogWarning($"[ObstructionInteractable] '{name}' is not on the Interactable " +
                             "layer, so the player's ray will never hit it.", this);
        }
    }

    // ---------------------------------------------------------------- IInteractable

    public float MaxDistance => maxDistance;

    /// <summary>The prompt flips depending on whether the wheel is currently hidden.</summary>
    public string Prompt => toggle != null && toggle.IsHidden ? showPrompt : hidePrompt;

    /// <summary>
    /// Only clickable while the player is actually in the view this toggle belongs to.
    /// The toggle already works that out, so we just borrow its answer.
    /// </summary>
    public bool IsAvailable => toggle != null && toggle.IsAvailable;

    public void OnFocusEnter()
    {
        if (highlighter != null) highlighter.SetHighlight(true);
    }

    public void OnFocusExit()
    {
        if (highlighter != null) highlighter.SetHighlight(false);
    }

    /// <summary>Hides the wheel, or brings it back.</summary>
    public void OnInteract()
    {
        if (toggle != null) toggle.Toggle();
    }
}
