using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using UnityEngine.Events;

public class PairRegister : MonoBehaviour
{
    [Header("Gameobjects to be aligned")]
    [SerializeField] private GameObject fragGroupObj1 = null;
    [SerializeField] private GameObject fragGroupObj2 = null;

    [Header("Param set")]
    [Tooltip("distance of two face center")]
    [SerializeField] private float distanceMax = 0.3f;
    [SerializeField] private float faceAngleMin = 45;
    [SerializeField] private static float gap = 0.05f;  

    static private string applicationDataPath;

    private AsynManager asynManager;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    private static List<Vector3> debugPos = new List<Vector3>();
    private static List<Color> debugPosColor = new List<Color>();
    private static List<Vector3> debugPos2 = new List<Vector3>();
    private static List<Color> debugPosColor2 = new List<Color>();

    #region MatrixExtraction
    public static class MatrixExtraction
    {
        public static Quaternion Rotation(Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            return Quaternion.LookRotation(forward, upwards);
        }

        public static Vector3 Position(Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = matrix.m03;
            position.y = matrix.m13;
            position.z = matrix.m23;
            return position;
        }

        public static Vector3 Scale(Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }
    }
    #endregion

    private void Start()
    {
        applicationDataPath = Application.dataPath;
        asynManager = GameObject.Find("AsynManager").GetComponent<AsynManager>();
    }

    static public Matrix4x4 MeshLabGP(Vector3[][] fPoints, Vector3[][] fNormals,
        out int targetidx, bool showRegisterWindow = false)
    {
        /// save obj to file
        string cachePath = applicationDataPath + "/Resources/RegisterCache";
        if (!Directory.Exists(cachePath))
            Directory.CreateDirectory(cachePath);

        string[] ifilePath = new string[] {
            Path.Combine(cachePath, "f1.xyz"),
            Path.Combine(cachePath, "f2.xyz")
        };
        string[] ofilePath = new string[] {
            Path.Combine(cachePath, "f1.ply"),
            Path.Combine(cachePath, "f2.ply")
        };
        string projectInfoPath = Path.Combine(cachePath, "logFile.mlp");

        for (int i = 0; i < 2; i++)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(fPoints[i].Length).Append("\n");
            for (int vi = 0; vi < fPoints[i].Length; vi++)
            {
                Vector3 wv = fPoints[i][vi];
                sb.Append(string.Format("{0} {1} {2}", wv.x, wv.y, wv.z));
                Vector3 wn = fNormals[i][vi];
                sb.Append(string.Format(" {0} {1} {2}\n", wn.x, wn.y, wn.z));
            }

            using (StreamWriter sw = new StreamWriter(ifilePath[i]))
            {
                sw.Write(sb.ToString());
            }
        }


        for (int i = 0; i < 2; i++)
        {
            EXERunner.CallExternalExe("meshlab",
                " -i " + ifilePath[i] + " -o " + ofilePath[i] +
                " -m vn sa" +
                " -s " + applicationDataPath + "/External/Meshlab/SimplePointNet.mlx",
                showRegisterWindow);
        }


        int referidx = 0;
        targetidx = 1;
        Matrix4x4 transformMatrix = GlobalRegister(projectInfoPath, ofilePath[referidx], ofilePath[targetidx],
            cachePath, showRegisterWindow);
        if (transformMatrix == Matrix4x4.zero)
        {
            referidx = 1;
            targetidx = 0;
            transformMatrix = GlobalRegister(projectInfoPath, ofilePath[referidx], ofilePath[targetidx],
                cachePath, showRegisterWindow);
        }

