using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    // add or remove an InteractionEvent component to this gameobject
    public bool useEvents;
    // msg displayed when looked at
    public string promptMessage;
    
    // called from player
    public void BaseInteract()
    {
        if(useEvents)
            GetComponent<InteractionEvent>().OnInteract.Invoke();
        Interact();
    }
    protected virtual void Interact()
    {
        // template function for subclasses
    }


}
