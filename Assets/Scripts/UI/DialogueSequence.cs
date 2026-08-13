/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * DialogueSequence.cs
 * A conversation, written on the character in the Inspector.
 */

using UnityEngine;

/// <summary>
/// A conversation, authored entirely in the Inspector. Put one on each person you can talk to.
///
/// No dialogue graph, no branching, no node editor. A list of lines and one optional button
/// at the end is all this game needs, and it takes thirty seconds to write a character's
/// account rather than thirty minutes to wire a tree.
/// </summary>
public class DialogueSequence : MonoBehaviour
{
    [SerializeField] private string speakerName = "Pedestrian";

    [Tooltip("One entry per page. The player clicks through them in order.")]
    [TextArea(2, 5)]
    [SerializeField] private string[] lines;

    /// <summary>What the extra button on the last page does, if there is one.</summary>
    public enum FinalButtonAction
    {
        /// <summary>No extra button. The player just leaves.</summary>
        None,

        /// <summary>Replays the incident through this person's eyes.</summary>
        PlayPovReplay,

        /// <summary>Runs whatever is wired into On Final Button in the Inspector.</summary>
        CustomEvent
    }

    [Header("Button on the last page")]
    /// <summary>What the extra button does, or None to leave it off.</summary>
    [Tooltip("Play Pov Replay for the two people in the crash.\n" +
             "Custom Event for the police trainer, wired to FinishBriefing below.")]
    [SerializeField] private FinalButtonAction finalButtonAction = FinalButtonAction.PlayPovReplay;

    /// <summary>The text on that button.</summary>
    [SerializeField] private string finalButtonLabel = "See what she saw";

    /// <summary>
    /// Runs when the button is pressed, if the action is Custom Event.
    /// For the trainer, wire this to ScenarioDirector.FinishBriefing().
    /// </summary>
    [SerializeField] private UnityEngine.Events.UnityEvent onFinalButton;

    [Tooltip("Which subject to replay. Leave empty to use the NpcSubject on this object.")]
    [SerializeField] private NpcSubject subject;

    public string SpeakerName => speakerName;
    public int LineCount => lines != null ? lines.Length : 0;
    public string PovButtonLabel => finalButtonLabel;

    /// <summary>
    /// Whether the last page should show its extra button. A POV replay also needs a
    /// subject to replay, so that case checks for one.
    /// </summary>
    public bool OfferPovReplay => finalButtonAction switch
    {
        FinalButtonAction.PlayPovReplay => Subject != null,
        FinalButtonAction.CustomEvent => true,
        _ => false
    };

    public NpcSubject Subject
    {
        get
        {
            if (subject == null) subject = GetComponentInParent<NpcSubject>();
            return subject;
        }
    }

    public string GetLine(int index)
    {
        if (lines == null || index < 0 || index >= lines.Length) return string.Empty;
        return lines[index];
    }

    /// <summary>
    /// Warns in the Inspector if any line is too long for the dialogue panel.
    ///
    /// 180 characters is roughly what fits without the text shrinking. Split anything longer
    /// into two entries — it reads better as two pages anyway.
    /// </summary>
    private void OnValidate()
    {
        const int maxLineLength = 180;

        if (lines == null) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] != null && lines[i].Length > maxLineLength)
            {
                Debug.LogWarning($"[{name}] Dialogue line {i + 1} is {lines[i].Length} " +
                                 $"characters. Keep it under {maxLineLength}, or split it " +
                                 "into two lines.", this);
            }
        }
    }

    /// <summary>
    /// Called when the player presses the button on the last page.
    /// Does whatever Final Button Action is set to.
    /// </summary>
    public void PlayPovReplay()
    {
        switch (finalButtonAction)
        {
            case FinalButtonAction.PlayPovReplay:
                if (Subject != null) Subject.PlayTheirReplay();
                break;

            case FinalButtonAction.CustomEvent:
                onFinalButton?.Invoke();
                break;
        }
    }
}
