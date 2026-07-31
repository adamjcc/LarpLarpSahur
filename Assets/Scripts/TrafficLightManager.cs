using UnityEngine;

public class TrafficLightManager : MonoBehaviour
{
    [Header("Stop Line Colliders")]
    
    public GameObject[] northSouthStopLines; 
    
    public GameObject[] eastWestStopLines;   

    [Header("Timing Configuration")]
    public float greenLightDuration = 7f;
    // can add a yellow light delay later if got time lol
    
    private float timer;
    private bool isNorthSouthGreen = true;

    void Start()
    {
        timer = greenLightDuration;
        UpdateLights();
    }

    void Update()
    {
        // Simple countdown timer
        timer -= Time.deltaTime;
        
        if (timer <= 0)
        {
            // Toggle the state
            isNorthSouthGreen = !isNorthSouthGreen;
            timer = greenLightDuration;
            UpdateLights();
        }
    }

    void UpdateLights()
    {
        // If North/South is green, turn OFF their stop lines (so they can drive)
        // At the same time, turn ON the East/West stop lines (so they brake)
        
        foreach (var line in northSouthStopLines)
        {
            line.SetActive(!isNorthSouthGreen);
        }

        foreach (var line in eastWestStopLines)
        {
            line.SetActive(isNorthSouthGreen);
        }
    }
}