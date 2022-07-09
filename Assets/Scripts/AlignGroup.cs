using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class AlignGroup : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool alignGroup = false;
    public bool alignGroupLast = false;
    
    public List<GameObject> selectedGroup;

    public bool confirmShapeAlignRequest = false;   
                                                    
    private bool confirmShapeAlignRequestLast = false;
    public List<KeyValuePair<CurveIndex, CurveIndex>> pairs;

    private AsynManager asynManager;

    [Header("debug")]
    [SerializeField] bool showDebug = false;
    List<Vector3> debugPos = new List<Vector3>();
    List<Color> debugPosColor = new List<Color>();

    private void Start()
    {
        asynManager = GameObject.Find("AsynManager").GetComponent<AsynManager>();
    }

    void Update()
    {
        if (alignGroup)
        {
            if (alignGroupLast == false)
            {
                fragmentRef = FragmentData.fragments;
                selectedGroup = null;
            }

            
            var tempSelected = GameObject.Find("AreaSelectFragments").GetComponent<AreaSelectFragments>()
                .GetSelectedFragments(FragmentTypes.Group, true);
            
            if (tempSelected.Count == 2)
                selectedGroup = tempSelected;

            alignGroupLast = true;
        }
        else if (alignGroupLast)
        {
            if (selectedGroup != null)
            {
                debugPos.Clear();
                debugPosColor.Clear();

                List<Fragment>[] groupFrag = new List<Fragment>[2];
                for (int i = 0; i < selectedGroup.Count; i++)
                {
                    groupFrag[i] = new List<Fragment>();
                    foreach (Transform fragTransform in selectedGroup[i].transform)
                        groupFrag[i].Add(FragmentData.GetFragmentByName(fragTransform.name));
                }

                var groupLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>[2];
                for (int i = 0; i < groupFrag.Length; i++)
                {
                    groupLinks[i] = new List<KeyValuePair<CurveIndex, CurveIndex>>();
                    foreach (var frag in groupFrag[i])
                        foreach (var link in frag.skeletonLink)
                            if (link.Value.face != -1)
                                groupLinks[i].Add(link);
                }

                
                var pairs = new List<KeyValuePair<CurveIndex, CurveIndex>>();
                for (int linki = 0; linki < groupLinks[0].Count; linki++)
                {
                    int pairLinki = groupLinks[1].FindIndex(
                        l => l.Value == groupLinks[0][linki].Key && l.Key == groupLinks[0][linki].Value);

                    if (pairLinki != -1)
                        pairs.Add(groupLinks[0][linki]);
                }

                AlignPair(pairs, selectedGroup[0], selectedGroup[1]);
            }

            selectedGroup = null;
            alignGroupLast = false;
        }

        if (confirmShapeAlignRequest)
        {
            AlignPair(pairs, selectedGroup[0], selectedGroup[1]);

            confirmShapeAlignRequest = false;
            confirmShapeAlignRequestLast = true;
        }
    }

    private void AlignPair(List<KeyValuePair<CurveIndex, CurveIndex>> pairs, GameObject groupObj1, GameObject groupObj2)
    {
        FragmentData.GetPairPoints(pairs, out Vector3[][] fPoints, out Vector3[][] fNormals);

        for (int i = 0; i < fPoints.Length; i++)
        {
            Color c = i == 0 ? Color.red : Color.blue;
            for (int j = 0; j < fPoints[i].Length; j++)
            {
                debugPos.Add(fPoints[i][j]);
                debugPosColor.Add(c);
            }
        }

        StartCoroutine(AsyncAlignGroup(fPoints, fNormals, groupObj1, groupObj2, pairs));
    }

    IEnumerator AsyncAlignGroup(Vector3[][] fPoints, Vector3[][] fNormals, GameObject groupObj1, GameObject groupObj2,
        List<KeyValuePair<CurveIndex, CurveIndex>> pairs)
    {
        asynManager.RunGR(0, fPoints, fNormals, groupObj1.transform, groupObj2.transform, PairRegister.TransferToAlignment);
        yield return new WaitUntil(() => asynManager.isRunEnd(0));

        FragmentData.GetPairPoints(pairs, out fPoints, out fNormals);

        PairRegister.FacePair(fPoints, fNormals, ref groupObj2);

        if (confirmShapeAlignRequestLast)
        {
            foreach (var group in selectedGroup)
            {
                while (group.transform.childCount > 0)
                    group.transform.GetChild(0).parent = group.transform.parent;
                Destroy(group);
            }
            selectedGroup = null;
            confirmShapeAlignRequestLast = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebug)
            return;

        float size = 0.02f;

        for (int i = 0; i < debugPos.Count; i++)
        {
            Gizmos.color = debugPosColor[i];
            Gizmos.DrawSphere(debugPos[i], size);
        }
    }
}
