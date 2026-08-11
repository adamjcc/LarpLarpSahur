/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * Highlighter.cs
 * Makes an object glow while the player is looking at it.
 */

using UnityEngine;

/// <summary>
/// Makes an object glow while the player is looking at it, then puts it back exactly
/// as it was.
///
/// WHY IT ISN'T ONE LINE. Your old InteractableEvidence did
/// `objRenderer.material = highlightMaterial`, which has two problems:
///   1. It only handles ONE renderer with ONE material. A car has dozens of both.
///   2. Reading `.material` (not `.sharedMaterial`) silently CLONES the material every
///      time. Do that a few hundred times in a session and you have leaked a few hundred
///      materials.
///
/// So this caches every renderer's full material array up front and restores it byte for
/// byte, using sharedMaterials throughout.
/// </summary>
public class Highlighter : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;

    [Tooltip("Leave EMPTY to use every renderer on this object and its children.\n\n" +
             "Fill it in when the collider lives on an empty GameObject and the mesh you " +
             "want to glow is somewhere else — e.g. Interact_Body highlighting the character.")]
    [SerializeField] private Renderer[] renderersOverride;

    private Renderer[] targets;
    private Material[][] originalMaterials;
    private bool isHighlighted;

    private void Awake()
    {
        targets = (renderersOverride != null && renderersOverride.Length > 0)
            ? renderersOverride
            : GetComponentsInChildren<Renderer>();

        // Remember exactly what each renderer had, so "off" is a perfect restore
        originalMaterials = new Material[targets.Length][];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null) originalMaterials[i] = targets[i].sharedMaterials;
        }
    }

    public void SetHighlight(bool on)
    {
        if (on == isHighlighted) return;      // nothing to do
        if (highlightMaterial == null) return;

        isHighlighted = on;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null || originalMaterials[i] == null) continue;

            if (on)
            {
                // A renderer with 3 material slots needs 3 copies of the highlight,
                // or Unity leaves the extra slots on the original materials.
                Material[] filled = new Material[originalMaterials[i].Length];
                for (int m = 0; m < filled.Length; m++) filled[m] = highlightMaterial;
                targets[i].sharedMaterials = filled;
            }
            else
            {
                targets[i].sharedMaterials = originalMaterials[i];
            }
        }
    }

    private void OnDisable()
    {
        // Never leave something stuck glowing because it got switched off mid-highlight
        SetHighlight(false);
    }
}
