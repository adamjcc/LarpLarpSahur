/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * ScenarioSettings.cs
 * Shared timing and geometry numbers for the whole incident.
 */

using UnityEngine;

/// <summary>
/// One place for every number that more than one script needs to agree on.
///
/// Without this you end up typing "7" into the pedestrian, the car, the director and the HUD,
/// then changing three of them and spending an evening on why the crash drifted. Put this on
/// SYSTEMS and everything reads from it.
/// </summary>
public class ScenarioSettings : MonoBehaviour
{
    [Header("The key moment")]
    [Tooltip("An empty GameObject sitting exactly where the collision happens. " +
             "Every actor measures itself against this.")]
    public Transform impactMarker;

    [Tooltip("Scenario time at which the car and the pedestrian meet at the marker.")]
    public float impactTime = 7f;

    [Tooltip("How long the simulation keeps running after impact, for the aftermath to settle.")]
    public float simEndTime = 10f;

    [Header("Free Roam — the frozen aftermath")]
    [Tooltip("Free Roam freezes the clock this many seconds AFTER impact.")]
    public float aftermathViewOffset = 2f;

    [Header("POV replay — triggered from dialogue")]
    public float povReplayLeadTime = 5f;   // starts this long before impact
    public float povReplayTailTime = 1f;   // ends this long after impact

    [Header("Intervene")]
    [Tooltip("Intervene starts this many scenario seconds before impact.")]
    public float interveneLeadTime = 3f;

    [Tooltip("How slowly the world runs during Intervene. 0.06 = one sixteenth speed.\n\n" +
             "Real seconds of play = interveneLeadTime / interveneTimeScale.")]
    public float interveneTimeScale = 0.06f;

    [Header("Geometry")]
    [Tooltip("How far before the impact point the near kerb sits. The pedestrian makes her " +
             "step-into-the-road decision here.")]
    public float kerbDistanceBeforeImpact = 3.5f;

    [Tooltip("How far before the impact point the driver would react, if he could see her.")]
    public float driverReactDistanceBeforeImpact = 20f;

    [Header("Read-only summary")]
    [TextArea(3, 6)]
    [SerializeField] private string summary;

    // ---- Handy computed values, so nobody does this arithmetic by hand ----

    /// <summary>
    /// The clock value Free Roam freezes at.
    /// </summary>
    public float FreeRoamTime => impactTime + aftermathViewOffset;

    /// <summary>
    /// The clock value Intervene rewinds to.
    /// </summary>
    public float InterveneStartTime => Mathf.Max(0f, impactTime - interveneLeadTime);

    /// <summary>
    /// The clock value a POV replay starts from.
    /// </summary>
    public float PovReplayStartTime => Mathf.Max(0f, impactTime - povReplayLeadTime);

    /// <summary>
    /// The clock value a POV replay stops at.
    /// </summary>
    public float PovReplayEndTime => impactTime + povReplayTailTime;

    /// <summary>
    /// How many REAL seconds the player actually gets during Intervene.
    /// </summary>
    public float InterveneRealSeconds => interveneLeadTime / Mathf.Max(0.001f, interveneTimeScale);

    // OnValidate runs whenever you change a value in the Inspector. This just keeps the
    // read-only summary box up to date so you can see the consequences immediately.
    private void OnValidate()
    {
        summary =
            $"Intervene gives the player {InterveneRealSeconds:F1} REAL seconds " +
            $"(= {interveneLeadTime:F1}s scenario / {interveneTimeScale:F2} speed)\n" +
            $"Intervene rewinds to t = {InterveneStartTime:F2}s\n" +
            $"Free Roam freezes at t = {FreeRoamTime:F2}s\n" +
            $"POV replay runs t = {PovReplayStartTime:F2}s  ->  {PovReplayEndTime:F2}s";
    }
}
