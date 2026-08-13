/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * BriefingSpawnPoint.cs
 * Marks where the player appears inside the Start Office scene.
 */

using UnityEngine;

/// <summary>
/// A marker for where the player should stand when the briefing scene opens.
///
/// WHY THIS EXISTS RATHER THAN A DRAGGED-IN TRANSFORM
/// Unity does not allow a reference from one scene to another. The director lives in
/// GameScene, so it cannot hold a link to something inside Start Office. Instead the office
/// carries this marker, and the director searches for it once the scene has finished
/// loading.
///
/// Put it on an empty GameObject in the Start Office scene, facing the way the player
/// should be looking when they arrive.
/// </summary>
public class BriefingSpawnPoint : MonoBehaviour
{
    /// <summary>
    /// Draws an arrow in the Scene view so you can see which way the player will face.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.4f);
        Gizmos.DrawLine(transform.position + Vector3.up,
                        transform.position + Vector3.up + transform.forward * 2f);
    }
}
