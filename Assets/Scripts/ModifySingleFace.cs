using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModifySingleFace : MonoBehaviour
{
    Fragment[] fragmentRef = null;
    public SelectFace selectFace;
    Overview overview;

    [Header("start button")]
    public bool modifySingleFace = false;
    private bool modifySingleFaceLast = false;

    [Header("operations")]
    [SerializeField] private bool addOpenLink = false;
    [SerializeField] private bool deleteOpenLink = false;
    [SerializeField] public bool switchOpenLink = false;
    [SerializeField] private bool saveResult = true;

    void Start()
    {
        selectFace = GameObject.Find("SelectFace").GetComponent<SelectFace>();
        overview = GameObject.Find("Overview").GetComponent<Overview>();
    }

    void Update()
    {
        if (modifySingleFace)
        {
            if (modifySingleFaceLast == false)
            {
                Debug.Log("Please select face to add or delete open link ...");
                fragmentRef = FragmentData.fragments;

                foreach (var frag in fragmentRef)
                    frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<BoxCollider>().enabled = false;

                selectFace.selectFace = true;
            }

            if (switchOpenLink)
            {
                if (selectFace.faceidx > 0)
                {
                    int linkIdx = selectFace.frag.skeletonLink.FindIndex(
                        s => s.Key.fragmentID == selectFace.frag.parent.name && s.Key.face == selectFace.faceidx);
                    
                    if (linkIdx == -1)
                    {
                        selectFace.frag.faceIsRough[selectFace.faceidx] = true;
                        selectFace.frag.skeletonLink.Add(new KeyValuePair<CurveIndex, CurveIndex>(
                            new CurveIndex(selectFace.frag.parent.name, selectFace.faceidx, 0), new CurveIndex(true)));
                        overview.ForceRedraw();
                        MeshColor.DrawMeshColor(fragmentRef);
                    }
                    else
                    {
                        int partnerFragIdx = FragmentData.GetFragmentIndexByName(
                            selectFace.frag.skeletonLink[linkIdx].Value.fragmentID);
                        if (partnerFragIdx == -1)
                        {
                            selectFace.frag.faceIsRough[selectFace.faceidx] = false;
                            selectFace.frag.skeletonLink.RemoveAt(linkIdx);
                            overview.ForceRedraw();
                            MeshColor.DrawMeshColor(fragmentRef);
                        }
                    }
                }

                switchOpenLink = false;
            }
            

            if (addOpenLink)
            {
                if (selectFace.faceidx > 0)
                {
                    int linkIdx = selectFace.frag.skeletonLink.FindIndex(
                        s => s.Key.fragmentID == selectFace.frag.parent.name && s.Key.face == selectFace.faceidx);

                    if (linkIdx == -1)
                    {
                        selectFace.frag.faceIsRough[selectFace.faceidx] = true;
                        selectFace.frag.skeletonLink.Add(new KeyValuePair<CurveIndex, CurveIndex>(
                            new CurveIndex(selectFace.frag.parent.name, selectFace.faceidx, 0), new CurveIndex(true)));
                        overview.ForceRedraw();
                        MeshColor.DrawMeshColor(fragmentRef);
                    }
                }

                addOpenLink = false;
            }


            if (deleteOpenLink)
            {
                if (selectFace.faceidx > 0)
                {
                    int linkIdx = selectFace.frag.skeletonLink.FindIndex(
                        s => s.Key.fragmentID == selectFace.frag.parent.name && s.Key.face == selectFace.faceidx);

                    if (linkIdx > 0)
                    {
                        int partnerFragIdx = FragmentData.GetFragmentIndexByName(
                            selectFace.frag.skeletonLink[linkIdx].Value.fragmentID);
                        if (partnerFragIdx == -1)
                        {
                            selectFace.frag.faceIsRough[selectFace.faceidx] = false;
                            selectFace.frag.skeletonLink.RemoveAt(linkIdx);
                            overview.ForceRedraw();
                            MeshColor.DrawMeshColor(fragmentRef);
                        }
                    }
                }

                deleteOpenLink = false;
            }

            modifySingleFaceLast = true;
        }
        else if (modifySingleFaceLast)
        {
            foreach (var frag in fragmentRef)
                frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<BoxCollider>().enabled = true;

            if (saveResult)
                foreach (var frag in fragmentRef)
                    frag.Save();

            selectFace.selectFace = false;
            modifySingleFaceLast = false;
        }
    }
}
