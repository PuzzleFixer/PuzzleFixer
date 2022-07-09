using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using XRInteraction;

public class XRSelect : MonoBehaviour
{
    private XRSimpleInteractable xrGrabInteractableSimple = null;
    private XRGrabInteractable xrGrabInteractable = null;
    private XRWorkflowControl xrWorkflowControl;
    private AreaSelectFragments areaSelectFragments;
    private CompareShape compareShape;
    private Transform Container;
    private Transform Shapes;
    private Transform Puzzling;
    private Transform Fragments;

    public Material selectedMaterial;
    public Material originMaterial;

    private RightHandWorkflow rightHandWorkflow;

    private GameObject grabPoint;

    void Start()
    {
        originMaterial = GetComponent<MeshRenderer>().materials[0];
        selectedMaterial = Resources.Load<Material>("MeshObj/MeshMatSelect");
        xrWorkflowControl = GameObject.Find("XRWorkflowControl").GetComponent<XRWorkflowControl>();
        try
        {
            rightHandWorkflow = GameObject.Find("RightHand Controller").transform.Find("HandPrefab")
            .GetComponent<RightHandWorkflow>();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
        }
        Container = GameObject.Find("NextShape").transform.Find("Container");
        Shapes = GameObject.Find("CompareShape").transform.Find("Shapes");
        areaSelectFragments = GameObject.Find("AreaSelectFragments").GetComponent<AreaSelectFragments>();
        compareShape = GameObject.Find("CompareShape").GetComponent<CompareShape>();
        Puzzling = GameObject.Find("Puzzling").transform;
        Fragments = null;
        var FragmentsObj = GameObject.Find("Fragments");
        if (FragmentsObj != null)
            Fragments = FragmentsObj.transform;
    }

    void Update()
    {
        if (xrGrabInteractableSimple == null)
        {
            xrGrabInteractableSimple = GetComponent<XRSimpleInteractable>();

            xrGrabInteractableSimple.selectEntered.AddListener(SwitchSelect);
            xrGrabInteractableSimple.selectExited.AddListener(ExitSelect);
        }

        if (xrGrabInteractable == null)
        {
            xrGrabInteractable = GetComponent<XRGrabInteractable>();

            xrGrabInteractable.selectEntered.AddListener(HoldingEnter);
            xrGrabInteractable.selectExited.AddListener(ExitHolding);
        }
    }

    private void SwitchSelect(SelectEnterEventArgs interactor)
    {
        Transform transform = GetCloestTransform(interactor);

        if (xrWorkflowControl.step == 3 && xrWorkflowControl.stepFlag)
        {
            List<Transform> availableFragment = GetLinkedFragments(transform);          
            
            AreaSelectFragments.SelectionFragment selectionFragment = areaSelectFragments.selectionFragment;
            
            foreach (var fragment in availableFragment)
            {
                if (selectionFragment.Contains(fragment))
                {
                    selectionFragment.RemoveTransforms(fragment);
                    fragment.GetComponent<MeshRenderer>().material = originMaterial;
                }
                else
                {
                    selectionFragment.AddTransforms(fragment);
                    fragment.GetComponent<MeshRenderer>().material = selectedMaterial;
                }
            }

        }

        // select one group 
        else if(xrWorkflowControl.step <= 4 && !xrWorkflowControl.stepFlag)
        {
            AreaSelectFragments.SelectionFragment selectionGroup = areaSelectFragments.selectionGroup;
            
            selectionGroup.ResetMaterialGroup();
            
            if (selectionGroup.Contains(transform.parent.parent.parent))
            {
                selectionGroup.RemoveTransforms(transform.parent.parent.parent);
            }
            else
            {
                selectionGroup.ClearAll();
                selectionGroup.AddTransforms(transform.parent.parent.parent);
                
                Transform groupT = transform.parent.parent.parent;
                
                foreach (Transform fragment in groupT)
                    fragment.GetChild(0).Find("default").GetComponent<MeshRenderer>().material = selectedMaterial;
            }

            xrWorkflowControl.step = 4;
        }

        else if (xrWorkflowControl.step == 11 && xrWorkflowControl.stepFlag)
        {            
            AreaSelectFragments.SelectionFragment selectionFragment = areaSelectFragments.selectionFragment;

            if (selectionFragment.Contains(transform))
            {
                selectionFragment.RemoveTransforms(transform);
            }
            else
            {
                selectionFragment.AddTransforms(transform);
            }
        }

    }

