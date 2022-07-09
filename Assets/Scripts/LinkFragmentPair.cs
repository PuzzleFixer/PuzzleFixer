using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LinkFragmentPair : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool linkPair = false;
    private bool linkPairLast = false;

    [Header("params")]
    [SerializeField] private float nearDistance = 1.0f;
    [Tooltip("whether two face normals are faced to each other")]
    [SerializeField] private float faceAngleTh = 10.0f;

    Overview overview;


    private bool nearbyFound = false;
    private KeyValuePair<CurveIndex, CurveIndex> openLinki;
    private KeyValuePair<CurveIndex, CurveIndex> openLinkj;
    private Fragment foundFragi;
    private Fragment foundFragj;

    [Header("debug")]
    [SerializeField] bool showDebug = false;
    List<Vector3> debugPos = new List<Vector3>();
    List<Color> debugPosColor = new List<Color>();

    void Start()
    {
        overview = GameObject.Find("Overview").GetComponent<Overview>();
    }

    void Update()
    {
        if (linkPair)
        {
            if (linkPairLast == false)
            {
                Debug.Log("Please move two fragments nearby to link...");
                fragmentRef = FragmentData.fragments;

                foundFragi = null;
                foundFragj = null;
                nearbyFound = false;
            }

            // new use VR controller to select
            List<GameObject> selectedFrag = GameObject.Find("AreaSelectFragments").GetComponent<AreaSelectFragments>()
                .GetSelectedFragments(true);
            
            if (selectedFrag.Count == 2)
            {
                int fragiidx = FragmentData.GetFragmentIndexByName(selectedFrag[0].name);
                int fragjidx = FragmentData.GetFragmentIndexByName(selectedFrag[1].name);
                Fragment fragi = fragmentRef[fragiidx];
                Fragment fragj = fragmentRef[fragjidx];
                Bounds boundi = fragi.objMesh.transform.
                    Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh.bounds;
                Bounds boundj = fragj.objMesh.transform.
                    Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh.bounds;
                float boundHalfDiagnoali = boundi.extents.magnitude;
                float boundHalfDiagnoalj = boundj.extents.magnitude;
                float dis = Vector3.Distance(overview.nodePos[fragiidx], overview.nodePos[fragjidx]) 
                    - boundHalfDiagnoali - boundHalfDiagnoalj;

                Debug.DrawLine(overview.nodePos[fragiidx], overview.nodePos[fragjidx], Color.white);

                if (dis < nearDistance)
                {
                    var openLinksi = fragi.skeletonLink.FindAll(s => s.Value.face == -1);
                    var openLinksj = fragj.skeletonLink.FindAll(s => s.Value.face == -1);
                    for (int i = 0; i < openLinksi.Count; i++)
                        for (int j = 0; j < openLinksj.Count; j++)
                        {
                            int linkPosi = overview.linkPosInfo.FindIndex(
                                info => info[0] == FragmentData.GetFragmentIndexByName(fragi.parent.name) &&
                                info[1] == fragi.skeletonLink.FindIndex(s => s.Key == openLinksi[i].Key && s.Value == openLinksi[i].Value));
                            int linkPosj = overview.linkPosInfo.FindIndex(
                                info => info[0] == FragmentData.GetFragmentIndexByName(fragj.parent.name) &&
                                info[1] == fragj.skeletonLink.FindIndex(s => s.Key == openLinksj[j].Key && s.Value == openLinksj[j].Value));

                            Vector3 faceNi = overview.linkPos[linkPosi][1] - overview.linkPos[linkPosi][0];
                            Vector3 faceNj = overview.linkPos[linkPosj][1] - overview.linkPos[linkPosj][0];

                            float faceAngle = Vector3.Angle(faceNi, -faceNj);
                            if (faceAngle < faceAngleTh)
                            {
                                nearbyFound = true;
                                openLinki = openLinksi[i];
                                openLinkj = openLinksj[j];
                                foundFragi = fragi;
                                foundFragj = fragj;
                                Debug.Log("nearbyFound");

                                Debug.DrawLine(overview.linkPos[linkPosi][0], overview.linkPos[linkPosi][1], Color.green);
                                Debug.DrawLine(overview.linkPos[linkPosj][0], overview.linkPos[linkPosj][1], Color.green);

                                break;
                            }
                        }
                }
            }

            linkPairLast = true;
        }
        else if (linkPairLast)
        {
            if (nearbyFound)
            {
                Debug.Log(foundFragi.parent.name + " and " + foundFragj.parent.name + " are linking");
                // merge two links
                PairMerge(openLinki, openLinkj, fragmentRef);
            }

            nearbyFound = false;
            foundFragi = null;
            foundFragj = null;
            linkPairLast = false;
        }
    }

    public void PairMerge(KeyValuePair<CurveIndex, CurveIndex> openLinki,
        KeyValuePair<CurveIndex, CurveIndex> openLinkj, Fragment[] fragmentRef, bool reDraw = true)
    {
        Fragment foundFragi = FragmentData.GetFragmentByName(openLinki.Key.fragmentID);
        Fragment foundFragj = FragmentData.GetFragmentByName(openLinkj.Key.fragmentID);

        openLinki.Value.fragmentID = openLinkj.Key.fragmentID;
        openLinki.Value.face = openLinkj.Key.face;
        openLinki.Value.curve = openLinkj.Key.curve;

        openLinkj.Value.fragmentID = openLinki.Key.fragmentID;
        openLinkj.Value.face = openLinki.Key.face;
        openLinkj.Value.curve = openLinki.Key.curve;


        int facei = openLinki.Key.face;
        int curvei = openLinki.Key.curve;
        int facej = openLinkj.Key.face;
        int curvej = openLinkj.Key.curve;

        MatchSim.CurvePairMatchScore(foundFragi, facei, curvei, foundFragj, facej, curvej);

        if (reDraw)
        {
            overview.ForceRedraw();
            MeshColor.DrawMeshColor(fragmentRef);
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebug)
        {
            float size = 0.02f;

            for (int i = 0; i < debugPos.Count; i++)
            {
                Gizmos.color = debugPosColor[i];
                Gizmos.DrawSphere(debugPos[i], size);
            }
        }
    }
}
