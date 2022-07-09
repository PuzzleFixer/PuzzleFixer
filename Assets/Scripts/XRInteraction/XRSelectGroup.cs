using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRSelectGroup : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable = null;
    
    void Start()
    {
        xrGrabInteractable = GetComponent<XRGrabInteractable>();
        xrGrabInteractable.trackPosition = false;
        xrGrabInteractable.trackRotation = false;
        xrGrabInteractable.selectEntered.AddListener(SwitchSelect);
    }

    void Update()
    {
        
    }

    private void SwitchSelect(SelectEnterEventArgs interactor)
    {
        Debug.Log("select");
    }
}
