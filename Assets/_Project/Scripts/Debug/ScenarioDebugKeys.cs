using UnityEngine;

/// Temporary keyboard controls + an on-screen readout, so you can drive the scenario clock
/// by hand while building. Put this on the same object as the ScenarioRunner.
///
/// Remove it (or untick Show On Screen) before you record any submission video.
///
///   P          play / pause
///   R          reset to scenario time 0
///   1          seek to Seek Target A
///   2          seek to Seek Target B
///   [  and  ]  slow down / speed up the scenario
public class ScenarioDebugKeys : MonoBehaviour
{
    [SerializeField] private ScenarioRunner runner;

    [Header("Seek shortcuts (scenario seconds)")]
    [SerializeField] private float seekTargetA = 3f;
    [SerializeField] private float seekTargetB = 7f;

    [Header("Display")]
    [SerializeField] private bool showOnScreen = true;

    private GUIStyle style;

    // Reset() runs when you first add the component in the editor. This auto-fills the
    // runner slot if it's on the same GameObject, so you can't forget it.
    private void Reset()
    {
        runner = GetComponent<ScenarioRunner>();
    }

    private void Awake()
    {
        if (runner == null) runner = GetComponent<ScenarioRunner>();
        if (runner == null) runner = FindFirstObjectByType<ScenarioRunner>();
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
        if (Input.GetKeyDown(KeyCode.Alpha2)) runner.SeekTo(seekTargetB);

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            runner.SetTimeScale(Mathf.Max(0.02f, runner.TimeScale - 0.02f));
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            runner.SetTimeScale(Mathf.Min(2f, runner.TimeScale + 0.02f));
        }
    }

    private void OnGUI()
    {
        if (!showOnScreen || runner == null) return;

        // build the style once; the default label style is dark and unreadable at night
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };
        }

        string state = runner.IsPlaying ? "PLAYING" : "PAUSED";

        GUI.Label(new Rect(12, 10, 700, 24),
            $"ScenarioTime  {runner.ScenarioTime:F2} s     " +
            $"TimeScale  {runner.TimeScale:F2}     " +
            $"{state}     Actors: {runner.ActorCount}", style);

        GUI.Label(new Rect(12, 32, 700, 24),
            $"P play/pause    R reset    1 seek {seekTargetA:F1}s    " +
            $"2 seek {seekTargetB:F1}s    [ ] time scale", style);
    }
}
