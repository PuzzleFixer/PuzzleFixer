using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SmallShapeAround : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool smallShapeAround = false;
    private bool smallShapeAroundLast = false;

    [SerializeField] private float planeScale = 0.5f;
    [SerializeField] float bbSizeScale = 0.4f;

    private SwitchSkeleton switchSkeleton;
    private SwitchShape switchShape;
    private GroupManager groupManager;
    public GameObject Shapes;  
    private SmallMultiplesAround smSkt;

    [Header("Show Debug")]
    public bool showDebug = false;
    List<Vector3> debugPos = new List<Vector3>();
    List<Color> debugPosColor = new List<Color>();

    private void Start()
    {
        switchSkeleton = GameObject.Find("SwitchSkeleton").GetComponent<SwitchSkeleton>();
        switchShape = GameObject.Find("SwitchShape").GetComponent<SwitchShape>();
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
        Shapes = transform.Find("Shapes").gameObject;
        smSkt = GameObject.Find("NextSkeleton").GetComponent<SmallMultiplesAround>();
    }

    void Update()
    {
        if (smallShapeAround)
        {
            if (smallShapeAroundLast == false)
            {
                debugPos.Clear();
                debugPosColor.Clear();
                if (switchSkeleton.selectedSmallSkeletonID.Count > 0)
                {
                    Debug.Log("Showing candidates shape overview...");
                    fragmentRef = FragmentData.fragments;
                    smSkt.ClearSmallMultiples();

                    GameObject fragments = groupManager.gameObject;
                    fragments.GetComponent<XROverviewSelect>().enabled = false;
                    fragments.GetComponent<BoxCollider>().enabled = false;
                    fragments.GetComponent<XRGrabInteractable>().enabled = false;
                    fragments.SetActive(false);
                }
                else
                {
                    Debug.Log("No group skeleton selected! Please select at least one group skeleton in SwitchSkeleton.");
                    smallShapeAround = false;
                    return;
                }

                ShowCandidatesShape();
            }

            smallShapeAroundLast = true;
        }
        else if (smallShapeAroundLast)
        {
            DestroyShapeObject();
            smallShapeAroundLast = false;
        }
    }
    
    private void ShowCandidatesShape()
    {
        List<List<string>> fNodeFragName = smSkt.fNodeFragName;

        var oIdx = smSkt.fOrignalIdx;
        var selectMatch = smSkt.selectGroupMatch;
        for (int i = 0; i < oIdx.Count; i++)
        {
            Debug.Log("MATCH: " + i);
            int idx = oIdx[i];
            for (int li = 0; li < selectMatch[idx].targetLinks.Count; li++)
            {
                var targetKey = selectMatch[idx].targetLinks[li].Key;
                var otherKey = selectMatch[idx].otherLinks[li].Key;
                Debug.Log(targetKey.fragmentID + "-" + targetKey.face + " --> " +
                    otherKey.fragmentID + "-" + otherKey.face);
            }
        }


        for (int i = 0; i < fNodeFragName.Count; i++)
        {
            GameObject candidateObj = new GameObject("candidate" + i);
            candidateObj.transform.parent = Shapes.transform;

            int originalIdx = smSkt.fOrignalIdx[i];
            var fragCenters = smSkt.fragCenterAlignResult[originalIdx];
            Transform[] t = new Transform[fNodeFragName[i].Count];
            for (int k = 0; k < fNodeFragName[i].Count; k++)
            {
                string fragName = fNodeFragName[i][k];
                t[k] = groupManager.CloneFragment(fragName);    
                var fragTransform = Array.Find(fragCenters, f => f.Key == fragName).Value;
                t[k].GetChild(0).position = fragTransform.Key;
                t[k].GetChild(0).rotation = fragTransform.Value;
                t[k].parent = candidateObj.transform;
            }
            MeshColor.OnlyOpenLinkFaceLeft(smSkt.selectGroupMatch[originalIdx],
                t, fNodeFragName[i], smSkt.fSkeletonNodeColor[i]);
            
            candidateObj.transform.localScale = new Vector3(smSkt.smallSkeletonScale, smSkt.smallSkeletonScale, smSkt.smallSkeletonScale);
        }

        
        float scatterLeft = smSkt.fSkeletonCenter.Min(p => p.x);
        float scatterBottom = smSkt.fSkeletonCenter.Min(p => p.y);
        float scatterRight = smSkt.fSkeletonCenter.Max(p => p.x);
        float scatterUp = smSkt.fSkeletonCenter.Max(p => p.y);
        int maxi = -1;
        float maxScore = -1;
        string pivotName = smSkt.targetGroupObj.transform.name;
        if (smSkt.targetGroupObj.name == FragmentTypes.Group)
            pivotName = smSkt.targetGroupObj.transform.GetChild(0).name;

        for (int i = 0; i < fNodeFragName.Count; i++)
        {
            Vector3 newSktCenter = smSkt.fSkeletonCenter[i];
            newSktCenter.x = ((newSktCenter.x - scatterLeft) / (scatterRight - scatterLeft) * smSkt.planeWidth - smSkt.planeWidth / 2) * planeScale;
            newSktCenter.y = ((newSktCenter.y - scatterBottom) / (scatterUp - scatterBottom) * smSkt.planeHeight - smSkt.planeHeight / 2) * planeScale;
            if (float.IsNaN(newSktCenter.x))
                newSktCenter.x = smSkt.fSkeletonCenter[i].x;
            if (float.IsNaN(newSktCenter.y))
                newSktCenter.y = smSkt.fSkeletonCenter[i].y;

            Vector3 shiftVec = newSktCenter - smSkt.fSkeletonCenter[i];
            for (int pi = 0; pi < smSkt.fSkeletonNodePoint[i].Count; pi++)
                smSkt.fSkeletonNodePoint[i][pi] += shiftVec;

            for (int pi = 0; pi < smSkt.fSkeletonCurvePoint[i].Count; pi++)
            {
                smSkt.fSkeletonCurvePoint[i][pi][0] += shiftVec;
                smSkt.fSkeletonCurvePoint[i][pi][1] += shiftVec;
            }
            smSkt.fSkeletonCenter[i] = newSktCenter;


            Transform candidateTran = Shapes.transform.Find("candidate" + i);
            Transform tselect = candidateTran.Find(pivotName + "(Clone)");
            int nodeIdx = smSkt.fNodeFragName[i].FindIndex(n => n == pivotName);
            Vector3 pivot = smSkt.fSkeletonNodePoint[i][nodeIdx];

            Transform fragDef = tselect.GetChild(0).GetChild(0);
            Vector3 tselectPos = fragDef.TransformPoint(fragDef.GetComponent<MeshFilter>().mesh.bounds.center);
            Vector3 tshift = pivot - tselectPos;
            candidateTran.position += tshift;

            int originalIdx = smSkt.fOrignalIdx[i];
            float score = smSkt.selectGroupMatch[originalIdx].matchScore;
            if (score > maxScore)
            {
                maxScore = score;
                maxi = i;
            }
        }


        SmallMultiplesAround.CompactScatterPlot(smSkt.fSkeletonCenter, 
            smSkt.bbSize * bbSizeScale, new Vector3(0, 0, smSkt.planeDepth), out Vector3[] compactCenter);
        for (int i = 0; i < compactCenter.Length; i++)
        {
            Vector3 shift = compactCenter[i] - smSkt.fSkeletonCenter[i];
            for (int ni = 0; ni < smSkt.fSkeletonNodePoint[i].Count; ni++)
                smSkt.fSkeletonNodePoint[i][ni] += shift;
            for (int cni = 0; cni < smSkt.fSkeletonCurvePoint[i].Count; cni++)
            {
                smSkt.fSkeletonCurvePoint[i][cni][0] += shift;
                smSkt.fSkeletonCurvePoint[i][cni][1] += shift;
            }
            smSkt.fSkeletonCenter[i] = compactCenter[i];

            Transform candidateTran = Shapes.transform.Find("candidate" + i);
            candidateTran.position += shift;
        }


        this.transform.Find("Container").GetComponent<XRShapeRotation>().InitializeShapeRotation();


        switchShape.SwitchTargetConnectShape(maxi, true);  
        switchShape.currentTargetShapeIdx = maxi;
    }

    private void DestroyShapeObject()
    {
        foreach (Transform t in Shapes.transform)
            Destroy(t.gameObject);
    }

    private void OnDrawGizmos()
    {
        if (!showDebug)
            return;

        float size = 0.25f;

        for (int i = 0; i < debugPos.Count; i++)
        {
            Gizmos.color = debugPosColor[i];
            Gizmos.DrawSphere(debugPos[i], size);
        }
    }
}
