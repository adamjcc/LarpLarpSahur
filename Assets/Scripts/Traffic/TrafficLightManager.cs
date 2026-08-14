/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * TrafficLightManager.cs
 * Cycles the junction's traffic lights between green, yellow and red
 */

using UnityEngine;

/// <summary>
/// Manages the state and timing of an intersection's traffic lights
/// Controls the transition between Green, Yellow, and Red phases for North/South and East/West directions
/// </summary>
public class TrafficLightManager : MonoBehaviour
{
    [Header("Visual Controllers")]
    /// <summary>
    /// Array of visual controllers for the North and South facing traffic lights.
    /// </summary>
    public TrafficLightVisual[] northSouthLights;
    
    /// <summary>
    /// Array of visual controllers for the East and West facing traffic lights.
    /// </summary>
    public TrafficLightVisual[] eastWestLights;

    [Header("Cycle Timings")]
    /// <summary>
    /// The duration in seconds that a light remains green.
    /// </summary>
    public float greenTime = 5f;
    
    /// <summary>
    /// The duration in seconds that a light remains yellow.
    /// </summary>
    public float yellowTime = 2f;
    
    /// <summary>
    /// The duration in seconds where all lights are red to allow the intersection to clear
    /// </summary>
    public float allRedClearTime = 1f;

    /// <summary>
    /// The internal timer used to track the current phase duration.
    /// </summary>
    private float currentTimer;
    
    /// <summary>
    /// The current phase of the traffic light cycle (0 through 5)
    /// </summary>
    private int trafficState = 0;

    /// <summary>
    /// Initializes the traffic light sequence by forcing the first state when the game starts
    /// </summary>
    void Start()
    {
        // Force the very first state the moment the game hits Play
        SetTrafficState(0);
    }

    /// <summary>
    /// Updates the internal timer every frame and transitions to the next traffic state when the timer reaches zero
    /// </summary>
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

    /// <summary>
    /// Sets the colors for all traffic lights based on the current active phase and resets the phase timer
    /// </summary>
    /// <param name="state">The index of the traffic phase to activate (0 to 5).</param>
    void SetTrafficState(int state)
    {
        switch (state)
        {
            case 0: // N/S Green, E/W Red
                SetLightGroup(northSouthLights, TrafficLightVisual.LightColor.Green);
                SetLightGroup(eastWestLights, TrafficLightVisual.LightColor.Red);
                currentTimer = greenTime;
                break;
                
            case 1: // N/S Yellow
                SetLightGroup(northSouthLights, TrafficLightVisual.LightColor.Yellow);
                currentTimer = yellowTime;
                break;
                
            case 2: // All Red
                SetLightGroup(northSouthLights, TrafficLightVisual.LightColor.Red);
                currentTimer = allRedClearTime;
                break;
                
            case 3: // E/W Green, N/S Red
                SetLightGroup(eastWestLights, TrafficLightVisual.LightColor.Green);
                currentTimer = greenTime;
                break;
                
            case 4: // E/W Yellow
                SetLightGroup(eastWestLights, TrafficLightVisual.LightColor.Yellow);
                currentTimer = yellowTime;
                break;
                
            case 5: // All Red
                SetLightGroup(eastWestLights, TrafficLightVisual.LightColor.Red);
                currentTimer = allRedClearTime;
                break;
        }
    }

    /// <summary>
    /// Helper function to iterate through an array of traffic light visuals and apply a specific color
    /// </summary>
    /// <param name="lightGroup">The array of TrafficLightVisual components to update</param>
    /// <param name="color">The target color state to apply to the group</param>
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