        return transformMatrix;
    }

    static public void TransferToAlignment(Matrix4x4 transformMatrix, int targetidx, Transform objMesh1, Transform objMesh2)
    {
        /// Transfer the object
        Vector3 targetTfPosition = MatrixExtraction.Position(transformMatrix);
        Quaternion targetTfRotation = MatrixExtraction.Rotation(transformMatrix);

        Transform meshTransform;
        if (targetidx == 1)
            meshTransform = objMesh2;
        else
            meshTransform = objMesh1;

        GameObject shellObj = new GameObject();
        shellObj.transform.parent = meshTransform.parent;
        meshTransform.parent = shellObj.transform;

        shellObj.transform.position = targetTfPosition;
        shellObj.transform.rotation = targetTfRotation;

        meshTransform.parent = shellObj.transform.parent;
        Destroy(shellObj);

        Debug.Log("aligning finish!");
    }

    static private Matrix4x4 GlobalRegister(string projectInfoPath, string inputFile1, string inputFile2, 
        string cachePath, bool showRegisterWindow)
    {
        EXERunner.CallExternalExe("meshlab",
                "-w " + projectInfoPath +
                " -i " + inputFile1 + " -i " + inputFile2 +
                " -s " + applicationDataPath + "/External/Meshlab/GR.mlx",
                showRegisterWindow);

        /// Read transfer matrix
        string resulrStr;
        using (StreamReader sr = new StreamReader(cachePath + "/logFile.mlp"))
        {
            resulrStr = sr.ReadToEnd();
        }

        string[] lines = resulrStr.Split('\n');
        Matrix4x4[] transformMatrix = new Matrix4x4[2];
        int mi = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string tag = Regex.Replace(lines[i], @"\s+", "");
            if (tag == "<MLMatrix44>")
            {
                for (int j = 0, li = i + 1; j < 4; j++, li++)
                {
                    int k = 0;
                    foreach (var e in lines[li].Split(' '))
                    {
                        try
                        {
                            transformMatrix[mi][j, k] = Convert.ToSingle(e);
                            if (float.IsNaN(transformMatrix[mi][j, k]))
                                Debug.LogWarning("Converting MLMatrix44 in PairRegister: NaN");
                        }
                        catch (Exception)
                        {
                            if (e == "nan")
                                return Matrix4x4.zero;
                            continue;
                        }
                        k += 1;
                    }
                }
                mi += 1;
                if (mi == 2)
                    break;
            }
        }

        return transformMatrix[1];
    }

    static public void FacePair(Vector3[][] fp, Vector3[][] fn, ref GameObject moveObj, bool showDebug = false)
    {
        Vector3[] fpAvg = new Vector3[] { Vector3.zero, Vector3.zero };
        Vector3[] fnAvg = new Vector3[] { Vector3.zero, Vector3.zero };
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < fp[i].Length; j++)
            {
                fpAvg[i] += fp[i][j];
                fnAvg[i] += fn[i][j];
            }
            fpAvg[i] /= fp[i].Length;
            fnAvg[i] /= fp[i].Length;
        }
        if (Vector3.Angle(fnAvg[0], fnAvg[1]) < 90)
        {
            moveObj.transform.RotateAround(fpAvg[1], Vector3.Cross(fnAvg[0], fnAvg[1]).normalized,
                Vector3.Angle(-fnAvg[0], fnAvg[1]));

        }
    }

    static public void PreciseAlignmentSim(List<Transform> t1, List<Fragment> frag1,
        List<Transform> t2, List<Fragment> frag2, Fragment[] fragmentRef, bool colideTest = false)
    {
        GameObject o1 = new GameObject();
        List<Transform> parentT1 = new List<Transform>();
        for (int i = 0; i < t1.Count; i++)
        {
            parentT1.Add(t1[i].parent);
            t1[i].parent = o1.transform;
        }
        GameObject o2 = new GameObject();
        List<Transform> parentT2 = new List<Transform>();
        for (int i = 0; i < t2.Count; i++)
        {
            parentT2.Add(t2[i].parent);
            t2[i].parent = o2.transform;
        }

        Fragment pivot = frag1[0];
        Transform pivotTransfrom = t1[0];

        var t3 = t1.ToList();
        for (int i = 0; i < frag2.Count; i++)
        {
            int fi = Array.FindIndex(fragmentRef, f => f.GetIDName() == frag2[i].GetIDName());
            Vector3 posDiff = pivot.relativePosition[fi];
            Quaternion rotDiff = pivot.relativeRotation[fi];
            Vector3 op = t2[i].position;
            Quaternion oq = t2[i].rotation;
            t2[i].position = pivotTransfrom.position + posDiff;
            t2[i].rotation = pivotTransfrom.rotation * rotDiff;
            if (colideTest)
            {
                bool overlap = false;
                Bounds targetBounds = t2[i].GetChild(0).GetComponent<Renderer>().bounds;
                Vector3 tsize = targetBounds.size;
                float targetShortLen = Mathf.Min(Mathf.Min(tsize.x, tsize.y), tsize.z) / 2.0f;

                for (int t3i = 0; t3i < t3.Count; t3i++)
                {
                    Bounds sourceBounds =
                        t3[t3i].GetChild(0).GetComponent<Renderer>().bounds;
                    Vector3 size = sourceBounds.size;
                    float sourceShortLen = Mathf.Min(Mathf.Min(size.x, size.y), size.z) / 2.0f;
                    if ((sourceBounds.center - targetBounds.center).magnitude <
                        (sourceShortLen + targetShortLen) * 0.9f)
                    {
                        overlap = true;
                        break;
                    }
                }
                if (overlap == true)
                {
                    t2[i].position = op;
                    t2[i].rotation = oq;
                }
            }
            t3.Add(t2[i]);
        }

        for (int i = 0; i < t1.Count; i++)
            t1[i].parent = parentT1[i];
        for (int i = 0; i < t2.Count; i++)
            t2[i].parent = parentT2[i];
        Destroy(o1);
        Destroy(o2);
    }

    static public List<KeyValuePair<CurveIndex, CurveIndex>> FastAlignment(
        GroupMatch groupMatch, GroupManager groupManager, Fragment[] fragmentRef, bool showDebug = false)
    {
        var targetLinks = groupMatch.targetLinks;
        var otherLinks = groupMatch.otherLinks;
        var matchTargetOtherInfo = new List<KeyValuePair<CurveIndex, CurveIndex>>();


        List<Transform> otherFragTransform = new List<Transform>();
        foreach (var li in otherLinks)
            otherFragTransform.Add(FragmentData.GetFragmentByName(li.Key.fragmentID).objMesh.transform);
        int[][] otherGroupIdx =
            groupManager.GetSecondaryParent(otherFragTransform, out List<Transform> otherSubGroupTransform);


        var groupTargetLinks = new List<List<KeyValuePair<CurveIndex, CurveIndex>>>();
        var groupOtherLinks = new List<List<KeyValuePair<CurveIndex, CurveIndex>>>();
        for (int gi = 0; gi < otherGroupIdx.Length; gi++)
        {
            var subGroupTargetLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
            var subGroupOtherLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
            foreach (var pairIdx in otherGroupIdx[gi])
            {
                subGroupTargetLinks.Add(targetLinks[pairIdx]);
                subGroupOtherLinks.Add(otherLinks[pairIdx]);
            }
            groupTargetLinks.Add(subGroupTargetLinks);
            groupOtherLinks.Add(subGroupOtherLinks);
        }


        int subGroupNum = otherSubGroupTransform.Count;
        for (int subGroupi = 0; subGroupi < subGroupNum; subGroupi++)
        {
            int pointNum = groupTargetLinks[subGroupi].Count;
            Transform groupTransform = otherSubGroupTransform[subGroupi];

            int alignCount = 0;
            Vector3 rotateAxis = Vector3.zero;
            Vector3 rotatePoint = Vector3.zero;
            for (int pi = 0; pi < pointNum; pi++)
            {
                CurveIndex targetInfo = groupTargetLinks[subGroupi][pi].Key;
                CurveIndex otherInfo = groupOtherLinks[subGroupi][pi].Key;
                Fragment targetFrag = FragmentData.GetFragmentByName(targetInfo.fragmentID);
                Fragment otherFrag = FragmentData.GetFragmentByName(otherInfo.fragmentID);


                Transform targetTransform = targetFrag.objMesh.transform.Find(FragmentTypes.Default);
                Transform otherTransform = otherFrag.objMesh.transform.Find(FragmentTypes.Default);


                if (pi == 0)
                {
                    GameObject initObj = new GameObject();
                    initObj.transform.position = otherTransform.position;
                    initObj.transform.rotation = otherTransform.rotation;
                    initObj.transform.parent = groupTransform.parent;
                    groupTransform.parent = initObj.transform;
                    List<Transform> t1 = new List<Transform>() { targetTransform.parent };
                    List<Fragment> f1 = new List<Fragment>() { targetFrag };
                    List<Transform> t2 = new List<Transform>() { initObj.transform };
                    List<Fragment> f2 = new List<Fragment>() { otherFrag };
                    PreciseAlignmentSim(t1, f1, t2, f2, fragmentRef);
                    groupTransform.parent = initObj.transform.parent;
                    Destroy(initObj);
                }

                Vector3 targetPoint = targetTransform.TransformPoint(targetFrag.faceCenter[targetInfo.face]);
                Vector3 targetCenterPoint = targetTransform.TransformPoint(
                    targetTransform.GetComponent<MeshFilter>().mesh.bounds.center);
                Vector3 targetFaceNormal = targetTransform.TransformDirection(targetFrag.faceNormal[targetInfo.face]);

                Vector3 otherPoint = otherFrag.faceCenter[otherInfo.face];
                Vector3 otherCenterPoint = otherTransform.GetComponent<MeshFilter>().mesh.bounds.center;
                Vector3 otherFaceNormal = otherFrag.faceNormal[otherInfo.face];

                Vector3 otherPointWorld = otherTransform.TransformPoint(otherPoint);
                Vector3 otherCenterPointWorld = otherTransform.TransformPoint(otherCenterPoint);
                Vector3 otherFaceNormalWorld = otherTransform.TransformDirection(otherFaceNormal);


                GameObject rotateObj = new GameObject();
                rotateObj.transform.position = otherCenterPointWorld;
                rotateObj.transform.parent = groupTransform.parent;
                groupTransform.parent = rotateObj.transform;

                if (alignCount == 0)
                {
                    matchTargetOtherInfo.Add(new KeyValuePair<CurveIndex, CurveIndex>(targetInfo, otherInfo));

                    Quaternion rotateQuat = Quaternion.FromToRotation(
                        otherFaceNormalWorld, -targetFaceNormal);
                    rotateObj.transform.localRotation = rotateQuat * rotateObj.transform.localRotation;

                    otherPointWorld = otherTransform.TransformPoint(otherPoint);
                    otherCenterPointWorld = otherTransform.TransformPoint(otherCenterPoint);
                    otherFaceNormalWorld = otherTransform.TransformDirection(otherFaceNormal);

                    Vector3 targetPos = targetPoint + targetFaceNormal.normalized * gap;
                    rotateObj.transform.position += targetPos - otherPointWorld;

                    otherPointWorld = otherTransform.TransformPoint(otherPoint);
                    otherCenterPointWorld = otherTransform.TransformPoint(otherCenterPoint);
                    otherFaceNormalWorld = otherTransform.TransformDirection(otherFaceNormal);

                    rotateAxis = otherFaceNormalWorld.normalized;
                    rotatePoint = rotateObj.transform.position;
                    
                    alignCount += 1;
                }
                else if (alignCount == 1)
                {
                    matchTargetOtherInfo.Add(new KeyValuePair<CurveIndex, CurveIndex>(targetInfo, otherInfo));


                    Vector3 p = targetPoint;
                    Vector3 c = rotatePoint + Vector3.Dot(otherPointWorld - rotatePoint, rotateAxis) / Vector3.Dot(rotateAxis, rotateAxis) * rotateAxis;
                    Vector3 r = otherPointWorld - c;
                    Vector3 q = p + Vector3.Dot(c - p, -rotateAxis.normalized) * -rotateAxis.normalized;
                    Vector3 k = c + (q - c).normalized * r.magnitude;
                    float angle = Vector3.SignedAngle(r, k - c, rotateAxis);

                    rotateObj.transform.RotateAround(rotatePoint, rotateAxis, angle);

                    alignCount += 1;
                }

                groupTransform.parent = rotateObj.transform.parent;
                Destroy(rotateObj);
            }
        }

        return matchTargetOtherInfo;
    }


    static public void FastCompareAlignment(Vector2Int[] matchIdx, CompareShape.MatchCues matchCues, float gap)
    {
        int pointNum = matchIdx.Length;
        int alignCount = 0;
        Vector3 rotateAxis = Vector3.zero;
        Vector3 rotatePoint = Vector3.zero;
        for (int pi = 0; pi < pointNum; pi++)
        {
            int li = matchIdx[pi].x;
            int lj = matchIdx[pi].y;
            CurveIndex targetInfo = new CurveIndex(matchCues.fragID[li], matchCues.faceID[li], 0);
            CurveIndex otherInfo = new CurveIndex(matchCues.fragID[lj], matchCues.faceID[lj], 0);
            Fragment targetFrag = FragmentData.GetFragmentByName(targetInfo.fragmentID);
            Fragment otherFrag = FragmentData.GetFragmentByName(otherInfo.fragmentID);


            Transform targetTransform = matchCues.def[li];
            Transform otherTransform = matchCues.def[lj];

            Vector3 targetPoint = targetTransform.TransformPoint(targetFrag.faceCenter[targetInfo.face]);
            Vector3 targetFaceNormal = targetTransform.TransformDirection(targetFrag.faceNormal[targetInfo.face]);

            Vector3 otherPoint = otherFrag.faceCenter[otherInfo.face];
            Vector3 otherCenterPoint = otherTransform.GetComponent<MeshFilter>().mesh.bounds.center;
            Vector3 otherFaceNormal = otherFrag.faceNormal[otherInfo.face];

            Vector3 otherPointWorld = otherTransform.TransformPoint(otherPoint);
            Vector3 otherCenterPointWorld = otherTransform.TransformPoint(otherCenterPoint);
            Vector3 otherFaceNormalWorld = otherTransform.TransformDirection(otherFaceNormal);


            GameObject rotateObj = new GameObject();
            rotateObj.name = "rotateObj";
            Transform groupTransform = otherTransform.parent.parent.parent;
            rotateObj.transform.position = otherCenterPointWorld;
            rotateObj.transform.parent = groupTransform.parent;
            groupTransform.parent = rotateObj.transform;

            if (alignCount == 0)
            {
                Quaternion rotateQuat = Quaternion.FromToRotation(
                    otherFaceNormalWorld, -targetFaceNormal);
                rotateObj.transform.localRotation = rotateQuat * rotateObj.transform.localRotation;

                otherPointWorld = otherTransform.TransformPoint(otherPoint);
                otherCenterPointWorld = otherTransform.TransformPoint(otherCenterPoint);
                otherFaceNormalWorld = otherTransform.TransformDirection(otherFaceNormal);

                Vector3 targetPos = targetPoint + targetFaceNormal.normalized * gap;
                rotateObj.transform.position += targetPos - otherPointWorld;

                otherPointWorld = otherTransform.TransformPoint(otherPoint);
                otherCenterPointWorld = otherTransform.TransformPoint(otherCenterPoint);
                otherFaceNormalWorld = otherTransform.TransformDirection(otherFaceNormal);

                rotateAxis = otherFaceNormalWorld.normalized;
                rotatePoint = rotateObj.transform.position;

                alignCount += 1;
            }
            else if (alignCount == 1)
            {
                Vector3 p = targetPoint;
                Vector3 c = rotatePoint + Vector3.Dot(otherPointWorld - rotatePoint, rotateAxis) / Vector3.Dot(rotateAxis, rotateAxis) * rotateAxis;
                Vector3 r = otherPointWorld - c;
                Vector3 q = p + Vector3.Dot(c - p, -rotateAxis.normalized) * -rotateAxis.normalized;
                Vector3 k = c + (q - c).normalized * r.magnitude;
                float angle = Vector3.SignedAngle(r, k - c, rotateAxis);

                rotateObj.transform.RotateAround(rotatePoint, rotateAxis, angle);

                alignCount += 1;
            }

            groupTransform.parent = rotateObj.transform.parent;
            Destroy(rotateObj);
        }
    }


    static public bool HasMatchPotential(GroupMatch groupMatch, GroupManager groupManager, float threshold = 0.9f)
    {
        Transform[] targetTransform = groupManager.GetBrotherNodes(
            groupManager.GetTransformByName(groupMatch.targetLinks[0].Key.fragmentID)[0]).ToArray();

        Transform[] otherTransform = groupMatch.otherLinks
            .Select(l => groupManager.GetTransformByName(l.Key.fragmentID)[0]).ToArray();


        List<Transform[]> allTransformGroups = otherTransform
            .GroupBy(t => t.parent)
            .Select(g => g.ToArray())
            .ToList();
        allTransformGroups.Add(targetTransform);

        
        List<Bounds[]> transformBounds = allTransformGroups
            .Select(group => group
                .Select(t => t.GetChild(0).GetChild(0).GetComponent<Renderer>().bounds)
                .ToArray())
            .ToList();

        List<float[]> shortLen = transformBounds
            .Select(group => group
                .Select(t => t.size)
                .Select(size => Mathf.Min(Mathf.Min(size.x, size.y), size.z) / 2.0f)
                .ToArray())
            .ToList();

        for (int gi = 0; gi < allTransformGroups.Count; gi++)
            for (int gj = gi + 1; gj < allTransformGroups.Count; gj++)
                for (int ti = 0; ti < allTransformGroups[gi].Length; ti++)
                    for (int tj = 0; tj < allTransformGroups[gj].Length; tj++)
                        if ((transformBounds[gi][ti].center - transformBounds[gj][tj].center).magnitude <
                            (shortLen[gi][ti] + shortLen[gj][tj]) * threshold)
                            return false;

        return true;
    }


    static public bool AvoidImpossibleMatch(GroupMatch groupMatch, GroupManager groupManager, float threshold = 0.5f)
    {
        var targetLinks = groupMatch.targetLinks;
        var otherLinks = groupMatch.otherLinks;


        List<Transform> otherFragTransform = new List<Transform>();
        foreach (var li in otherLinks)
            otherFragTransform.Add(FragmentData.GetFragmentByName(li.Key.fragmentID).objMesh.transform);
        int[][] otherGroupIdx =
            groupManager.GetSecondaryParent(otherFragTransform, out List<Transform> otherSubGroupTransform);


        var groupTargetLinks = new List<List<KeyValuePair<CurveIndex, CurveIndex>>>();
        var groupOtherLinks = new List<List<KeyValuePair<CurveIndex, CurveIndex>>>();
        for (int gi = 0; gi < otherGroupIdx.Length; gi++)
        {
            var subGroupTargetLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
            var subGroupOtherLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
            foreach (var pairIdx in otherGroupIdx[gi])
            {
                subGroupTargetLinks.Add(targetLinks[pairIdx]);
                subGroupOtherLinks.Add(otherLinks[pairIdx]);
            }
            groupTargetLinks.Add(subGroupTargetLinks);
            groupOtherLinks.Add(subGroupOtherLinks);
        }

        int subGroupNum = otherSubGroupTransform.Count;
        for (int subGroupi = 0; subGroupi < subGroupNum; subGroupi++)
        {
            int pointNum = groupTargetLinks[subGroupi].Count;
            Vector3[] targetFacePoints = new Vector3[pointNum];
            Vector3[] otherFacePoints = new Vector3[pointNum];
            for (int pi = 0; pi < pointNum; pi++)
            {
                CurveIndex targetInfo = groupTargetLinks[subGroupi][pi].Key;
                CurveIndex otherInfo = groupOtherLinks[subGroupi][pi].Key;
                Fragment targetFrag = FragmentData.GetFragmentByName(targetInfo.fragmentID);
                Fragment otherFrag = FragmentData.GetFragmentByName(otherInfo.fragmentID);


                Transform targetTransform = targetFrag.objMesh.transform.Find(FragmentTypes.Default);
                Transform otherTransform = otherFrag.objMesh.transform.Find(FragmentTypes.Default);

                targetFacePoints[pi] = targetTransform.TransformPoint(targetFrag.faceCenter[targetInfo.face]);
                otherFacePoints[pi] = otherTransform.TransformPoint(otherFrag.faceCenter[otherInfo.face]);
            }


            float minDiff = threshold;
            float maxDiff = 1.0f / threshold;
            for (int i = 0; i < pointNum - 1; i++)
                for (int j = i + 1; j < pointNum; j++)
                {
                    float targetPointsDis = (targetFacePoints[i] - targetFacePoints[j]).magnitude;
                    float otherPointsDis = (otherFacePoints[i] - otherFacePoints[j]).magnitude;
                    float disDiff = targetPointsDis / otherPointsDis;
                    if (disDiff < minDiff || disDiff > maxDiff)
                        return false;
                }
        }

        return true;
    }

    static public void ClearDebug()
    {
        debugPos.Clear();
        debugPosColor.Clear();
        debugPos2.Clear();
        debugPosColor2.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!showDebug)
            return;

        for (int i = 0; i < debugPos.Count; i++)
        {
            Gizmos.color = debugPosColor[i];
            Gizmos.DrawSphere(debugPos[i], 0.1f);
        }
        
        for (int i = 0; i < debugPos2.Count; i += 2)
        {
            Gizmos.color = debugPosColor2[i];
            Gizmos.DrawLine(debugPos2[i], debugPos2[i + 1]);
        }
    }
}

