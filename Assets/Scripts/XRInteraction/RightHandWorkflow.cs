using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;


namespace XRInteraction
{
    public class RightHandWorkflow : MonoBehaviour
    {
        public XRWorkflowControl xrWorkflowControl;
        // different stepOrder:
        // 0: Overview
        // 1: CutSkeleton
        // 2: ModifySingleFace
        // 3: AreaSelectFragments
        // 4: ListCandidate
        // 5: SelectTarget
        // 6: SmallMultiplesAround
        // 7: SwitchSkeleton
        // 8: SmallShapeAround
        // 9: SwitchShape
        // 10:CompareShape
        // 11:ConfirmShape
        
        private Hand hand;
        private GameObject Fragments;
        private AreaSelectFragments areaSelectFragments;

        private void Start()
        {
            hand = GetComponent<Hand>();
            Fragments = GameObject.Find("Fragments");
            areaSelectFragments = GameObject.Find("AreaSelectFragments").GetComponent<AreaSelectFragments>();
        }
        

        private bool lastPrimaryAxisClicked = false;
        private Vector2 lastPrimaryAxisInputAxis;

        private bool lastPrimayAxisTouched = false;

        private bool lastTriggerClicked = false;
        private float lastTriggerValue = 0;

        private bool lastGripClicked = false;
        private float lastGripValue = 0;

        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastFwdDir;

        public bool have_fragment = false;
        public bool grabingCompare = false;

        private void FixedUpdate()
        {
            Transform currentT = transform;
            
            if (hand.targetDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool Clicked))
            {
                if (Clicked)
                {
                    if (hand.targetDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 inputAxis))
                    {
                        lastPrimaryAxisClicked = true;
                        lastPrimaryAxisInputAxis = inputAxis;

                        if (xrWorkflowControl.step <= 3 && !xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.StartCutSkeleton();
                        }
                    }
                }
                else
                {
                    if (lastPrimaryAxisClicked)
                    {
                        if (xrWorkflowControl.step <= 3 && xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.CutSkeleton();
                        }

                        if ((xrWorkflowControl.step == 7 || xrWorkflowControl.step == 71) && xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.SelectCurrentSkeleton();
                        }

                        if ((xrWorkflowControl.step == 9 || xrWorkflowControl.step == 91) && xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.SelectCurrentShape();
                        }
                    }

                    lastPrimaryAxisClicked = false;
                }
            }

