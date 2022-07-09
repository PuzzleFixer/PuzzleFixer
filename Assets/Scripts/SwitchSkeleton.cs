using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataStructures.ViliWonka.KDTree;
using UnityEngine;

public class SwitchSkeleton : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool switchSkeleton = false;
    private bool switchSkeletonLast = false;

    [Header("select")]
    [SerializeField] public bool selectCurrentSkeleton = false;
    public List<int> selectedSmallSkeletonID;   

    [Header("Panning Speed")]
    [SerializeField] private float panSpeed = 0.5f;

    [Header("other")]
    [SerializeField] int maxShapeNum = 10;

    public int currentCenterTargetIdx;           

    private SmallMultiplesAround smSkt;
    private NodeVisualizer selectSkeletonNodeVizor;     
    private CurveVisualizer selectSkeletonCurveVizor;


    public Vector3[] selectNodePoint;
    public Vector4[] selectNodeColor;
    public Vector3[][] selectCurvePoint;
    public Vector4[] selectCurveColor;

    public Vector3 moveAway = new Vector3(0, 1000, 0);

    public Vector3 panningVec = Vector3.zero;   

    public Vector3 panningDir = Vector3.zero;   
    
    [Header("Show Debug")]
    public bool showDebug = false;
    List<Vector3> debugPos = new List<Vector3>();
    List<Color> debugPosColor = new List<Color>();

    private void Start()
    {
        smSkt = GameObject.Find("NextSkeleton").GetComponent<SmallMultiplesAround>();
        selectSkeletonNodeVizor = transform.Find("SelectSkeletonNode").GetComponent<NodeVisualizer>();
        selectSkeletonCurveVizor = transform.Find("SelectSkeletonCurve").GetComponent<CurveVisualizer>();
        currentCenterTargetIdx = -1;
        selectedSmallSkeletonID = new List<int>();
    }

    void Update()
    {
        if (switchSkeleton)
        {
            if (switchSkeletonLast == false)  
            {
                if (smSkt.smallMultiplesAround == true)
                {
                    Debug.Log("pan the controller to switch candidate skeleton...");
                    fragmentRef = FragmentData.fragments;
                    selectedSmallSkeletonID = new List<int>();

                }
                else
                {
                    Debug.Log("SwitchSkeleton error: please generate skeleton candidate in SmallMultiplesAround first!");
                    switchSkeleton = false;
                    return;
                }
            }

            if (selectCurrentSkeleton)
            {
                selectedSmallSkeletonID.Add(currentCenterTargetIdx);

                selectCurrentSkeleton = false;
            }

            PanningSmallMultiplesFinishController();
            PanningSmallMultiplesKeyBoard();

            switchSkeletonLast = true;
        }
        else if (switchSkeletonLast)  
        {
            RecoverClusterSkeleton();
            selectSkeletonNodeVizor.ClearNodes();
            selectSkeletonCurveVizor.ClearCurve();
            currentCenterTargetIdx = -1;
            selectedSmallSkeletonID = selectedSmallSkeletonID.Distinct().ToList();
            PanningSmallMultiplesFinishKeyBoard();
            panningVec = Vector3.zero;
            CacheFilteredOrignalSkeleton();

            switchSkeletonLast = false;
        }
    }


    public void SwitchTargetConnectSkeleton(int classi, bool alignSmallSkeleton)
    {
        RecoverClusterSkeleton();


        if (alignSmallSkeleton)
        {
            Vector3 panVec = -smSkt.gSkeletonCenter[classi];
            panVec.z = 0;
            int classNum = smSkt.gSkeletonCenter.Count;
            for (int i = 0; i < classNum; i++)
            {
                smSkt.gSkeletonCenter[i] += panVec;
                int nn = smSkt.gSkeletonNodePoint[i].Count;
                for (int j = 0; j < nn; j++)
                    smSkt.gSkeletonNodePoint[i][j] += panVec;
                int cn = smSkt.gSkeletonCurvePoint[i].Count;
                for (int j = 0; j < cn; j++)
                {
                    smSkt.gSkeletonCurvePoint[i][j][0] += panVec;
                    smSkt.gSkeletonCurvePoint[i][j][1] += panVec;
                }
            }
            panningVec += panVec;
        }


        selectNodePoint = smSkt.gSkeletonNodePoint[classi].ToArray();
        selectNodeColor = smSkt.gSkeletonNodeColor[classi].ToArray();
        selectCurvePoint = smSkt.gSkeletonCurvePoint[classi].Select(a => a.ToArray()).ToArray();
        selectCurveColor = smSkt.gSkeletonCurveColor[classi].ToArray();

        Vector3 center = Vector3.zero;
        for (int i = 0; i < selectNodePoint.Length; i++)
            center += selectNodePoint[i];
        center /= selectNodePoint.Length;
        float rescale = 1.0f / smSkt.smallSkeletonScale;
        for (int i = 0; i < selectNodePoint.Length; i++)
            selectNodePoint[i] = (selectNodePoint[i] - center) * rescale + center;
        for (int i = 0; i < selectCurvePoint.Length; i++)
        {
            selectCurvePoint[i][0] = (selectCurvePoint[i][0] - center) * rescale + center;
            selectCurvePoint[i][1] = (selectCurvePoint[i][1] - center) * rescale + center;
        }

        Fragment frag = FragmentData.GetFragmentByName(smSkt.gNodeFragName[classi][selectNodePoint.Length - 1]);
        Transform fragDef = frag.objMesh.transform.Find(FragmentTypes.Default);
        Vector3 fragCenter = fragDef.TransformPoint(fragDef.GetComponent<MeshFilter>().mesh.bounds.center);
        Vector3 shift = fragCenter - selectNodePoint[selectNodePoint.Length - 1];

        for (int i = 0; i < selectNodePoint.Length; i++)
            selectNodePoint[i] += shift;
        for (int i = 0; i < selectCurvePoint.Length; i++)
        {
            selectCurvePoint[i][0] += shift;
            selectCurvePoint[i][1] += shift;
        }

        UpdateCenterSkeleton();


        int nodeNum = smSkt.gSkeletonNodePoint[classi].Count;
        for (int i = 0; i < nodeNum; i++)
            smSkt.gSkeletonNodePoint[classi][i] += moveAway;
        int curveNum = smSkt.gSkeletonCurvePoint[classi].Count;
        for (int i = 0; i < curveNum; i++)
        {
            smSkt.gSkeletonCurvePoint[classi][i][0] += moveAway;
            smSkt.gSkeletonCurvePoint[classi][i][1] += moveAway;
        }
        smSkt.UpdateGroupSmallMultiples();


        int sgi = Array.FindIndex(smSkt.candidateClassTag, t => t == classi);
        var targetFragInfo = smSkt.selectGroupMatch[sgi].targetLinks.Select(l => l.Key).ToArray();
        for (int i = 0; i < targetFragInfo.Length; i++)
        {
            Color faceColor = smSkt.gSkeletonCurveColor[classi][0];
            MeshColor.DrawFaceColor(FragmentData.GetFragmentByName(targetFragInfo[i].fragmentID),
                targetFragInfo[i].face, faceColor);
        }
    }
    
    private void PanningSmallMultiplesFinishController()
    {
        Vector3 direction = new Vector3(panningDir.x, panningDir.y, 0) * panSpeed;
        
        if (direction == Vector3.zero)
            return;

        float[] dis = new float[smSkt.gSkeletonCenter.Count];

        for (int i = 0; i < smSkt.gSkeletonCenter.Count; i++)
        {
            smSkt.gSkeletonCenter[i] += direction;
            dis[i] = smSkt.gSkeletonCenter[i].sqrMagnitude;
        }


        bool outofBound = smSkt.gSkeletonCenter.Min(s => s.x) > 0.1f || smSkt.gSkeletonCenter.Min(s => s.y) > 0.1f
                                                                     || smSkt.gSkeletonCenter.Max(s => s.x) < -0.1f || smSkt.gSkeletonCenter.Max(s => s.y) < -0.1f;
        if (outofBound)
        {
            for (int i = 0; i < smSkt.gSkeletonCenter.Count; i++)
                smSkt.gSkeletonCenter[i] -= direction;
            return;
        }

        for (int i = 0; i < smSkt.gSkeletonNodePoint.Count; i++)
        for (int j = 0; j < smSkt.gSkeletonNodePoint[i].Count; j++)
            smSkt.gSkeletonNodePoint[i][j] += direction;

        for (int i = 0; i < smSkt.gSkeletonCurvePoint.Count; i++)
        for (int j = 0; j < smSkt.gSkeletonCurvePoint[i].Count; j++)
        {
            smSkt.gSkeletonCurvePoint[i][j][0] += direction;
            smSkt.gSkeletonCurvePoint[i][j][1] += direction;
        }

        panningVec += direction;
        smSkt.UpdateGroupSmallMultiples();


        int closesti = Array.FindIndex(dis, d => d == dis.Min());
        if (closesti != currentCenterTargetIdx)
        {
            SwitchTargetConnectSkeleton(closesti, false);
            currentCenterTargetIdx = closesti;
        }
    }

    private void PanningSmallMultiplesKeyBoard()
    {
        Vector3 speedUp = Vector3.zero;
        Vector3 speedDown = Vector3.zero;
        Vector3 speedLeft = Vector3.zero;
        Vector3 speedRight = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) speedUp = Vector3.up;
        if (Input.GetKey(KeyCode.S)) speedDown = Vector3.down;
        if (Input.GetKey(KeyCode.A)) speedLeft = -Vector3.left;
        if (Input.GetKey(KeyCode.D)) speedRight = -Vector3.right;

        Vector3 direction = (speedUp + speedDown + speedLeft + speedRight) * panSpeed;

        if (direction == Vector3.zero)
            return;

        float[] dis = new float[smSkt.gSkeletonCenter.Count];

        for (int i = 0; i < smSkt.gSkeletonCenter.Count; i++)
        {
            smSkt.gSkeletonCenter[i] += direction;
            dis[i] = smSkt.gSkeletonCenter[i].sqrMagnitude;
        }


        bool outofBound = smSkt.gSkeletonCenter.Min(s => s.x) > 0.1f || smSkt.gSkeletonCenter.Min(s => s.y) > 0.1f
            || smSkt.gSkeletonCenter.Max(s => s.x) < -0.1f || smSkt.gSkeletonCenter.Max(s => s.y) < -0.1f;
        if (outofBound)
        {
            for (int i = 0; i < smSkt.gSkeletonCenter.Count; i++)
                smSkt.gSkeletonCenter[i] -= direction;
            return;
        }

        for (int i = 0; i < smSkt.gSkeletonNodePoint.Count; i++)
            for (int j = 0; j < smSkt.gSkeletonNodePoint[i].Count; j++)
                smSkt.gSkeletonNodePoint[i][j] += direction;

        for (int i = 0; i < smSkt.gSkeletonCurvePoint.Count; i++)
            for (int j = 0; j < smSkt.gSkeletonCurvePoint[i].Count; j++)
            {
                smSkt.gSkeletonCurvePoint[i][j][0] += direction;
                smSkt.gSkeletonCurvePoint[i][j][1] += direction;
            }

        panningVec += direction;
        smSkt.UpdateGroupSmallMultiples();


        int closesti = Array.FindIndex(dis, d => d == dis.Min());
        if (closesti != currentCenterTargetIdx)
        {
            SwitchTargetConnectSkeleton(closesti, false);
            currentCenterTargetIdx = closesti;
        }
    }


    private void PanningSmallMultiplesFinishKeyBoard()
    {
        for (int i = 0; i < smSkt.skeletonCenter.Count; i++)
            smSkt.skeletonCenter[i] += panningVec;

        for (int i = 0; i < smSkt.skeletonNodePoint.Count; i++)
            for (int j = 0; j < smSkt.skeletonNodePoint[i].Count; j++)
                smSkt.skeletonNodePoint[i][j] += panningVec;

        for (int i = 0; i < smSkt.skeletonCurvePoint.Count; i++)
            for (int j = 0; j < smSkt.skeletonCurvePoint[i].Count; j++)
            {
                smSkt.skeletonCurvePoint[i][j][0] += panningVec;
                smSkt.skeletonCurvePoint[i][j][1] += panningVec;
            }
    }

    private void CacheFilteredOrignalSkeleton()
    {
        smSkt.fOrignalIdx = new List<int>();
        smSkt.fNodeFragName = new List<List<string>>();
        smSkt.fSkeletonNodePoint = new List<List<Vector3>>();
        smSkt.fSkeletonNodeColor = new List<List<Vector4>>();
        smSkt.fSkeletonCurvePoint = new List<List<Vector3[]>>();
        smSkt.fSkeletonCurveColor = new List<List<Vector4>>();
        smSkt.fSkeletonCenter = new List<Vector3>();

        int[] classTag = smSkt.candidateClassTag;
        var candidate = new List<KeyValuePair<int, GroupMatch>>();

        for (int i = 0; i < selectedSmallSkeletonID.Count; i++)
        {
            List<int> ski = classTag.Select((tag, idx) => new { tag, idx })
                .Where(t => t.tag == selectedSmallSkeletonID[i])
                .Select(t => t.idx)
                .ToList();

            foreach (var candidatei in ski)
                candidate.Add(new KeyValuePair<int, GroupMatch>(
                    candidatei, smSkt.selectGroupMatch[candidatei]));

        }

        List<int> candidateIdx = new List<int>();
        if (candidate.Count > maxShapeNum)
            candidateIdx = candidate
                .OrderByDescending(c => c.Value.matchScore)
                .Select(c => c.Key)
                .ToList()
                .GetRange(0, maxShapeNum);
        else
            candidateIdx = candidate
                .Select(c => c.Key)
                .ToList();

        foreach (var ci in candidateIdx)
        {
            smSkt.fOrignalIdx.Add(ci);
            smSkt.fNodeFragName.Add(smSkt.nodeFragName[ci].ToList());
            smSkt.fSkeletonNodePoint.Add(smSkt.skeletonNodePoint[ci].ToList());
            smSkt.fSkeletonNodeColor.Add(smSkt.skeletonNodeColor[ci].ToList());
            smSkt.fSkeletonCurvePoint.Add(smSkt.skeletonCurvePoint[ci].ToList());
            smSkt.fSkeletonCurveColor.Add(smSkt.skeletonCurveColor[ci].ToList());
            smSkt.fSkeletonCenter.Add(smSkt.skeletonCenter[ci]);
        }
    }

    private void RecoverClusterSkeleton()
    {
        if (currentCenterTargetIdx >= 0)
        {
            for (int i = 0; i < smSkt.gSkeletonNodePoint[currentCenterTargetIdx].Count; i++)
                smSkt.gSkeletonNodePoint[currentCenterTargetIdx][i] -= moveAway;
            for (int i = 0; i < smSkt.gSkeletonCurvePoint[currentCenterTargetIdx].Count; i++)
            {
                smSkt.gSkeletonCurvePoint[currentCenterTargetIdx][i][0] -= moveAway;
                smSkt.gSkeletonCurvePoint[currentCenterTargetIdx][i][1] -= moveAway;
            }
        }
    }

    public void UpdateCenterSkeleton()
    {
        selectSkeletonNodeVizor.UpdatePointsCloud(selectNodePoint, selectNodeColor);
        selectSkeletonCurveVizor.UpdateCurve(selectCurvePoint, selectCurveColor);
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
