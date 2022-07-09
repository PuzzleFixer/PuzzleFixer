using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

public class SwitchShape : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool switchShape = false;
    private bool switchShapeLast = false;

    [Header("select")]
    [SerializeField] public bool selectCurrentShape = false;
    [SerializeField] private List<int> selectedShapeID;     

    public Vector3[] selectNodePoint = null;   
    private Vector4[] selectNodeColor = null;
    public Vector3[][] selectCurvePoint = null;
    private Vector4[] selectCurveColor = null;
    private string[] selectNodeFragName = null;
    private Vector3 selectCenter;

    [Header("Panning Speed")]
    [SerializeField] private float panSpeed = 0.5f;

    public int currentTargetShapeIdx;           

    private SmallMultiplesAround smSkt;
    private SmallShapeAround smShp;
    private NodeVisualizer selectSkeletonNodeVizor;    
    private CurveVisualizer selectSkeletonCurveVizor;
    private GameObject selectShape;
    private Transform compareShapeTransform;    
    private CompareShape compareShape;
    private XRShapeRotation shapeRotate;

    [SerializeField] public Vector3 moveAway = new Vector3(0, 1000, 0);

    public Vector3 panningVec = Vector3.zero;   

    public Vector3 panningDir = Vector3.zero;   

    public Quaternion rotation = new Quaternion();
    
    [Header("Show Debug")]
    public bool showDebug = false;
    List<Vector3> debugPos = new List<Vector3>();
    List<Color> debugPosColor = new List<Color>();

    private void Start()
    {
        smSkt = GameObject.Find("NextSkeleton").GetComponent<SmallMultiplesAround>();
        smShp = GameObject.Find("NextShape").GetComponent<SmallShapeAround>();
        compareShape = GameObject.Find("CompareShape").GetComponent<CompareShape>();
        selectSkeletonNodeVizor = transform.Find("SelectSkeletonNode").GetComponent<NodeVisualizer>();
        selectSkeletonCurveVizor = transform.Find("SelectSkeletonCurve").GetComponent<CurveVisualizer>();
        currentTargetShapeIdx = -1;
        selectShape = transform.Find("SelectShape").gameObject;
        shapeRotate = smShp.transform.Find("Container").GetComponent<XRShapeRotation>();
    }

    void Update()
    {
        if (switchShape)
        {
            if (switchShapeLast == false)  
            {
                if (smSkt.smallMultiplesAround == true)
                {
                    Debug.Log("pan the controller to switch candidate skeleton...");
                    fragmentRef = FragmentData.fragments;
                    compareShape.selectedShapeID = new List<int>();
                    selectedShapeID = compareShape.selectedShapeID;
                    
                    compareShape.selectNodePoint = new List<List<Vector3>>();
                    compareShape.selectNodeColor = new List<List<Vector4>>();
                    compareShape.selectCurvePoint = new List<List<Vector3[]>>();
                    compareShape.selectCurveColor = new List<List<Vector4>>();
                    compareShape.selectNodeFragName = new List<List<string>>();
                    compareShape.selectCenter = new List<Vector3>();
                    compareShape.matchCues = new List<CompareShape.MatchCues>();
                    compareShape.drawCuesPoint = new List<List<Vector3[]>>();
                    compareShape.drawCuesColor = new List<List<Vector4>>();

                    compareShapeTransform = GameObject.Find("CompareShape").GetComponent<CompareShape>().transform.Find("Shapes");
                    selectShape = transform.Find("SelectShape").gameObject;

                    debugPos.Clear();
                    debugPosColor.Clear();
                }
                else
                {
                    Debug.Log("SwitchSkeleton error: please generate skeleton candidate in SmallMultiplesAround first!");
                    switchShape = false;
                    return;
                }
            }

            PanningSmallMultiplesController();
            PanningSmallMultiplesKeyBoard();

            if (selectCurrentShape)
            {
                if (!compareShape.selectedShapeID.Exists(si => si == currentTargetShapeIdx))
                {
                    compareShape.selectedShapeID.Add(currentTargetShapeIdx);

                    Transform t = selectShape.transform.GetChild(0);
                    Transform newt = Instantiate(t);    
                    newt.parent = compareShapeTransform;
                    newt.position = t.position;
                    newt.rotation = t.rotation;
                    newt.name = t.name;

                    var selectNode = selectNodePoint.ToList();
                    var selectCurve = selectCurvePoint.Select(a => a.ToArray()).ToList();
                    compareShape.selectNodePoint.Add(selectNode);
                    compareShape.selectNodeColor.Add(selectNodeColor.ToList());
                    compareShape.selectCurvePoint.Add(selectCurve);
                    compareShape.selectCurveColor.Add(selectCurveColor.ToList());
                    compareShape.selectNodeFragName.Add(selectNodeFragName.ToList());
                    compareShape.selectCenter.Add(selectCenter);


                    var currentCues = new CompareShape.MatchCues();
                    int oid = smSkt.fOrignalIdx[currentTargetShapeIdx];
                    GroupMatch selectMatchInfo = smSkt.selectGroupMatch[oid];
                    for (int i = 0; i < selectMatchInfo.targetLinks.Count; i++)
                    {
                        string fragmentID = selectMatchInfo.targetLinks[i].Key.fragmentID;
                        int face = selectMatchInfo.targetLinks[i].Key.face;
                        Fragment frag = FragmentData.GetFragmentByName(fragmentID);
                        Transform def = newt.Find(frag.GetIDName() + "(Clone)").GetChild(0).GetChild(0);
                        currentCues.fragID.Add(fragmentID);
                        currentCues.faceID.Add(face);
                        currentCues.isTarget.Add(true);
                        currentCues.def.Add(def);
                        currentCues.normalLocal.Add(frag.faceNormal[face]);

                        currentCues.matchCuesPoint.Add(new Vector3[] {
                            def.GetComponent<Renderer>().bounds.center,
                            def.TransformPoint(frag.faceCenter[face])});
                        currentCues.cuesColor.Add(selectNodeColor[0]);  
                        currentCues.cuesColorOrign.Add(selectNodeColor[0]);
                    }
                    for (int i = 0; i < selectMatchInfo.otherLinks.Count; i++)
                    {
                        string fragmentID = selectMatchInfo.otherLinks[i].Key.fragmentID;
                        int face = selectMatchInfo.otherLinks[i].Key.face;
                        Fragment frag = FragmentData.GetFragmentByName(fragmentID);
                        Transform def = newt.Find(frag.GetIDName() + "(Clone)").GetChild(0).GetChild(0);
                        currentCues.fragID.Add(fragmentID);
                        currentCues.faceID.Add(face);
                        currentCues.isTarget.Add(false);
                        currentCues.def.Add(def);
                        currentCues.normalLocal.Add(frag.faceNormal[face]);

                        currentCues.matchCuesPoint.Add(new Vector3[] {
                            def.GetComponent<Renderer>().bounds.center,
                            def.TransformPoint(frag.faceCenter[face])});
                        currentCues.cuesColor.Add(selectNodeColor[0]);  
                        currentCues.cuesColorOrign.Add(selectNodeColor[0]);
                    }
                    compareShape.matchCues.Add(currentCues);

                    var cuePoint = new List<Vector3[]>();
                    cuePoint.Add(new Vector3[] { Vector3.zero, Vector3.zero });
                    compareShape.drawCuesPoint.Add(cuePoint);
                    var cueColor = new List<Vector4>();
                    cueColor.Add(Vector4.zero);
                    compareShape.drawCuesColor.Add(cueColor);

                    shapeRotate.AddLocalCompareSkeleton(selectNode, selectCurve, currentCues.matchCuesPoint, newt.name);
                }

                selectCurrentShape = false;
            }

            switchShapeLast = true;
        }
        else if (switchShapeLast)
        {
            selectSkeletonNodeVizor.ClearNodes();
            selectSkeletonCurveVizor.ClearCurve();
            currentTargetShapeIdx = -1;
            PanningSmallMultiplesFinishKeyBoard();
            panningVec = Vector3.zero;
            DestroyShapeObject();

            switchShapeLast = false;
        }
    }

    public void SwitchTargetConnectShape(int shapei, bool alignShape)
    {
        RecoveryShape();

        if (alignShape)
        {
            Vector3 panVec = -smSkt.fSkeletonCenter[shapei];
            panVec.z = 0;
            int shapeNum = smSkt.fSkeletonCenter.Count;
            for (int i = 0; i < shapeNum; i++)
            {
                smSkt.fSkeletonCenter[i] += panVec;
                int nn = smSkt.fSkeletonNodePoint[i].Count;
                for (int j = 0; j < nn; j++)
                    smSkt.fSkeletonNodePoint[i][j] += panVec;
                int cn = smSkt.fSkeletonCurvePoint[i].Count;
                for (int j = 0; j < cn; j++)
                {
                    smSkt.fSkeletonCurvePoint[i][j][0] += panVec;
                    smSkt.fSkeletonCurvePoint[i][j][1] += panVec;
                }

                Transform tshape = smShp.Shapes.transform.GetChild(i);
                tshape.position += panVec;
            }

            panningVec += panVec;
        }


        foreach (Transform t in selectShape.transform)  
            Destroy(t.gameObject);
        Transform s = smShp.Shapes.transform.Find("candidate" + shapei);
        Quaternion rot = s.localRotation;
        s.localRotation = Quaternion.identity;          
        Transform newSelectedShape = Instantiate(s);    
        s.gameObject.SetActive(false);                  
        newSelectedShape.parent = selectShape.transform;
        newSelectedShape.name = "candidate" + shapei;
        newSelectedShape.localScale /= smSkt.smallSkeletonScale;
        var transformInfo = smSkt.fragCenterAlignResult[smSkt.fOrignalIdx[shapei]]; 
        for (int k = 0; k < smSkt.fNodeFragName[shapei].Count; k++)
        {
            string fragName = smSkt.fNodeFragName[shapei][k];
            Transform t = newSelectedShape.GetChild(k);
            var fragTransform = Array.Find(transformInfo, f => f.Key == fragName).Value;
            t.GetChild(0).position = fragTransform.Key;
            t.GetChild(0).rotation = fragTransform.Value;
        }
        GroupManager.SetFragment2Center(newSelectedShape);
        s.localRotation = rot;
        newSelectedShape.localRotation = rot;
        

        selectNodePoint = smSkt.fSkeletonNodePoint[shapei].ToArray();
        selectNodeColor = smSkt.fSkeletonNodeColor[shapei].ToArray();
        selectCurvePoint = smSkt.fSkeletonCurvePoint[shapei].Select(a => a.ToArray()).ToArray();
        selectCurveColor = smSkt.fSkeletonCurveColor[shapei].ToArray();
        selectNodeFragName = smSkt.fNodeFragName[shapei].ToArray();
        selectCenter = smSkt.fSkeletonCenter[shapei];

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
        selectCenter = center;

        Transform fragDef = newSelectedShape.Find(selectNodeFragName[selectNodeFragName.Length - 1] + "(Clone)").GetChild(0).GetChild(0);
        Vector3 fragCenter = fragDef.TransformPoint(fragDef.GetComponent<MeshFilter>().mesh.bounds.center);
        Vector3 shift = fragCenter - selectNodePoint[selectNodePoint.Length - 1];
        for (int i = 0; i < selectNodePoint.Length; i++)
            selectNodePoint[i] += shift;
        for (int i = 0; i < selectCurvePoint.Length; i++)
        {
            selectCurvePoint[i][0] += shift;
            selectCurvePoint[i][1] += shift;
        }
        selectCenter += shift;
        UpdateCenterSkeleton();


        int nodeNum = smSkt.fSkeletonNodePoint[shapei].Count;
        for (int i = 0; i < nodeNum; i++)
            smSkt.fSkeletonNodePoint[shapei][i] += moveAway;
        int curveNum = smSkt.fSkeletonCurvePoint[shapei].Count;
        for (int i = 0; i < curveNum; i++)
        {
            smSkt.fSkeletonCurvePoint[shapei][i][0] += moveAway;
            smSkt.fSkeletonCurvePoint[shapei][i][1] += moveAway;
        }
        smSkt.UpdateShapeSmallMultiples();

        shapeRotate.UpdateLocalCenterSkeleton(selectNodePoint, selectCurvePoint, newSelectedShape.name);
    }

    private void PanningSmallMultiplesController()
    {
        Vector3 direction = new Vector3(panningDir.x, panningDir.y, 0) * panSpeed;

        if (direction == Vector3.zero)
            return;

        float[] dis = new float[smSkt.fSkeletonCenter.Count];
        
        for (int i = 0; i < smSkt.fSkeletonCenter.Count; i++)
        {
            smSkt.fSkeletonCenter[i] += direction;
            dis[i] = smSkt.fSkeletonCenter[i].sqrMagnitude;
        }


        bool outofBound = smSkt.fSkeletonCenter.Min(s => s.x) > 0.1f || smSkt.fSkeletonCenter.Min(s => s.y) > 0.1f
            || smSkt.fSkeletonCenter.Max(s => s.x) < -0.1f || smSkt.fSkeletonCenter.Max(s => s.y) < -0.1f;
        if (outofBound)
        {
            for (int i = 0; i < smSkt.fSkeletonCenter.Count; i++)
                smSkt.fSkeletonCenter[i] -= direction;
            return;
        }

        for (int i = 0; i < smSkt.fSkeletonNodePoint.Count; i++)
            for (int j = 0; j < smSkt.fSkeletonNodePoint[i].Count; j++)
                smSkt.fSkeletonNodePoint[i][j] += direction;

        for (int i = 0; i < smSkt.fSkeletonCurvePoint.Count; i++)
            for (int j = 0; j < smSkt.fSkeletonCurvePoint[i].Count; j++)
            {
                smSkt.fSkeletonCurvePoint[i][j][0] += direction;
                smSkt.fSkeletonCurvePoint[i][j][1] += direction;
            }

        panningVec += direction;
        smSkt.UpdateShapeSmallMultiples();


        foreach (Transform t in smShp.Shapes.transform)
            t.position += direction;


        int closesti = Array.FindIndex(dis, d => d == dis.Min());
        if (closesti != currentTargetShapeIdx)
        {
            SwitchTargetConnectShape(closesti, false);
            currentTargetShapeIdx = closesti;
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

        float[] dis = new float[smSkt.fSkeletonCenter.Count];

        for (int i = 0; i < smSkt.fSkeletonCenter.Count; i++)
        {
            smSkt.fSkeletonCenter[i] += direction;
            dis[i] = smSkt.fSkeletonCenter[i].sqrMagnitude;
        }


        bool outofBound = smSkt.fSkeletonCenter.Min(s => s.x) > 0.1f || smSkt.fSkeletonCenter.Min(s => s.y) > 0.1f
            || smSkt.fSkeletonCenter.Max(s => s.x) < -0.1f || smSkt.fSkeletonCenter.Max(s => s.y) < -0.1f;
        if (outofBound)
        {
            for (int i = 0; i < smSkt.fSkeletonCenter.Count; i++)
                smSkt.fSkeletonCenter[i] -= direction;
            return;
        }

        for (int i = 0; i < smSkt.fSkeletonNodePoint.Count; i++)
            for (int j = 0; j < smSkt.fSkeletonNodePoint[i].Count; j++)
                smSkt.fSkeletonNodePoint[i][j] += direction;

        for (int i = 0; i < smSkt.fSkeletonCurvePoint.Count; i++)
            for (int j = 0; j < smSkt.fSkeletonCurvePoint[i].Count; j++)
            {
                smSkt.fSkeletonCurvePoint[i][j][0] += direction;
                smSkt.fSkeletonCurvePoint[i][j][1] += direction;
            }

        panningVec += direction;
        smSkt.UpdateShapeSmallMultiples();


        foreach (Transform t in smShp.Shapes.transform)
            t.position += direction;


        int closesti = Array.FindIndex(dis, d => d == dis.Min());
        if (closesti != currentTargetShapeIdx)
        {
            SwitchTargetConnectShape(closesti, false);
            currentTargetShapeIdx = closesti;
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

        for (int i = 0; i < smSkt.gSkeletonCenter.Count; i++)
            smSkt.gSkeletonCenter[i] += panningVec;

        for (int i = 0; i < smSkt.gSkeletonNodePoint.Count; i++)
            for (int j = 0; j < smSkt.gSkeletonNodePoint[i].Count; j++)
                smSkt.gSkeletonNodePoint[i][j] += panningVec;

        for (int i = 0; i < smSkt.gSkeletonCurvePoint.Count; i++)
            for (int j = 0; j < smSkt.gSkeletonCurvePoint[i].Count; j++)
            {
                smSkt.gSkeletonCurvePoint[i][j][0] += panningVec;
                smSkt.gSkeletonCurvePoint[i][j][1] += panningVec;
            }
    }

    public void UpdateCenterSkeleton()
    {
        selectSkeletonNodeVizor.UpdatePointsCloud(selectNodePoint, selectNodeColor);
        selectSkeletonCurveVizor.UpdateCurve(selectCurvePoint, selectCurveColor);
    }

    private void RecoveryShape()
    {
        if (currentTargetShapeIdx >= 0)
        {
            for (int i = 0; i < smSkt.fSkeletonNodePoint[currentTargetShapeIdx].Count; i++)
                smSkt.fSkeletonNodePoint[currentTargetShapeIdx][i] -= moveAway;
            for (int i = 0; i < smSkt.fSkeletonCurvePoint[currentTargetShapeIdx].Count; i++)
            {
                smSkt.fSkeletonCurvePoint[currentTargetShapeIdx][i][0] -= moveAway;
                smSkt.fSkeletonCurvePoint[currentTargetShapeIdx][i][1] -= moveAway;
            }

            Transform s = smShp.Shapes.transform.Find("candidate" + currentTargetShapeIdx);
            s.gameObject.SetActive(true);
        }
    }

    private void DestroyShapeObject()
    {
        foreach (Transform t in selectShape.transform)
            Destroy(t.gameObject);
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