            if (hand.targetDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerClicked))
            {
                if (triggerClicked)
                {
                    if (hand.targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) &&
                        triggerValue > 0.8f)
                    {
                        lastTriggerClicked = true;
                        lastTriggerValue = triggerValue;
                    }
                }
                else
                {
                    if (lastTriggerClicked)
                    {
                        lastTriggerClicked = false;
                        have_fragment = false;

                        // select fragments to group
                        if (false && xrWorkflowControl.step == 3 && xrWorkflowControl.stepFlag)
                        {
                            GameObject.Find("AreaSelectFragments")
                                .GetComponent<AreaSelectFragments>().GroupFragmentsByVR();
                        }
                        else if (xrWorkflowControl.step == 4 && !xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.StartListCandidate();
                        }
                        else if (xrWorkflowControl.step == 6 && !xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.StartSwitchSkeleton();
                        }
                        else if (xrWorkflowControl.step == 71 && xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.SwitchSkeleton();
                        }
                        else if (xrWorkflowControl.step == 8 && !xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.StartSwitchShape();
                        }
                        else if (xrWorkflowControl.step == 91 && xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.SwitchShape();
                        }
                        else if (xrWorkflowControl.step == 91 && !xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.CompareShape();
                        }
                        else if (xrWorkflowControl.step == 10 && xrWorkflowControl.stepFlag)
                        {
                            xrWorkflowControl.StartConfirmShape();
                        }
                        else if (xrWorkflowControl.step == 11 && xrWorkflowControl.stepFlag)
                        {
                        }
                        
                    }

                }
            }
            
            if (hand.targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryClicked))
            {
                if (primaryClicked)
                {
                    
                }
            }

            if (hand.targetDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool gripClicked))
            {
                if (gripClicked)
                {
                    if (hand.targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue) &&
                        gripValue > 0.1f)
                    {
                        lastGripClicked = true;
                        lastGripValue = gripValue;
                        
                        if ((xrWorkflowControl.step == 7 || xrWorkflowControl.step == 71) &&
                            xrWorkflowControl.stepFlag && !have_fragment)
                        {
                            if (lastPosition != Vector3.zero)
                            {
                                if (currentT.position != lastPosition)
                                {
                                    Vector3 shift = currentT.position - lastPosition;
                                    shift.z = 0;

                                    xrWorkflowControl.ResetPanningDir(shift * 150);
                                    lastPosition = currentT.position;
                                }
                            }
                            else
                            {
                                lastPosition = currentT.position;
                            }
                        }
                        
                        else if ((xrWorkflowControl.step == 9 || xrWorkflowControl.step == 91) &&
                                 xrWorkflowControl.stepFlag && !have_fragment)
                        {
                            if (lastPosition != Vector3.zero)
                            {
                                if (currentT.position != lastPosition)
                                {
                                    Vector3 shift = currentT.position - lastPosition;
                                    shift.z = 0;

                                    xrWorkflowControl.ResetPanningDirShape(shift * 150);
                                    lastPosition = currentT.position;
                                }
                            }
                            else
                            {
                                lastPosition = currentT.position;
                            }
                        }
                        
                        else if (have_fragment && (xrWorkflowControl.step == 7 || xrWorkflowControl.step == 71 
                            || xrWorkflowControl.step == 9 || xrWorkflowControl.step == 91))
                        {
                            if (lastPosition != Vector3.zero)
                            {
                                if (currentT.position != lastPosition)
                                {
                                    Quaternion cr = currentT.rotation;
                                    Quaternion dirShift = new Quaternion(cr.x - lastRotation.x, cr.y - lastRotation.y,
                                        cr.z - lastRotation.z, cr.w - lastRotation.w);
                                    xrWorkflowControl.CandidateRotate(dirShift);
                                    lastPosition = currentT.position;
                                    lastRotation = cr;
                                }
                            }
                            else
                            {
                                lastPosition = currentT.position;
                                lastRotation = currentT.rotation;
                            }
                        }
                        else if (xrWorkflowControl.step < 5 && !xrWorkflowControl.stepFlag)
                        {
                            if (lastPosition != Vector3.zero)
                            {
                                if (currentT.position != lastPosition)
                                {
                                    Vector3 shift = currentT.position - lastPosition;

                                    Quaternion cr = currentT.parent.localRotation;
                                    Quaternion dirShift = Quaternion.Inverse(lastRotation) * cr;
                                    
                                    lastPosition = currentT.position;
                                    lastRotation = cr;
                                    lastFwdDir = currentT.forward;
                                }
                            }
                            else
                            {
                                lastPosition = currentT.position;
                                lastRotation = currentT.parent.localRotation;
                                lastFwdDir = currentT.forward;
                            }
                        }
                    }
                }
                else
                {
                    if (lastGripClicked)
                    {
                        lastGripClicked = false;

                        if ((xrWorkflowControl.step == 7 || xrWorkflowControl.step == 71) && xrWorkflowControl.stepFlag)
                        {
                            lastPosition = Vector3.zero;
                            xrWorkflowControl.ResetPanningDir(lastPosition);
                        }
                        else if ((xrWorkflowControl.step == 9 || xrWorkflowControl.step == 91) &&
                                 xrWorkflowControl.stepFlag)
                        {
                            lastPosition = Vector3.zero;
                            xrWorkflowControl.ResetPanningDirShape(lastPosition);
                        }
                        else if (xrWorkflowControl.step < 5 && !xrWorkflowControl.stepFlag)
                        {
                            lastPosition = Vector3.zero;
                            lastRotation = new Quaternion(0, 0, 0, 0);
                            lastFwdDir = Vector3.forward;
                        }
                    }
                }
            }
        }
    }
}