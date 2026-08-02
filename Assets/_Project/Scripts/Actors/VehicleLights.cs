using UnityEngine;

/// Turns the headlights on and off. Deliberately tiny.
///
/// In Part 6 the headlight button inside the car calls SetOn(true) through a UnityEvent you
/// wire up in the Inspector — no extra code needed.
public class VehicleLights : MonoBehaviour
{
    [Tooltip("Drag the headlight Light components in here. Two spot lights is plenty.")]
    [SerializeField] private Light[] headlights;

    [Tooltip("Optional: an emissive lamp-glass mesh to switch on at the same time.")]
    [SerializeField] private GameObject[] glowObjects;

    [SerializeField] private bool startOn = false;

    public bool IsOn { get; private set; }

    private void Awake()
    {
        SetOn(startOn);
    }

    public void SetOn(bool on)
    {
        IsOn = on;

        for (int i = 0; i < headlights.Length; i++)
        {
            if (headlights[i] != null) headlights[i].enabled = on;
        }

        for (int i = 0; i < glowObjects.Length; i++)
        {
            if (glowObjects[i] != null) glowObjects[i].SetActive(on);
        }
    }

    /// Parameterless version, because UnityEvent buttons in the Inspector are easiest to
    /// wire up when the method takes nothing.
    public void TurnOn() => SetOn(true);

    public void TurnOff() => SetOn(false);
}
