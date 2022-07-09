using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.XR.Interaction.Toolkit;



public class ConfirmShape : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool confirmShape = false;
    private bool confirmShapeLast = false;

    private GroupManager groupManager;
    private SmallMultiplesAround smSkt;
    private SmallShapeAround smShp;
    private CompareShape cmShp;
    private GameObject Fragments;
    private Overview overview;
    private LinkFragmentPair linkFragmentPair;
    private CutSkeleton cutSkeleton;
    private AlignGroup alignGroup;
    private SelectTarget selectTarget;
    private AreaSelectFragments areaSelectFragments;

    [Header("Show Debug")]
    public bool showDebug = false;
    List<Vector3> debugPos = new List<Vector3>();
    List<Color> debugPosColor = new List<Color>();

    private void Start()
    {
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
        smSkt = GameObject.Find("NextSkeleton").GetComponent<SmallMultiplesAround>();
        smShp = GameObject.Find("NextShape").GetComponent<SmallShapeAround>();
        cmShp = GameObject.Find("CompareShape").GetComponent<CompareShape>();
        Fragments = GameObject.Find("Fragments");
        overview = GameObject.Find("Overview").GetComponent<Overview>();
        linkFragmentPair = GameObject.Find("LinkFragmentPair").GetComponent<LinkFragmentPair>();
        cutSkeleton = GameObject.Find("CutSkeleton").GetComponent<CutSkeleton>();
        alignGroup = GameObject.Find("AlignGroup").GetComponent<AlignGroup>();
        selectTarget = GameObject.Find("SelectTarget").GetComponent<SelectTarget>();
        areaSelectFragments = GameObject.Find("AreaSelectFragments").GetComponent<AreaSelectFragments>();
    }

    void Update()
    {
        if (confirmShape)
        {
            if (confirmShapeLast == false)
            {
                debugPos.Clear();
                debugPosColor.Clear();
                if (cmShp.transform.Find("Shapes").childCount >= 1)
                {
                    Debug.Log("Confirming Candidates...");
                    fragmentRef = FragmentData.fragments;
                }
                else
                {
                    Debug.Log("No shape for confirm!");
                    confirmShape = false;
                    return;
                }

                GrabSetDown();
            }

            ConfirmMatch();

            confirmShapeLast = true;
        }
        else if (confirmShapeLast)
        {
            // next iteration
            overview.overviewVisualize = true;
            smSkt.smallMultiplesAround = false;
            smShp.smallShapeAround = false;
            cmShp.compareShape = false;

            confirmShapeLast = false;
        }
    }

    private void ConfirmMatch()
    {
        List<GameObject> selectedFrag = areaSelectFragments.GetSelectedFragments(true);

        if (selectedFrag.Count == 0)
            return;
        else
            foreach (var obj in selectedFrag)
                Debug.Log(obj.transform.name);

        Transform selectedCandidate = selectedFrag[0].transform.parent;
        Vector3 candidateCenter = GroupManager.GetFragmentsCenter(selectedCandidate);
        
        int compareShapeID = 0;
        for (int i = 0; i < cmShp.Shapes.transform.childCount; i++)
            if (cmShp.Shapes.transform.GetChild(i) == selectedCandidate)
            {
                compareShapeID = i;
                break;
            }

        var matchCues = cmShp.matchCues[compareShapeID];
        var confirmNearIdx = matchCues.confirmNearIdx;
        GroupMatch selectMatchInfo = new GroupMatch();
        if (confirmNearIdx.Count == 0)
        {
            int oid = smSkt.fOrignalIdx[cmShp.selectedShapeID[compareShapeID]];
            selectMatchInfo = smSkt.selectGroupMatch[oid];
        }
        else
        {
            var targetLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
            var otherLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
            for (int i = 0; i < confirmNearIdx.Count; i++)
            {
                int li = confirmNearIdx[i].x;
                int lj = confirmNearIdx[i].y;
                targetLinks.Add(new KeyValuePair<CurveIndex, CurveIndex>(
                    new CurveIndex(matchCues.fragID[li], matchCues.faceID[li], 0),
                    new CurveIndex(true)));
                otherLinks.Add(new KeyValuePair<CurveIndex, CurveIndex>(
                    new CurveIndex(matchCues.fragID[lj], matchCues.faceID[lj], 0),
                    new CurveIndex(true)));
            }
            selectMatchInfo.targetLinks = targetLinks;
            selectMatchInfo.otherLinks = otherLinks;
        }

        List<string> candidateFragNames = new List<string>();
        List<string> allFragNames = new List<string>();
        List<string> leftFragNames = new List<string>();
        int confirmFragNum = selectMatchInfo.targetLinks.Count;
        for (int i = 0; i < confirmFragNum; i++)
        {
            string name = selectMatchInfo.targetLinks[i].Key.fragmentID;
            candidateFragNames.Add(name);
            name = selectMatchInfo.otherLinks[i].Key.fragmentID;
            candidateFragNames.Add(name);
        }

        Transform targetGroupTf = selectTarget.groupSelected.transform;
        for (int i = 0; i < targetGroupTf.childCount; i++)
            candidateFragNames.Add(targetGroupTf.GetChild(i).name);
        candidateFragNames = candidateFragNames.Distinct().ToList();
        foreach (var frag in fragmentRef)
            allFragNames.Add(frag.GetIDName());
        leftFragNames = allFragNames.Except(candidateFragNames).ToList();  

        List<GameObject> selectedFragObj = new List<GameObject>();
        foreach (var name in candidateFragNames)
            selectedFragObj.Add(groupManager.GetTransformByName(name)[0].gameObject);
        
        List<GameObject> leftFragObj = new List<GameObject>();
        foreach (var name in leftFragNames)
            leftFragObj.Add(groupManager.GetTransformByName(name)[0].gameObject);

        
        int[] selectedFragTargetOther = new int[selectedFragObj.Count];
        for (int i = 0; i < selectedFragTargetOther.Length; i++)
            if (selectedFragObj[i].transform.parent == selectTarget.groupSelected.transform)
                selectedFragTargetOther[i] = 0;
            else
                selectedFragTargetOther[i] = 1;


        string[] selectCandidateFragNames = new string[selectedCandidate.childCount];
        for (int i = 0; i < selectCandidateFragNames.Length; i++)
        {
            string name = selectedCandidate.GetChild(i).name;
            selectCandidateFragNames[i] = name.Remove(name.Length - 7);
        }

        for (int i = 0; i < candidateFragNames.Count; i++)
        {
            int candidateFragId = Array.FindIndex(selectCandidateFragNames, s => s == candidateFragNames[i]);
            selectedFragObj[i].transform.GetChild(0).position =
                selectedCandidate.GetChild(candidateFragId).GetChild(0).position;
            selectedFragObj[i].transform.GetChild(0).rotation =
                selectedCandidate.GetChild(candidateFragId).GetChild(0).rotation;
        }


        GameObject groupObj = groupManager.Group(selectedFragObj);

             
        int linkNum = selectMatchInfo.targetLinks.Count;
        
        var linkPair = new List<KeyValuePair<CurveIndex, CurveIndex>>();
        for (int li = 0; li < linkNum; li++)
        {
            string tarFragName = selectMatchInfo.targetLinks[li].Key.fragmentID;
            Fragment tarFrag = FragmentData.GetFragmentByName(tarFragName);
            int tarFaceID = selectMatchInfo.targetLinks[li].Key.face;
            int tarCurveID = selectMatchInfo.targetLinks[li].Key.curve;
            var tarOpenLink = tarFrag.skeletonLink.Find(
                s => s.Key.fragmentID == tarFragName && s.Key.face == tarFaceID && s.Key.curve == tarCurveID);

            string otFragName = selectMatchInfo.otherLinks[li].Key.fragmentID;
            Fragment otFrag = FragmentData.GetFragmentByName(otFragName);
            int otFaceID = selectMatchInfo.otherLinks[li].Key.face;
            int otCurveID = selectMatchInfo.otherLinks[li].Key.curve;
            var otOpenLink = otFrag.skeletonLink.Find(
                s => s.Key.fragmentID == otFragName && s.Key.face == otFaceID && s.Key.curve == otCurveID);

            linkFragmentPair.PairMerge(tarOpenLink, otOpenLink, fragmentRef, false);
            linkPair.Add(tarOpenLink);
        }

        for (int gi = 0; gi < Fragments.transform.childCount; gi++)
        {
            FragmentData.GetAllExternalLinks(
                Fragments.transform.GetChild(gi), out List<Vector2Int> skeletonidx);
            for (int exi = 0; exi < skeletonidx.Count; exi++)
            {
                int fragi = skeletonidx[exi].x;
                int skeletoni = skeletonidx[exi].y;
                if (!fragmentRef[fragi].skeletonLink[skeletoni].Value.IsEmpty())
                    cutSkeleton.CutSkeletonLink(fragi, skeletoni, false);
            }
        }

        List<Transform>[] selectedGroup = new List<Transform>[] {
                new List<Transform>(),
                new List<Transform>() };
        List<Fragment>[] frags = new List<Fragment>[] {
                new List<Fragment>(),
                new List<Fragment>()};


        List<Transform> targets = new List<Transform>();
        foreach (var fragObj in selectedFragObj)
            targets.Add(fragObj.transform.GetChild(0));


        for (int i = 0; i < selectedFragTargetOther.Length; i++)
        {
            int groupi = selectedFragTargetOther[i];
            frags[groupi].Add(FragmentData.GetFragmentByName(targets[i].parent.name));
            selectedGroup[groupi].Add(targets[i]);
        }

        PairRegister.PreciseAlignmentSim(selectedGroup[0], frags[0],
            selectedGroup[1], frags[1], fragmentRef, false);


        Fragments.SetActive(true);
        foreach (Transform t in Fragments.transform)
            t.gameObject.SetActive(true);


        MatchLeftFragments(fragmentRef, groupObj);


        groupManager.CenterGroup(groupManager.gameObject, fragmentRef[0], candidateCenter);
        GroupManager.SetFragment2Center();


        overview.ForceRedraw();


        GameObject.Find("XRWorkflowControl").GetComponent<XRWorkflowControl>().ConfirmShape();
        confirmShape = false;
    }


    private void GrabSetDown()
    {
        Transform cshapeTransform = cmShp.Shapes.transform;
        foreach (Transform shape in cshapeTransform)
        {
            List<GameObject> groups = new List<GameObject>();
            foreach (Transform group in shape)
                groups.Add(group.gameObject);

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];

                group.gameObject.GetComponent<BoxCollider>().enabled = false;
                group.gameObject.GetComponent<XRGrabInteractable>().enabled = false;
                while (group.transform.childCount > 0)
                {
                    Transform frag = group.transform.GetChild(0);
                    var def = frag.GetChild(0).GetChild(0);
                    var si = def.GetComponent<XRSimpleInteractable>();
                    if (si != null)
                        si.enabled = true;
                    def.GetComponent<BoxCollider>().enabled = true;
                    def.GetComponent<MeshCollider>().enabled = true;
                    frag.parent = group.transform.parent;
                }
                Destroy(group.gameObject);
            }
        }
    }

    private void MatchLeftFragments(Fragment[] fragmentRef, GameObject groupObjInCurrentRound)
    {
        List<Transform>[] fragsTf = new List<Transform>[] {
                new List<Transform>(),
                new List<Transform>() };
        List<Fragment>[] frags = new List<Fragment>[] {
                new List<Fragment>(),
                new List<Fragment>()};

        Transform groupTfInCurrentRound = groupObjInCurrentRound.transform;
        for (int i = 0; i < fragmentRef.Length; i++)
        {
            if (groupTfInCurrentRound.Find(fragmentRef[i].GetIDName()) != null)
            {
                fragsTf[0].Add(fragmentRef[i].objMesh.transform);
                frags[0].Add(fragmentRef[i]);
            }
            else
            {
                fragsTf[1].Add(fragmentRef[i].objMesh.transform);
                frags[1].Add(fragmentRef[i]);
            }
        }

        PairRegister.PreciseAlignmentSim(fragsTf[0], frags[0],
            fragsTf[1], frags[1], fragmentRef, false);

        List<string> groupObjInCurrentRoundName = new List<string>();
        foreach (Transform t in groupObjInCurrentRound.transform)
            groupObjInCurrentRoundName.Add(t.name);


        groupManager.UnGroupAll(new List<GameObject>());


        Transform fragGroup = fragmentRef[0].objMesh.transform.parent.parent;
        var openLinks = FragmentData.GetAllOpenLinks(fragGroup);
        Fragment[] openfrag = new Fragment[openLinks.Count];
        Transform[] openfragTf = new Transform[openLinks.Count];
        Vector3[] faceCenter = new Vector3[openLinks.Count];
        for (int i = 0; i < openLinks.Count; i++)
        {
            openfrag[i] = FragmentData.GetFragmentByName(openLinks[i].Key.fragmentID);
            openfragTf[i] = openfrag[i].objMesh.transform;
            faceCenter[i] = openfragTf[i].TransformPoint(openfrag[i].faceCenter[openLinks[i].Key.face]);
        }
        bool[] openLinksMark = new bool[openLinks.Count];
        for (int i = 0; i < openLinks.Count; i++)
            for (int j = i + 1; j < openLinks.Count; j++)
            {
                if (openLinksMark[i] || openLinksMark[j] || openfrag[i] == openfrag[j])
                    continue;

                if ((faceCenter[i] - faceCenter[j]).magnitude < 0.2f)
                {
                    linkFragmentPair.PairMerge(openLinks[i], openLinks[j], fragmentRef, false);
                    openLinksMark[i] = true;
                    openLinksMark[j] = true;
                }
            }


        groupManager.UnGroupAll(new List<GameObject>());

    }

}
