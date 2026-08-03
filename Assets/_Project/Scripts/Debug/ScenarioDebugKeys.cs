using UnityEngine;

/// Temporary keyboard controls + an on-screen readout, so you can drive the scenario clock
/// by hand while building. Put this on the same object as the ScenarioRunner.
///
/// Remove it (or untick Show On Screen) before recording any submission video.
///
///   P          play / pause
///   R          reset to scenario time 0
///   1          seek to Seek Target A
///   2          seek to impact time
///   3          seek to where Intervene begins
///   4          seek to where Free Roam freezes (the aftermath)
///   [  and  ]  slow down / speed up the scenario
///   I          jump straight to the Intervene setup (rewind + slow motion + play)
public class ScenarioDebugKeys : MonoBehaviour
{
    [SerializeField] private ScenarioRunner runner;
    [SerializeField] private ScenarioSettings settings;
    [SerializeField] private ImpactDetector impactDetector;

    [Header("Manual seek target for key 1")]
    [SerializeField] private float seekTargetA = 3f;

    [Header("Display")]
    [SerializeField] private bool showOnScreen = true;
    [SerializeField] private int fontSize = 22;

    // [ and ] step through these instead of nudging by 0.02, which took 47 presses
    // to get from 1.0 down to 0.06.
    private static readonly float[] speedPresets = { 0.02f, 0.06f, 0.1f, 0.25f, 0.5f, 1f, 2f };

    private GUIStyle style;

    // Reset() runs when you first add the component in the editor, so the slots
    // fill themselves in and you can't forget one.
    private void Reset()
    {
        runner = GetComponent<ScenarioRunner>();
        settings = GetComponent<ScenarioSettings>();
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<ScenarioRunner>();
        if (runner == null) runner = FindFirstObjectByType<ScenarioRunner>();
        if (settings == null) settings = FindFirstObjectByType<ScenarioSettings>();
        if (impactDetector == null) impactDetector = FindFirstObjectByType<ImpactDetector>();
    }

    private void Update()
    {
        if (runner == null) return;

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (runner.IsPlaying) runner.Pause();
            else runner.Play();
        }

        if (Input.GetKeyDown(KeyCode.R)) runner.ResetScenario();

        if (Input.GetKeyDown(KeyCode.Alpha1)) runner.SeekTo(seekTargetA);

        if (settings != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha2)) runner.SeekTo(settings.impactTime);
            if (Input.GetKeyDown(KeyCode.Alpha3)) runner.SeekTo(settings.InterveneStartTime);
            if (Input.GetKeyDown(KeyCode.Alpha4)) runner.SeekTo(settings.FreeRoamTime);

            // Dry run of what the real Intervene phase does. This is the key that sets
            // the rewind, the slow motion AND play all at once — the number keys only seek.
            if (Input.GetKeyDown(KeyCode.I))
            {
                runner.SeekTo(settings.InterveneStartTime);
                runner.SetTimeScale(settings.interveneTimeScale);
                runner.Play();

                Debug.Log($"<color=cyan>[Intervene mode]</color> rewound to " +
                          $"t={settings.InterveneStartTime:F2}s, timeScale={settings.interveneTimeScale:F2} " +
                          $"({settings.InterveneRealSeconds:F0} real seconds of play)");
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket)) StepSpeed(-1);
        if (Input.GetKeyDown(KeyCode.RightBracket)) StepSpeed(+1);
    }

    /// Jump to the next or previous speed preset.
    private void StepSpeed(int direction)
    {
        // find whichever preset we're closest to right now
        int nearest = 0;
        float bestGap = float.MaxValue;

        for (int i = 0; i < speedPresets.Length; i++)
        {
            float gap = Mathf.Abs(speedPresets[i] - runner.TimeScale);
            if (gap < bestGap) { bestGap = gap; nearest = i; }
        }

        int next = Mathf.Clamp(nearest + direction, 0, speedPresets.Length - 1);
        runner.SetTimeScale(speedPresets[next]);
    }

    private void OnGUI()
    {
        if (!showOnScreen || runner == null) return;

        // Build the style once. The default label style is dark and unreadable on a night scene.
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = Color.white }
            };
        }

        float lineHeight = fontSize + 8;
        float y = 10f;
        string state = runner.IsPlaying ? "PLAYING" : "PAUSED";

        GUI.Label(new Rect(12, y, 1400, lineHeight),
            $"t = {runner.ScenarioTime:F2}s      " +
            $"speed x{runner.TimeScale:F2}      {state}      " +
            $"actors {runner.ActorCount}", style);
        y += lineHeight;

        string impact = impactDetector != null && impactDetector.HasImpacted
            ? $"IMPACT at {impactDetector.ImpactedAtTime:F2}s"
            : "IMPACT: none yet";

        GUI.Label(new Rect(12, y, 1400, lineHeight), impact, style);
        y += lineHeight;

        GUI.Label(new Rect(12, y, 1400, lineHeight),
            "P play/pause    R reset    1 manual    2 impact    3 intervene    4 aftermath",
            style);
        y += lineHeight;

        // Called out on its own line, because it is the only key that changes the SPEED.
        // The number keys just move the clock.
        string interveneHint = settings != null
            ? $"I = intervene mode  (rewind + slow to x{settings.interveneTimeScale:F2} + play)"
            : "I = intervene mode  (NO ScenarioSettings ASSIGNED)";

        GUI.Label(new Rect(12, y, 1400, lineHeight),
            interveneHint + "        [ ] cycle speed presets", style);
    }
}
