using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dragger : MonoBehaviour
{  
    private Transform targetTransform = null;

    [Header("physics effect")]
    [SerializeField] float dragForce = 0.5f;
    [SerializeField] float rotateForce = 1.5f;

    // scyn
    private List<Vector3> anchorPoints = new List<Vector3>();           
    private List<Vector3> dragPointsLocal = new List<Vector3>();        
    private List<Vector3> dragDirLocal = new List<Vector3>();           
    private List<Transform> dragFragTransform = new List<Transform>();

    // origin transform
    private Vector3 originPositon;
    private Quaternion originRotationLocal;

    // last transform
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Quaternion lastRotationLocal;
    private Vector3 lastTargetCenter;

    // temp drag transform
    GameObject dragShell = null;

    private GroupManager groupManager;

    private void Start()
    {
        transform.position = Vector3.zero;
        groupManager = GameObject.Find("Fragments").GetComponent<GroupManager>();
    }

    private void Update()
    {
        if (targetTransform != null)
        {
            TransformGroup();
        }
    }

    private void LateUpdate()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastRotationLocal = transform.localRotation;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (targetTransform == null && groupManager.IsFragmentChild(other.transform))
        {
            targetTransform = other.transform.parent.parent;
            if (targetTransform.parent.name == FragmentTypes.Group)
                targetTransform = targetTransform.parent;

            
            lastTargetCenter = GroupManager.GetFragmentsCenter(targetTransform);
            dragShell = new GameObject();
            dragShell.name = "dragShell";
            dragShell.transform.position = lastTargetCenter;
            dragShell.transform.parent = targetTransform.parent;
            targetTransform.parent = dragShell.transform;

            originRotationLocal = dragShell.transform.localRotation;
            originPositon = dragShell.transform.position;

            transform.localRotation = dragShell.transform.localRotation;

            List<KeyValuePair<CurveIndex, CurveIndex>> links = 
                FragmentData.GetAllExternalLinks(targetTransform, out _);


            anchorPoints = new List<Vector3>();
            dragPointsLocal = new List<Vector3>();
            dragDirLocal = new List<Vector3>();
            dragFragTransform = new List<Transform>();
            for (int li = 0; li < links.Count; li++)
            {
                Fragment fragAnchor = FragmentData.GetFragmentByName(links[li].Value.fragmentID);
                Fragment fragDrag = FragmentData.GetFragmentByName(links[li].Key.fragmentID);
                Transform anchorDefault = fragAnchor.objMesh.transform.Find(FragmentTypes.Default);
                Transform dragDefault = fragDrag.objMesh.transform.Find(FragmentTypes.Default);

                anchorPoints.Add(anchorDefault.TransformPoint(anchorDefault.GetComponent<MeshFilter>().mesh.bounds.center));
                dragPointsLocal.Add(dragDefault.GetComponent<MeshFilter>().mesh.bounds.center);
                dragFragTransform.Add(dragDefault);

                Vector3 anchorDir = anchorPoints[li] - dragFragTransform[li].TransformPoint(dragPointsLocal[li]);
                dragDirLocal.Add(dragFragTransform[li].InverseTransformDirection(anchorDir));
            }
        }
    }


    private void TransformGroup()
    {
        Transform dragShellT = dragShell.transform;
        Vector3 dragDelta = transform.position - lastTargetCenter;
        Quaternion rotateDelta = transform.localRotation * Quaternion.Inverse(dragShellT.localRotation);

        Debug.DrawLine(lastTargetCenter, transform.position);

        if (anchorPoints.Count > 0)
        {
            Vector3 tpos = dragShellT.position;
            Quaternion trot = dragShellT.localRotation;
            dragShellT.position += dragDelta;
            dragShellT.localRotation = rotateDelta * dragShellT.localRotation;
            float angleDiff = 0.0f;
            for (int i = 0; i < dragFragTransform.Count; i++)
            {
                Vector3 dragDirFix = dragFragTransform[i].TransformDirection(dragDirLocal[i]);
                Vector3 dragCenter = dragFragTransform[i].TransformPoint(dragPointsLocal[i]);
                Vector3 anchorDir = anchorPoints[i] - dragCenter;
                angleDiff += Vector3.Angle(dragDirFix, anchorDir);

                // debug
                Debug.DrawLine(dragCenter, dragCenter + dragDirFix, Color.green);
                Debug.DrawLine(dragCenter, dragCenter + anchorDir, Color.cyan);
            }
            float damp = Mathf.Exp(-angleDiff * 0.009f);
            

            if (dragDelta.magnitude > 1)
                dragDelta = dragDelta.normalized;
            lastTargetCenter += dragDelta * Time.deltaTime * dragForce * damp;
            dragShellT.position = tpos + dragDelta * Time.deltaTime * dragForce * damp;
            dragShellT.localRotation = Quaternion.RotateTowards(trot, dragShellT.localRotation,
                Time.deltaTime * rotateForce * damp);
        }
        else
        {
            lastTargetCenter += dragDelta;
            dragShellT.position += dragDelta;
            dragShellT.localRotation = rotateDelta * dragShellT.localRotation;
        }
    }

    public void ResetTransform()
    {
        if (targetTransform != null)
        {
            dragShell.transform.position = originPositon;
            dragShell.transform.localRotation = originRotationLocal;
            lastTargetCenter = GroupManager.GetFragmentsCenter(targetTransform);
        }
    }

    private void OnBecameVisible()
    {
        targetTransform = null;
        dragShell = null;
    }

    private void OnBecameInvisible()
    {
        if (dragShell != null)
        {
            targetTransform.parent = dragShell.transform.parent;
            Destroy(dragShell);
        }
        targetTransform = null;
    }
}
