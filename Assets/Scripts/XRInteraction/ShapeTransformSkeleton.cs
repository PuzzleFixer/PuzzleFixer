using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XRInteraction;


public class ShapeTransformSkeleton : MonoBehaviour
{
    public int compareCandidateIdx = -1;
    public Transform parent;

    CompareShape compareShape;
    List<Vector2Int> nodeIdx;
    List<Vector3Int> curveIdx;
    List<Vector3Int> cuesIdx;
    List<Transform> nodeTransform;
    List<Transform> curveTransform;
    List<Transform> cuesTransform;
    Vector3[] localNode;
    Vector3[] localCurve;
    Vector3[] localCues;
    Vector3[] worldNode;
    Vector3[] worldCurve;
    Vector3[] worldCues;

    private RightHandWorkflow rightHandWorkflow = null;
    private CompareShape cmShp;
    private XRGrabInteractable grabComponent;

    private float smoothRotOrign;
    private float tightRotOrign;
    private float smoothPosOrign;
    private float tightPosOrign;
    private float smoothRotForce;
    private float tightRotForce;
    private float smoothPosForce;
    private float tightPosForce;
    private float lastDis;
    private Quaternion lastRot;

    private void Start()
    {
        try
        {
            rightHandWorkflow = GameObject.Find("RightHand Controller").transform.Find("HandPrefab")
            .GetComponent<RightHandWorkflow>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
        
        cmShp = GameObject.Find("CompareShape").GetComponent<CompareShape>();
    }

    private void LateUpdate()
    {
        // transform trigger
        if (this.transform.hasChanged)
        {
            TransformChanged();
            this.transform.hasChanged = false;
        }
    }

    public void Initiate(int candidateIdx)
    {
        parent = transform.parent;
        compareCandidateIdx = candidateIdx;

        nodeIdx = new List<Vector2Int>();
        curveIdx = new List<Vector3Int>();
        cuesIdx = new List<Vector3Int>();
        nodeTransform = new List<Transform>();
        curveTransform = new List<Transform>();
        cuesTransform = new List<Transform>();

        compareShape = GameObject.Find("CompareShape").GetComponent<CompareShape>();
        List<string> candidateFragNames = compareShape.selectNodeFragName[compareCandidateIdx];
        foreach (Transform t in this.transform)
        {
            int cidx = candidateFragNames.FindIndex(n => n == t.name.Substring(0, t.name.Length - 7));
            if (cidx < 0)
                Debug.LogError("unfind frag name in compare candidates!");
            nodeIdx.Add(new Vector2Int(compareCandidateIdx, cidx));
            nodeTransform.Add(t.GetChild(0).GetChild(0));
        }

        List<Vector3> nodes = compareShape.selectNodePoint[compareCandidateIdx];
        List<Vector3> groupNodes = new List<Vector3>();
        foreach (var ni in nodeIdx)
            groupNodes.Add(nodes[ni.y]);
        List<Vector3[]> curveNodes = compareShape.selectCurvePoint[compareCandidateIdx];
        for (int i = 0; i < curveNodes.Count; i++)
        {
            int rlt0 = groupNodes.FindIndex(p => p == curveNodes[i][0]);
            int rlt1 = groupNodes.FindIndex(p => p == curveNodes[i][1]);

            if (rlt0 >= 0 || rlt1 >= 0)
            {
                curveIdx.Add(new Vector3Int(compareCandidateIdx, i, 0));
                curveIdx.Add(new Vector3Int(compareCandidateIdx, i, 1));

                if (rlt0 >= 0 && rlt1 >= 0)
                {
                    curveTransform.Add(nodeTransform[rlt0]);
                    curveTransform.Add(nodeTransform[rlt1]);
                }
                else if (rlt0 >= 0)
                {
                    curveTransform.Add(nodeTransform[rlt0]);
                    curveTransform.Add(nodeTransform[rlt0]);
                }
                else    // rlt1 >= 0
                {
                    curveTransform.Add(nodeTransform[rlt1]);
                    curveTransform.Add(nodeTransform[rlt1]);
                }
            }
        }

        List<Transform> cuesDef = compareShape.matchCues[compareCandidateIdx].def;
        List<Vector3[]> cuesNodes = compareShape.matchCues[compareCandidateIdx].matchCuesPoint;
        for (int i = 0; i < cuesDef.Count; i++)
        {
            int rlt = nodeTransform.FindIndex(p => p == cuesDef[i]);

            if (rlt >= 0)
            {
                cuesIdx.Add(new Vector3Int(compareCandidateIdx, i, 0));
                cuesIdx.Add(new Vector3Int(compareCandidateIdx, i, 1));

                cuesTransform.Add(nodeTransform[rlt]);
                cuesTransform.Add(nodeTransform[rlt]);
            }
        }
        if (cuesTransform.Count == 0)
            Debug.LogError("no cues");

        localNode = new Vector3[nodeIdx.Count];
        localCurve = new Vector3[curveIdx.Count];
        localCues = new Vector3[cuesIdx.Count];
        for (int i = 0; i < nodeIdx.Count; i++)
        {
            Vector3 point = nodes[nodeIdx[i].y];
            localNode[i] = nodeTransform[i].InverseTransformPoint(point);
        }
        for (int i = 0; i < curveIdx.Count; i++)
        {
            Vector3 point = curveNodes[curveIdx[i].y][curveIdx[i].z];
            localCurve[i] = curveTransform[i].InverseTransformPoint(point);
        }
        for (int i = 0; i < cuesIdx.Count; i++)
        {
            Vector3 point = cuesNodes[cuesIdx[i].y][cuesIdx[i].z];
            localCues[i] = cuesTransform[i].InverseTransformPoint(point);
        }

        worldNode = new Vector3[localNode.Length];
        worldCurve = new Vector3[localCurve.Length];
        worldCues = new Vector3[localCues.Length];

        grabComponent = transform.GetComponent<XRGrabInteractable>();
        smoothPosOrign = grabComponent.smoothPositionAmount;
        tightPosOrign = grabComponent.tightenPosition;
        smoothRotOrign = grabComponent.smoothRotationAmount;
        tightRotOrign = grabComponent.tightenRotation;
        lastDis = 0;
        lastRot = this.transform.rotation;
        smoothRotForce = 0.02f;
        tightRotForce = 0.01f;
        smoothPosForce = 0.001f;
        tightPosForce = 0.01f;

        this.transform.hasChanged = false;
    }

    private void TransformChanged()
    {
        if (rightHandWorkflow == null)
            return;

        // set force
        var currentMatchCues = cmShp.matchCues[compareCandidateIdx];
        if (rightHandWorkflow.grabingCompare)
        {
            float curDis = currentMatchCues.GetSumDis();
            if (currentMatchCues.forceNearIdx.Count > 0)
            {
                if (curDis > lastDis)
                {
                    grabComponent.smoothPositionAmount = smoothPosForce;
                    grabComponent.tightenPosition = tightPosForce;
                    grabComponent.smoothRotationAmount = smoothRotForce;
                    grabComponent.tightenRotation = tightRotForce;
                }
                else
                {
                    grabComponent.smoothPositionAmount = smoothPosOrign;
                    grabComponent.tightenPosition = tightPosOrign;
                    grabComponent.smoothRotationAmount = smoothRotOrign;
                    grabComponent.tightenRotation = tightRotOrign;
                }
            }
            else
            {
                grabComponent.smoothPositionAmount = smoothPosOrign;
                grabComponent.tightenPosition = tightPosOrign;
                grabComponent.smoothRotationAmount = smoothRotOrign;
                grabComponent.tightenRotation = tightRotOrign;
            }
            
            lastDis = curDis;
            lastRot = this.transform.rotation;
        }

        // map transform to all nodes
        for (int i = 0; i < worldNode.Length; i++)
            worldNode[i] = nodeTransform[i].TransformPoint(localNode[i]);
        for (int i = 0; i < worldCurve.Length; i++)
            worldCurve[i] = curveTransform[i].TransformPoint(localCurve[i]);
        for (int i = 0; i < worldCues.Length; i++)
            worldCues[i] = cuesTransform[i].TransformPoint(localCues[i]);

        // update nodes
        for (int i = 0; i < nodeIdx.Count; i++)
            compareShape.selectNodePoint[nodeIdx[i].x][nodeIdx[i].y] = worldNode[i];
        for (int i = 0; i < curveIdx.Count; i++)
            compareShape.selectCurvePoint[curveIdx[i].x][curveIdx[i].y][curveIdx[i].z] = worldCurve[i];
        for (int i = 0; i < cuesIdx.Count; i++)
            compareShape.matchCues[cuesIdx[i].x].matchCuesPoint[cuesIdx[i].y][cuesIdx[i].z] = worldCues[i];

        compareShape.UpdateCues(compareCandidateIdx);

        compareShape.UpdateSkeleton();
    }
}
