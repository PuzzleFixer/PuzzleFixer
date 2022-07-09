using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class LeftHand : MonoBehaviour
{

    private Hand hand;
    private XRWorkflowControl xrWorkflowControl;
    private Transform rig;

    private bool LastTriggerClicked = false;
    private float LastTriggerValue = 0;

    [SerializeField] private float shiftMultiple = 5f;

    private Vector3 lastPosition;

    private ContinuousMovement movement;

    void Start()
    {
        hand = GetComponent<Hand>();
        xrWorkflowControl = GameObject.Find("XRWorkflowControl").GetComponent<XRWorkflowControl>();
        rig = GameObject.Find("XR Rig").transform;
        movement = rig.GetComponent<ContinuousMovement>();
        
    }
    
    private void FixedUpdate()
    {
        if (hand.targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerClicked))
        {
            if (triggerClicked)
            {
                if (hand.targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) 
                    && triggerValue > 0.8f)
                {
                    LastTriggerClicked = true;
                    LastTriggerValue = triggerValue;

                    {
                        Vector3 currentPos = transform.position;
                        if (lastPosition != Vector3.zero)
                        {
                            Vector3 shift = (lastPosition - currentPos) * shiftMultiple;
                            movement.SetTranslate(shift);
                            lastPosition = transform.position;
                        }
                        else
                            lastPosition = currentPos;
                    }
                }
            }
            else
            {
                if (LastTriggerClicked)
                {
                    lastPosition = Vector3.zero;
                }

                LastTriggerClicked = false;
            }
        }

    }
}
