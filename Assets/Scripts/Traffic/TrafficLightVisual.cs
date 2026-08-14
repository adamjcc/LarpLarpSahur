/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * TrafficLightVisual.cs
 * Switches the bulbs on one traffic light and toggles its stop line.
 */

using UnityEngine;

/// <summary>
/// Controls the visual bulb objects and the physical stop-line collider for a single traffic light
/// </summary>
public class TrafficLightVisual : MonoBehaviour
{
    /// <summary>
    /// Represents the possible color states of the traffic light
    /// </summary>
    public enum LightColor { Red, Yellow, Green }

    [Header("Light Bulbs (GameObjects)")]
    /// <summary>
    /// The GameObject representing the illuminated red bulb
    /// </summary>
    public GameObject redBulb;
    
    /// <summary>
    /// The GameObject representing the illuminated yellow bulb
    /// </summary>
    public GameObject yellowBulb;
    
    /// <summary>
    /// The GameObject representing the illuminated green bulb
    /// </summary>
    public GameObject greenBulb;

    [Header("Physics Blocker")]
    /// <summary>
    /// The invisible Box Collider that physically stops ambient cars from entering the intersection
    /// </summary>
    [Tooltip("The invisible Box Collider that stops ambient cars")]
    public Collider stopLineCollider;

    /// <summary>
    /// Updates the visual state of the traffic light and toggles the physics collider to stop or allow traffic
    /// </summary>
    /// <param name="color">The target color state to apply to the light</param>
    public void SetLight(LightColor color)
    {
        // Force all bulbs off first to prevent multiple lights being active simultaneously
        if (redBulb != null) redBulb.SetActive(false);
        if (yellowBulb != null) yellowBulb.SetActive(false);
        if (greenBulb != null) greenBulb.SetActive(false);

        // Turn on the specific bulb AND toggle the stop line
        switch (color)
        {
            case LightColor.Red: 
                if (redBulb != null) redBulb.SetActive(true); 
                // Enable collider to block cars
                if (stopLineCollider != null) stopLineCollider.enabled = true; 
                break;
                
            case LightColor.Yellow: 
                if (yellowBulb != null) yellowBulb.SetActive(true); 
                // Enable collider so cars stop for yellow (safest for ambient traffic)
                if (stopLineCollider != null) stopLineCollider.enabled = true; 
                break;
                
            case LightColor.Green: 
                if (greenBulb != null) greenBulb.SetActive(true); 
                // Disable collider so cars can drive through
                if (stopLineCollider != null) stopLineCollider.enabled = false; 
                break;
        }
    }
}