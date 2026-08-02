using UnityEngine;

public class TrafficLightVisual : MonoBehaviour
{
    public enum LightColor { Red, Yellow, Green }

    [Header("Light Bulbs (GameObjects)")]
    public GameObject redBulb;
    public GameObject yellowBulb;
    public GameObject greenBulb;

    [Header("Physics Blocker")]
    [Tooltip("The invisible Box Collider that stops ambient cars")]
    public Collider stopLineCollider;

    public void SetLight(LightColor color)
    {
        // force all bulbs off first
        if (redBulb != null) redBulb.SetActive(false);
        if (yellowBulb != null) yellowBulb.SetActive(false);
        if (greenBulb != null) greenBulb.SetActive(false);

        // turn on the specific bulb AND toggle the stop line
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