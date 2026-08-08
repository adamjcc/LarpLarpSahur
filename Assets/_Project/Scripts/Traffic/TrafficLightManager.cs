using UnityEngine;

public class TrafficLightManager : MonoBehaviour
{
    [Header("Visual Controllers")]
    public TrafficLightVisual[] northSouthLights;
    public TrafficLightVisual[] eastWestLights;

    [Header("Cycle Timings")]
    public float greenTime = 5f;
    public float yellowTime = 2f;
    public float allRedClearTime = 1f;

    private float currentTimer;
    private int trafficState = 0;

    void Start()
    {
        // Force the very first state the moment the game hits Play
        SetTrafficState(0);
    }

    void Update()
    {
        // Manually tick down the clock every frame
        currentTimer -= Time.deltaTime;

        // When the timer hits zero, move to the next phase
        if (currentTimer <= 0f)
        {
            trafficState++;
            
            // If we run out of states, loop back to the beginning
            if (trafficState > 5) 
            {
                trafficState = 0;
            }

            SetTrafficState(trafficState);
        }
    }

    // This handles exactly what the lights should do in every phase
    void SetTrafficState(int state)
    {
        switch (state)
        {
            case 0: // PHASE 0: N/S Green, E/W Red
                SetLightGroup(northSouthLights, TrafficLightVisual.LightColor.Green);
                SetLightGroup(eastWestLights, TrafficLightVisual.LightColor.Red);
                currentTimer = greenTime;
                break;
                
            case 1: // PHASE 1: N/S Yellow
                SetLightGroup(northSouthLights, TrafficLightVisual.LightColor.Yellow);
                currentTimer = yellowTime;
                break;
                
            case 2: // PHASE 2: All Red
                SetLightGroup(northSouthLights, TrafficLightVisual.LightColor.Red);
                currentTimer = allRedClearTime;
                break;
                
            case 3: // PHASE 3: E/W Green, N/S Red
                SetLightGroup(eastWestLights, TrafficLightVisual.LightColor.Green);
                currentTimer = greenTime;
                break;
                
            case 4: // PHASE 4: E/W Yellow
                SetLightGroup(eastWestLights, TrafficLightVisual.LightColor.Yellow);
                currentTimer = yellowTime;
                break;
                
            case 5: // PHASE 5: All Red
                SetLightGroup(eastWestLights, TrafficLightVisual.LightColor.Red);
                currentTimer = allRedClearTime;
                break;
        }
    }

    // Helper function to turn on the correct bulbs
    void SetLightGroup(TrafficLightVisual[] lightGroup, TrafficLightVisual.LightColor color)
    {
        foreach (var light in lightGroup)
        {
            if (light != null) 
            {
                light.SetLight(color);
            }
        }
    }
}