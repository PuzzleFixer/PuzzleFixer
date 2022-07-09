using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Specialized;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using ProtoBuf;
using System.IO;

public class SmallMultiplesAround : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool smallMultiplesAround = false;
    private bool smallMultiplesAroundLast = false;

    [Header("settings")]
    [SerializeField] int maxCandidateNum = 100;
    [SerializeField] [Range(0.0f, 1.0f)] public float smallSkeletonScale = 0.1f;
    [SerializeField] public float planeDepth = 3.0f;
    [SerializeField] public float planeHeight = 3.0f;
    [SerializeField] public float planeWidth = 5.0f;
    [SerializeField] public Vector2 planeOffset = new Vector2(0, 0);
    [SerializeField] private Vector3 cameraPos = new Vector3(0, -1.2f, 5);
    [SerializeField] private Vector3 cameraEuler = new Vector3(0, -90, 0);
    [SerializeField] private bool doNotDrawAvgOtherFragNode = false;
    [SerializeField] private float neighborMatchPointTh = 0.8f;
    [SerializeField] private float colideTh = 0.9f;
    [SerializeField] private float shapeTh = 0.15f;
    static private int round = -1;

    public bool showSmallMultiplesFinish = false;

    private ListCandidate candidates;
    private GroupManager groupManager;
    private MessageManager messageManager;
    private NodeVisualizer smallSkeletonNodeVizor;  
    private CurveVisualizer smallSkeletonCurveVizor;
    private SelectTarget selectTarget;
    private Overview overview;
    private SwitchSkeleton switchSkeleton;

    //********* results *********//
    public List<GroupMatch> selectGroupMatch;       
    public List<List<string>> nodeFragName;         
    public List<List<Vector3>> skeletonNodePoint;
    public List<List<Vector4>> skeletonNodeColor;
    public List<List<KeyValuePair<int, string>>> curveInfo; 
    public List<List<Vector3[]>> skeletonCurvePoint;
    public List<List<Vector4>> skeletonCurveColor;   
    public List<Vector3> skeletonCenter;
    [HideInInspector] public List<KeyValuePair<string, KeyValuePair<Vector3, Quaternion>>[]> fragCenterAlignResult;

    //********* results of grouped skeletons *********//
    public int[] candidateClassTag; 
                                    
    public List<List<string>> gNodeFragName;    
    public List<List<Vector3>> gSkeletonNodePoint;
    public List<List<Vector4>> gSkeletonNodeColor;
    public List<List<Vector3[]>> gSkeletonCurvePoint;
    public List<List<Vector4>> gSkeletonCurveColor;   
    public List<Vector3> gSkeletonCenter;
    public GameObject targetGroupObj;   
    private GameObject Container;   
    public List<float> gAvgMatchScore;

    //********* results of filtered original skeletons *********//
    
    public List<int> fOrignalIdx;   
    public List<List<string>> fNodeFragName;    
    public List<List<Vector3>> fSkeletonNodePoint;
    public List<List<Vector4>> fSkeletonNodeColor;
    public List<List<Vector3[]>> fSkeletonCurvePoint;
    public List<List<Vector4>> fSkeletonCurveColor;
    public List<Vector3> fSkeletonCenter;

    public Vector3 bbSize;
    [SerializeField] float scaleBBSize = 0.55f;

    [SerializeField] private bool forceReSaveCache = false;
    private string savePath;
    [ProtoContract, Serializable]
    class DRInOut
    {
        [ProtoMember(1)]
        public List<GroupMatch> selectGroupMatch;
        [ProtoMember(2)]
        public byte[] output;

        public DRInOut()
        {
            selectGroupMatch = new List<GroupMatch>();
            output = null;
        }
    }
    [ProtoContract, Serializable]
    class ListDRInOut
    {
        [ProtoMember(1)]
        public List<DRInOut> listDRInOut;

        public ListDRInOut()
        {
            listDRInOut = new List<DRInOut>();
        }

        public float Count
        {
            get { return listDRInOut.Count; }
        }
    }
    private ListDRInOut listDRInOut;

    public GUIProcess guiProcess = new GUIProcess();

    private void Start()
    {
        candidates = GameObject.Find("ListCandidate").GetComponent<ListCandidate>();
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
        messageManager = GameObject.Find("MessageManager").GetComponent<MessageManager>();
        smallSkeletonNodeVizor = transform.Find("SmallSkeletonNode").GetComponent<NodeVisualizer>();
        smallSkeletonCurveVizor = transform.Find("SmallSkeletonCurve").GetComponent<CurveVisualizer>();
        selectTarget = GameObject.Find("SelectTarget").GetComponent<SelectTarget>();
        overview = GameObject.Find("Overview").GetComponent<Overview>();
        switchSkeleton = GameObject.Find("SwitchSkeleton").GetComponent<SwitchSkeleton>();
        fragCenterAlignResult = new List<KeyValuePair<string, KeyValuePair<Vector3, Quaternion>>[]>();

        savePath = Application.dataPath + "/Resources/ListPotentialCache/DRInOut";
        if (forceReSaveCache)
            listDRInOut = null;
        else
            LoadCache();
    }

    void Update()
    {
        if (smallMultiplesAround)
        {
            if (smallMultiplesAroundLast == false)  
            {
                if (candidates.potentialMatches.Count > 0)
                {
                    Debug.Log("Showing candidates overview...");
                    fragmentRef = FragmentData.fragments;
                    selectGroupMatch = null;
                    skeletonNodePoint = new List<List<Vector3>>();
                    skeletonNodeColor = new List<List<Vector4>>();
                    curveInfo = new List<List<KeyValuePair<int, string>>>();
                    skeletonCurvePoint = new List<List<Vector3[]>>();
                    skeletonCurveColor = new List<List<Vector4>>();
                    skeletonCenter = new List<Vector3>();
                    nodeFragName = new List<List<string>>();

                    candidateClassTag = null;
                    targetGroupObj = null;
                    gSkeletonNodePoint = new List<List<Vector3>>();
                    gSkeletonNodeColor = new List<List<Vector4>>();
                    gSkeletonCurvePoint = new List<List<Vector3[]>>();
                    gSkeletonCurveColor = new List<List<Vector4>>();
                    gSkeletonCenter = new List<Vector3>();
                    gNodeFragName = new List<List<string>>();
                    gAvgMatchScore = new List<float>();    

                    fragCenterAlignResult = new List<KeyValuePair<string, KeyValuePair<Vector3, Quaternion>>[]>();

                    bbSize = Vector3.zero;
                }
                else
                {
                    Debug.LogWarning("No candidates detect!");
                    smallMultiplesAround = false;
                    return;
                }

                round += 1;
                StartCoroutine(GetCandidatesTable());
            }

            smallMultiplesAroundLast = true;
        }
        else if (smallMultiplesAroundLast)
        {
            smallSkeletonNodeVizor.ClearNodes();
            smallSkeletonCurveVizor.ClearCurve();

            smallMultiplesAroundLast = false;
        }
    }

    private IEnumerator GetCandidatesTable()
    {
        guiProcess.showGUI = true;

        selectGroupMatch = new List<GroupMatch>();
        List<Vector2Int> fragSkeletonIdx = selectTarget.skeletonIdxSelect;
        foreach (var match in candidates.potentialMatches)
        {
            int matchCount = 0;
            foreach (var link in match.targetLinks)
                foreach (var select in fragSkeletonIdx)
                    if (fragmentRef[select.x].parent.name == link.Key.fragmentID &&
                        select.y == link.Key.face)
                        matchCount += 1;
            if (matchCount == 0)
                foreach (var link in match.otherLinks)
                    foreach (var select in fragSkeletonIdx)
                        if (fragmentRef[select.x].parent.name == link.Key.fragmentID &&
                            select.y == link.Key.face)
                            matchCount += 1;


            if (matchCount == candidates.maxMatchCount)
                selectGroupMatch.Add(match);
        }

        if (selectGroupMatch.Count == 0)
        {
            Debug.LogError("no potential candidate matched!");
            yield break;
        }

        if (selectGroupMatch.Count > maxCandidateNum)
        {
            selectGroupMatch = selectGroupMatch
                .OrderByDescending(m => m.matchScore)
                .ToList()
                .GetRange(0, maxCandidateNum);
        }


        for (int i = 0; i < selectGroupMatch.Count; i++)
        {
            var curGroupMatch = selectGroupMatch[i];
            var sortLink = curGroupMatch.targetLinks
                .Select((l, idx) => new KeyValuePair<KeyValuePair<CurveIndex, CurveIndex>, int>(l, idx))
                .OrderBy(s => s.Key.Key.fragmentID)
                .ThenBy(s => s.Key.Key.face)
                .ToList();
            curGroupMatch.targetLinks = sortLink.Select(s => s.Key).ToList();
            var ol = curGroupMatch.otherLinks;
            curGroupMatch.otherLinks = sortLink.Select(s => ol[s.Value]).ToList();
        }


        int candidateNum = selectGroupMatch.Count;
        

        List<List<Vector3>> SkeletonNFPosition = new List<List<Vector3>>();
        var SkeletonNFSign = new List<List<KeyValuePair<int, string>>>();
        List<Vector3> candidateBBSize = new List<Vector3>();


        HashSet<string> matchLinkCache = new HashSet<string>();
        List<List<Vector3>> matchPosCache = new List<List<Vector3>>();  

        var selectGroupMatchFilter = new List<GroupMatch>();    
        groupManager.CenterFragment(FragmentData.GetFragmentByName(selectGroupMatch[0].targetLinks[0].Key.fragmentID),
            Vector3.zero, out GameObject centerObj, out Vector3 centerPos, out Quaternion centerRot); 

        // *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** //

        bool[] canMatch = new bool[candidateNum];
        float minScore = float.MaxValue;
        float maxScore = float.MinValue;
        for (int i = 0; i < candidateNum; i++)
        {
            if (candidateNum > 100)
            {
                if (i >= 10)     
                {
                    bool possibleMatch = PairRegister.AvoidImpossibleMatch(selectGroupMatch[i], groupManager, neighborMatchPointTh);
                    if (possibleMatch == false)
                        continue;
                }

                var matchTargetOtherInfo = PairRegister.FastAlignment(selectGroupMatch[i], groupManager, fragmentRef);
                
                string matchCode = "";
                for (int mInfoi = 0; mInfoi < matchTargetOtherInfo.Count; mInfoi++)
                    matchCode += matchTargetOtherInfo[mInfoi].Key.fragmentID + matchTargetOtherInfo[mInfoi].Key.face.ToString()
                        + matchTargetOtherInfo[mInfoi].Value.fragmentID + matchTargetOtherInfo[mInfoi].Value.face.ToString();
                if (matchLinkCache.Contains(matchCode))
                    continue;
                matchLinkCache.Add(matchCode);

                if (i >= 10)     
                {
                    bool canbeMatched = PairRegister.HasMatchPotential(selectGroupMatch[i], groupManager, colideTh);
                    if (canbeMatched == false)
                        continue;
                }

                var fragsName = selectGroupMatch[i].targetLinks
                    .Select(l => l.Key.fragmentID).ToList();
                fragsName.AddRange(selectGroupMatch[i].otherLinks
                    .Select(l => l.Key.fragmentID));
                fragsName = fragsName.OrderBy(n => n).Distinct().ToList();
                var frags = fragsName.Select(n => FragmentData.GetFragmentByName(n));
                List<Vector3> fragsCenterPos = new List<Vector3>();
                foreach (var f in frags)
                    fragsCenterPos.Add(f.objMesh.transform.Find(FragmentTypes.Default)
                        .GetComponent<Renderer>().bounds.center);
                int fragsCount = fragsCenterPos.Count;
                bool flag = false;
                foreach (var matchPosCachei in matchPosCache)
                    if (matchPosCachei.Count == fragsCount)
                    {
                        float maxDis = 0;
                        for (int cj = 0; cj < fragsCount; cj++)
                        {
                            float curDis = (matchPosCachei[cj] - fragsCenterPos[cj]).magnitude;
                            if (curDis > maxDis)
                                maxDis = curDis;
                        }
                        if (maxDis < shapeTh)
                        {
                            flag = true;
                            break;
                        }
                    }
                if (flag)
                    continue;
                matchPosCache.Add(fragsCenterPos);
            }

            canMatch[i] = true;
            var score = selectGroupMatch[i].matchScore;
            if (score < minScore)
                minScore = score;
            if (score > maxScore)
                maxScore = score;
        }



        float scoreRange = maxScore - minScore;
        for (int i = 0; i < candidateNum; i++)
        {
            if (canMatch[i] == false)
                continue;


            var matchTargetOtherInfo = PairRegister.FastAlignment(selectGroupMatch[i], groupManager, fragmentRef);

            selectGroupMatchFilter.Add(selectGroupMatch[i]);

            FragmentData.GetCandidatePointCloud(
                selectGroupMatch[i], groupManager,
                out KeyValuePair<string, KeyValuePair<Vector3, Quaternion>>[] fragCenters,
                out List<Vector3> currentSkeletonNFPosition,
                out List<KeyValuePair<int, string>> currentSign);
            fragCenterAlignResult.Add(fragCenters);
            SkeletonNFSign.Add(currentSign);
            SkeletonNFPosition.Add(currentSkeletonNFPosition);


            int[] fragsIdx = FragmentData.GetRefFragmentofGroupMatch(selectGroupMatch[i], groupManager);    
            Vector3[] bmin = new Vector3[fragsIdx.Length];
            Vector3[] bmax = new Vector3[fragsIdx.Length];
            for (int fi = 0; fi < fragsIdx.Length; fi++)
            {
                int fidx = fragsIdx[fi];
                Transform deft = fragmentRef[fidx].objMesh.transform.GetChild(0);
                bmin[fi] = deft.GetComponent<Renderer>().bounds.min;
                bmax[fi] = deft.GetComponent<Renderer>().bounds.max;
            }
            candidateBBSize.Add(new Vector3(bmax.Max(b => b.x) - bmin.Min(b => b.x),
                bmax.Max(b => b.y) - bmin.Min(b => b.y),
                bmax.Max(b => b.z) - bmin.Min(b => b.z)));


            List<Vector3> skeletonDrawPosition = new List<Vector3>(currentSkeletonNFPosition);
            for (int pi = 0; pi < skeletonDrawPosition.Count; pi++)
                if (skeletonDrawPosition[pi] != Vector3.one * 100)
                    skeletonDrawPosition[pi] = skeletonDrawPosition[pi] * smallSkeletonScale;   


            Color[] nodeColor = new Color[] { 
                MeshColor.matchBest,   
                Color.Lerp(MeshColor.matchWorst, MeshColor.matchBest, (selectGroupMatch[i].matchScore - minScore) / scoreRange) };
            List<Vector3[]> currentCurvePoint = new List<Vector3[]>();
            List<Vector4> currentCurveColor = new List<Vector4>();
            List<Vector3> currentNodePoint = new List<Vector3>();
            List<Vector4> currentNodeColor = new List<Vector4>();
            List<string> currentNodeFragName = new List<string>();
            var endNodeInfo = new List<KeyValuePair<int, int>>();       
            var endNodeSign = new List<KeyValuePair<int, string>>();    
            for (int key = -1; key <= 0; key++)
            {
                var candidateFragKey = currentSign.Select(s => s.Key == key).ToList();
                var candidateFragCenter = skeletonDrawPosition
                    .Where((item, idx) => candidateFragKey[idx] == true)
                    .ToList();
                var candidateFragName = currentSign.Where(s => s.Key == key).Select(s => s.Value);

                currentNodePoint.AddRange(candidateFragCenter);
                currentNodeColor.AddRange(Enumerable.Repeat((Vector4)nodeColor[-key], candidateFragCenter.Count).ToList());
                currentNodeFragName.AddRange(candidateFragName);

                List<int> candidateFragIdx = currentSign
                    .Select((item, idx) => new { item, idx })
                    .Where(s => s.item.Key == key)
                    .Select(s => s.idx)
                    .ToList();  
                for (int nodei = 0; nodei < candidateFragIdx.Count; nodei++)
                {
                    int fragNodeIdx = candidateFragIdx[nodei];
                    var currentNode = currentSign[fragNodeIdx];
                    Fragment currentFrag = FragmentData.GetFragmentByName(currentNode.Value);
                    int nodeIdx = fragNodeIdx + 1;  
                    while (nodeIdx < currentSign.Count && currentSign[nodeIdx].Key > 0) 
                    {
                        int faceIdx = Convert.ToInt32(currentSign[nodeIdx].Value);
                        if (currentSign[nodeIdx].Key == 1) 
                        {
                            Vector3[] curveEndPoint = new Vector3[] {
                                skeletonDrawPosition[fragNodeIdx], Vector3.zero };
                            string pairFragName = currentFrag.skeletonLink
                                .Where(s => s.Key.face == faceIdx)
                                .Select(s => s.Value.fragmentID)
                                .First();
                            int pairIdx = currentSign.FindIndex(s => s.Value == pairFragName);
                            if (skeletonDrawPosition[pairIdx] != Vector3.one * 100) 
                            {
                                curveEndPoint[1] = skeletonDrawPosition[pairIdx];
                                currentCurvePoint.Add(curveEndPoint);
                                currentCurveColor.Add(nodeColor[-key]);
                                currentCurveColor.Add(nodeColor[-key]);
                                endNodeInfo.Add(new KeyValuePair<int, int>(fragNodeIdx, pairIdx));
                                endNodeSign.Add(currentSign[fragNodeIdx]);
                                endNodeSign.Add(currentSign[pairIdx]);
                            }
                        }
                        else if (currentSign[nodeIdx].Key == 3)     
                        {
                            Vector3[] curveEndPoint = new Vector3[] {
                                skeletonDrawPosition[fragNodeIdx],
                                skeletonDrawPosition[nodeIdx] };

                            currentCurvePoint.Add(curveEndPoint);
                            currentCurveColor.Add(nodeColor[-key]);
                            currentCurveColor.Add(nodeColor[-key]);
                            endNodeInfo.Add(new KeyValuePair<int, int>(fragNodeIdx, -1));
                            endNodeSign.Add(currentSign[fragNodeIdx]);
                            endNodeSign.Add(currentSign[nodeIdx]);
                        }
                        nodeIdx += 1;
                    }
                }
            }
            skeletonNodePoint.Add(currentNodePoint);
            skeletonNodeColor.Add(currentNodeColor);
            nodeFragName.Add(currentNodeFragName);


            List<int> removeIdxs = new List<int>();
            for (int infoi = 0; infoi < endNodeInfo.Count; infoi++)
                if (endNodeInfo[infoi].Value != -1)
                {
                    int pairidx = endNodeInfo.FindIndex(
                        e => e.Key == endNodeInfo[infoi].Value && e.Value == endNodeInfo[infoi].Key);
                    if (pairidx >= 0)
                    {
                        removeIdxs.Add(pairidx);
                        currentCurveColor[infoi * 2 + 1] = currentCurveColor[pairidx * 2];
                        endNodeInfo[pairidx] = new KeyValuePair<int, int>(endNodeInfo[pairidx].Key, -1);    
                    }
                }


            removeIdxs = removeIdxs.OrderByDescending(r => r).ToList();
            foreach (int removeIdx in removeIdxs)
            {
                currentCurvePoint.RemoveAt(removeIdx);
                currentCurveColor.RemoveAt(removeIdx * 2 + 1);
                currentCurveColor.RemoveAt(removeIdx * 2);
                endNodeSign.RemoveAt(removeIdx * 2 + 1);
                endNodeSign.RemoveAt(removeIdx * 2);
            }


            Vector3 avgPos = Vector3.zero;
            foreach (var p in currentCurvePoint)
                avgPos += p[0] + p[1];
            skeletonCenter.Add(avgPos / (currentCurvePoint.Count * 2));

            skeletonCurvePoint.Add(currentCurvePoint);
            skeletonCurveColor.Add(currentCurveColor);
            curveInfo.Add(endNodeSign);

            if (i % 10 == 0)
            {
                guiProcess.progress = (float)i / candidateNum;
                guiProcess.guiText = "Matching " + i + "/" + candidateNum
                    + " current (" + (guiProcess.progress * 100.0f).ToString("f2") + "%)";
                yield return null;
            }
        }

        // *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* *_* //

        groupManager.DeleteCenterFragment(centerObj, centerPos, centerRot);
        selectGroupMatch = selectGroupMatchFilter;
        candidateNum = selectGroupMatch.Count;

        guiProcess.showGUI = false;


        bbSize = new Vector3(candidateBBSize.Max(s => s.x), candidateBBSize.Max(s => s.y), candidateBBSize.Max(s => s.z));


        float[] flatPos = new float[SkeletonNFPosition[0].Count * 3 * candidateNum];

        int flatPosi = 0;
        for (int i = 0; i < candidateNum; i++)
        {
            bool[] matchPointInCandidate = new bool[SkeletonNFPosition[i].Count];
            int[] matchIdx = SkeletonNFSign[i]
                .Select((s, idx) => new KeyValuePair<KeyValuePair<int,string>, int>(s, idx))
                .Where(s => s.Key.Key == 3)
                .Select(s => s.Value)
                .ToArray();
            for (int mi = 0; mi < matchIdx.Length; mi++)
            {
                int nfidx = matchIdx[mi];
                matchPointInCandidate[nfidx] = true;


                nfidx -= 1;
                while (nfidx >= 0)  
                {
                    int sign = SkeletonNFSign[i][nfidx].Key;
                    if (sign == 0 || sign == -1)
                    {
                        matchPointInCandidate[nfidx] = true;
                        break;
                    }
                    nfidx -= 1;
                }
            }

            for (int pi = 0; pi < SkeletonNFPosition[i].Count; pi++)
            {
                if (matchPointInCandidate[pi])
                {
                    flatPos[flatPosi] = SkeletonNFPosition[i][pi].x;
                    flatPos[flatPosi + 1] = SkeletonNFPosition[i][pi].y;
                    flatPos[flatPosi + 2] = SkeletonNFPosition[i][pi].z;
                }
                else
                {
                    flatPos[flatPosi] = 100;
                    flatPos[flatPosi + 1] = 100;
                    flatPos[flatPosi + 2] = 100;
                }
                flatPosi += 3;
            }
        }


        byte[] bytePos = new byte[flatPos.Length * sizeof(float)];
        Buffer.BlockCopy(flatPos, 0, bytePos, 0, bytePos.Length);
        bool hit = CacheHit(selectGroupMatch, out byte[] output);
        if (!hit)
            messageManager.SendMessage(candidateNum, bytePos, MessageManager.Command.tSNECluster, ShowSmallMultiples);
        else
            StartCoroutine(ShowSmallMultiples(output));

        yield return null;
    }


    private IEnumerator ShowSmallMultiples(byte[] output)
    {
        float[] clusterSkeletonInfo = new float[output.Length / sizeof(float)];
        Buffer.BlockCopy(output, 0, clusterSkeletonInfo, 0, output.Length);

        int skeletonNum = clusterSkeletonInfo.Length / 3;
        Vector3[] sktPosition = new Vector3[skeletonNum];
        int[] sktClass = new int[skeletonNum];  
        for (int i = 0; i < skeletonNum; i++)
        {
            sktPosition[i] = new Vector3(clusterSkeletonInfo[i * 3], clusterSkeletonInfo[i * 3 + 1], planeDepth);
            sktClass[i] = Convert.ToInt32(clusterSkeletonInfo[i * 3 + 2]);
        }


        float scatterLeft = sktPosition.Min(p => p.x);
        float scatterBottom = sktPosition.Min(p => p.y);
        float scatterRight = sktPosition.Max(p => p.x);
        float scatterUp = sktPosition.Max(p => p.y);
        for (int i = 0; i < skeletonNum; i++)   
        {
            sktPosition[i].x = (sktPosition[i].x - scatterLeft) / (scatterRight - scatterLeft) * planeWidth - planeWidth / 2 + planeOffset.x;
            sktPosition[i].y = (sktPosition[i].y - scatterBottom) / (scatterUp - scatterBottom) * planeHeight - planeHeight / 2 + planeOffset.y;
        }

        for (int i = 0; i < skeletonNum; i++)
        {
            Vector3 shiftVec = sktPosition[i] - skeletonCenter[i];
            for (int pi = 0; pi < skeletonNodePoint[i].Count; pi++)
                skeletonNodePoint[i][pi] += shiftVec;

            for (int pi = 0; pi < skeletonCurvePoint[i].Count; pi++)
            {
                skeletonCurvePoint[i][pi][0] += shiftVec;
                skeletonCurvePoint[i][pi][1] += shiftVec;
            }
            skeletonCenter[i] = sktPosition[i];
        }

        int curveCount = skeletonCurvePoint[0].Count;
        for (int i = 1; i < skeletonCurvePoint.Count; i++)  
            if (skeletonCurvePoint[i].Count != curveCount)
                Debug.LogError("There are different number of links in skeletons!");


        int classNum = sktClass.Distinct().Count();
        candidateClassTag = new int[selectGroupMatch.Count];
        for (int i = 0; i < classNum; i++)
        {
            var idx = sktClass.Select((c, ci) => new { c, ci })
                    .Where(x => x.c == i)
                    .Select(x => x.ci).ToList();


            List<int> nodeCount = new List<int>();
            List<Vector2Int> nodeCountIdx = new List<Vector2Int>();
            for (int skti = 0; skti < idx.Count; skti++)
            {
                int nodeCountCurrent = skeletonNodePoint[idx[skti]].Count;
                nodeCount.Add(nodeCountCurrent);
                nodeCountIdx.Add(new Vector2Int(nodeCountCurrent, idx[skti]));
            }
            nodeCount = nodeCount.Distinct().ToList();

            for (int groupi = 0; groupi < nodeCount.Count; groupi++)
            {
                var groupIdx = nodeCountIdx.Where(nc => nc.x == nodeCount[groupi]).Select(nc => nc.y).ToList(); 


                Vector3 center = Vector3.zero;
                int nodeNum = nodeCount[groupi];
                Vector3[] node = new Vector3[nodeNum];
                Vector4[] ncolor = new Vector4[nodeNum];
                List<string> nodeName = new List<string>();     


                float scoreSum = 0; 
                var allTargetOpenlink = new List<CurveIndex>();  
                for (int j = 0; j < groupIdx.Count; j++) 
                {
                    int skii = groupIdx[j];
                    var currentMatch = selectGroupMatch[skii];
                    scoreSum += currentMatch.matchScore;

                    allTargetOpenlink.AddRange(currentMatch.targetLinks.Select(l => l.Key));
                }
                var targetOpenlinkGroup = allTargetOpenlink
                    .GroupBy(l => l)
                    .ToList();
                int allTargetOpenLinkNum = targetOpenlinkGroup.Count;
                int oneTargetOpenLinkGroupNum = selectGroupMatch[0].targetLinks.Count;
                int cnodeNum = curveCount - oneTargetOpenLinkGroupNum + allTargetOpenLinkNum;
                Vector3[][] cnode = new Vector3[cnodeNum][];
                for (int ci = 0; ci < cnodeNum; ci++)
                    cnode[ci] = new Vector3[2];
                Vector4[] ccolor = new Vector4[cnodeNum * 2];


                var targetCurvePairInfo = new List<List<KeyValuePair<Vector3Int, Vector3Int>>>(); 
                for (int j = 0; j < groupIdx.Count; j++) 
                {
                    int ski = groupIdx[j];

                    float weight = 1.0f / groupIdx.Count;

                    center += skeletonCenter[ski] * weight;
                    candidateClassTag[ski] = gSkeletonCenter.Count;


                    int nodei = 0;
                    var currentGroupMatch = selectGroupMatch[ski];
                    List<string> otherNodeName = currentGroupMatch.otherLinks
                        .Select(l => l.Key.fragmentID)
                        .Distinct()
                        .ToList();
                    int matchNum = otherNodeName.Count;
                    List<string> currentNodeName = nodeFragName[ski];
                    nodeName = currentNodeName;
                    for (int nOti = 0; nOti < matchNum; nOti++)
                    {
                        string otherNodeNamei = otherNodeName[nOti];
                        int mergeNodei = currentNodeName.FindIndex(n => n == otherNodeNamei);
                        node[nodei] += skeletonNodePoint[ski][mergeNodei] * weight;
                        ncolor[nodei] += skeletonNodeColor[ski][mergeNodei] * weight;
                        if (doNotDrawAvgOtherFragNode)
                            ncolor[nodei].w = 0;
                        nodei += 1;
                    }
                    var leftNodeName = currentNodeName.Except(otherNodeName).ToList();
                    for (int nTgi = 0; nTgi < leftNodeName.Count; nTgi++)
                    {
                        int mergeNodei = currentNodeName.FindIndex(n => n == leftNodeName[nTgi]);
                        node[nodei] += skeletonNodePoint[ski][mergeNodei] * weight;
                        ncolor[nodei] += skeletonNodeColor[ski][mergeNodei] * weight;
                        nodei += 1;
                    }


                    int curvei = 0;
                    var currentCurveInfo = curveInfo[ski]
                        .Select((info, infoi) => new KeyValuePair<KeyValuePair<int, string>, int>(info, infoi))
                        .ToList();
                    var otherLinks = currentGroupMatch.otherLinks.Select(l => l.Key).ToList();
                    matchNum = otherLinks.Count;
                    var mInfoIdx = new List<int>();

                    for (int nOti = 0; nOti < matchNum; nOti++)
                    {
                        int mergeCurvei = currentCurveInfo
                            .FindIndex(info => info.Key.Value == otherLinks[nOti].fragmentID
                            && currentCurveInfo[info.Value + 1].Key.Value == otherLinks[nOti].face.ToString());
                        mInfoIdx.Add(mergeCurvei);
                        mInfoIdx.Add(mergeCurvei + 1);
                        mergeCurvei /= 2;

                        cnode[curvei][0] += skeletonCurvePoint[ski][mergeCurvei][0] * weight;
                        cnode[curvei][1] += skeletonCurvePoint[ski][mergeCurvei][1] * weight;
                        ccolor[curvei * 2] += skeletonCurveColor[ski][mergeCurvei * 2] * weight;
                        ccolor[curvei * 2 + 1] += skeletonCurveColor[ski][mergeCurvei * 2 + 1] * weight;
                        curvei += 1;
                    }

                    mInfoIdx = mInfoIdx.OrderByDescending(oti => oti).ToList();
                    for (int mi = 0; mi < mInfoIdx.Count; mi++)
                        currentCurveInfo.RemoveAt(mInfoIdx[mi]);
                    
                    var leftCurvePairInfo = new List<KeyValuePair<Vector3Int, Vector3Int>>();
                    List<CurveIndex> currentTargetOpenLink = new List<CurveIndex>();
                    for (int cci = 0; cci < currentCurveInfo.Count; cci += 2)
                    {
                        int ccj = cci, cck = 1;
                        if (currentCurveInfo[cci].Key.Key == currentCurveInfo[cci + 1].Key.Key)
                        {
                            if (string.Compare(currentCurveInfo[cci].Key.Value, currentCurveInfo[cci + 1].Key.Value) > 0)
                            {
                                ccj = cci + 1;
                                cck = -1;
                            }
                            leftCurvePairInfo.Add(new KeyValuePair<Vector3Int, Vector3Int>(
                                new Vector3Int(currentCurveInfo[ccj].Key.Key,
                                FragmentData.GetFragmentIndexByName(currentCurveInfo[ccj].Key.Value),
                                currentCurveInfo[ccj].Value),
                                new Vector3Int(currentCurveInfo[ccj + cck].Key.Key,
                                FragmentData.GetFragmentIndexByName(currentCurveInfo[ccj + cck].Key.Value),
                                currentCurveInfo[ccj + cck].Value)));
                        }
                        else
                        {
                            if (currentCurveInfo[cci].Key.Key > currentCurveInfo[cci + 1].Key.Key)
                            {
                                ccj = cci + 1;
                                cck = -1;
                            }
                            leftCurvePairInfo.Add(new KeyValuePair<Vector3Int, Vector3Int>(
                                new Vector3Int(currentCurveInfo[ccj].Key.Key,
                                FragmentData.GetFragmentIndexByName(currentCurveInfo[ccj].Key.Value),
                                currentCurveInfo[ccj].Value),
                                new Vector3Int(currentCurveInfo[ccj + cck].Key.Key,
                                Convert.ToInt32(currentCurveInfo[ccj + cck].Key.Value),
                                currentCurveInfo[ccj + cck].Value)));
                        }
                    }

                    leftCurvePairInfo = leftCurvePairInfo
                        .GroupBy(info => info.Value.x)  
                        .Select(group => group
                            .OrderBy(info => info.Key.y)
                            .ThenBy(info => info.Value.y))
                        .SelectMany(group => group)
                        .ToList();
                    targetCurvePairInfo.Add(leftCurvePairInfo);
                }

                var targetOpenLink = targetOpenlinkGroup.Select(g => g.Key).ToList();
                var targetOpenLinkCount = targetOpenlinkGroup.Select(g => g.Count()).ToList();
                for (int j = 0; j < groupIdx.Count; j++) 
                {
                    int ski = groupIdx[j];
                    int curveidx = oneTargetOpenLinkGroupNum;   

                    float weight = 1.0f / groupIdx.Count;


                    var currentCurvePairFragInfo = targetCurvePairInfo[j]
                        .Where(info => info.Value.x == 0)
                        .ToList();
                    for (int nTgi = 0; nTgi < currentCurvePairFragInfo.Count; nTgi++)
                    {
                        int mergeCurvei = currentCurvePairFragInfo[nTgi].Key.z;
                        int mergeCurvej = currentCurvePairFragInfo[nTgi].Value.z;
                        int mcii = mergeCurvei / 2, mcij = mergeCurvei % 2;
                        int mcji = mergeCurvej / 2, mcjj = mergeCurvej % 2;

                        cnode[curveidx][0] += skeletonCurvePoint[ski][mcii][mcij] * weight;   
                        cnode[curveidx][1] += skeletonCurvePoint[ski][mcji][mcjj] * weight;
                        ccolor[curveidx * 2] += skeletonCurveColor[ski][mergeCurvei] * weight;
                        ccolor[curveidx * 2 + 1] += skeletonCurveColor[ski][mergeCurvej] * weight;
                        curveidx += 1;
                    }


                    var currentCurvePairOpenInfo = targetCurvePairInfo[j]
                        .Where(info => info.Value.x == 3)
                        .ToList();
                    for (int nTgi = 0; nTgi < currentCurvePairOpenInfo.Count; nTgi++)
                    {
                        var openPair = currentCurvePairOpenInfo[nTgi];
                        int openCurveidx = targetOpenLink
                            .FindIndex(l => FragmentData.GetFragmentIndexByName(l.fragmentID) == openPair.Key.y
                            && l.face == openPair.Value.y);
                        float openWeight = 1.0f / targetOpenLinkCount[openCurveidx];

                        int mergeCurvei = openPair.Key.z;
                        int mergeCurvej = openPair.Value.z;
                        int mcii = mergeCurvei / 2, mcij = mergeCurvei % 2;
                        int mcji = mergeCurvej / 2, mcjj = mergeCurvej % 2;

                        int ci = curveidx + openCurveidx;
                        cnode[ci][0] += skeletonCurvePoint[ski][mcii][mcij] * openWeight;   
                        cnode[ci][1] += skeletonCurvePoint[ski][mcji][mcjj] * openWeight;
                        ccolor[ci * 2] += skeletonCurveColor[ski][mergeCurvei] * openWeight;
                        ccolor[ci * 2 + 1] += skeletonCurveColor[ski][mergeCurvej] * openWeight;
                    }
                }

                int cnodeStart = cnode.Length - targetOpenLink.Count;
                for (int oli = 0; oli < targetOpenLink.Count; oli++)
                {
                    int nodeIdx = nodeName.FindIndex(name => name == targetOpenLink[oli].fragmentID);
                    Vector3 shift = node[nodeIdx] - cnode[cnodeStart + oli][0]; 
                    cnode[cnodeStart + oli][0] += shift;
                    cnode[cnodeStart + oli][1] += shift;
                }

                gSkeletonCenter.Add(center);
                gSkeletonNodePoint.Add(node.ToList());
                gNodeFragName.Add(nodeName.ToList());
                gSkeletonNodeColor.Add(ncolor.ToList());
                gSkeletonCurvePoint.Add(cnode.ToList());
                gSkeletonCurveColor.Add(ccolor.ToList());
                gAvgMatchScore.Add(scoreSum / groupIdx.Count);
            }
        }
        UpdateGroupSmallMultiples();


        CompactScatterPlot(gSkeletonCenter, bbSize * scaleBBSize, new Vector3(0 ,0, planeDepth), out Vector3[] compactCenter);
        for (int i = 0; i < compactCenter.Length; i++)
        {
            Vector3 shift = compactCenter[i] - gSkeletonCenter[i];
            for (int ni = 0; ni < gSkeletonNodePoint[i].Count; ni++)
                gSkeletonNodePoint[i][ni] += shift;
            for (int cni = 0; cni < gSkeletonCurvePoint[i].Count; cni++)
            {
                gSkeletonCurvePoint[i][cni][0] += shift;
                gSkeletonCurvePoint[i][cni][1] += shift;
            }
            gSkeletonCenter[i] = compactCenter[i];
        }


        Fragment oneFraginGroup = FragmentData.GetFragmentByName(selectTarget.groupSelected.transform.GetChild(0).name);
        targetGroupObj = groupManager.GetGroup(oneFraginGroup.parent);
        groupManager.CenterGroup(targetGroupObj, oneFraginGroup, new Vector3(0, 0, planeDepth));

        
        groupManager.ShowGroupOnly(targetGroupObj);

        
        ContinuousMovement movement = GameObject.Find("XR Rig").GetComponent<ContinuousMovement>();
        movement.SetTransform(cameraPos, cameraEuler);

        overview.overviewVisualize = false;    


        Container = this.transform.Find("Container").gameObject;
        Destroy(Container);
        Container = new GameObject("Container");
        Container.transform.parent = this.transform;
        

        GameObject fragments = GameObject.Find("Fragments");
        fragments.GetComponent<BoxCollider>().enabled = true;
        try
        {
            fragments.GetComponent<XROverviewSelect>().InitializeSkeletonRotation(Container);
            fragments.GetComponent<XROverviewSelect>().enabled = true;
            fragments.GetComponent<XRGrabInteractable>().enabled = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
        


        float maxScore = -1;
        int maxScoreIdx = 0;
        for (int i = 0; i < gAvgMatchScore.Count; i++)
        {
            float score = gAvgMatchScore[i];
            if (score > maxScore)
            {
                maxScore = score;
                maxScoreIdx = i;
            }
        }

        switchSkeleton.SwitchTargetConnectSkeleton(maxScoreIdx, true);
        switchSkeleton.currentCenterTargetIdx = maxScoreIdx;

        showSmallMultiplesFinish = true;

        yield return null;
    }

    public void UpdateGroupSmallMultiples()
    {
        Vector3[] pointCloudPos = gSkeletonNodePoint.SelectMany(p => p).ToArray();
        Vector4[] pointCloudColor = gSkeletonNodeColor.SelectMany(p => p).ToArray();
        smallSkeletonNodeVizor.UpdatePointsCloud(pointCloudPos, pointCloudColor);
        Vector3[][] pointCurvePos = gSkeletonCurvePoint.SelectMany(p => p).ToArray();
        Vector4[] pointCurveColor = gSkeletonCurveColor.SelectMany(p => p).ToArray();
        smallSkeletonCurveVizor.UpdateCurve(pointCurvePos, pointCurveColor);
    }

    public void ClearSmallMultiples()
    {
        smallSkeletonNodeVizor.ClearNodes();
        smallSkeletonCurveVizor.ClearCurve();
    }

    public void UpdateShapeSmallMultiples()
    {
        Vector3[] pointCloudPos = fSkeletonNodePoint.SelectMany(p => p).ToArray();
        Vector4[] pointCloudColor = fSkeletonNodeColor.SelectMany(p => p).ToArray();
        smallSkeletonNodeVizor.UpdatePointsCloud(pointCloudPos, pointCloudColor);
        Vector3[][] pointCurvePos = fSkeletonCurvePoint.SelectMany(p => p).ToArray();
        Vector4[] pointCurveColor = fSkeletonCurveColor.SelectMany(p => p).ToArray();
        smallSkeletonCurveVizor.UpdateCurve(pointCurvePos, pointCurveColor);
    }


    static public void CompactScatterPlot(List<Vector3> center, Vector3 boundSize, Vector3 canvasCenter, 
        out Vector3[] compactCenter)
    {
        int sktNum = center.Count;
        Vector3[] noOverlapCenter = center.ToArray();


        float expandRate = 2f;
        while (true)
        {
            bool overlap = false;
            Vector3[] boundsMin = noOverlapCenter
                .Select(c => c - boundSize / 2).ToArray();
            Vector3[] boundsMax = noOverlapCenter
                .Select(c => c + boundSize / 2).ToArray();
            for (int i = 0; i < sktNum; i++)
            {
                for (int j = i + 1; j < sktNum; j++)
                {
                    if (Utilities.isOverLapping3D(boundsMin[i], boundsMax[i], boundsMin[j], boundsMax[j]))
                    {
                        overlap = true;
                        break;
                    }
                }
                if (overlap)
                    break;
            }

            if (!overlap)
                break;
            else
                noOverlapCenter = noOverlapCenter
                    .Select(c => new Vector3(c.x * expandRate, c.y * expandRate, c.z)).ToArray();
        }


        compactCenter = new Vector3[sktNum];
        List<int> closeToCanvasCenterIdx = center
            .Select((c, idx) => new { c, idx })
            .OrderBy(p => p.c.sqrMagnitude)
            .Select(p => p.idx)
            .ToList();
        List<Vector3> positionedCenter = new List<Vector3>();
        List<Vector3> positionedBoundsMin = new List<Vector3>();
        List<Vector3> positionedBoundsMax = new List<Vector3>();
        int closei = closeToCanvasCenterIdx[0];
        compactCenter[closei] = canvasCenter;  
        positionedCenter.Add(canvasCenter);
        positionedBoundsMin.Add(canvasCenter - boundSize / 2);
        positionedBoundsMax.Add(canvasCenter + boundSize / 2);
        float extentLen = boundSize.magnitude / 2.0f;

        for (int i = 1; i < sktNum; i++)
        {
            closei = closeToCanvasCenterIdx[i];
            Vector3 currentCenter = noOverlapCenter[closei];

            int pi;
            float t = 0;
            for (pi = 0; pi < positionedCenter.Count; pi++)
            {
                t = Utilities.RayCastAABB(currentCenter, canvasCenter - currentCenter,
                       positionedBoundsMin[pi], positionedBoundsMax[pi]);
                if (t >= 0)
                    break;
            }
            if (t < 0)
                Debug.LogError("unexpected raycasting: no center found!");
            
            Vector3 dir = (canvasCenter - currentCenter).normalized;
            dir.z = 0;
            currentCenter = currentCenter + t * dir - extentLen * dir;
            
            positionedBoundsMin.Add(currentCenter - boundSize / 2);
            positionedBoundsMax.Add(currentCenter + boundSize / 2);

            
            while (true)
            {
                bool overlap = false;
                int lastIdx = positionedCenter.Count;
                int j;
                for (j = 0; j < positionedCenter.Count; j++)
                    if (Utilities.isOverLapping3D(positionedBoundsMin[j], positionedBoundsMax[j],
                        positionedBoundsMin[lastIdx], positionedBoundsMax[lastIdx]))
                    {
                        overlap = true;
                        break;
                    }


                if (!overlap)
                    break;

                currentCenter -= dir * (2 * extentLen - (currentCenter - positionedCenter[j]).magnitude);
                positionedBoundsMin[lastIdx] = currentCenter - boundSize / 2;
                positionedBoundsMax[lastIdx] = currentCenter + boundSize / 2;
            }

            positionedCenter.Add(currentCenter);
            compactCenter[closei] = currentCenter;
        }
    }


    private void LoadCache()
    {
        listDRInOut = null;
        try
        {
            using (var file = File.OpenRead(savePath))
            {
                listDRInOut = Serializer.Deserialize<ListDRInOut>(file);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
        
    }

    private bool CacheHit(List<GroupMatch> selectGroupMatch, out byte[] output)
    {
        output = null;
        if (listDRInOut == null || listDRInOut.Count == 0)
            return false;

        for (int i = 0; i < listDRInOut.Count; i++)
        {
            var curSelectGroupMatch = listDRInOut.listDRInOut[i].selectGroupMatch;
            if (curSelectGroupMatch.Count != selectGroupMatch.Count)
                continue;

            bool unfind = false;
            for (int j = 0; j < selectGroupMatch.Count; j++)
            {
                var cacheTargetLinks = curSelectGroupMatch[j].targetLinks;
                var cacheOtherLinks = curSelectGroupMatch[j].otherLinks;
                var curTargetLinks = selectGroupMatch[j].targetLinks;
                var curOtherLinks = selectGroupMatch[j].otherLinks;
                for (int l = 0; l < cacheTargetLinks.Count; l++)
                    if (cacheTargetLinks[l].Key != curTargetLinks[l].Key || cacheOtherLinks[l].Key != curOtherLinks[l].Key)
                    {
                        unfind = true;
                        break;
                    }
                if (unfind)
                    break;
            }

            if (!unfind)
            {
                output = listDRInOut.listDRInOut[i].output;
                return true;
            }
        }

        return false;
    }

    private void SaveCache(List<GroupMatch> selectGroupMatch, byte[] output)
    {
        if (listDRInOut == null)
            listDRInOut = new ListDRInOut();

        var dRInOut = new DRInOut();
        dRInOut.selectGroupMatch = selectGroupMatch;
        dRInOut.output = output;
        listDRInOut.listDRInOut.Add(dRInOut);

        using (FileStream file = File.Create(savePath))
        {
            Serializer.Serialize(file, listDRInOut);
        }
    }

    private void OnGUI()
    {
        guiProcess.DrawGUIBar();
    }
}

