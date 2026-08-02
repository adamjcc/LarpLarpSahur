using UnityEngine;

public class TrafficLightVisual : MonoBehaviour
{
    [Header("Light Bulbs (GameObjects)")]
    public GameObject redBulb;
    public GameObject yellowBulb;
    public GameObject greenBulb;

    // gets called by your main Manager script
    public void SetLight(string color)
    {
        redBulb.SetActive(color == "Red");
        yellowBulb.SetActive(color == "Yellow");
        greenBulb.SetActive(color == "Green");
    }
}