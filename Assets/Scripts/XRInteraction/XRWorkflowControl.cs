using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;

public class XRWorkflowControl : MonoBehaviour
{

    [SerializeField]
    [Header("For rotatation")]
    public GameObject fragment;
    public GameObject candidateShapes;
    public GameObject selectCandidateShape;
    
    [SerializeField]
    [Header("For the workflow")]
    public CutSkeleton cutSkeleton;
    public AreaSelectFragments areaSelectFragments;
    public ModifySingleFace modifySingleFace;
    public ListCandidate listCandidate;
    public SelectTarget selectTarget;
    public SmallMultiplesAround smallMultiplesAround;
    public SwitchSkeleton switchSkeleton;
    public SmallShapeAround smallShapeAround;
    public SwitchShape switchShape;
    public CompareShape compareShape;
    public ConfirmShape confirmShape;
    public GroupManager groupManager;
    public int step = 0;
    public bool stepFlag = false;

    [SerializeField] private TextMesh stepDes;
    // 0: overview
    // 1: cut skeleton
    // 2: modify single face
    // 3: area select fragment (fragment group)
    // 4: list candidate (select one group)
    // 7: switch skeleton (71: after choosing one skeleton)
    // 9: switch shape (91: after choosing one shape)
    // 10: compare candidate
    // 11: confirm candidate
    

    void Update()
    {
        if (step == 4 && !stepFlag && listCandidate.if_ended)
        {
            SelectTarget();
            listCandidate.if_ended = false;
        }
        else if (smallMultiplesAround.showSmallMultiplesFinish)
        {
            stepDes.text = "Please select a cluster";
            smallMultiplesAround.showSmallMultiplesFinish = false;
        }
    }

    public void CandidateRotate(Quaternion r)
    {
        Transform father = candidateShapes.GetComponent<Transform>();

        Quaternion cr;
        
        for (int i = 0; i < father.childCount; i++)
        {
            Transform candidate = father.GetChild(i);
            cr = candidate.rotation;
            candidate.rotation = new Quaternion(cr.x + r.x, cr.y + r.y, cr.z + r.z, cr.w + r.w);
        }

        Transform selectedCandidate = selectCandidateShape.GetComponent<Transform>().GetChild(0);
        cr = selectedCandidate.rotation;
        selectedCandidate.rotation = new Quaternion(cr.x + r.x, cr.y + r.y, cr.z + r.z, cr.w + r.w);
    }

    private void SetFragmentRotateActive(bool enable)
    {
        fragment.GetComponent<BoxCollider>().enabled = enable;
        fragment.GetComponent<XRGrabInteractable>().enabled = enable;
        fragment.GetComponent<XROverviewSelect>().enabled = enable;
    }


    ////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////// workflow steps //////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    // step=1, stepFlag=true
    public void StartCutSkeleton()
    {
        var groups = groupManager.GetAllGroups();
        int groupNum = groups.Count;
        int mismatchNum = 0;
        for (int i = 0; i < groupNum; i++)
            mismatchNum += FragmentData.GetAllOpenLinks(groups[i]).Count;
        
        if (groupNum > 5 || mismatchNum > 22)
        {
            stepDes.text = "too many mismatches, return";
            Debug.Log("too many mismatches: " + groupNum + ": " + mismatchNum);
            return;
        }

        cutSkeleton.cutSkeleton = true;
        step = 1;
        stepFlag = true;

        SetFragmentRotateActive(false);

        areaSelectFragments.selectionGroup.ClearAll();

        stepDes.text = "Cut Skeleton";
    }
    
    //step=1, stepFlag=false
    public void CutSkeleton()
    {
        cutSkeleton.cutSkeleton = !cutSkeleton.cutSkeleton;
        stepFlag = false;

        groupManager.UnGroupAll(new List<GameObject>());
        groupManager.GroupAllFragmentByLinks(FragmentData.fragments);

        SetFragmentRotateActive(true);

        stepDes.text = "Cut Skeleton (finished)";
    }

