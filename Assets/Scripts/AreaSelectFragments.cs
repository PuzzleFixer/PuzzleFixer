using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;

public class AreaSelectFragments : MonoBehaviour
{
    Fragment[] fragmentRef = null;

    [Header("start button")]
    [SerializeField] public bool groupFragment = false;
    private bool groupFragmentLast = false;

    private GroupManager groupManager;

    private static Material defaultMaterial;
    public SelectionFragment selectionFragment = new SelectionFragment();
    public SelectionFragment selectionGroup = new SelectionFragment();
    
    public class SelectionFragment
    {
        public List<Transform> transforms = new List<Transform>();

        public bool Contains(Transform t)
        {
            return transforms.Contains(t);
        }
        
        public void RemoveTransforms(Transform t)
        {
            if (!transforms.Contains(t))
            {
                Debug.Log("No such transform");
            }
            else
            {
                transforms.Remove(t);
            }
        }

        public void AddTransforms(Transform t)
        {
            if (t == null)
                Debug.LogError("add illness transforms");
            transforms.Add(t);
        }

        public void ClearAll()
        {
            
            transforms.Clear();
        }

        public void ResetMaterial()
        {
            foreach (var transform in transforms)
            {
                if (transform.name == FragmentTypes.Default)
                {
                    transform.gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
                }
            }
        }

        public void ResetMaterialGroup()
        {
            foreach (var transform in transforms)
            {
                if (transform.name == FragmentTypes.Group)
                {
                    Transform groupT = transform;
                

                    foreach (Transform fragment in groupT)
                    {
                        fragment.GetChild(0).Find("default").GetComponent<MeshRenderer>().material = defaultMaterial;
                    }
                }
            }
        }
    }

    private void Start()
    {
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
        defaultMaterial = Resources.Load<Material>("MeshObj/MeshMat");
    }

    private void Update()
    {
        if (groupFragment)
        {
            if (groupFragmentLast == false)
            {
                Debug.Log("Please select fragments to group in scene view and click in game view...");
                fragmentRef = FragmentData.fragments;
            }

            groupFragmentLast = true;
        }
        else if (groupFragmentLast)
        {
            groupFragmentLast = false;
        }
    }

    private void OnGUI()
    {
        if (groupFragment && Event.current.type == EventType.MouseUp)
        {
            List<GameObject> selectedFrag = GetSelectedFragments();

            if (selectedFrag.Count > 0)
            {
                Debug.Log("event mouse up: grouping...");
                
                GroupFragments(groupManager, selectedFrag);
                
            }
        }
    }

    public void GroupFragmentsByVR()
    {
        if (groupFragment)
        {
            List<GameObject> selectedFrag = GetSelectedFragments(true);

            if (selectedFrag.Count > 0)
            {
                Debug.Log("controller trigger: grouping...");
                
                selectionFragment.ResetMaterial();
                
                GroupFragments(groupManager, selectedFrag);
                
                selectionFragment.ClearAll();
            }
        }
    }

    static public GameObject GroupFragments(GroupManager groupManager, List<GameObject> fragToBeGrouped)
    {
        GameObject groupObj = groupManager.Group(fragToBeGrouped);

        GroupFragmentColorSettings(groupObj);

        return groupObj;
    }

    static public void GroupFragmentColorSettings(GameObject groupObj)
    {
        Transform[] fragsTransform = GroupManager.GetAllFragTransform(groupObj.transform);
        Transform[] fragsParentTransform = fragsTransform.Select(t => t.parent).ToArray();
        var internalLinks = FragmentData.GetAllInternalLinks(fragsParentTransform);
        foreach (var internalLink in internalLinks)
        {
            Fragment frag = FragmentData.GetFragmentByName(internalLink.Key.fragmentID);
            MeshColor.DrawFaceColor(frag, internalLink.Key.face, MeshColor.resetColor);
        }
    }

    public List<GameObject> GetSelectedFragments(bool if_VR = false)
    {
        if (!if_VR)
        {
            Debug.LogWarning("can not using UnityEditor.Selection");
            return null;
        }
        else
        {
            var selectedObj = selectionFragment.transforms;
            selectionFragment.ResetMaterial();
            
            List<GameObject> selectedFrag = new List<GameObject>();
            
            foreach (var obj in selectedObj)
                if (obj.name == FragmentTypes.Default)
                    selectedFrag.Add(obj.transform.parent.parent.gameObject);
            
            selectedFrag = selectedFrag.Distinct().ToList();
            
            return selectedFrag;
        }
    }
    
    public List<GameObject> GetSelectedFragments(string selectName, bool if_VR = false)
    {
        if (!if_VR)
        {
            var selectedObj = new Transform[] { GameObject.Find("Fragment0").transform.parent };
            List<GameObject> selectedFrag = new List<GameObject>();

            foreach (var obj in selectedObj)
                if (obj.name == selectName)
                    selectedFrag.Add(obj.gameObject);

            selectedFrag = selectedFrag.Distinct().ToList();

            return selectedFrag;
        }
        else
        {
            var selectedObj = selectionGroup.transforms;
            List<GameObject> selectedFrag = new List<GameObject>();

            foreach (var obj in selectedObj)
                if (obj.name == selectName)
                    selectedFrag.Add(obj.gameObject);

            selectedFrag = selectedFrag.Distinct().ToList();

            return selectedFrag;
        }
    }
}
