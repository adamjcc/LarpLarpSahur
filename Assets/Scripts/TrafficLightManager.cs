using UnityEngine;

public class TrafficLightManager : MonoBehaviour
{
    [Header("Stop Line Colliders")]
    [Tooltip("Invisible walls for North/South traffic")]
    public GameObject[] northSouthStopLines; 
    
    [Tooltip("Invisible walls for East/West traffic")]
    public GameObject[] eastWestStopLines;   

    [Header("Timing")]
    public float greenLightDuration = 7f;
    
    private float timer;
    private bool isNorthSouthGreen = true;

    void Start()
    {
        timer = greenLightDuration;
        UpdateLights();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        
        if (timer <= 0)
        {
            isNorthSouthGreen = !isNorthSouthGreen;
            timer = greenLightDuration;
            UpdateLights();
        }
    }

    void UpdateLights()
    {
        // When North/South is green, turn OFF their walls (so cars can drive)
        foreach (var line in northSouthStopLines)
        {
            line.SetActive(!isNorthSouthGreen);
        }

        // When North/South is green, turn ON East/West walls (so cross-traffic brakes)
        foreach (var line in eastWestStopLines)
        {
            line.SetActive(isNorthSouthGreen);
        }
    }
}