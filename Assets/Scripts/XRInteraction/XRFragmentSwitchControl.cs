using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace XRInteraction
{
    public class XRFragmentSwitchControl : MonoBehaviour
    {
        private XRGrabInteractable xrGrabInteractable;
        private XRWorkflowControl xrWorkflowControl;

        private void Start()
        {
            xrGrabInteractable = new XRGrabInteractable();
            xrWorkflowControl = GameObject.Find("XRWorkflowControl").GetComponent<XRWorkflowControl>();
        }

        private void Update()
        {
            if (xrWorkflowControl.step == 6 || xrWorkflowControl.step == 8 || xrWorkflowControl.step == 10)
            {
                xrGrabInteractable.enabled = true;
            }
            else
            {
                xrGrabInteractable.enabled = false;
            }
        }
    }
}