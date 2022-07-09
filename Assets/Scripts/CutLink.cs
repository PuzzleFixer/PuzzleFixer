using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class CutLink : MonoBehaviour
{
    private Overview overview;

    Fragment[] fragmentRef = null;

    [Header("start button")]
    public bool cutLinkStart = false;
    private bool cutLinkStartLast = false;

    private GameObject Cutter;
    private Vector3 initPos;
    private Vector3 initRot;

    private GameObject controller = null;

    private int leftHandMask = 1 << 9;
    private int rightHandMask = 1 << 10;
    private int layerMask;

    // cut result
    [HideInInspector] public int raycastidx;            // the result of first cutted link info
    [HideInInspector] public List<int> raycastAllidx;   // the result of all cutted link info

    [Header("debug")]
    [SerializeField] private bool showDebug = false;

    struct RayInfo
    {
        public Ray ray;
        public float distance;

        public RayInfo(Ray ray, float distance)
        {
            this.ray = ray;
            this.distance = distance;
        }
    }
    RayInfo[] linkRay;

    private void Start()
    {
        overview = GameObject.Find("Overview").GetComponent<Overview>();

        controller = GameObject.Find("RightHand Controller");

        Cutter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Cutter.name = "Cutter";
        Cutter.transform.parent = transform;
        Cutter.transform.localScale = new Vector3(0.5f, 0.5f, 0.05f);
        Cutter.transform.localScale *= 0.4f;
        Cutter.GetComponent<MeshRenderer>().enabled = false;

        var XRGrab = Cutter.AddComponent<XRGrabInteractable>();
        XRGrab.smoothPosition = true;
        XRGrab.smoothRotation = true;
        XRGrab.interactionLayers = 1 << InteractionLayerMask.NameToLayer("RightHand");


        Rigidbody cutterRigidbody = Cutter.GetComponent<Rigidbody>();
        cutterRigidbody.useGravity = false;
        cutterRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        cutterRigidbody.isKinematic = true;

        ResetCutter();
        RemoveCutter();
        layerMask = ~(leftHandMask | rightHandMask);
    }

    private void Update()
    {
        if (cutLinkStart)
        {
            if (cutLinkStartLast == false)
            {
                Debug.Log("Cutting link...");
                fragmentRef = FragmentData.fragments;

                Debug.Log("fragmentRef.Length = " + fragmentRef.Length);

                // disable all colliders
                foreach (var frag in fragmentRef)
                {
                    frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<BoxCollider>().enabled = false;
                    frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshCollider>().enabled = false;
                }

                InitializeRay();

                ResetCutter();

                raycastidx = -1;
                raycastAllidx = new List<int>();
            }

            if (overview.anyObjChanged)
                InitializeRay();

            // set raycasting
            bool isCut = false;
            int firstRayi = -1;
            raycastAllidx = new List<int>();
            RaycastHit hit = new RaycastHit();
            for (int rayi = 0; rayi < linkRay.Length; rayi++)
            {
                var rayInfo = linkRay[rayi];
                bool isHit = Physics.Raycast(rayInfo.ray, out hit, rayInfo.distance, layerMask);
                if (isHit && hit.transform.name == Cutter.name)
                {
                    isCut = true;
                    if (showDebug)
                        Debug.DrawLine(rayInfo.ray.origin, rayInfo.ray.origin + rayInfo.ray.direction * rayInfo.distance, Color.red);

                    if (firstRayi == -1)
                        firstRayi = rayi;
                    raycastAllidx.Add(rayi);
                }

                if (showDebug)
                    Debug.DrawLine(rayInfo.ray.origin, rayInfo.ray.origin + rayInfo.ray.direction * rayInfo.distance, Color.white);
            }

            if (isCut)
            {
                raycastidx = firstRayi;
                int[] linkInfo = overview.linkPosInfo[raycastidx];
                int fragi = linkInfo[0];
                int skeletoni = linkInfo[1];

                //Debug.Log("fragi = " + fragi + " face = " + fragmentRef[fragi].skeletonLink[skeletoni].Key.face
                //    + "\tfragj = " + fragmentRef[fragi].skeletonLink[skeletoni].Value.fragmentID 
                //    + " face = " + fragmentRef[fragi].skeletonLink[skeletoni].Value.face);
            }

            cutLinkStartLast = true;
        }
        else if (cutLinkStartLast)
        {
            foreach (var frag in fragmentRef)
            {
                frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<BoxCollider>().enabled = true;
                frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshCollider>().enabled = true;
            }

            raycastidx = -1;
            RemoveCutter();
            cutLinkStartLast = false;
        }
        
    }

    public void ResetCutter()
    {
        if (Cutter)
        {
            Cutter.SetActive(true);
            if (controller != null && controller.transform.position != Vector3.zero)
            {
                Cutter.transform.position = controller.transform.position;
                Cutter.transform.rotation = controller.transform.rotation;
            }

            else
            {
                Cutter.transform.position = initPos;
                Cutter.transform.eulerAngles = initRot;
            }
        }
    }

    private void RemoveCutter()
    {
        if (Cutter)
        {
            var pos = Cutter.transform.position;
            pos.y += 1000;
            Cutter.transform.position = pos;
            Cutter.SetActive(false);
        }
    }

    private void InitializeRay()
    {
        // construct ray
        linkRay = new RayInfo[overview.linkPos.Count];
        for (int i = 0; i < overview.linkPos.Count; i++)
            linkRay[i] = new RayInfo(
                new Ray(overview.linkPos[i][0], (overview.linkPos[i][1] - overview.linkPos[i][0]).normalized),
                (overview.linkPos[i][1] - overview.linkPos[i][0]).magnitude);
    }

    public void SetCutterTransform(Vector3 pos, Vector3 erot)
    {

        initPos = pos;
        initRot = erot;
    }

    public void SetCutterToHand()
    {
        Cutter.transform.position = controller.transform.position;
        Cutter.transform.rotation = controller.transform.rotation;
    }
}
