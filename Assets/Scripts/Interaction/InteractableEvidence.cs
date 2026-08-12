/*
 * Larp Larp Sahur Studios
 * Adam Jamal Clark, Pinili Kian Marcus Valdez, Darryl Yap, Isaiah Tsai
 * Y2S1 IP - Integrated Project
 *
 * InteractableEvidence.cs
 * OLD PROTOTYPE - superseded by HazardInteractable. Kept for reference.
 */

using UnityEngine;

public class InteractableEvidence : MonoBehaviour
{
    public Material normalMaterial;
    public Material highlightMaterial;
    public string evidenceName; // for identification
    
    private Renderer objRenderer;
    private bool isHighlighted = false;

    void Start()
    {
        // dubs the renderer so we can change its materials later for highlight
        objRenderer = GetComponent<Renderer>();
        if (normalMaterial == null) normalMaterial = objRenderer.material;
    }

    // done when player looking at the object
    public void HighlightOn()
    {
        if (!isHighlighted)
        {
            objRenderer.material = highlightMaterial;
            isHighlighted = true;
        }
    }

    // when the player is looking away
    public void HighlightOff()
    {
        if (isHighlighted)
        {
            objRenderer.material = normalMaterial;
            isHighlighted = false;
        }
    }

    //when the player clicks the mouse
    public void Interact()
    {
        Debug.Log("Player interacted with: " + evidenceName);
        // Add your logic here! (e.g., turning off the phone, ringing the bell)
    }
}