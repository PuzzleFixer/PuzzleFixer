using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.XR.Interaction.Toolkit;


public class GroupManager : MonoBehaviour
{
    public GameObject Group(List<GameObject> objList)
    {
        GameObject groupObj;
        List<GameObject> groups = new List<GameObject>();   
        List<List<Transform>> structTransform = new List<List<Transform>>();    
                                                                                
        structTransform.Add(new List<Transform>());


        foreach (var obj in objList)
        {
            if (obj.transform.parent.name == FragmentTypes.Group)
            {
                int groupidx = groups.FindIndex(g => g == obj.transform.parent.gameObject);
                if (groupidx != -1)
                    structTransform[groupidx + 1].Add(obj.transform);
                else
                {
                    groups.Add(obj.transform.parent.gameObject);
                    structTransform.Add(new List<Transform>() { obj.transform });
                }
            }
            else
            {
                structTransform[0].Add(obj.transform);
                if (obj.transform.GetChild(0).transform.Find("default"))
                {

                }
            }
        }


        if (groups.Count == 0)
        {
            groupObj = NewGroupObj(structTransform[0]);
        }
        else
        {
            List<Transform> transformToBeGrouped = new List<Transform>();
            List<GameObject> emptyGroupObj = new List<GameObject>();
            transformToBeGrouped.AddRange(structTransform[0]);
            for (int gi = 0; gi < groups.Count; gi++)
            {
                Transform[] fragTransform = GetAllFragTransform(groups[gi].transform);
                Transform[] fragParentTransform = new Transform[fragTransform.Length];
                for (int ti = 0; ti < fragTransform.Length; ti++)
                    fragParentTransform[ti] = fragTransform[ti].parent;

                transformToBeGrouped.AddRange(structTransform[gi + 1].Intersect(fragParentTransform));
                if (fragParentTransform.Length == structTransform[gi + 1].Count)
                    emptyGroupObj.Add(groups[gi]);
            }

            groupObj = NewGroupObj(transformToBeGrouped);

            for (int i = 0; i < emptyGroupObj.Count; i++)
                Destroy(emptyGroupObj[i]);
        }

        return groupObj;
    }


    public void UnGroupAll(List<GameObject> exp)
    {
        List<Transform> groupChildren = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            int rlt = exp.FindIndex(g => g == child);
            if (child.name == FragmentTypes.Group && rlt == -1)
                groupChildren.Add(child.transform);
        }