    private void ExitSelect(SelectExitEventArgs interactor)
    {

    }

    private void HoldingEnter(SelectEnterEventArgs interactor)
    {
        if (xrWorkflowControl.step == 10 && xrWorkflowControl.stepFlag)
        {
            rightHandWorkflow.grabingCompare = true;
            
            Transform shapes = Shapes.transform;
            foreach (Transform candidates in shapes)
                foreach (Transform group in candidates)
                    group.GetComponent<BoxCollider>().enabled = false;
            Container.GetComponent<BoxCollider>().enabled = false;

            rightHandWorkflow.have_fragment = true;

            
            grabPoint = new GameObject("GrabPoint");
            grabPoint.transform.parent = interactor.interactableObject.transform;
            grabPoint.transform.position = interactor.interactorObject.transform.position;
            grabPoint.transform.rotation = interactor.interactorObject.transform.rotation;
            xrGrabInteractable.attachTransform = grabPoint.transform;
        }
    }

    private void ExitHolding(SelectExitEventArgs interactor)
    {
        if (xrWorkflowControl.step == 10 && xrWorkflowControl.stepFlag)
        {
            rightHandWorkflow.grabingCompare = false;
            
            AreaSelectFragments.SelectionFragment selectionGroup = areaSelectFragments.selectionGroup;
            selectionGroup.ClearAll();
            selectionGroup.AddTransforms(transform.parent.parent.parent);
            
            compareShape.MagneticEffect();

            Transform shapes = Shapes.transform;
            foreach (Transform candidates in shapes)
                foreach (Transform group in candidates)
                {
                    var bc = group.GetComponent<BoxCollider>();
                    if (bc != null)
                        bc.enabled = true;
                }
                    
            Container.GetComponent<BoxCollider>().enabled = true;

            rightHandWorkflow.have_fragment = false;

            Destroy(grabPoint);
        }
    }


    static private List<Transform> AvailableFragment(Transform originFrag, List<string> visitedFrag)
    {
        
        List<Transform> reachFragments = new List<Transform>();

        reachFragments.Add(originFrag);
        
        Fragment currentFrag = FragmentData.GetFragmentByName(originFrag.parent.parent.name);
        
        List<KeyValuePair<CurveIndex, CurveIndex>> skeletonLink = currentFrag.skeletonLink;
        
        if (skeletonLink.Count == 0)
            return reachFragments;
        
        foreach (var link in skeletonLink)
        {
            if (link.Value.fragmentID != "unknown")
            {
                Transform fragParent = originFrag.parent.parent.parent;

                if (!visitedFrag.Contains(link.Value.fragmentID))
                {
                    visitedFrag.Add(link.Value.fragmentID);
                    
                    List<Transform> reachableFragments = AvailableFragment(fragParent.Find(link.Value.fragmentID).GetChild(0).GetChild(0), visitedFrag);
                    if (reachableFragments.Count > 0)
                    {
                        reachFragments = reachFragments.Union(reachableFragments).ToList();
                    }
                }
            }
        }

        return reachFragments;
    }


    static public List<Transform> GetLinkedFragments(Transform transform)
    {
        List<string> visitedFrag = new List<string>();
        visitedFrag.Clear();
        visitedFrag.Add(transform.parent.parent.name);

        List<Transform> linkedFragment = new List<Transform>();
        linkedFragment = linkedFragment.Union(AvailableFragment(transform, visitedFrag)).ToList();
        return linkedFragment;
    }

    
    private Transform GetCloestTransform(SelectEnterEventArgs interactor)
    {
        Vector3 handPosition = interactor.interactorObject.transform.position;
        List<Transform> activeFragShape = new List<Transform>();
        
        activeFragShape.AddRange(Puzzling.GetComponentsInChildren<Transform>()
            .Where(t => t.name == FragmentTypes.Default)
            .ToArray());
        if (Fragments != null)
        {
            activeFragShape.AddRange(Fragments.GetComponentsInChildren<Transform>()
            .Where(t => t.name == FragmentTypes.Default)
            .ToArray());
        }
        float[] dis = activeFragShape
            .Select(s => (s.GetComponent<Renderer>().bounds.center - handPosition).magnitude)
            .ToArray();
        int cloestIdx = Array.IndexOf(dis, dis.Min());

        Transform transform = activeFragShape[cloestIdx];

        return transform;
    }
}
