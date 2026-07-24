using UnityEngine;

public interface IInteractable
{
    void Interact();
    string GetInteractText(); // Optional: To show "Press E to Open"
}