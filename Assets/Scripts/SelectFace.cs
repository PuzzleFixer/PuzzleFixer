using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SelectFace : MonoBehaviour
{
    [Header("start button")]
    public bool selectFace = false;
    private bool selectFaceLast = false;
    public bool showHitPoint = true;
    public bool setHitPos = false;
    public Transform hitPosT;
    
    [HideInInspector] public GameObject hitPos = null;
    [SerializeField] private Vector3 hitBallSize = Vector3.one * 0.1f;
    [SerializeField] bool showHitPos = false;

    // select result
    [HideInInspector] public Fragment frag;
    [HideInInspector] public int faceidx;
    [HideInInspector] public int pointIdx;
    [HideInInspector] public Transform hitTransform;
    [HideInInspector] public Vector3 hitPosition;

    [SerializeField] bool showDebug;
    private Ray ray;

    void Update()
    {
        if (selectFace)
        {
            if (selectFaceLast == false)
            {
                if (showHitPos)
                {
                    hitPos = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    hitPos.transform.position = new Vector3(-100, 0, 0);
                    hitPos.transform.localScale = hitBallSize;
                    if (showHitPoint == false)
                        hitPos.transform.localScale = Vector3.zero;
                    hitPos.name = "hitPos";
                    hitPos.transform.parent = transform;
                    Destroy(hitPos.GetComponent<SphereCollider>());

                    var gi = hitPos.AddComponent<XRGrabInteractable>();
                    gi.interactionLayers = (1 << LayerMask.NameToLayer("RightHand")) |
                        (1 << LayerMask.NameToLayer("LeftHand"));
                    Rigidbody hitPosRigidbody = hitPos.GetComponent<Rigidbody>();
                    hitPosRigidbody.useGravity = false;
                    hitPosRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                    hitPosRigidbody.isKinematic = true;
                }

                frag = null;
                faceidx = -1;
                pointIdx = -1;
                hitTransform = null;
            }


            if (Input.GetMouseButton(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hitTransform = hit.transform;
                    hitPosition = hit.point;

                    if (showHitPos)
                        hitPos.transform.position = hit.point;


                    if (hit.triangleIndex >= 0)
                    {
                        frag = FragmentData.GetFragmentByName(hit.transform.parent.parent.name);
                        if (frag != null)
                        {
                            Mesh mesh = frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh;
                            pointIdx = mesh.triangles[hit.triangleIndex * 3];
                            faceidx = frag.faceIdx[pointIdx];

                            Debug.Log(hit.transform.parent.parent.name + "\tface: " + faceidx + "\tpid: " + pointIdx);
                        }
                    }
                }
            }

            // VR
            if (setHitPos)
            {
                ray = Camera.main.ScreenPointToRay(
                    Camera.main.WorldToScreenPoint(hitPosT.position, Camera.MonoOrStereoscopicEye.Left), Camera.MonoOrStereoscopicEye.Left);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hitTransform = hit.transform;
                    hitPosition = hit.point;

                    if (showHitPos)
                        hitPos.transform.position = hit.point;


                    if (hit.triangleIndex >= 0)
                    {
                        frag = FragmentData.GetFragmentByName(hit.transform.parent.parent.name);
                        if (frag != null)
                        {
                            Mesh mesh = frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh;
                            faceidx = frag.faceIdx[mesh.triangles[hit.triangleIndex * 3]];

                            Debug.Log(hit.transform.parent.parent.name + "\tface: " + faceidx);
                        }
                    }
                    else
                    {
                        faceidx = -1;
                    }
                }

                setHitPos = false;
            }

            selectFaceLast = true;
        }
        else if (selectFaceLast)
        {
            frag = null;
            faceidx = -1;
            hitTransform = null;
            selectFaceLast = false;

            if (showHitPos)
                Destroy(hitPos);
        }
    }

    private void OnDrawGizmos()
    {
        if (selectFace && showDebug)
        {
            Debug.DrawRay(Camera.main.transform.position, ray.direction, Color.cyan);
        }
    }
}
