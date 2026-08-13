/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * BriefingFinishButton.cs
 * Ends the briefing and sends the player to the incident.
 */

using UnityEngine;

/// <summary>
/// Ends the briefing when the trainer's last dialogue page is finished with.
///
/// WHY THIS TINY SCRIPT EXISTS
/// The trainer lives in the Start Office scene and the director lives in GameScene, and
/// Unity does not let you drag a reference from one scene into another. So instead of
/// wiring the button straight to the director, you wire it to this — which sits in the
/// office beside the trainer and finds the director for itself at runtime.
///
/// Put it on the trainer, and point the dialogue's On Final Button at Finish().
/// </summary>
public class BriefingFinishButton : MonoBehaviour
{
    /// <summary>
    /// Closes the office and starts the incident.
    /// Called by the trainer's dialogue, from its last page.
    /// </summary>
    public void Finish()
    {
        // Searched for now rather than dragged in, because the director is in the other
        // scene. FindFirstObjectByType looks across every scene that is currently loaded.
        ScenarioDirector director = FindFirstObjectByType<ScenarioDirector>();

        if (director == null)
        {
            Debug.LogError("[BriefingFinishButton] No ScenarioDirector found. Is GameScene " +
                           "still loaded underneath the office?", this);
            return;
        }

        director.FinishBriefing();
    }
}
