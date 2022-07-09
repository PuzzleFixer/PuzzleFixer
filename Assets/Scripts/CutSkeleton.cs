using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutSkeleton : MonoBehaviour
{
    Fragment[] fragmentRef = null;
    CutLink cutLink;
    Overview overview;

    [Header("start button")]
    public bool cutSkeleton = false;
    private bool cutSkeletonLast = false;

    [Header("setups")]
    private bool cutAll = true;

    private GroupManager groupManager;

    private void Start()
    {
        cutLink = GameObject.Find("CutLink").GetComponent<CutLink>();
        overview = GameObject.Find("Overview").GetComponent<Overview>();
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
    }

    private void Update()
    {
        if (cutSkeleton)
        {
            if (cutSkeletonLast == false)
            {
                Debug.Log("Cutting skeleton...");
                fragmentRef = FragmentData.fragments;
                cutLink.cutLinkStart = true;
            }

            cutLink.SetCutterToHand();

            cutSkeletonLast = true;
        }
        else if (cutSkeletonLast)
        {
            List<int> linkidx = new List<int>();
            if (cutAll)
                linkidx.AddRange(cutLink.raycastAllidx);
            else
                linkidx.Add(cutLink.raycastidx);

            if (linkidx.Count >= 0)
            {
                foreach (var li in linkidx)
                {
                    int[] linkInfo = overview.linkPosInfo[li];
                    int fragi = linkInfo[0];
                    int skeletoni = linkInfo[1];
                    CutSkeletonLink(fragi, skeletoni);
                }
            }
            else
                Debug.Log("nothing is cutted");

            cutLink.cutLinkStart = false;
            cutSkeletonLast = false;
        }
    }


    public void CutSkeletonLink(int fragi, int skeletoni, bool reDraw = true)
    {
        var skeletonLinki = fragmentRef[fragi].skeletonLink[skeletoni];
        int fragj = FragmentData.GetFragmentIndexByName(skeletonLinki.Value.fragmentID);

        if (fragj == -1)
        {

        }
        else
        {
            int skeletonj = fragmentRef[fragj].skeletonLink.FindIndex(
                s => s.Value == skeletonLinki.Key && s.Key == skeletonLinki.Value);
            fragmentRef[fragi].skeletonLink[skeletoni].Value.Clear();
            fragmentRef[fragj].skeletonLink[skeletonj].Value.Clear();

            groupManager.Split(fragmentRef[fragi].objMesh.transform.parent.gameObject,
                fragmentRef[fragj].objMesh.transform.parent.gameObject);
        }

        if (reDraw)
        {
            MeshColor.DrawMeshColor(fragmentRef);
            overview.ForceRedraw();
        }
    }
}
