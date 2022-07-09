using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UIElements;
using MathNet.Numerics;
using System.Collections.Immutable;

public class CompareShape : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool compareShape = false;
    private bool compareShapeLast = false;

    [Header("setups")]
    [SerializeField] private float nearDisTh = 0.5f;
    [SerializeField] private float similarAngleTh = 20;

    [SerializeField] private Vector3 cameraPos = new Vector3(0, -1.2f, 2);
    [SerializeField] private Vector3 cameraEuler = new Vector3(0, -90, 0);

    [SerializeField] float bbSizeScale = 0.8f;

    private GroupManager groupManager;
    private SmallMultiplesAround smSkt;
    private SmallShapeAround smShp;
    private NodeVisualizer compareNodeVizor;
    private CurveVisualizer compareCurveVizor;
    private CurveVisualizer matchCuesVizor;
    [SerializeField] private WIM WorldInMiniature;

    // selected candidate info
    public GameObject Shapes;
    public List<int> selectedShapeID;
                                        

    
    public List<List<string>> selectNodeFragName;
    public List<List<Vector3>> selectNodePoint;
    public List<List<Vector4>> selectNodeColor;
    public List<List<Vector3[]>> selectCurvePoint;
    public List<List<Vector4>> selectCurveColor;
    public List<Vector3> selectCenter;

    
    public class MatchCues  
    {
        public List<Vector3[]> matchCuesPoint; 
        public List<string> fragID;
        public List<int> faceID;
        public List<bool> isTarget;
        public List<Vector4> cuesColor;
        public List<Vector4> cuesColorOrign;
        public List<Vector2Int> currentNearIdx;
        public List<Vector2Int> forceNearIdx;
        public List<Vector2Int> confirmNearIdx;
        public List<Transform> def;
        public List<Vector3> normalLocal;
        static public int interactCompareCandidateIdx;

        public MatchCues()
        {
            matchCuesPoint = new List<Vector3[]>();
            fragID = new List<string>();
            faceID = new List<int>();
            isTarget = new List<bool>();
            cuesColor = new List<Vector4>();
            cuesColorOrign = new List<Vector4>();
            currentNearIdx = new List<Vector2Int>();
            forceNearIdx = new List<Vector2Int>();
            confirmNearIdx = new List<Vector2Int>();
            def = new List<Transform>();
            normalLocal = new List<Vector3>();
            interactCompareCandidateIdx = 0;
        }

        public void GetNearOpenlinks(float disTh, float angleTh, 
            out List<Vector3[]> cuesLink, out List<Vector4> color)
        {
            cuesLink = new List<Vector3[]>();
            color = new List<Vector4>();
            currentNearIdx = new List<Vector2Int>();
            int linkNum = matchCuesPoint.Count;
            int pairNum = linkNum / 2;
            
            var closePair = new List<KeyValuePair<Vector2Int, float>>();
            for (int i = 0; i < pairNum; i++)   // target 
                for (int j = pairNum; j < linkNum; j++)     // other
                {
                    float dis = (matchCuesPoint[i][1] - matchCuesPoint[j][1]).magnitude;
                    float angle = Vector3.Angle(def[i].TransformDirection(normalLocal[i]),
                        -def[j].TransformDirection(normalLocal[j]));
                    if (dis < disTh && angle < angleTh)
                        closePair.Add(new KeyValuePair<Vector2Int, float>(new Vector2Int(i, j), dis));
                }
            
            if (closePair.Count > 0)
            {
                var closePairIdx = closePair.OrderBy(idx => idx.Value).Select(idx => idx.Key).ToList();
                bool[] recordedij = new bool[linkNum];
                for (int cpi = 0; cpi < closePairIdx.Count; cpi++)
                {
                    int i = closePairIdx[cpi].x;
                    int j = closePairIdx[cpi].y;
                    if (recordedij[i] == false && recordedij[j] == false)
                    {
                        cuesLink.Add(new Vector3[] { matchCuesPoint[i][1], matchCuesPoint[j][1] });
                        var nearIdx = new Vector2Int(i, j);
                        if (!forceNearIdx.Contains(nearIdx))
                        {
                            color.Add(cuesColorOrign[i]);
                            color.Add(cuesColorOrign[j]);
                            cuesColor[i] = cuesColorOrign[i];
                            cuesColor[j] = cuesColorOrign[j];
                        }
                        else
                        {
                            color.Add(cuesColor[i]);
                            color.Add(cuesColor[j]);
                        }
                        currentNearIdx.Add(nearIdx);
                        recordedij[i] = true;
                        recordedij[j] = true;
                    }
                }
            }
            var exceptNear = forceNearIdx.Except(currentNearIdx).ToList();
            foreach (var en in exceptNear)
                forceNearIdx.Remove(en);

            if (cuesLink.Count == 0)
            {
                cuesLink.Add(new Vector3[] { Vector3.zero, Vector3.zero });
                color.Add(Vector4.zero);
            }
        }

        public List<Vector2Int[]> GetPairMatchIdx()
        {
            if (currentNearIdx.Count == 0)
                return null;

            float[] dis = new float[currentNearIdx.Count];
            for (int i = 0; i < currentNearIdx.Count; i++)
            {
                int i1 = currentNearIdx[i].x;
                int i2 = currentNearIdx[i].y;
                dis[i] = (matchCuesPoint[i1][1] - matchCuesPoint[i2][1]).magnitude;
            }

            var sortCurrentNearIdx = currentNearIdx.ToList();
            for (int i = 0; i < currentNearIdx.Count; i++)
                if (!isTarget[currentNearIdx[i].x])
                    currentNearIdx[i] = new Vector2Int(currentNearIdx[i].y, currentNearIdx[i].x);
            var sortNearIdx = dis
                .Select((d, idx) => new { d, idx })
                .GroupBy(di => def[sortCurrentNearIdx[di.idx].y].parent.parent.parent)
                .Select(g => g
                    .OrderBy(di => di.d)
                    .Select(di => sortCurrentNearIdx[di.idx]))
                .Select(g => g.ToArray())
                .ToList();

            return sortNearIdx;
        }

        public void Update(CompareShape cmshp)
        {
            for (int i = 0; i < currentNearIdx.Count; i++)
            {
                int li = currentNearIdx[i].x;
                int lj = currentNearIdx[i].y;
                cuesColor[li] = cuesColorOrign[li] * 2;
                cuesColor[lj] = cuesColorOrign[li] * 2;
            }
            cmshp.UpdateSkeleton();

            confirmNearIdx = currentNearIdx.ToList();
            forceNearIdx = currentNearIdx.ToList();
        }

        public float GetSumDis()
        {
            float dis = 0;
            for (int i = 0; i < forceNearIdx.Count; i++)
            {
                int li = forceNearIdx[i].x;
                int lj = forceNearIdx[i].y;
                dis += (matchCuesPoint[li][1] - matchCuesPoint[lj][1]).magnitude;
            }

            return dis;
        }
    }
    public List<MatchCues> matchCues;
    public List<List<Vector3[]>> drawCuesPoint;
    public List<List<Vector4>> drawCuesColor;
    
    [Header("Show Debug")]
    public bool showDebug = false;
    List<Vector3> debugPos = new List<Vector3>();
    List<Color> debugPosColor = new List<Color>();

    private void Start()
    {
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
        Shapes = transform.Find("Shapes").gameObject;
        Shapes.SetActive(false);
        smSkt = GameObject.Find("NextSkeleton").GetComponent<SmallMultiplesAround>();
        smShp = GameObject.Find("NextShape").GetComponent<SmallShapeAround>();
        compareNodeVizor = transform.Find("CompareSkeletonNode").GetComponent<NodeVisualizer>();
        compareCurveVizor = transform.Find("CompareSkeletonCurve").GetComponent<CurveVisualizer>();
        matchCuesVizor = transform.Find("CompareCues").GetComponent<CurveVisualizer>();
        selectedShapeID = new List<int>();
    }

    void Update()
    {
        if (compareShape)
        {
            if (compareShapeLast == false)
            {
                debugPos.Clear();
                debugPosColor.Clear();
                if (Shapes.transform.childCount >= 1)
                {
                    Debug.Log("Comparing Candidates...");
                    fragmentRef = FragmentData.fragments;
                    groupManager.gameObject.SetActive(false);
                    smSkt.ClearSmallMultiples();
                    Shapes.SetActive(true);
                    smShp.smallShapeAround = false;
                    Camera.main.GetComponent<SphereCollider>().enabled = true;

                    ContinuousMovement movement = GameObject.Find("XR Rig").GetComponent<ContinuousMovement>();
                    movement.SetTransform(cameraPos, cameraEuler);
                }
                else
                {
                    Debug.Log("No shape for comparison! Please select at least two shapes in SwitchShape.");
                    compareShape = false;
                    return;
                }

                ShowCandidatesShape();

                GrabSetup();

                WorldInMiniature.ShowLookAt(true);
            }

            compareShapeLast = true;
        }
        else if (compareShapeLast)
        {
            DisableRotation();
            DestroyShapeObject();
            compareNodeVizor.ClearNodes();
            compareCurveVizor.ClearCurve();
            matchCuesVizor.ClearCurve();
            Shapes.SetActive(false);
            compareShapeLast = false;
            WorldInMiniature.ShowLookAt(false);
            Camera.main.GetComponent<SphereCollider>().enabled = false;
        }
    }


    private void ShowCandidatesShape()
    {
        SmallMultiplesAround.CompactScatterPlot(selectCenter, smSkt.bbSize * bbSizeScale, 
            new Vector3(0, 0, smSkt.planeDepth), out Vector3[] compactCenter);
        for (int i = 0; i < compactCenter.Length; i++)
        {
            Vector3 shift = compactCenter[i] - selectCenter[i];
            selectCenter[i] = compactCenter[i];
            for (int j = 0; j < selectNodePoint[i].Count; j++)
                selectNodePoint[i][j] += shift;
            for (int j = 0; j < selectCurvePoint[i].Count; j++)
            {
                selectCurvePoint[i][j][0] += shift;
                selectCurvePoint[i][j][1] += shift;
            }
            for (int j = 0; j < matchCues[i].matchCuesPoint.Count; j++)
            {
                matchCues[i].matchCuesPoint[j][0] += shift;
                matchCues[i].matchCuesPoint[j][1] += shift;
            }

            Transform shape = Shapes.transform.GetChild(i);
            shape.position += shift;
        }

        UpdateSkeleton();
    }

    private void GrabSetup()
    {
        debugPos.Clear();
        debugPosColor.Clear();

        Transform cshapeTransform = Shapes.transform;
        for (int candidateIdx = 0; candidateIdx < cshapeTransform.childCount; candidateIdx++)
        {
            Transform shape = cshapeTransform.GetChild(candidateIdx);

            Vector3 tempPos = shape.position;
            shape.position = Vector3.zero;
            List<GameObject> fragGroupObj = new List<GameObject>();
            List<List<Transform>> groupedFrag = new List<List<Transform>>();
            List<Vector3> groupCenter = new List<Vector3>();
            foreach (Transform frag in shape)
            {
                var def = frag.GetChild(0).GetChild(0);
                
                var si = def.GetComponent<XRSimpleInteractable>();
                if (si != null)
                    si.enabled = false;
                def.GetComponent<BoxCollider>().enabled = false;
                def.GetComponent<MeshCollider>().enabled = false;

                GameObject groupObj = groupManager.GetGroup(groupManager.GetTransformByName(frag.name.Substring(0, frag.name.Length - 7))[0].gameObject);
                int gi = fragGroupObj.FindIndex(g => g == groupObj);
                if (gi >= 0)
                {
                    groupedFrag[gi].Add(frag);
                    groupCenter[gi] += def.GetComponent<Renderer>().bounds.center;
                }
                else
                {
                    fragGroupObj.Add(groupObj);
                    groupedFrag.Add(new List<Transform>());
                    groupCenter.Add(def.GetComponent<Renderer>().bounds.center);
                    groupedFrag[groupedFrag.Count - 1].Add(frag);
                }
            }

            // merge mesh
            List<GameObject> groupObjList = new List<GameObject>();
            for (int gi = 0; gi < groupedFrag.Count; gi++)
            {
                GameObject groupObj = new GameObject(FragmentTypes.Group);
                groupObj.transform.parent = shape;
                groupObj.transform.position = groupCenter[gi] / groupedFrag[gi].Count;
                debugPos.Add(groupCenter[gi] / groupedFrag[gi].Count);
                debugPosColor.Add(Color.red);

                List<MeshFilter> meshFilters = new List<MeshFilter>();
                foreach (Transform frag in groupedFrag[gi])
                {
                    frag.parent = groupObj.transform;
                    var def = frag.GetChild(0).GetChild(0);
                    meshFilters.Add(def.GetComponent<MeshFilter>());
                }
                CombineInstance[] combine = new CombineInstance[meshFilters.Count];

                int i = 0;
                while (i < meshFilters.Count)
                {
                    combine[i].mesh = meshFilters[i].sharedMesh;
                    combine[i].transform = groupObj.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
                    i++;
                }
                var mf = groupObj.AddComponent<MeshFilter>();
                mf.mesh = new Mesh();
                mf.mesh.CombineMeshes(combine);
                var rb = groupObj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                var mr = groupObj.AddComponent<MeshRenderer>();
                mr.enabled = false;
                var bc = groupObj.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                var si = groupObj.AddComponent<XRSimpleInteractable>();
                si.enabled = false;
                si.interactionLayers = (1 << LayerMask.NameToLayer("RightHand")) |
                    (1 << LayerMask.NameToLayer("LeftHand"));
                var grabCom = groupObj.AddComponent<XRGrabInteractable>();
                grabCom.smoothPosition = true;
                grabCom.smoothRotation = true;
                grabCom.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                grabCom.interactionLayers = (1 << LayerMask.NameToLayer("RightHand")) |
                    (1 << LayerMask.NameToLayer("LeftHand"));
                var select = groupObj.AddComponent<XRSelect>();
                select.enabled = true;
                groupObj.SetActive(true);
                groupObj.AddComponent<ShapeTransformSkeleton>();
                
                groupObjList.Add(groupObj);
            }
            shape.position = tempPos;

            foreach (var groupObj in groupObjList)
                groupObj.GetComponent<ShapeTransformSkeleton>().Initiate(candidateIdx);
        }
    }

    public void UpdateCues(int candidateIdx)
    {
        debugPos.Clear();
        debugPosColor.Clear();

        for (int i = 0; i < matchCues[candidateIdx].matchCuesPoint.Count; i++)
        {
            debugPos.Add(matchCues[candidateIdx].matchCuesPoint[i][0]);
            debugPos.Add(matchCues[candidateIdx].matchCuesPoint[i][1]);
            debugPosColor.Add(Color.red);
            debugPosColor.Add(Color.red);
        }

        matchCues[candidateIdx].GetNearOpenlinks(nearDisTh, similarAngleTh, 
            out List<Vector3[]> cuesLink, out List<Vector4> color);

        drawCuesPoint[candidateIdx] = cuesLink;
        drawCuesColor[candidateIdx] = color;
        MatchCues.interactCompareCandidateIdx = candidateIdx;
    }

    public void MagneticEffect()
    {
        int compareCandidateIdx = MatchCues.interactCompareCandidateIdx;

        var curMatchCues = matchCues[compareCandidateIdx];
        List<Vector2Int[]> matchIdx = curMatchCues.GetPairMatchIdx();
        if (matchIdx != null)
            for (int i = 0; i < matchIdx.Count; i++)
                PairRegister.FastCompareAlignment(matchIdx[i], curMatchCues, 0.05f);

        curMatchCues.Update(this);
    }

    public void UpdateSkeleton()
    {
        compareNodeVizor.UpdatePointsCloud(selectNodePoint.SelectMany(p => p).ToArray(), selectNodeColor.SelectMany(p => p).ToArray());
        compareCurveVizor.UpdateCurve(selectCurvePoint.SelectMany(p => p).ToArray(), selectCurveColor.SelectMany(p => p).ToArray());
        matchCuesVizor.UpdateCurve(drawCuesPoint.SelectMany(p => p).ToArray(), drawCuesColor.SelectMany(p => p).ToArray());
    }

    private void DestroyShapeObject()
    {
        foreach (Transform t in Shapes.transform)
            Destroy(t.gameObject);
    }

    private void DisableRotation()
    {
        Transform c = GameObject.Find("NextShape").transform.Find("Container");
        c.rotation = Quaternion.identity;
        c.GetComponent<BoxCollider>().enabled = false;
        c.GetComponent<XRGrabInteractable>().enabled = false;
        c.GetComponent<XRShapeRotation>().enabled = false;
    }

    private void OnDrawGizmos()
    {
        if (!showDebug)
            return;

        float size = 0.1f;

        for (int i = 0; i < debugPos.Count; i++)
        {
            Gizmos.color = debugPosColor[i];
            Gizmos.DrawSphere(debugPos[i], size);
        }
    }
}
