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

            // Dry run of what the real Intervene phase will do in Part 6
            if (Input.GetKeyDown(KeyCode.I))
            {
                runner.SeekTo(settings.InterveneStartTime);
                runner.SetTimeScale(settings.interveneTimeScale);
                runner.Play();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
            runner.SetTimeScale(Mathf.Max(0.02f, runner.TimeScale - 0.02f));

        if (Input.GetKeyDown(KeyCode.RightBracket))
            runner.SetTimeScale(Mathf.Min(2f, runner.TimeScale + 0.02f));
    }

    private void OnGUI()
    {
        if (!showOnScreen || runner == null) return;

        // Build the style once. The default label style is dark and unreadable on a night scene.
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };
        }

        string state = runner.IsPlaying ? "PLAYING" : "PAUSED";

        GUI.Label(new Rect(12, 10, 900, 24),
            $"ScenarioTime  {runner.ScenarioTime:F2} s      " +
            $"TimeScale  {runner.TimeScale:F2}      {state}      " +
            $"Actors: {runner.ActorCount}", style);

        string impact = "IMPACT: none yet";
        if (impactDetector != null && impactDetector.HasImpacted)
        {
            impact = $"IMPACT at {impactDetector.ImpactedAtTime:F2} s";
        }

        GUI.Label(new Rect(12, 32, 900, 24), impact, style);

        GUI.Label(new Rect(12, 54, 900, 24),
            "P play/pause   R reset   1 manual   2 impact   3 intervene   4 aftermath   " +
            "I intervene-mode   [ ] speed", style);
    }
}
