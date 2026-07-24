using UnityEngine;
using TMPro; // Add this if you are using TextMeshPro for UI

public class PlayerInteract : MonoBehaviour
{
    public Camera cam;
    public float interactDistance = 3f;
    public LayerMask interactLayer; // Filter to only hit interactables
    public TextMeshProUGUI interactText; // Optional: Drag your UI Text here

    void Update()
    {
        // Shoot a ray from the center of the screen
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Check if the ray hits something within distance
        if (Physics.Raycast(ray, out hit, interactDistance, interactLayer))
        {
            // Try to get the IInteractable component from the object we hit
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // 1. Show UI Text (Optional)
                if (interactText != null)
                {
                    interactText.text = interactable.GetInteractText();
                    interactText.gameObject.SetActive(true);
                }

                // 2. Check for Input
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();
                }
            }
            else
            {
                // Hit something, but it's not interactable
                if (interactText != null) interactText.gameObject.SetActive(false);
            }
        }
        else
        {
            // Hit nothing
            if (interactText != null) interactText.gameObject.SetActive(false);
        }
    }
}