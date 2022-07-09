using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WIM : MonoBehaviour
{
    private bool show;
    private Transform showingAllTransform = null;
    private Transform showingTansform = null;   
    private Transform showingTansform2 = null;  
    private Transform lastCandidate = null;
    private Transform cameraColliderEnter = null;

    [Header("Setup of WIM")]
    [SerializeField] float fowardRate = 0.3f;
    [SerializeField] float downRate = 0.1f;
    [SerializeField] float scaleRate = 0.5f;
    [SerializeField] float showInDis = 1.0f;

    private int layerMask = 1;

    [SerializeField] bool showDebug;

    private void Start()
    {
        layerMask = ~(1 << LayerMask.NameToLayer("WIM"));   
        showingAllTransform = new GameObject("allWIMTransforms").transform;
        showingAllTransform.position = Vector3.zero;
        showingAllTransform.parent = this.transform;
    }

    private void Update()
    {
        if (show)
        {
            var camTf = Camera.main.transform;
            if (Physics.Raycast(camTf.position, camTf.forward,
                out var HitInfo, 100.0f, layerMask) || cameraColliderEnter != null)
            {
                Debug.Log("hit: " + HitInfo.transform.name);
                if (cameraColliderEnter != null)
                    Debug.Log("trigger enter: " + cameraColliderEnter.name);

                Transform target = null;
                if ((HitInfo.transform.position - camTf.position).magnitude < showInDis)
                    target = HitInfo.transform;
                else if (cameraColliderEnter != null)
                    target = cameraColliderEnter;
                if (target != null)
                {
                    Transform parentTran = null;
                    bool isGroup = false;
                    if (target.transform.name == FragmentTypes.Group)
                    {
                        isGroup = true;
                        parentTran = target.transform.parent;
                    }

                    if (parentTran != null)  // candidate
                    {
                        bool hasChanged = parentTran.hasChanged;
                        if (hasChanged)
                            parentTran.hasChanged = false;
                        else
                            foreach (Transform group in parentTran)
                            {
                                hasChanged = group.hasChanged;
                                if (hasChanged)
                                {
                                    group.hasChanged = false;
                                    break;
                                }
                            }

                        if (hasChanged || showingTansform == null)
                        {
                            if (showingTansform != null)
                                Destroy(showingTansform.gameObject);

                            showingTansform = CreateWIMTransform(parentTran);
                        }

                        // set position
                        SetWIMPositionScale(showingAllTransform);

                        lastCandidate = parentTran;
                        if (showingTansform2 != null)
                        {
                            Destroy(showingTansform2.gameObject);
                            showingTansform2 = null;
                        }
                    }
                    else if (isGroup)
                    {
                        if (lastCandidate != null)
                        {
                            if (showingTansform != null)
                                Destroy(showingTansform.gameObject);

                            showingTansform = CreateWIMTransform(lastCandidate);
                        }

                        if (showingTansform2 != null)
                            Destroy(showingTansform2.gameObject);

                        showingTansform2 = CreateWIMTransform(target.transform);

                        SetWIMPositionScale(showingAllTransform);
                    }
                    else
                    {
                        ClearWIM();
                    }
                }
                else
                {
                    ClearWIM();
                }
            }

            if (showDebug)
                Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward, Color.red);
        }

    }

    


    public void ShowLookAt(bool show)
    {
        this.show = show;

        if (show == false && showingTansform != null)
        {
            Destroy(showingTansform.gameObject);
            showingTansform = null;
        }
    }

    private Transform CreateWIMTransform(Transform t)
    {
        Transform showingTansform = Instantiate(t);
        var xrGrab = showingTansform.GetComponentsInChildren<XRGrabInteractable>(true);
        foreach (var xrgrab in xrGrab)
            xrgrab.enabled = false;
        var xrSelect = showingTansform.GetComponentsInChildren<XRSelect>(true);
        foreach (var xrselect in xrSelect)
            xrselect.enabled = false;
        var shTf = showingTansform.GetComponentsInChildren<ShapeTransformSkeleton>(true);
        foreach (var s in shTf)
            s.enabled = false;
        var tfs = showingTansform.GetComponentsInChildren<Transform>(true);
        foreach (var tf in tfs)
            tf.gameObject.layer = LayerMask.NameToLayer("WIM");
        var bcs = showingTansform.GetComponentsInChildren<BoxCollider>(true);
        foreach (var bc in bcs)
            bc.enabled = false;
        var mcs = showingTansform.GetComponentsInChildren<MeshCollider>(true);
        foreach (var mc in mcs)
            mc.enabled = false;
        var sbcs = showingTansform.GetComponents<BoxCollider>();
        foreach (var sbc in sbcs)
            sbc.enabled = false;
        var smcs = showingTansform.GetComponents<MeshCollider>();
        foreach (var smc in smcs)
            smc.enabled = false;

        showingAllTransform.position = Vector3.zero;
        showingAllTransform.localScale = Vector3.one;
        showingTansform.parent = showingAllTransform;

        return showingTansform;
    }

    private void SetWIMPositionScale(Transform wimTf)
    {
        var camTf = Camera.main.transform;
        var centers = showingAllTransform.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == FragmentTypes.Default)
            .Select(t => t.GetComponent<Renderer>().bounds.center);
        Vector3 center = Vector3.zero;
        foreach (var c in centers)
            center += c;
        center /= centers.Count();

        Transform newt = new GameObject().transform;
        while (showingAllTransform.childCount > 0)
            showingAllTransform.GetChild(0).parent = newt;
        showingAllTransform.position = center;
        while (newt.childCount > 0)
            newt.GetChild(0).parent = showingAllTransform;
        Destroy(newt.gameObject);
        if (showingAllTransform.localScale == Vector3.one)
            showingAllTransform.localScale *= scaleRate;

        wimTf.position = camTf.position + camTf.forward * fowardRate - camTf.up * downRate;
    }

    private void ClearWIM()
    {
        if (showingTansform != null)
        {
            Destroy(showingTansform.gameObject);
            showingTansform = null;
        }

        if (showingTansform2 != null)
        {
            Destroy(showingTansform2.gameObject);
            showingTansform2 = null;
        }
    }

    public void SetColliderEnter(Transform t)
    {
        cameraColliderEnter = t;
    }
    
    public void SetColliderExit(Transform t)
    {
        if (cameraColliderEnter != null &&  t == cameraColliderEnter)
            cameraColliderEnter = null;
    }

}
