using UnityEngine;

public class PlayerRaycaster : MonoBehaviour
{
    public float interactionDistance = 5f; // How far the player can reach
    private InteractableEvidence currentTarget;

    void Update()
    {
        // use the ray from the center of the screen (the crosshair)
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // pew beem go
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // check if hit
            InteractableEvidence interactable = hit.collider.GetComponent<InteractableEvidence>();

            if (interactable != null)
            {
                // If we are looking at a NEW object, turn off the old one first
                if (currentTarget != interactable && currentTarget != null)
                {
                    currentTarget.HighlightOff();
                }

                currentTarget = interactable;
                currentTarget.HighlightOn();

                // Check for mouse click 
                if (Input.GetMouseButtonDown(0))
                {
                    currentTarget.Interact();
                }
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.HighlightOff();
            currentTarget = null;
        }
    }
}