        for (int i = 0; i < groupChildren.Count; i++)
        {
            Transform group = groupChildren[i];
            while (group.childCount > 0)
                group.GetChild(0).parent = group.parent;
            Destroy(group.gameObject);
        }
            
    }


    public void Split(GameObject obj1, GameObject obj2)
    {
        if (obj1.transform.parent == obj2.transform.parent &&
            obj1.transform.parent != transform)
        {
            Transform group = obj1.transform.parent;
            List<List<Transform>> newGroupList = new List<List<Transform>>();
            List<Transform> leftObj = new List<Transform>();
            foreach (Transform obj in group.transform)
                leftObj.Add(obj);

            List<Transform> newGroup = new List<Transform>();
            Stack<Transform> nextObj = new Stack<Transform>();
            while (leftObj.Count > 0 || nextObj.Count > 0)
            {
                Transform obj;  // add current obj
                if (nextObj.Count == 0)
                {
                    newGroupList.Add(newGroup);
                    newGroup = new List<Transform>();

                    obj = leftObj[0];
                    newGroup.Add(obj);
                    leftObj.RemoveAt(0);
                }
                else
                {
                    obj = nextObj.Pop();
                    newGroup.Add(obj);
                }


                List<Transform> neighborTransform = new List<Transform>();
                var link = FragmentData.GetFragmentByName(obj.name).skeletonLink;
                foreach (var l in link)
                {
                    var linkFrag = FragmentData.GetFragmentByName(l.Value.fragmentID);
                    if (linkFrag != null)
                        neighborTransform.Add(linkFrag.objMesh.transform.parent);
                }


                neighborTransform = neighborTransform.Intersect(leftObj).ToList();
                foreach (var t in neighborTransform)
                    nextObj.Push(t);
                leftObj = leftObj.Except(neighborTransform).ToList();
            }
            newGroupList.Add(newGroup);
            newGroupList.RemoveAt(0);


            foreach (var newgroup in newGroupList)
                NewGroupObj(newgroup);
            Destroy(group.gameObject);
        }
    }


    private GameObject NewGroupObj(List<Transform> children)
    {
        GameObject groupObj = new GameObject();
        groupObj.name = FragmentTypes.Group;
        groupObj.transform.parent = transform;
        groupObj.AddComponent<ParentSelect>();

        foreach (var obj in children)
            obj.transform.parent = groupObj.transform;
        
        return groupObj;
    }

    private GameObject NewGroupObj(List<GameObject> children)
    {
        List<Transform> childrenTransform = new List<Transform>();
        foreach (var c in children)
            childrenTransform.Add(c.transform);

        return NewGroupObj(childrenTransform);
    }


    static public Transform[] GetAllFragTransform(Transform transform)
    {
        var meshTransforms = transform.GetComponentsInChildren<Transform>(true).
            Where(t => t.name == FragmentTypes.Default).ToArray();
        
        Transform[] fragTransforms = new Transform[meshTransforms.Length];
        for (int i = 0; i < fragTransforms.Length; i++)
            fragTransforms[i] = meshTransforms[i].parent;

        return fragTransforms;
    }


    public Transform[] GetAllSecondaryTransforms()
    {
        List<Transform> secondTransform = new List<Transform>();
        foreach (Transform t in transform)
            secondTransform.Add(t);

        return secondTransform.ToArray();
    }


    public int[][] GetSecondaryParent(List<Transform> objTransform, out List<Transform> sParent)
    {
        sParent = new List<Transform>();
        List<List<int>> groupIdx = new List<List<int>>();
        int transIdx = 0;
        foreach (var trans in objTransform)
        {
            Transform fragmentTransform = trans.parent;
            Transform parentTransform;
            if (fragmentTransform.parent == transform)
                parentTransform = fragmentTransform;
            else
                parentTransform = fragmentTransform.parent;

            int spi = sParent.FindIndex(p => p == parentTransform);
            if (spi == -1)
            {
                sParent.Add(parentTransform);
                groupIdx.Add(new List<int>() { transIdx });
            }
            else
                groupIdx[spi].Add(transIdx);

            transIdx += 1;
        }

        return groupIdx.Select(a => a.ToArray()).ToArray();
    }


    public void RelocateGroup(Transform group, Vector3 center)
    {
        Transform[] fragTransform = GetAllFragTransform(group);
        Vector3 groupCenter = Vector3.zero;
        foreach (var t in fragTransform)
        {
            groupCenter += t.Find(FragmentTypes.Default).GetComponent<Renderer>().bounds.center;
        }
        groupCenter /= fragTransform.Length;

        group.position += center - groupCenter;
    }


    public List<Transform> GetBrotherNodes(Transform t)
    {
        List<Transform> brothers = new List<Transform>();
        Transform parentTransform = t.parent;
        foreach (Transform bt in parentTransform)
            brothers.Add(bt);

        return brothers;
    }

    public bool IsFragmentChild(Transform t)
    {
        return t.IsChildOf(transform);
    }


    static public Vector3 GetFragmentsCenter(Transform fragParent)
    {
        Vector3 center = Vector3.zero;
        Transform[] fragTransform = GetAllFragTransform(fragParent);
        for (int i = 0; i < fragTransform.Length; i++)
            center += fragTransform[i].Find(FragmentTypes.Default)
                .GetComponent<Renderer>().bounds.center;
        center /= fragTransform.Length;

        return center;
    }


    public void CenterFragment(Fragment frag, Vector3 centerPos, out GameObject center, out Vector3 pos, out Quaternion rot)
    {
        center = new GameObject();
        center.name = FragmentTypes.Center;
        Transform defTransform = frag.objMesh.transform.Find(FragmentTypes.Default);
        pos = defTransform.position;
        rot = defTransform.rotation;
        center.transform.position = pos;
        center.transform.rotation = rot;
        while (transform.childCount != 0)
            foreach (Transform t in transform)
                t.parent = center.transform;
        center.transform.parent = transform;
        center.transform.position = centerPos;
        center.transform.rotation = Quaternion.identity;
    }

    public void DeleteCenterFragment(GameObject center, Vector3 pos, Quaternion rot)
    {
        Transform centerTransform = center.transform;
        centerTransform.position = pos;
        centerTransform.rotation = rot;
        while (centerTransform.childCount != 0)
            foreach (Transform t in centerTransform)
                t.parent = centerTransform.parent;
        Destroy(center);
    }

    public void DeleteCenterFragment(GameObject center)
    {
        Transform centerTransform = center.transform;
        while (centerTransform.childCount != 0)
            foreach (Transform t in centerTransform)
                t.parent = centerTransform.parent;
        Destroy(center);
    }


    public void CenterGroup(GameObject groupObj, Fragment fragInGroup, Vector3 centerPos)
    {
        GameObject center = new GameObject();
        center.name = FragmentTypes.Center;

        Transform[] fragTrans =  GetAllFragTransform(groupObj.transform);
        Vector3 groupCenter = Vector3.zero;
        for (int i = 0; i < fragTrans.Length; i++)
        {
            Fragment frag = FragmentData.GetFragmentByName(fragTrans[i].parent.name);
            groupCenter += frag.GetCenterPos();
        }
        groupCenter /= fragTrans.Length;
        center.transform.position = groupCenter;
        center.transform.rotation = fragInGroup.objMesh.transform.Find(FragmentTypes.Default).rotation;

        center.transform.parent = groupObj.transform.parent;
        groupObj.transform.parent = center.transform;
        center.transform.position = centerPos;
        center.transform.rotation = Quaternion.identity;

        groupObj.transform.parent = center.transform.parent;
        Destroy(center);
    }

    public Transform CloneAllFragments()
    {
        Transform cloneTransform = Instantiate(transform);
        return cloneTransform;
    }

    public Transform CloneFragment(string transformName)
    {
        Transform[] allTransforms = transform.GetComponentsInChildren<Transform>(true);
        Transform t = Array.Find(allTransforms, at => at.name == transformName);
        return Instantiate(t);
    }


    public List<Transform> GetTransformByName(string name)
    {
        return transform.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == name).ToList();
    }


    public GameObject GetGroup(GameObject fragObj)
    {
        Transform group = fragObj.transform;
        while (group.parent != null && group.parent != this.transform)
            group = group.parent;

        return group.gameObject;
    }

    public bool isBelongTo(GameObject group, GameObject frag)
    {
        Transform obj = frag.transform;
        while (obj.parent != null && obj.parent != group.transform)
            obj = obj.parent;

        if (obj.parent == group.transform)
            return true;
        else
            return false;
    }


    public void ShowGroupOnly(GameObject group)
    {
        foreach (Transform obj in transform)
            obj.gameObject.SetActive(false);
        group.SetActive(true);
    }
    

    public List<GameObject> GetGroups(List<GameObject> fragObj)
    {
        List<GameObject> group = new List<GameObject>();
        foreach (var obj in fragObj)
        {
            Transform t = obj.transform;
            while (t.name != FragmentTypes.Group && t.parent != transform)
                t = t.parent;
            if (t.name == FragmentTypes.Group)
                group.Add(t.gameObject);
        }

        return group.Distinct().ToList();
    }


    static public void SetFragment2Center()
    {
        Fragment[] fragmentRef = FragmentData.fragments;
        Vector3 center = Vector3.zero;
        foreach (var frag in fragmentRef)
        {
            Transform deft = frag.objMesh.transform.GetChild(0);
            center += deft.GetComponent<Renderer>().bounds.center;
        }
        center /= fragmentRef.Length;

        SetFragment2Position(center);
    }


    static public void SetFragment2Center(Transform fragParent)
    {
        Vector3 center = GroupManager.GetFragmentsCenter(fragParent);
        SetFragment2Position(fragParent, center);
    }


    static public void SetFragment2Position(Vector3 pos)
    {
        GameObject temp = new GameObject();
        Transform ft = GameObject.Find("Fragments").transform;
        while (ft.childCount > 0)
            ft.GetChild(0).parent = temp.transform;
        ft.position = pos;
        while (temp.transform.childCount > 0)
            temp.transform.GetChild(0).parent = ft;
        Destroy(temp);
    }

    static public void SetFragment2Position(Transform fragParent, Vector3 pos)
    {
        GameObject temp = new GameObject();
        while (fragParent.childCount > 0)
            fragParent.GetChild(0).parent = temp.transform;
        fragParent.position = pos;
        while (temp.transform.childCount > 0)
            temp.transform.GetChild(0).parent = fragParent;
        Destroy(temp);
    }


    public List<Transform> GroupAllFragmentByLinks(Fragment[] allFragments)
    {
        List<string> fragNameList = allFragments.Select(f => f.GetIDName()).ToList();
        List<Transform> groupTransform = new List<Transform>();
        while (fragNameList.Count > 0)
        {
            Transform def = FragmentData.GetFragmentByName(fragNameList[0]).objMesh.transform.GetChild(0);
            List<Transform> linkedFragDef = XRSelect.GetLinkedFragments(def);
            List<GameObject> linkedFragObj = linkedFragDef.Select(t => t.parent.parent.gameObject).ToList();
            List<string> linkedName = linkedFragObj.Select(obj => obj.name).ToList();
            fragNameList = fragNameList.Except(linkedName).ToList();
            List<Fragment> linkedFrag = linkedName.Select(name => FragmentData.GetFragmentByName(name)).ToList();
            GameObject groupObj = Group(linkedFragObj);    // group
            groupTransform.Add(groupObj.transform);
        }

        return groupTransform;
    }


    public int GetGroupNum()
    {
        int groupCount = 0;
        foreach (Transform t in this.transform)
            if (t.name == FragmentTypes.Group)
                groupCount += 1;

        return groupCount;
    }

    public List<Transform> GetAllGroups()
    {
        List<Transform> groups = new List<Transform>();
        foreach (Transform t in this.transform)
            if (t.name == FragmentTypes.Group)
                groups.Add(t);

        return groups;
    }
}
