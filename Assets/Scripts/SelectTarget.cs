using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class SelectTarget : MonoBehaviour
{
    [Header("start button")]
    [SerializeField] public bool selectFragments = false;

    private DetectFragmentLinks selector;
    private CutLink cutLink;
    private GroupManager groupManager;
    private Overview overview;
    private ListCandidate listCandidate;


    public GameObject groupSelected;            
    public List<Vector2Int> skeletonIdxSelect;  

    private void Start()
    {
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
        overview = GameObject.Find("Overview").GetComponent<Overview>();
        listCandidate = GameObject.Find("ListCandidate").GetComponent<ListCandidate>();
        skeletonIdxSelect = new List<Vector2Int>();
    }

    void Update()
    {
        if (selectFragments)
        {
            groupSelected = listCandidate.groups[listCandidate.groupi];


            skeletonIdxSelect = new List<Vector2Int>();
            Transform groupT = groupSelected.transform;
            Transform[] groupChildren = new Transform[groupT.childCount];
            for (int i = 0; i < groupChildren.Length; i++)
                groupChildren[i] = groupT.GetChild(i).GetChild(0);
            var targetOpenLinks = FragmentData.GetAllOpenLinks(groupChildren);
            foreach (var l in targetOpenLinks)
                skeletonIdxSelect.Add(new Vector2Int(
                    FragmentData.GetFragmentIndexByName(l.Key.fragmentID),
                    l.Key.face));

            selectFragments = false;
            return;
        }
    }
}