    //step=4, stepFlag=true
    public void StartListCandidate()
    {
        listCandidate.listCandidate = true;
        step = 4;
        stepFlag = true;

        SetFragmentRotateActive(false);

        Transform groupT = fragment.transform.Find(FragmentTypes.Group);
        if (groupT == null)
            fragment.GetComponent<GroupManager>().GroupAllFragmentByLinks(FragmentData.fragments);

        ListCandidate();
    }
    
    //step=4, stepFlag=false
    public void ListCandidate(bool hardCode = false)
    {
        AreaSelectFragments.SelectionFragment selectionGroup = areaSelectFragments.selectionGroup;
        if (selectionGroup.transforms.Count > 0 || hardCode)
        {
            GameObject.Find("Fragments").transform.rotation = Quaternion.identity;
            
            stepFlag = false;
            areaSelectFragments.selectionGroup.ResetMaterialGroup();

            stepDes.text = "Listing candidates (please wait)";
        }
        else
        {
            step = 3;
            stepFlag = false;

            stepDes.text = "Listing candidates (finished) no group selected";
        }

    }

    public void SelectTarget()
    {
        selectTarget.selectFragments = true;

        SmallMultiplesAround();
    }

    //step=6, stepFlag=false
    public void SmallMultiplesAround()
    {
        smallMultiplesAround.smallMultiplesAround = true;
        step = 6;

        StartSwitchSkeleton();
    }

    //step=7, stepFlag=true
    public void StartSwitchSkeleton()
    {
        switchSkeleton.switchSkeleton = true;
        step = 7;
        stepFlag = true;

        GroupManager.SetFragment2Position(new Vector3(0, 0, smallMultiplesAround.planeDepth));
    }

    public void ResetPanningDir(Vector3 shift)
    {
        switchSkeleton.panningDir = shift;
    }

    //step=71, stepFlag=true
    public void SelectCurrentSkeleton()
    {
        switchSkeleton.selectCurrentSkeleton = true;
        step = 71;
        stepDes.text = "Cluster Selected";
    }
    
    //step=7, stepFlag=false
    public void SwitchSkeleton()
    {
        switchSkeleton.switchSkeleton = false;
        stepFlag = false;
        stepDes.text = "";
        SmallShapeAround();
    }

    //step=8, stepFlag=false
    public void SmallShapeAround()
    {
        GameObject.Find("Fragments").transform.rotation = Quaternion.identity;
        smallShapeAround.smallShapeAround = true;
        step = 8;


        stepDes.text = "";
        StartSwitchShape();

    }

    //step=9, stepFlag=true
    public void StartSwitchShape()
    {
        switchShape.switchShape = true;
        step = 9;
        stepFlag = true;

        stepDes.text = "Please select candidates";
    }

    public void ResetPanningDirShape(Vector3 shift)
    {
        switchShape.panningDir = shift;
    }

    //step=91, stepFlag=true
    public void SelectCurrentShape()
    {
        switchShape.selectCurrentShape = true;
        step = 91;
        stepDes.text = "Candidate selected";
    }

    //step=91, stepFlag=false
    public void SwitchShape()
    {
        switchShape.switchShape = false;
        stepFlag = false;
        stepDes.text = "";
        CompareShape();
    }

    //step=10, stepFlag=true
    public void CompareShape()
    {
        compareShape.compareShape = true;
        step = 10;
        stepFlag = true;

        stepDes.text = "Please align the fragments";
    }

    //step=11, stepFlag=true
    public void StartConfirmShape()
    {
        step = 11;
        stepFlag = true;
        confirmShape.confirmShape = true;

        stepDes.text = "Please select the reassembly";
    }

    //step=0, stepFlag=false
    public void ConfirmShape()
    {
        GameObject.Find("Fragments").transform.rotation = Quaternion.identity;
        confirmShape.confirmShape = false;
        stepFlag = false;
        step = 0;

        GroupManager.SetFragment2Center();
        SetFragmentRotateActive(true);

        areaSelectFragments.selectionFragment.ClearAll();
        areaSelectFragments.selectionGroup.ClearAll();

        stepDes.text = "Overview";
    }
}
