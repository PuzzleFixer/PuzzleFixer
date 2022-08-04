using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSim : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    private bool firstLoading = true;

    [Header("settings")]
    [SerializeField] Vector3 shift = Vector3.zero;  
    [SerializeField] [Range(0, 1)] float scale = 0.2f;
    [SerializeField] float normalSimiarityAngleTh = 30;

    [SerializeField] private Vector3 cameraPos = new Vector3(0, 0f, 3);
    [SerializeField] private Vector3 cameraEuler = new Vector3(0, -90, 0);

    GUIProcess guiProcess = new GUIProcess();

    void Update()
    {
        if (firstLoading)
        {
            fragmentRef = FragmentData.fragments;
            
            foreach (var frag in fragmentRef)
            {
                frag.objMesh.transform.position = frag.position;
                frag.objMesh.transform.rotation = frag.rotation;
                GroupManager.SetFragment2Center(frag.objMesh.transform.parent);
            }

            Transform Fragment = GameObject.Find("Fragments").transform;
            Fragment.localScale *= scale;
            Fragment.rotation = Quaternion.identity;

            foreach (var frag in fragmentRef)
                frag.objMesh.transform.parent.position += shift;

            GroupManager.SetFragment2Center();

            ContinuousMovement movement = GameObject.Find("XR Rig").GetComponent<ContinuousMovement>();
            movement.SetTransform(cameraPos, cameraEuler);

            firstLoading = false;
        }
    }

    static public float CurvePairMatchScore(Fragment frafRefi, int facei, int loopi, Fragment frafRefj, int facej, int loopj)
    {
        int rlti = frafRefi.SimilarCurve[facei][loopi]
            .FindIndex(s => s.fragmentID == frafRefj.parent.name && s.face == facej && s.curve == loopj);
        int rltj = frafRefj.SimilarCurve[facej][loopj]
            .FindIndex(s => s.fragmentID == frafRefi.parent.name && s.face == facei && s.curve == loopi);
        float score = float.MinValue;
        bool hasScore = false;
        if (rlti >= 0)
        {
            score = Mathf.Max(frafRefi.SimilarCurve[facei][loopi][rlti].matchScore, score);
            hasScore = true;
        }
        if (rltj >= 0)
        {
            score = Mathf.Max(frafRefj.SimilarCurve[facej][loopj][rltj].matchScore, score);
            hasScore = true;
        }
        if (hasScore == false)
        {
            Debug.LogWarning("unfind match score!");
            score = -1;
        }
        return score;
    }

    static public float CurvePairMatchScore(KeyValuePair<CurveIndex, CurveIndex> link)
    {
        Fragment frafRefi = FragmentData.GetFragmentByName(link.Key.fragmentID);
        int facei = link.Key.face;
        int loopi = link.Key.curve;
        Fragment frafRefj = FragmentData.GetFragmentByName(link.Value.fragmentID);
        int facej = link.Value.face;
        int loopj = link.Value.curve;

        return CurvePairMatchScore(frafRefi, facei, loopi, frafRefj, facej, loopj);
    }

    private void OnGUI()
    {
        guiProcess.DrawGUIBar();
    }
}
