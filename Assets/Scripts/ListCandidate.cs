using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using ProtoBuf;
using System.IO;


public class ListCandidate : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool listCandidate = false;
    private bool listCandidateLast = false;
    [SerializeField] float filterScore = 0.0f;

    public List<GameObject> groups;
    public int groupi;
    private GroupManager groupManager;

    public bool if_ended = false;

    // cache save
    private string savePath;
    [ProtoContract, Serializable]
    class ListInOut
    {
        [ProtoMember(1)]
        public List<KeyValuePair<CurveIndex, CurveIndex>> saveOpenLinks;
        [ProtoMember(2)]
        public List<KeyValuePair<CurveIndex, CurveIndex>> saveOtherOpenLinks;
        [ProtoMember(3)]
        public List<GroupMatch> savePotentialMatches;

        public ListInOut()
        {
            saveOpenLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
            saveOtherOpenLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
            savePotentialMatches = new List<GroupMatch>();
        }
    }
    [ProtoContract, Serializable]
    class ListsInOut
    {
        [ProtoMember(1)]
        public List<ListInOut> listsInOut;

        public ListsInOut()
        {
            listsInOut = new List<ListInOut>();
        }

        public float Count
        {
            get { return listsInOut.Count; }
        }
    }
    private ListsInOut listsInOut;

    // results
    public List<GroupMatch> potentialMatches;   
                                                
    public int maxMatchCount;

    public GUIProcess guiProcess = new GUIProcess();

    private void Start()
    {
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
        potentialMatches = new List<GroupMatch>();

        savePath = Application.dataPath + "/Resources/ListPotentialCache/ListInOut";
        LoadCache();
    }

    void Update()
    {
        if (listCandidate)
        {
            if (listCandidateLast == false)
            {
                Debug.Log("Select one group with mismatches...");
                fragmentRef = FragmentData.fragments;
                maxMatchCount = 0;
                if_ended = false;
            }

            var selectRlt = GameObject.Find("AreaSelectFragments").GetComponent<AreaSelectFragments>()
                .GetSelectedFragments(FragmentTypes.Group, true);

            if (selectRlt.Count > 0)
            {
                groups = selectRlt;
                listCandidate = false;
            }

            listCandidateLast = true;
        }
        else if (listCandidateLast)
        {
            if (groups != null && groups.Count > 0)
            {
                potentialMatches = new List<GroupMatch>();

                groupi = 0;
                while (groups[groupi] == null)
                    groupi += 1;

                Transform[] fragTransform = GroupManager.GetAllFragTransform(groups[groupi].transform);  // only the first group
                var openLinks = FragmentData.GetAllOpenLinks(fragTransform);

                Transform[] fragParents = groupManager.GetAllSecondaryTransforms();
                var otherOpenLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
                foreach (var parent in fragParents)
                {
                    if (parent == groups[0].transform)
                        continue;

                    var groupOpenLinks = FragmentData.GetAllOpenLinks(GroupManager.GetAllFragTransform(parent));
                    otherOpenLinks.AddRange(groupOpenLinks);
                }

                if (!CacheHit(openLinks, otherOpenLinks))
                {
                    guiProcess.showGUI = true;
                    StartCoroutine(GetAllPotentialMatches(openLinks, otherOpenLinks));
                }
            }
            else
                Debug.LogWarning("no group is selected!");

            listCandidateLast = false;
        }
    }

    IEnumerator GetAllPotentialMatches(List<KeyValuePair<CurveIndex, CurveIndex>> openLinks,
        List<KeyValuePair<CurveIndex, CurveIndex>> otherOpenLinks)
    {
        float[,] pairScore = new float[openLinks.Count, otherOpenLinks.Count];
        for (int i = 0; i < openLinks.Count; i++)
        {
            Fragment fragT = FragmentData.GetFragmentByName(openLinks[i].Key.fragmentID);
            int faceT = openLinks[i].Key.face;
            int curveT = openLinks[i].Key.curve;
            for (int j = 0; j < otherOpenLinks.Count; j++)
            {
                Fragment fragO = FragmentData.GetFragmentByName(otherOpenLinks[j].Key.fragmentID);
                int faceO = otherOpenLinks[j].Key.face;
                int curveO = otherOpenLinks[j].Key.curve;
                pairScore[i, j] = MatchSim.CurvePairMatchScore(fragT, faceT, curveT, fragO, faceO, curveO);
            }
        }

        int iterNum = Mathf.Min(otherOpenLinks.Count, openLinks.Count);

        int matchNumStart = iterNum;
        maxMatchCount = iterNum;
        for (int matchNum = matchNumStart; matchNum <= iterNum; matchNum++)
        {
            int[][] targetPotential =
                Utilities.GetKCombs(Enumerable.Range(0, openLinks.Count), matchNum)
                .Select(a => a.ToArray()).ToArray();
            int[][] otherPotential =
                Utilities.GetKCombs(Enumerable.Range(0, otherOpenLinks.Count), matchNum)
                .Select(a => a.ToArray()).ToArray();
            int[][] matchPotential =
                Utilities.GetPermutations(Enumerable.Range(0, matchNum), matchNum)
                .Select(a => a.ToArray()).ToArray();


            int[] otherLinki = new int[matchNum];
            for (int targeti = 0; targeti < targetPotential.Length; targeti++)
            {
                int[] targetLinki = targetPotential[targeti];
                for (int otheri = 0; otheri < otherPotential.Length; otheri++)
                {
                    for (int matchi = 0; matchi < matchPotential.Length; matchi++)
                    {
                        for (int i = 0; i < matchNum; i++)
                            otherLinki[i] = otherPotential[otheri][matchPotential[matchi][i]];

                        bool lowScoreFlag = false;
                        for (int mi = 0; mi < matchNum; mi++)
                        {
                            if (pairScore[targetLinki[mi], otherLinki[mi]] < filterScore)
                            {
                                lowScoreFlag = true;
                                break;
                            }
                        }
                        if (lowScoreFlag)
                            break;

                        var targetSkeletonLink = new List<KeyValuePair<CurveIndex, CurveIndex>>();
                        var otherSkeletonLink = new List<KeyValuePair<CurveIndex, CurveIndex>>();
                        float curveScore = 0.0f;
                        Vector3[] targetFaceNormals = new Vector3[matchNum];
                        Vector3[] otherFaceNormals = new Vector3[matchNum];
                        for (int mi = 0; mi < matchNum; mi++)
                        {
                            int tragetli = targetLinki[mi];
                            int otherli = otherLinki[mi];
                            Fragment fragT = FragmentData.GetFragmentByName(openLinks[tragetli].Key.fragmentID);
                            int faceT = openLinks[tragetli].Key.face;
                            int curveT = openLinks[tragetli].Key.curve;
                            Fragment fragO = FragmentData.GetFragmentByName(otherOpenLinks[otherli].Key.fragmentID);
                            int faceO = otherOpenLinks[otherli].Key.face;
                            int curveO = otherOpenLinks[otherli].Key.curve;

                            targetSkeletonLink.Add(openLinks[tragetli]);
                            otherSkeletonLink.Add(otherOpenLinks[otherli]);
                            
                            var temp = MatchSim.CurvePairMatchScore(fragT, faceT, curveT, fragO, faceO, curveO);
                            curveScore += pairScore[tragetli, otherli];

                            targetFaceNormals[mi] = fragT.faceNormal[faceT];
                            otherFaceNormals[mi] = fragO.faceNormal[faceO];
                        }
                        curveScore /= matchNum;

                        float matchScore = curveScore;
                        potentialMatches.Add(new GroupMatch(targetSkeletonLink, otherSkeletonLink, matchScore));
                    }
                }
                guiProcess.progress = ((float)matchPotential.Length + 1.0f + otherPotential.Length * matchPotential.Length + targeti * otherPotential.Length * matchPotential.Length) /
                        ((float)targetPotential.Length * otherPotential.Length * matchPotential.Length);
                guiProcess.guiText = "Matching " + matchNum + "/" + iterNum
                    + " current (" + (guiProcess.progress * 100.0f).ToString("f2") + "%)";
                yield return null;
            }
        }
        potentialMatches = potentialMatches.OrderByDescending(m => m.matchScore).ToList();
        Debug.Log("potentialMatches from ListCandidate: " + potentialMatches.Count);

        guiProcess.showGUI = false;

        if_ended = true;
    }

    private void LoadCache()
    {
        listsInOut = null;
        try
        {
            using (var file = File.OpenRead(savePath))
            {
                listsInOut = Serializer.Deserialize<ListsInOut>(file);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
        
    }

    private bool CacheHit(List<KeyValuePair<CurveIndex, CurveIndex>> openLinks,
        List<KeyValuePair<CurveIndex, CurveIndex>> otherOpenLinks)
    {
        if (listsInOut == null || listsInOut.Count == 0)
            return false;

        for (int i = 0; i < listsInOut.Count; i++)
        {
            var curlistsInOut = listsInOut.listsInOut[i];
            if (openLinks.Count != curlistsInOut.saveOpenLinks.Count ||
                otherOpenLinks.Count != curlistsInOut.saveOtherOpenLinks.Count)
                continue;

            bool unfind = false;
            var saveOpenLinks = curlistsInOut.saveOpenLinks;
            foreach (var link in openLinks)
                if (!saveOpenLinks.Contains(link))
                {
                    unfind = true;
                    break;
                }

            if (!unfind)
            {
                var saveOtherOpenLinks = curlistsInOut.saveOtherOpenLinks;
                foreach (var link in otherOpenLinks)
                    if (!saveOtherOpenLinks.Contains(link))
                    {
                        unfind = true;
                        break;
                    }
            }

            if (!unfind)
            {
                potentialMatches = curlistsInOut.savePotentialMatches;
                maxMatchCount = Mathf.Min(otherOpenLinks.Count, openLinks.Count);
                if_ended = true;
                return true;
            }
        }

        return false;
    }

    private void SaveCache(List<KeyValuePair<CurveIndex, CurveIndex>> openLinks,
        List<KeyValuePair<CurveIndex, CurveIndex>> otherOpenLinks)
    {
        if (listsInOut == null)
            listsInOut = new ListsInOut();

        ListInOut listInOut = new ListInOut();
        listInOut.saveOpenLinks = openLinks;
        listInOut.saveOtherOpenLinks = otherOpenLinks;
        listInOut.savePotentialMatches = potentialMatches;
        listsInOut.listsInOut.Add(listInOut);

        using (FileStream file = File.Create(savePath))
        {
            Serializer.Serialize(file, listsInOut);
        }
    }

    private void OnGUI()
    {
        guiProcess.DrawGUIBar();
    }
}
