using MathNet.Numerics.Integration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Overview : MonoBehaviour
{
    Fragment[] fragmentRef = null;
    private Fragment[] currentFragments = null;

    [Header("start button")]
    [SerializeField] public bool overviewVisualize = false;
    private bool overviewVisualizeLast = false;
    
    [HideInInspector] private bool forceRedraw = false;
    [HideInInspector] public bool anyObjChanged = false;

    // drawer
    NodeVisualizer nodeVizor;
    CurveVisualizer curveVizor;

    // cache current skeleton positions info (nodes & links)
    [HideInInspector] public Vector3[] nodePos = null;  
    [HideInInspector] public Vector4[] nodeColor = null;
    [HideInInspector] public List<Vector3[]> linkPos;   
    [HideInInspector] public Vector3[][] linkPosArr;
    [HideInInspector] public List<Vector4> linkColor;
    [HideInInspector] public List<int[]> linkPosInfo;       
    private List<KeyValuePair<bool, KeyValuePair<int, int>>> curveNodeInfo;
                                                                                

    [Header("Show Debug")]
    public bool showDebug = false;
    List<Vector3> debugPos = new List<Vector3>();
    List<Color> debugPosColor = new List<Color>();

    private void Start()
    {
        nodeVizor = GameObject.Find("NodeVisualizer").GetComponent<NodeVisualizer>();
        curveVizor = GameObject.Find("CurveVisualizer").GetComponent<CurveVisualizer>();
    }

    void Update()
    {
        if (overviewVisualize)
        {
            if (overviewVisualizeLast == false)
            {
                Debug.Log("Overview Visualizing...");
                fragmentRef = FragmentData.fragments;
                if (currentFragments == null || currentFragments.Length == 0)
                    currentFragments = fragmentRef;
                foreach (var f in currentFragments)
                    f.objMesh.transform.GetChild(0).hasChanged = false;
                MeshColor.DrawMeshColor(currentFragments);
            }

            DrawSkeleton();

            overviewVisualizeLast = true;
        }
        else if (overviewVisualizeLast)
        {
            DarkSkeleton();
            overviewVisualizeLast = false;
        }
    }

    private void LateUpdate()
    {
        // transform trigger
        anyObjChanged = false;
        foreach (var f in currentFragments)
        {
            if (f.objMesh.transform.childCount > 0)
                anyObjChanged |= f.objMesh.transform.GetChild(0).hasChanged;
        }
            
    }

    void DrawSkeleton()
    {
        if (overviewVisualizeLast == false || anyObjChanged || forceRedraw)
        {
            if (overviewVisualizeLast == false || forceRedraw)
            {
                nodePos = new Vector3[currentFragments.Length];
                nodeColor = new Vector4[currentFragments.Length];
            }
            int[] nodePosIndex = new int[fragmentRef.Length];
            for (int i = 0; i < currentFragments.Length; i++)
            {
                if (currentFragments[i].objMesh.transform.childCount > 0)
                    currentFragments[i].objMesh.transform.GetChild(0).hasChanged = false;

                nodePos[i] = currentFragments[i].objMesh.transform.Find(FragmentTypes.Default).GetComponent<Renderer>().bounds.center;
                
                var fragFaceColors = MeshColor.faceColor.FindAll(fc => fc.Key.x == i).Select(fc => (Vector4)fc.Value);
                Vector4 avgColor = new Vector4();
                foreach (var c in fragFaceColors)
                    avgColor += c;
                nodeColor[i] = avgColor / fragFaceColors.Count();
                nodeColor[i].w = 1.0f;

                int fi = FragmentData.GetFragmentIndexByName(currentFragments[i].parent.name);
                nodePosIndex[fi] = i;
            }

            if (overviewVisualizeLast == false || forceRedraw)
            {
                linkPos = new List<Vector3[]>();
                linkPosInfo = new List<int[]>();
                linkColor = new List<Vector4>();
                HashSet<string> pairSet;    
                pairSet = new HashSet<string>();
                curveNodeInfo = new List<KeyValuePair<bool, KeyValuePair<int, int>>>();
                for (int i = 0; i < currentFragments.Length; i++)    
                {
                    int fi = FragmentData.GetFragmentIndexByName(currentFragments[i].parent.name);
                    for (int li = 0; li < currentFragments[i].skeletonLink.Count; li++)
                    {
                        var partner = currentFragments[i].skeletonLink[li];
                        int fj = FragmentData.GetFragmentIndexByName(partner.Value.fragmentID);
                        if (pairSet.Contains(fi.ToString() + "-" +  partner.Key.face.ToString() + "-" + partner.Key.curve.ToString()
                             + "-" +  fj.ToString() + "-" + partner.Value.face.ToString() + "-" + partner.Value.curve.ToString()))
                            continue;

                        if (fj >= 0)
                        {
                            linkPos.Add(new Vector3[] { nodePos[nodePosIndex[fi]], nodePos[nodePosIndex[fj]] });
                            curveNodeInfo.Add(new KeyValuePair<bool, KeyValuePair<int, int>>(true,
                                new KeyValuePair<int, int>(nodePosIndex[fi], nodePosIndex[fj])));
                            
                            linkColor.Add(MeshColor.faceColor.Find(fc => fc.Key.x == fi && fc.Key.y == partner.Key.face).Value);
                            linkColor.Add(MeshColor.faceColor.Find(fc => fc.Key.x == fj && fc.Key.y == partner.Value.face).Value);
                        }
                            
                        else
                        {
                            Transform t = currentFragments[i].objMesh.transform.Find(FragmentTypes.Default);
                            linkPos.Add(new Vector3[] { nodePos[nodePosIndex[fi]], t.TransformPoint(currentFragments[i].faceCenter[partner.Key.face]) });
                            curveNodeInfo.Add(new KeyValuePair<bool, KeyValuePair<int, int>>(false,
                                new KeyValuePair<int, int>(nodePosIndex[fi], li)));
                            
                            linkColor.Add(MeshColor.faceColor.Find(fc => fc.Key.x == fi && fc.Key.y == partner.Key.face).Value);
                            linkColor.Add(linkColor[linkColor.Count - 1]);
                        }

                        pairSet.Add(fi.ToString() + "-" + partner.Key.face.ToString() + "-" + partner.Key.curve.ToString()
                             + "-" + fj.ToString() + "-" + partner.Value.face.ToString() + "-" + partner.Value.curve.ToString());
                        pairSet.Add(fj.ToString() + "-" + partner.Value.face.ToString() + "-" + partner.Value.curve.ToString()
                             +"-" + fi.ToString() + "-" + partner.Key.face.ToString() + "-" + partner.Key.curve.ToString());

                        linkPosInfo.Add(new int[] { i, li });
                    }
                }
                linkPosArr = linkPos.ToArray();
            }
            else
            {
                for (int li = 0; li < curveNodeInfo.Count; li++)
                {
                    int ci = curveNodeInfo[li].Value.Key;
                    int cj = curveNodeInfo[li].Value.Value;
                    if (curveNodeInfo[li].Key == true)
                        linkPosArr[li] = new Vector3[] {
                            nodePos[ci],
                            nodePos[cj] };
                    else
                        linkPosArr[li] = new Vector3[] {
                            nodePos[ci],
                            currentFragments[ci].objMesh.transform.Find(FragmentTypes.Default)
                            .TransformPoint(currentFragments[ci].faceCenter[currentFragments[ci].skeletonLink[cj].Key.face]) };
                }

                linkPos = linkPosArr.ToList();
            }

            // draw nodes
            nodeVizor.UpdatePointsCloud(nodePos, nodeColor);

            // draw curves
            curveVizor.UpdateCurve(linkPosArr, linkColor.ToArray());

            // debug
            debugPos.Clear();
            debugPosColor.Clear();
            for (int i = 0; i < nodePos.Length; i++)
            {
                debugPos.Add(nodePos[i]);
                debugPosColor.Add(nodeColor[i]);
            }

            forceRedraw = false;
        }
    }

    public void UpdateOverviewSkeleton(Vector3[] np, Vector4[] nc, Vector3[][] lp, Vector4[] lc)
    {
        nodeVizor.UpdatePointsCloud(np, nc);

        curveVizor.UpdateCurve(lp, lc);
    }

    public void DarkSkeleton()
    {
        nodeVizor.ClearNodes();
        curveVizor.ClearCurve();
    }

    public void ForceRedraw()
    {
        forceRedraw = true;
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
