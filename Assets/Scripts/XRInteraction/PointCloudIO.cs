using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine.XR.Interaction.Toolkit;
using XRInteraction;

public class PointCloudIO : MonoBehaviour {

	[Header("File settings")]
	[SerializeField] private string filename;
	static private string savePath;
	public bool loadFace = false;

	// GUI
	private bool meshloaded = false;
	GUIProcess guiProcess = new GUIProcess();

	// Fragment PointCloud
	public Fragment[] fragments;

	// Fragment overall settings
	[Header("Fragment overall settings")]
	public bool forceReload = false;
	public bool forceReconstructAdjGraph = false;
	public float scale = 0.2f;
	public bool invertYZ = false;
	public bool lrConvert = true;	// left/right hand coordinate
	public GameObject transformObj;
	
	void Start()
	{
		guiProcess.showGUI = !meshloaded;

		// Create Resources folder
		createFolders();

		// Get Filename
		savePath = Application.dataPath + "/Resources/PointCloudObj/" + filename;

		// Create fragments data
		if (Directory.Exists(savePath))
		{
			string[] filesFullPath = Directory.GetFiles(savePath, "*.obj");
			fragments = new Fragment[filesFullPath.Length];
			for (int fragmentIdx = 0; fragmentIdx < filesFullPath.Length; fragmentIdx++)
			{
				fragments[fragmentIdx] = new Fragment("Fragment" + fragmentIdx, savePath);
				fragments[fragmentIdx].Load();
			}
		}
		else
		{
			Debug.LogError("no fragment point cloud found!");
			return;
		}

		if (loadFace)
			RemeshFacesloader();
		else
		{
			meshloaded = true;
			guiProcess.showGUI = !meshloaded;
		}

		TransformCollect();

		// send back
		FragmentData.SetFragments(fragments);
	}

	/// put obj files under /Resources path & set Read/Write Enable to true in obj inspector
	/// empty all files before put new set of objs
	void RemeshFacesloader()
	{
		if(Directory.Exists (savePath)){
			string[] filesFullPath = Directory.GetFiles(savePath, "*.obj");
			for (int fragmentIdx = 0; fragmentIdx < filesFullPath.Length; fragmentIdx++)
			{
				// load obj file
				var source = Resources.Load("PointCloudObj/" + filename + "/" +
					Path.GetFileNameWithoutExtension(filesFullPath[fragmentIdx]));
				GameObject objMesh = GameObject.Instantiate(source) as GameObject;

				if (objMesh != null)
				{
					GameObject childObj = objMesh.transform.GetChild(0).gameObject;
					childObj.name = FragmentTypes.Default;
					MeshRenderer objRender = childObj.GetComponent<MeshRenderer>();
					objRender.material = Resources.Load<Material>("MeshObj/MeshMat");
					

					objMesh.transform.parent = fragments[fragmentIdx].parent.transform;
					fragments[fragmentIdx].parent.AddComponent<ParentSelect>();

                    childObj.transform.localScale *= scale;

					var bc = childObj.AddComponent<BoxCollider>();
					bc.isTrigger = true;
					childObj.AddComponent<MeshCollider>();
					var rb = childObj.AddComponent<Rigidbody>();
					rb.isKinematic = true;
					rb.useGravity = false;

					var gi = childObj.AddComponent<XRGrabInteractable>();
					gi.interactionLayers = (1 << LayerMask.NameToLayer("RightHand")) |
						(1 << LayerMask.NameToLayer("LeftHand"));
					gi.enabled = false;
					var si = childObj.AddComponent<XRSimpleInteractable>();
					si.interactionLayers = (1 << LayerMask.NameToLayer("RightHand")) |
						(1 << LayerMask.NameToLayer("LeftHand"));

					var select = childObj.AddComponent<XRSelect>();
					select.enabled = true;

					fragments[fragmentIdx].objMesh = objMesh;

                    if (fragments[fragmentIdx].VertexGraph == null || forceReconstructAdjGraph)
						StartCoroutine(ConstructAdjacentGraph(objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh,
							fragmentIdx, savePath, Path.GetFileNameWithoutExtension(filesFullPath[fragmentIdx])));
					else
                    {
						meshloaded = true;
						guiProcess.showGUI = !meshloaded;
					}
				}
                else
                {
					Debug.LogWarning("obj file is null!");
                }
			}
		}
		else
			Debug.LogError("Obj directory '" + savePath + "' could not be found"); 
	}

	IEnumerator ConstructAdjacentGraph(Mesh mesh, int fragmentIdx, string path, string filename)
	{
		List<int>[] VertexGraph = new List<int>[mesh.vertexCount];
		for (int i = 0; i < mesh.vertexCount; i++)
			VertexGraph[i] = new List<int>();
		var triangles = mesh.triangles;
		float lastTime = Time.time;
		int lasti = 0;
		for (int ti = 0; ti < triangles.Length; ti += 3)
		{
			int v1 = triangles[ti];
			int v2 = triangles[ti + 1];
			int v3 = triangles[ti + 2];

			VertexGraph[v1].Add(v2);
			VertexGraph[v1].Add(v3);
			VertexGraph[v2].Add(v1);
			VertexGraph[v2].Add(v3);
			VertexGraph[v3].Add(v1);
			VertexGraph[v3].Add(v2);

            // GUI
            if (ti % 2000 == 0)
            {
				float nowTime = Time.time;

				guiProcess.progress = ti * 1.0f / (triangles.Length - 1) * 1.0f;
				guiProcess.guiText = "constructing mesh adj graph" + ti.ToString() + "/" +
						triangles.Length.ToString() + 
						"estimate time left: " + (triangles.Length - 1.0f - ti) / ((ti -lasti) / (nowTime - lastTime)) / 60.0f + "min";

				lastTime = nowTime;
				lasti = ti;

				yield return null;
			}
		}

		// remove duplicate
		fragments[fragmentIdx].VertexGraph = new int[mesh.vertexCount][];
		for (int i = 0; i < mesh.vertexCount; i++)
			fragments[fragmentIdx].VertexGraph[i] = VertexGraph[i].Distinct().ToArray();

		// save adjgraph
		fragments[fragmentIdx].Save();

		meshloaded = true;
		guiProcess.showGUI = !meshloaded;
	}

	void createFolders()
	{
		string path = Application.dataPath + "/Resources/";
		if (!Directory.Exists (path))
			Directory.CreateDirectory(path);

		if (!Directory.Exists (Application.dataPath + "/Resources/PointCloudMeshes/"))
			Directory.CreateDirectory(Application.dataPath + "/Resources/PointCloudMeshes/");
	}

	void TransformCollect()
	{
		for (int i = 0; i < fragments.Length; i++)
			fragments[i].parent.transform.parent = transformObj.transform;
	}

    private void OnGUI()
    {
		guiProcess.DrawGUIBar();
	}
}
