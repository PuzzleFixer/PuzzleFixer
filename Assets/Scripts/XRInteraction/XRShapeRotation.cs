using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XRInteraction;

public class XRShapeRotation : MonoBehaviour
{
    private XRGrabInteractable xrGrabInteractable = null;
    private XRWorkflowControl xrWorkflowControl;
    private SmallMultiplesAround smSkt;
    private SwitchShape switchShp;
    private CompareShape compareShp;

    private GameObject grabPoint;
    private List<Transform> shapeParentPointer;

    private bool grabSign = false;

    private Vector3[][] localNode;
    private Vector3[][][] localCurve;
    private Vector3[] localCenterNode;
    private Vector3[][] localCenterCurve;
    private List<Vector3[]> localCompareNode;
    private List<Vector3[][]> localCompareCurve;
    private List<List<Vector3[]>> localCompareCues;


    void Start()
    {
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
        if ((xrWorkflowControl.step == 9 || xrWorkflowControl.step == 91 || xrWorkflowControl.step == 10) 
            && xrWorkflowControl.stepFlag)
        {
            Debug.Log("shape grab XRSelect: enter");
            
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
        if ((xrWorkflowControl.step == 9 || xrWorkflowControl.step == 91 || xrWorkflowControl.step == 10) 
            && xrWorkflowControl.stepFlag)
        {
            Debug.Log("shape grab XRSelect: exit");
            Destroy(grabPoint);

            grabSign = false;
        }
    }

    public void InitializeShapeRotation()
    {
        xrWorkflowControl = GameObject.Find("XRWorkflowControl").GetComponent<XRWorkflowControl>();
        smSkt = GameObject.Find("NextSkeleton").GetComponent<SmallMultiplesAround>();
        switchShp = GameObject.Find("SwitchShape").GetComponent<SwitchShape>();
        compareShp = GameObject.Find("CompareShape").GetComponent<CompareShape>();

        this.transform.rotation = Quaternion.identity;
        this.transform.position = new Vector3(0, 0, smSkt.planeDepth);

        // enable components
        this.transform.GetComponent<BoxCollider>().enabled = true;
        this.transform.GetComponent<XRGrabInteractable>().enabled = true;
        this.transform.GetComponent<XRShapeRotation>().enabled = true;

        shapeParentPointer = new List<Transform>();

        Transform shapes = this.transform.parent.Find("Shapes");
        
        for (int i = 0; i < shapes.childCount; i++)
        {
            Transform t = shapes.GetChild(i);
            GroupManager.SetFragment2Center(t);
            t.localRotation = this.transform.localRotation;
        }
        shapeParentPointer.Add(shapes);

        shapeParentPointer.Add(GameObject.Find("SwitchShape").transform.Find("SelectShape"));

        shapeParentPointer.Add(GameObject.Find("CompareShape").transform.Find("Shapes"));

        localNode = new Vector3[smSkt.fSkeletonNodePoint.Count][];
        for (int i = 0; i < localNode.Length; i++)
        {
            localNode[i] = new Vector3[smSkt.fSkeletonNodePoint[i].Count];
            for (int j = 0; j < localNode[i].Length; j++)
                localNode[i][j] = shapes.GetChild(i).InverseTransformPoint(smSkt.fSkeletonNodePoint[i][j]);
        }
        localCurve = new Vector3[smSkt.fSkeletonCurvePoint.Count][][];
        for (int i = 0; i < localCurve.Length; i++)
        {
            localCurve[i] = new Vector3[smSkt.fSkeletonCurvePoint[i].Count][];
            for (int j = 0; j < localCurve[i].Length; j++)
                localCurve[i][j] = new Vector3[] {
                    shapes.GetChild(i).InverseTransformPoint(smSkt.fSkeletonCurvePoint[i][j][0]),
                    shapes.GetChild(i).InverseTransformPoint(smSkt.fSkeletonCurvePoint[i][j][1])
                };
        }

        localCompareNode = new List<Vector3[]>();
        localCompareCurve = new List<Vector3[][]>();
        localCompareCues = new List<List<Vector3[]>>();

        this.transform.hasChanged = false;
    }

    private void TransformSkeleton()
    {
        if (grabSign && (xrWorkflowControl.step == 9 || xrWorkflowControl.step == 91 || xrWorkflowControl.step == 10)
            && xrWorkflowControl.stepFlag)
        {
            // 同步transform的localRotation
            for (int i = 0; i < shapeParentPointer.Count; i++)
            {
                for (int j = 0; j < shapeParentPointer[i].childCount; j++)
                {
                    Transform shape = shapeParentPointer[i].GetChild(j);
                    shape.localRotation = this.transform.localRotation;
                }
            }

            Transform smallShapes = shapeParentPointer[0];
            if (smallShapes.childCount > 0)
            {
                for (int i = 0; i < smallShapes.childCount; i++)
                {
                    Transform candidate = smallShapes.GetChild(i);
                    if (i == switchShp.currentTargetShapeIdx)
                        candidate.position += switchShp.moveAway;

                    for (int j = 0; j < smSkt.fSkeletonNodePoint[i].Count; j++)
                        smSkt.fSkeletonNodePoint[i][j] = candidate.TransformPoint(localNode[i][j]);

                    for (int j = 0; j < smSkt.fSkeletonCurvePoint[i].Count; j++)
                    {
                        smSkt.fSkeletonCurvePoint[i][j][0] = candidate.TransformPoint(localCurve[i][j][0]);
                        smSkt.fSkeletonCurvePoint[i][j][1] = candidate.TransformPoint(localCurve[i][j][1]);
                    }

                    if (i == switchShp.currentTargetShapeIdx)
                        candidate.position -= switchShp.moveAway;
                }
                smSkt.UpdateShapeSmallMultiples();
            }

            if (shapeParentPointer[1].childCount > 0)
            {
                Transform center = shapeParentPointer[1].GetChild(0);
                for (int i = 0; i < switchShp.selectNodePoint.Length; i++)
                    switchShp.selectNodePoint[i] = center.TransformPoint(localCenterNode[i]);
                for (int i = 0; i < switchShp.selectCurvePoint.Length; i++)
                {
                    switchShp.selectCurvePoint[i][0] = center.TransformPoint(localCenterCurve[i][0]);
                    switchShp.selectCurvePoint[i][1] = center.TransformPoint(localCenterCurve[i][1]);
                }
                switchShp.UpdateCenterSkeleton();
            }
            
            Transform compare = shapeParentPointer[2];
            if (compare.childCount > 0)
            {
                for (int i = 0; i < compare.childCount; i++)
                {
                    Transform candidate = compare.GetChild(i);
                    for (int j = 0; j < compareShp.selectNodePoint[i].Count; j++)
                        compareShp.selectNodePoint[i][j] = candidate.TransformPoint(localCompareNode[i][j]);
                    for (int j = 0; j < compareShp.selectCurvePoint[i].Count; j++)
                    {
                        compareShp.selectCurvePoint[i][j][0] = candidate.TransformPoint(localCompareCurve[i][j][0]);
                        compareShp.selectCurvePoint[i][j][1] = candidate.TransformPoint(localCompareCurve[i][j][1]);
                    }
                    for (int j = 0; j < localCompareCues[i].Count; j++)
                    {
                        compareShp.matchCues[i].matchCuesPoint[j][0] = candidate.TransformPoint(localCompareCues[i][j][0]);
                        compareShp.matchCues[i].matchCuesPoint[j][1] = candidate.TransformPoint(localCompareCues[i][j][1]);
                    }
                }
                if (xrWorkflowControl.step == 10 && xrWorkflowControl.stepFlag)
                    compareShp.UpdateSkeleton();
            }
        }
    }

    public void UpdateLocalCenterSkeleton(Vector3[] centerNode, Vector3[][] centerCurve, string name)
    {
        Transform center = shapeParentPointer[1].Find(name);

        localCenterNode = new Vector3[centerNode.Length];
        for (int i = 0; i < localCenterNode.Length; i++)
            localCenterNode[i] = center.InverseTransformPoint(centerNode[i]);

        localCenterCurve = new Vector3[centerCurve.Length][];
        for (int i = 0; i < localCenterCurve.Length; i++)
            localCenterCurve[i] = new Vector3[] {
                center.InverseTransformPoint(centerCurve[i][0]),
                center.InverseTransformPoint(centerCurve[i][1])
            };
    }

    public void AddLocalCompareSkeleton(List<Vector3> compareNode, List<Vector3[]> compareCurve,
        List<Vector3[]> compareCues, string name)
    {
        Transform newShape = shapeParentPointer[2].Find(name);

        Vector3[] localNode = new Vector3[compareNode.Count];
        for (int i = 0; i < localNode.Length; i++)
            localNode[i] = newShape.InverseTransformPoint(compareNode[i]);
        localCompareNode.Add(localNode);

        Vector3[][] localCurve = new Vector3[compareCurve.Count][];
        for (int i = 0; i < localCurve.Length; i++)
            localCurve[i] = new Vector3[] {
                newShape.InverseTransformPoint(compareCurve[i][0]),
                newShape.InverseTransformPoint(compareCurve[i][1])
            };
        localCompareCurve.Add(localCurve);

        List<Vector3[]> localCues = new List<Vector3[]>();
        for (int i = 0; i < compareCues.Count; i++)
            localCues.Add(new Vector3[] {
                newShape.InverseTransformPoint(compareCues[i][0]),
                newShape.InverseTransformPoint(compareCues[i][1])
            });
        localCompareCues.Add(localCues);
    }
}
