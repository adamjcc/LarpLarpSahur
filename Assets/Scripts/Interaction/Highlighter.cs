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
/// Lights an object up while the player is looking at it, then puts it back exactly as it was.
///
/// HOW IT WORKS
/// At startup it makes a private copy of every material the object uses, tints that copy
/// toward the highlight colour and turns on its emission. Looking at the object swaps in the
/// copies; looking away swaps the originals back.
///
/// WHY COPIES AND NOT A MaterialPropertyBlock
/// A property block is the textbook answer, but URP's SRP Batcher ignores them on a lot of
/// shaders, which is why some objects lit up and others did not. Real material copies always
/// work. They cost a little memory, but only one per material per highlightable object, and
/// they are cleaned up in OnDestroy.
///
/// WHY NOT JUST REPLACE THE MATERIAL
/// Because then a person turns into a flat yellow person. Copying and tinting keeps the
/// object looking like itself, just lit — skin stays skin, paint stays paint.
/// </summary>
public class Highlighter : MonoBehaviour
{
    [Header("Look")]
    /// <summary>The colour everything is tinted toward while highlighted.</summary>
    [Tooltip("Tint colour. A warm yellow reads well against a night scene.")]
    [SerializeField] private Color highlightTint = new Color(1f, 0.85f, 0.3f);

    /// <summary>How far the object's own colour is pulled toward the tint. 0 = no change.</summary>
    [Range(0f, 1f)]
    [SerializeField] private float tintAmount = 0.45f;

    /// <summary>
    /// How brightly it glows. 0 turns emission off entirely and only the tint is used.
    /// </summary>
    [Range(0f, 3f)]
    [SerializeField] private float emissionStrength = 0.8f;

    [Header("Which renderers")]
    /// <summary>
    /// Leave empty to light up every renderer on this object and its children.
    /// Fill it in when the collider is on an empty GameObject and the mesh lives elsewhere,
    /// e.g. Interact_Body lighting up the character model.
    /// </summary>
    [Tooltip("Leave EMPTY to use every renderer on this object and its children.\n\n" +
             "Fill it in when the collider is on an empty GameObject and the mesh you want " +
             "to light up is somewhere else — e.g. Interact_Body lighting the character.")]
    [SerializeField] private Renderer[] renderersOverride;

    private Renderer[] targets;
    private Material[][] originalMaterials;
    private Material[][] highlightMaterials;
    private bool isHighlighted;

    // URP calls the main colour "_BaseColor". Older built-in shaders call it "_Color",
    // so we check for both rather than silently doing nothing.
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    /// <summary>
    /// Collects the renderers and builds a tinted copy of every material they use.
    /// </summary>
    private void Awake()
    {
        targets = (renderersOverride != null && renderersOverride.Length > 0)
            ? renderersOverride
            : GetComponentsInChildren<Renderer>();

        originalMaterials = new Material[targets.Length][];
        highlightMaterials = new Material[targets.Length][];

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null) continue;

            originalMaterials[i] = targets[i].sharedMaterials;
            highlightMaterials[i] = new Material[originalMaterials[i].Length];

            for (int m = 0; m < originalMaterials[i].Length; m++)
            {
                highlightMaterials[i][m] = MakeHighlightCopy(originalMaterials[i][m]);
            }
        }
    }

    /// <summary>
    /// Builds one tinted, glowing copy of a material.
    /// </summary>
    /// <param name="source">The material to copy. Never modified.</param>
    /// <returns>A new material that looks like the original, but lit up.</returns>
    private Material MakeHighlightCopy(Material source)
    {
        if (source == null) return null;

        // new Material(source) copies the shader, every texture and every setting,
        // so the object keeps its own look
        Material copy = new Material(source);
        copy.name = source.name + " (Highlight)";

        // ---- tint the base colour toward the highlight colour ----
        int colourId = copy.HasProperty(BaseColorId) ? BaseColorId
                     : copy.HasProperty(LegacyColorId) ? LegacyColorId
                     : -1;

        if (colourId != -1)
        {
            Color original = copy.GetColor(colourId);
            Color tinted = Color.Lerp(original, highlightTint, tintAmount);

            // Keep the original transparency, or glass would turn solid
            tinted.a = original.a;
            copy.SetColor(colourId, tinted);
        }

        // ---- add a glow ----
        if (emissionStrength > 0f && copy.HasProperty(EmissionColorId))
        {
            // The keyword has to be switched on by hand. A material that has never had
            // emission enabled ignores _EmissionColor completely, which is the usual reason
            // "my emission isn't working" turns out to be true.
            copy.EnableKeyword("_EMISSION");
            copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            copy.SetColor(EmissionColorId, highlightTint * emissionStrength);
        }

        return copy;
    }

    /// <summary>
    /// Turns the highlight on or off by swapping between the original and copied materials.
    /// </summary>
    public void SetHighlight(bool on)
    {
        if (on == isHighlighted) return;     // nothing to do
        isHighlighted = on;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null || originalMaterials[i] == null) continue;

            targets[i].sharedMaterials = on ? highlightMaterials[i] : originalMaterials[i];
        }
    }

    /// <summary>Never leave something stuck glowing because it was switched off mid-highlight.</summary>
    private void OnDisable()
    {
        SetHighlight(false);
    }

    /// <summary>
    /// Cleans up the copies. Materials made with "new Material(...)" are not garbage
    /// collected on their own, so without this they would pile up every time the scene
    /// reloads.
    /// </summary>
    private void OnDestroy()
    {
        if (highlightMaterials == null) return;

        foreach (Material[] set in highlightMaterials)
        {
            if (set == null) continue;
            foreach (Material m in set)
            {
                if (m != null) Destroy(m);
            }
        }
    }
}
