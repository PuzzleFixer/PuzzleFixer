using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XRInteraction;

public class XROverviewSelect : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable = null;
    private XRWorkflowControl xrWorkflowControl;
    private RightHandWorkflow rightHandWorkflow;
    private SmallMultiplesAround smSkt;
    private SwitchSkeleton switchSkt;
    private SelectTarget selectTarget;

    private GameObject grabPoint;
    private Transform[] sktHolder;
    private Vector3[][] gLocalSkeletonNodePoint;
    private Vector3[][][] gLocalSkeletonCurvePoint;

    private bool grabSign = false;
    

    void Start()
    {
        xrWorkflowControl = GameObject.Find("XRWorkflowControl").GetComponent<XRWorkflowControl>();
        try
        {
            rightHandWorkflow =
            GameObject.Find("RightHand Controller").transform.Find("HandPrefab")
            .GetComponent<RightHandWorkflow>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
        smSkt = GameObject.Find("NextSkeleton").GetComponent<SmallMultiplesAround>();
        switchSkt = GameObject.Find("SwitchSkeleton").GetComponent<SwitchSkeleton>();
        selectTarget = GameObject.Find("SelectTarget").GetComponent<SelectTarget>();

        xrGrabInteractable = GetComponent<XRGrabInteractable>();

        xrGrabInteractable.selectEntered.AddListener(HoldingEnter);
        xrGrabInteractable.selectExited.AddListener(ExitHolding);
    }

    private void LateUpdate()
    {
        // transform trigger
        if (this.transform.hasChanged)
        {
            TransformSkeleton();
            this.transform.hasChanged = false;
        }
    }

    private void HoldingEnter(SelectEnterEventArgs interactor)
    {
        if ((xrWorkflowControl.step < 5 && !xrWorkflowControl.stepFlag)
            || ((xrWorkflowControl.step == 7 || xrWorkflowControl.step == 71) && xrWorkflowControl.stepFlag))
        {
            Debug.Log("overview grab XRSelect: enter");
            
            grabPoint = new GameObject("GrabPoint");
            grabPoint.transform.parent = interactor.interactableObject.transform;
            grabPoint.transform.position = interactor.interactorObject.transform.position;
            grabPoint.transform.rotation = interactor.interactorObject.transform.rotation;
            xrGrabInteractable.attachTransform = grabPoint.transform;

            grabSign = true;
        }
    }

    private void ExitHolding(SelectExitEventArgs interactor)
    {
        if ((xrWorkflowControl.step < 5 && !xrWorkflowControl.stepFlag)
            || ((xrWorkflowControl.step == 7 || xrWorkflowControl.step == 71) && xrWorkflowControl.stepFlag))
        {
            Debug.Log("overview grab XRSelect: exit");
            Destroy(grabPoint);

            grabSign = false;
        }
    }

    public void InitializeSkeletonRotation(GameObject container)
    {
        int groupCount = smSkt.gSkeletonCenter.Count;
        sktHolder = new Transform[groupCount + 1];
        GameObject gSktHolder;
        for (int i = 0; i < groupCount; i++)
        {
            gSktHolder = new GameObject("sktHolder" + i);
            gSktHolder.transform.position = smSkt.gSkeletonCenter[i];
            gSktHolder.transform.localRotation = this.transform.localRotation;
            gSktHolder.transform.parent = container.transform;
            sktHolder[i] = gSktHolder.transform;
        }
        gSktHolder = new GameObject("sktHolder" + groupCount);
        gSktHolder.transform.position = new Vector3(0, 0, smSkt.planeDepth);
        gSktHolder.transform.localRotation = this.transform.localRotation;
        gSktHolder.transform.parent = container.transform;
        sktHolder[groupCount] = gSktHolder.transform;
        
        gLocalSkeletonNodePoint = new Vector3[smSkt.gSkeletonNodePoint.Count][];
        gLocalSkeletonCurvePoint = new Vector3[smSkt.gSkeletonCurvePoint.Count][][];
        for (int i = 0; i < smSkt.gSkeletonNodePoint.Count; i++)
        {
            gLocalSkeletonNodePoint[i] = new Vector3[smSkt.gSkeletonNodePoint[i].Count];
            for (int j = 0; j < smSkt.gSkeletonNodePoint[i].Count; j++)
                gLocalSkeletonNodePoint[i][j] =
                    container.transform.GetChild(i).InverseTransformPoint(smSkt.gSkeletonNodePoint[i][j]);
        }
        for (int i = 0; i < smSkt.gSkeletonCurvePoint.Count; i++)
        {
            gLocalSkeletonCurvePoint[i] = new Vector3[smSkt.gSkeletonCurvePoint[i].Count][];
            for (int j = 0; j < smSkt.gSkeletonCurvePoint[i].Count; j++)
                gLocalSkeletonCurvePoint[i][j] = new Vector3[] {
                    container.transform.GetChild(i).InverseTransformPoint(smSkt.gSkeletonCurvePoint[i][j][0]),
                    container.transform.GetChild(i).InverseTransformPoint(smSkt.gSkeletonCurvePoint[i][j][1])
                };
        }

        this.transform.hasChanged = false;
    }

    private void TransformSkeleton()
    {
        if (grabSign && (xrWorkflowControl.step == 7 || xrWorkflowControl.step == 71) && xrWorkflowControl.stepFlag)
        {
            for (int i = 0; i < sktHolder.Length - 1; i++)
            {
                sktHolder[i].position = smSkt.gSkeletonCenter[i];
                sktHolder[i].localRotation = this.transform.localRotation;
            }
            sktHolder[sktHolder.Length - 1].localRotation = this.transform.localRotation;

            for (int i = 0; i < sktHolder.Length - 1; i++)
            {
                if (i == switchSkt.currentCenterTargetIdx)
                    sktHolder[i].position += switchSkt.moveAway;
                
                for (int j = 0; j < smSkt.gSkeletonNodePoint[i].Count; j++)
                    smSkt.gSkeletonNodePoint[i][j] = sktHolder[i].TransformPoint(gLocalSkeletonNodePoint[i][j]);

                for (int j = 0; j < smSkt.gSkeletonCurvePoint[i].Count; j++)
                {
                    smSkt.gSkeletonCurvePoint[i][j][0] = sktHolder[i].TransformPoint(gLocalSkeletonCurvePoint[i][j][0]);
                    smSkt.gSkeletonCurvePoint[i][j][1] = sktHolder[i].TransformPoint(gLocalSkeletonCurvePoint[i][j][1]);
                }
            }
            smSkt.UpdateGroupSmallMultiples();

            int selectIdx = switchSkt.currentCenterTargetIdx;
            for (int j = 0; j < switchSkt.selectNodePoint.Length; j++)
                switchSkt.selectNodePoint[j] = sktHolder[sktHolder.Length - 1].
                    TransformPoint(gLocalSkeletonNodePoint[selectIdx][j] / smSkt.smallSkeletonScale);
            for (int j = 0; j < switchSkt.selectCurvePoint.Length; j++)
            {
                switchSkt.selectCurvePoint[j][0] = sktHolder[sktHolder.Length - 1].
                    TransformPoint(gLocalSkeletonCurvePoint[selectIdx][j][0] / smSkt.smallSkeletonScale);
                switchSkt.selectCurvePoint[j][1] = sktHolder[sktHolder.Length - 1].
                    TransformPoint(gLocalSkeletonCurvePoint[selectIdx][j][1] / smSkt.smallSkeletonScale);
            }
            
            Transform pivotTransform = selectTarget.groupSelected.transform.GetChild(0);
            Vector3 pivot = GroupManager.GetFragmentsCenter(pivotTransform);
            int pivotSktIdx = smSkt.gNodeFragName[selectIdx].FindIndex(n => n == pivotTransform.name);
            Vector3 shift = pivot - switchSkt.selectNodePoint[pivotSktIdx];
            for (int j = 0; j < switchSkt.selectNodePoint.Length; j++)
                switchSkt.selectNodePoint[j] += shift;
            for (int j = 0; j < switchSkt.selectCurvePoint.Length; j++)
            {
                switchSkt.selectCurvePoint[j][0] += shift;
                switchSkt.selectCurvePoint[j][1] += shift;
            }
            switchSkt.UpdateCenterSkeleton();
        }
    }
}
