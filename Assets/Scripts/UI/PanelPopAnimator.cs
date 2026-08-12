/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * PanelPopAnimator.cs
 * Gives a UI panel a small fade and scale as it appears.
 */

using UnityEngine;

/// <summary>
/// Makes a panel fade and grow slightly into place instead of appearing instantly.
///
/// Add it to any panel and it works on its own — the animation runs from OnEnable, which
/// fires every time something calls SetActive(true), so nothing else has to know about it.
///
/// Deliberately small. A panel that leaps around draws attention to itself instead of to
/// what it says.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PanelPopAnimator : MonoBehaviour
{
    /// <summary>How long the entrance takes, in real seconds.</summary>
    [SerializeField] private float duration = 0.18f;

    /// <summary>The size it starts at. 0.94 means it grows in by 6 per cent.</summary>
    [SerializeField] private float startScale = 0.94f;

    /// <summary>How far it slides up on the way in, in canvas units. 0 to slide nowhere.</summary>
    [SerializeField] private float riseDistance = 14f;

    private CanvasGroup canvasGroup;
    private RectTransform rect;

    private Vector2 restPosition;
    private float elapsed;
    private bool resting;   // set once we know where the panel is supposed to sit

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rect = GetComponent<RectTransform>();

        restPosition = rect.anchoredPosition;
        resting = true;
    }

    /// <summary>
    /// Restarts the animation. Runs every time the panel is switched on.
    /// </summary>
    private void OnEnable()
    {
        // Awake has not run yet the very first time if the panel starts enabled
        if (!resting) return;

        elapsed = 0f;
        Apply(0f);
    }

    private void Update()
    {
        if (elapsed >= duration) return;

        // unscaledDeltaTime so the animation plays at the same speed regardless of how
        // slowly the incident happens to be running behind it
        elapsed += Time.unscaledDeltaTime;

        Apply(Mathf.Clamp01(elapsed / duration));
    }

    /// <summary>
    /// Places the panel partway through its entrance.
    /// </summary>
    /// <param name="t">0 at the start of the animation, 1 when it has finished.</param>
    private void Apply(float t)
    {
        // Ease out: fast at first, settling gently. Much softer than a straight line.
        float eased = 1f - Mathf.Pow(1f - t, 3f);

        canvasGroup.alpha = eased;
        rect.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, eased);
        rect.anchoredPosition = restPosition + Vector2.down * (riseDistance * (1f - eased));
    }
}
