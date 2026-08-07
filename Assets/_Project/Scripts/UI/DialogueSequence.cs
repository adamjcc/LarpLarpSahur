using UnityEngine;

/// A conversation, authored entirely in the Inspector. Put one on each person you can talk to.
///
/// No dialogue graph, no branching, no node editor. A list of lines and one optional button
/// at the end is all this game needs, and it takes thirty seconds to write a character's
/// account rather than thirty minutes to wire a tree.
public class DialogueSequence : MonoBehaviour
{
    [SerializeField] private string speakerName = "Pedestrian";

    [Tooltip("One entry per page. The player clicks through them in order.")]
    [TextArea(2, 5)]
    [SerializeField] private string[] lines;

    [Header("Point-of-view button")]
    [Tooltip("Show a second button on the LAST page that replays the final seconds through " +
             "this person's eyes.")]
    [SerializeField] private bool offerPovReplay = true;

    [SerializeField] private string povButtonLabel = "See what she saw";

    [Tooltip("Which subject to replay. Leave empty to use the NpcSubject on this object.")]
    [SerializeField] private NpcSubject subject;

    public string SpeakerName => speakerName;
    public int LineCount => lines != null ? lines.Length : 0;
    public bool OfferPovReplay => offerPovReplay && Subject != null;
    public string PovButtonLabel => povButtonLabel;

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

    /// Called by the dialogue panel's POV button.
    public void PlayPovReplay()
    {
        if (Subject != null) Subject.PlayTheirReplay();
    }
}
