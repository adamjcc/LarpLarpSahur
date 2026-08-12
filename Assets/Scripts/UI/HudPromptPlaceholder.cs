/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * HudPromptPlaceholder.cs
 * DEBUG - temporary on-screen prompt, replaced by the Canvas.
 */

using UnityEngine;

/// <summary>
/// TEMPORARY crosshair and interaction prompt, drawn with OnGUI so it needs no setup.
/// A proper Canvas replaces this in Part 5 alongside the dialogue panel. Delete it then.
/// </summary>
public class HudPromptPlaceholder : MonoBehaviour
{
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private ScenarioDirector director;

    [SerializeField] private bool show = true;
    [SerializeField] private int fontSize = 24;
    [SerializeField] private float crosshairSize = 6f;

    private GUIStyle prompt;
    private Texture2D dot;

    private void Awake()
    {
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        if (director == null) director = FindFirstObjectByType<ScenarioDirector>();
    }

    private void OnGUI()
    {
        if (!show || interactor == null || director == null) return;
        BuildStyles();

        float w = Screen.width;
        float h = Screen.height;

        // ---- crosshair, only while walking (cursor locked means we aim from the centre) ----
        if (director.CanInteract && Cursor.lockState == CursorLockMode.Locked)
        {
            GUI.DrawTexture(new Rect((w - crosshairSize) * 0.5f,
                                     (h - crosshairSize) * 0.5f,
                                     crosshairSize, crosshairSize), dot);
        }

        // ---- prompt for whatever we're looking at ----
        IInteractable focused = interactor.Focused;
        if (focused != null)
        {
            GUI.Label(new Rect(0, h * 0.58f, w, 40), $"[LMB]  {focused.Prompt}", prompt);
        }

        // ---- reminder of how to get back out of someone's head ----
        if (director.IsInNpcView)
        {
            GUI.Label(new Rect(0, h - 40, w, 34),
                      "[Q] or [right-click] step back out", prompt);
        }
    }

    private void BuildStyles()
    {
        if (prompt != null) return;

        prompt = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white }
        };

        // a 1x1 white texture, stretched into the crosshair dot
        dot = new Texture2D(1, 1);
        dot.SetPixel(0, 0, Color.white);
        dot.Apply();
    }
}
