using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ProtoBuf;
using System.Linq;

// fragment data class
[Serializable]
public class Fragment
{
	public GameObject parent;       // ID -> parent.name
	public GameObject pointCloud;
	public GameObject objMesh;      

    #region save variable

    #region only precompute

    public int numPoints;
	public int numPointGroups;
	public Vector3[] points;
	public Vector3[] normals;
	public Color[] colors;

    /// points & faces
    public int[][] VertexGraph;         
	public int[][] faceSetGraph;
	public float[][] pointRoughness;

	public int[][][] boundaryIdxSet;        
	public Vector3[][][] boundaryPoints;    

	public float[] meanCurvature;
	public float[] gaussCurvature;
	public float[] k1;
	public float[] k2;

	// matching info
	public List<CurveMatch>[][] SimilarCurve;   
												
												
	#endregion

	// segmentation
	public int[] faceIdx;               
	public int[][] faceIdxSet;			
	public Vector3[] faceNormal;
	public Vector3[] faceCenter;

	// match
	public bool[] faceIsRough;

	public Vector3 position;    
	public Quaternion rotation; 

	// match skeleton
	public List<KeyValuePair<CurveIndex, CurveIndex>> skeletonLink; 
																	
																	


	public Vector3[] relativePosition;
	public Quaternion[] relativeRotation;
	#endregion

	public string savePath = null;

	public Fragment(string name, string savePath)
	{
		parent = new GameObject(name);
		this.savePath = savePath;
	}

	public string GetIDName()
    {
		return parent.name;
    }

	public Vector3 GetCenterPos()
    {
		Transform defTrans = objMesh.transform.Find(FragmentTypes.Default);
		
		return defTrans.GetComponent<Renderer>().bounds.center;
	}


	public void ConvertVNLocal2World(Vector3[] verticesLocal, Vector3[] normalsLocal,
		out Vector3[] verticesWorld, out Vector3[] normalsWorld)
	{
		MeshFilter mf = objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>();
		int vCount = verticesLocal.Length;
		verticesWorld = new Vector3[vCount];
		normalsWorld = new Vector3[vCount];
		for (int i = 0; i < vCount; i++)
		{
			verticesWorld[i] = mf.transform.TransformPoint(verticesLocal[i]);
			normalsWorld[i] = mf.transform.TransformDirection(normalsLocal[i]);
		}
	}

	/// <summary>
	/// get face points & normals in local position
	/// </summary>
	/// <param name="faceID"></param>
	/// <param name="fp"></param>
	/// <param name="fn"></param>
	public void GetFaceVN(int faceID, out Vector3[] fp, out Vector3[] fn)
	{
		Vector3[] v = objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh.vertices;
		Vector3[] n = objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh.normals;
		fp = new Vector3[faceIdxSet[faceID].Length];
		fn = new Vector3[fp.Length];
		for (int j = 0; j < fp.Length; j++)
		{
			int pi = faceIdxSet[faceID][j];
			fp[j] = v[pi];
			fn[j] = n[pi];
		}
	}

	/// <summary>
	/// get face points & normals in world position
	/// </summary>
	/// <param name="faceID"></param>
	/// <param name="verticesWorld"></param>
	/// <param name="normalsWorld"></param>
	public void GetFaceWorldVN(int faceID, out Vector3[] verticesWorld, out Vector3[] normalsWorld)
    {
		GetFaceVN(faceID, out Vector3[] fp, out Vector3[] fn);
		ConvertVNLocal2World(fp, fn, out verticesWorld, out normalsWorld);
	}

	/////////////////////// load & save ///////////////////////

	public void Save()
	{
		if (savePath == null)
		{
			Debug.LogError("can't save fragment: savePath is null!");
			return;
		}

		try
		{
			FragmentSave fs = new FragmentSave(this);
			using (FileStream file = File.Create(savePath + "/" + parent.name + ".temp"))
			{
				Serializer.Serialize(file, fs);
			}
		}
		catch (Exception e)
		{
			Debug.LogError("save error: " + e);
			throw;
		}

		File.Copy(savePath + "/" + parent.name + ".temp", savePath + "/" + parent.name + ".proto", true);
		File.Delete(savePath + "/" + parent.name + ".temp");
	}

	public void Load()
	{
		if (savePath == null)
		{
			Debug.LogError("can't save fragment: savePath is null!");
			return;
		}

		FragmentSave fs = null;
		try
		{
			using (FileStream file = File.OpenRead(savePath + "/" + parent.name + ".proto"))
			{
				fs = Serializer.Deserialize<FragmentSave>(file);
			}
		}
		catch (System.Exception e)
		{
			Debug.Log(e);
			return;
		}

		this.numPoints = fs.numPoints;
		this.numPointGroups = fs.numPointGroups;
		this.points = FragmentSave.FloatVector3(fs.points);
		this.normals = FragmentSave.FloatVector3(fs.normals);
		this.colors = FragmentSave.FloatColor(fs.colors);
		this.VertexGraph = FragmentSave.Array2Zig(fs.VertexGraph, fs.VertexGraphCount);
		this.faceIdx = fs.faceIdx;
		this.faceIdxSet = FragmentSave.Array2Zig(fs.faceIdxSet, fs.faceIdxSetCount);
		this.faceNormal = FragmentSave.FloatVector3(fs.faceNormal);
		this.faceSetGraph = FragmentSave.Array2Zig(fs.faceSetGraph, fs.faceSetGraphCount);
		if (this.faceSetGraph == null)
			this.faceSetGraph = new int[0][];
		this.pointRoughness = FragmentSave.Array2Zig(fs.pointRoughness, fs.pointRoughnessCount);
		this.faceIsRough = fs.faceIsRough;
		this.faceCenter = FragmentSave.FloatVector3(fs.faceCenter);
		this.boundaryIdxSet = FragmentSave.Array3Zig(fs.boundaryIdxSet, fs.boundaryIdxSetCount);
		this.boundaryPoints = FragmentSave.Array4Zig(fs.boundaryPoints, fs.boundaryPointsCount);
		this.meanCurvature = fs.meanCurvature;
		this.gaussCurvature = fs.gaussCurvature;
		this.k1 = fs.k1;
		this.k2 = fs.k2;
		this.SimilarCurve = FragmentSave.Array3Zig(fs.SimilarCurve, fs.SimilarCurveCount);
		if (this.SimilarCurve == null)
			this.SimilarCurve = new List<CurveMatch>[0][];
		this.position = FragmentSave.FloatVector3Single(fs.position);
		this.rotation = FragmentSave.FloatQuaternionSingle(fs.rotation);
		this.skeletonLink = fs.skeletonLink;
		if (this.skeletonLink == null)
			this.skeletonLink = new List<KeyValuePair<CurveIndex, CurveIndex>>();
		this.relativePosition = FragmentSave.FloatVector3(fs.relativePosition);
		this.relativeRotation = FragmentSave.FloatQuaternion(fs.relativeRotation);
	}
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////// save & other classes /////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[Serializable, ProtoContract]
class FragmentSave
{
	[ProtoMember(1)]
	public int numPoints;
	[ProtoMember(2)]
	public int numPointGroups;

	[ProtoMember(3)]
	public float[] points;
	[ProtoMember(4)]
	public float[] normals;
	[ProtoMember(5)]
	public float[] colors;

	[ProtoMember(6)]
	public int[] VertexGraph;
	[ProtoMember(7)]
	public int[] VertexGraphCount;
	[ProtoMember(8)]
	public int[] faceIdx;
	[ProtoMember(9)]
	public int[] faceIdxSet;
	[ProtoMember(10)]
	public int[] faceIdxSetCount;
	[ProtoMember(11)]
	public float[] faceNormal;
	[ProtoMember(12)]
	public int[] faceSetGraph;
	[ProtoMember(13)]
	public int[] faceSetGraphCount;
	[ProtoMember(14)]
	public float[] pointRoughness;
	[ProtoMember(15)]
	public int[] pointRoughnessCount;
	[ProtoMember(16)]
	public bool[] faceIsRough;
	[ProtoMember(17)]
	public float[] faceCenter;
	[ProtoMember(18)]
	public int[] boundaryIdxSet;
	[ProtoMember(19)]
	public int[] boundaryIdxSetCount;
	[ProtoMember(20)]
	public float[] boundaryPoints;
	[ProtoMember(21)]
	public int[] boundaryPointsCount;
	[ProtoMember(22)]
	public float[] meanCurvature;
	[ProtoMember(23)]
	public float[] gaussCurvature;
	[ProtoMember(24)]
	public float[] k1;
	[ProtoMember(25)]
	public float[] k2;
	[ProtoMember(26)]
	public CurveMatch[] SimilarCurve;
	[ProtoMember(27)]
	public int[] SimilarCurveCount;
	[ProtoMember(28)]
	public float[] position;
	[ProtoMember(29)]
	public float[] rotation;
	[ProtoMember(30)]
	public List<KeyValuePair<CurveIndex, CurveIndex>> skeletonLink;
	[ProtoMember(31)]
	public float[] relativePosition;
	[ProtoMember(32)]
	public float[] relativeRotation;

	public FragmentSave()
    {
		numPoints = 0;
		numPointGroups = 0;

		points = null;
		normals = null;
		colors = null;

		VertexGraph = null;
		VertexGraphCount = null;
		faceIdx = null;
		faceIdxSet = null;
		faceIdxSetCount = null;
		faceNormal = null;
		faceSetGraph = null;
		faceSetGraphCount = null;
		pointRoughness = null;
		pointRoughnessCount = null;
		faceIsRough = null;
		faceCenter = null;
		boundaryIdxSet = null;
		boundaryIdxSetCount = null;
		boundaryPoints = null;
		boundaryPointsCount = null;
		meanCurvature = null;
		gaussCurvature = null;
		k1 = null;
		k2 = null;
		SimilarCurve = null;
		SimilarCurveCount = null;
		position = null;
		rotation = null;
		skeletonLink = null;
		relativePosition = null;
		relativeRotation = null;
	}

	public FragmentSave(Fragment f)
    {
		this.numPoints = f.numPoints;
		this.numPointGroups = f.numPointGroups;
		
		this.points = Vector3Float(f.points);
		this.normals = Vector3Float(f.normals);
		this.colors = ColorFloat(f.colors);
		
		this.VertexGraph = Zig2Array(f.VertexGraph, out this.VertexGraphCount);

		this.faceIdx = f.faceIdx;
		this.faceIdxSet = Zig2Array(f.faceIdxSet, out this.faceIdxSetCount);
		this.faceNormal = Vector3Float(f.faceNormal);
		this.faceSetGraph = Zig2Array(f.faceSetGraph, out this.faceSetGraphCount);
		this.pointRoughness = Zig2Array(f.pointRoughness, out this.pointRoughnessCount);
		this.faceIsRough = f.faceIsRough;
		this.faceCenter = Vector3Float(f.faceCenter);
		this.boundaryIdxSet = Zig3Array(f.boundaryIdxSet, out this.boundaryIdxSetCount);
		this.boundaryPoints = Zig4Array(f.boundaryPoints, out this.boundaryPointsCount);
		this.meanCurvature = f.meanCurvature;
		this.gaussCurvature = f.gaussCurvature;
		this.k1 = f.k1;
		this.k2 = f.k2;
		this.SimilarCurve = Zig3Array(f.SimilarCurve, out this.SimilarCurveCount);
		this.position = Vector3FloatSingle(f.position);
		this.rotation = QuaternionFloatSingle(f.rotation);
		this.skeletonLink = f.skeletonLink;
		this.relativePosition = Vector3Float(f.relativePosition);
		this.relativeRotation = QuaternionFloat(f.relativeRotation);
	}

	private static float[] Vector3Float(Vector3[] v)
	{
		if (v == null)
			return null;

		float[] f = new float[v.Length * 3];
		int fi = 0;
		for (int vi = 0; vi < v.Length; vi++)
		{
			f[fi] = v[vi].x;
			f[fi + 1] = v[vi].y;
			f[fi + 2] = v[vi].z;
			fi += 3;
		}

		return f;
	}

	public static Vector3[] FloatVector3(float[] f)
	{
		if (f == null)
			return null;

		Vector3[] v = new Vector3[f.Length / 3];
		int fi = 0;
		for (int vi = 0; vi < v.Length; vi++)
		{
			v[vi] = new Vector3(f[fi], f[fi + 1], f[fi + 2]);
			fi += 3;
		}

		return v;
	}

	private static float[] ColorFloat(Color[] c)
	{
		if (c == null)
			return null;

		float[] f = new float[c.Length * 4];
		int fi = 0;
		for (int ci = 0; ci < c.Length; ci++)
		{
			f[fi] = c[ci].r;
			f[fi + 1] = c[ci].g;
			f[fi + 2] = c[ci].b;
			f[fi + 3] = c[ci].a;
			fi += 4;
		}

		return f;
	}

	public static Color[] FloatColor(float[] f)
	{
		if (f == null)
			return null;

		Color[] c = new Color[f.Length / 4];
		int fi = 0;
        for (int ci = 0; ci < c.Length; ci++)
        {
			c[ci] = new Color(f[fi], f[fi + 1], f[fi + 2], f[fi + 3]);
			fi += 4;
        }

		return c;
	}

	private static int[] Zig2Array(int[][] zig, out int[] count)
    {
		if (zig == null)
        {
			count = null;
			return null;
		}

		count = new int[zig.Length];
		List<int> array = new List<int>();
        for (int i = 0; i < zig.Length; i++)
        {
			count[i] = zig[i].Length;
			array.AddRange(zig[i]);
		}

		return array.ToArray();
    }

	public static int[][] Array2Zig(int[] array, int[] count)
    {
		if (array == null)
			return null;

		int[][] zig = new int[count.Length][];
		int ai = 0;
        for (int i = 0; i < zig.Length; i++)
        {
			zig[i] = new int[count[i]];
            for (int j = 0; j < count[i]; j++)
            {
				zig[i][j] = array[ai];
				ai += 1;
			}
		}

		return zig;
    }

	private static float[] Zig2Array(float[][] zig, out int[] count)
	{
		if (zig == null)
		{
			count = null;
			return null;
		}

		count = new int[zig.Length];
		List<float> array = new List<float>();
		for (int i = 0; i < zig.Length; i++)
		{
			count[i] = zig[i].Length;
			array.AddRange(zig[i]);
		}

		return array.ToArray();
	}

	public static float[][] Array2Zig(float[] array, int[] count)
	{
		if (array == null)
			return null;

		float[][] zig = new float[count.Length][];
		int ai = 0;
		for (int i = 0; i < zig.Length; i++)
		{
			zig[i] = new float[count[i]];
			for (int j = 0; j < count[i]; j++)
			{
				zig[i][j] = array[ai];
				ai += 1;
			}
		}

		return zig;
	}

	private static int[] Zig3Array(int[][][] zig, out int[] count)
	{
		if (zig == null)
		{
			count = null;
			return null;
		}

		List<int> countList = new List<int>();
		List<int> array = new List<int>();
		countList.Add(zig.Length);
		for (int i = 0; i < zig.Length; i++)
		{
			countList.Add(zig[i].Length);
            for (int j = 0; j < zig[i].Length; j++)
            {
				countList.Add(zig[i][j].Length);
				array.AddRange(zig[i][j]);
			}
		}

		count = countList.ToArray();
		return array.ToArray();
	}

	public static int[][][] Array3Zig(int[] array, int[] count)
	{
		if (array == null)
			return null;

		int[][][] zig = new int[count[0]][][];
		int ai = 0;
		int ci = 1;
		for (int i = 0; i < zig.Length; i++)
		{
			int c1 = count[ci];
			ci += 1;
			zig[i] = new int[c1][];
			for (int j = 0; j < c1; j++)
			{
				int c2 = count[ci];
				ci += 1;
				zig[i][j] = new int[c2];
                for (int k = 0; k < c2; k++)
                {
					zig[i][j][k] = array[ai];
					ai += 1;
				}
			}
		}

		return zig;
	}

	private static float[] Zig4Array(Vector3[][][] zig, out int[] count)
	{
		if (zig == null)
		{
			count = null;
			return null;
		}

		List<int> countList = new List<int>();
		List<float> array = new List<float>();
		int c0 = zig.Length;
		countList.Add(c0);
		for (int i = 0; i < c0; i++)
		{
			int c1 = zig[i].Length;
			countList.Add(c1);
			for (int j = 0; j < c1; j++)
			{
				int c2 = zig[i][j].Length;
				countList.Add(c2);
                for (int k = 0; k < c2; k++)
                {
					countList.Add(3);
					array.Add(zig[i][j][k].x);
					array.Add(zig[i][j][k].y);
					array.Add(zig[i][j][k].z);
				}
			}
		}

		count = countList.ToArray();
		return array.ToArray();
	}

	public static Vector3[][][] Array4Zig(float[] array, int[] count)
	{
		if (array == null)
			return null;

		Vector3[][][] zig = new Vector3[count[0]][][];
		int ai = 0;
		int ci = 1;
		for (int i = 0; i < zig.Length; i++)
		{
			int c1 = count[ci];
			ci += 1;
			zig[i] = new Vector3[c1][];
			for (int j = 0; j < c1; j++)
			{
				int c2 = count[ci];
				ci += 1;
				zig[i][j] = new Vector3[c2];
				for (int k = 0; k < c2; k++)
				{
					int c3 = count[ci];	// c3 = 3
					ci += 1;
					zig[i][j][k] = new Vector3(array[ai], array[ai + 1], array[ai + 2]);
					ai += 3;
				}
			}
		}

		return zig;
	}

	private static CurveMatch[] Zig3Array(List<CurveMatch>[][] zig, out int[] count)
	{
		if (zig == null)
		{
			count = null;
			return null;
		}

		List<int> countList = new List<int>();
		List<CurveMatch> array = new List<CurveMatch>();
		countList.Add(zig.Length);
		for (int i = 0; i < zig.Length; i++)
		{
			countList.Add(zig[i].Length);
			for (int j = 0; j < zig[i].Length; j++)
			{
				countList.Add(zig[i][j].Count);
				array.AddRange(zig[i][j]);
			}
		}

		count = countList.ToArray();
		return array.ToArray();
	}

	public static List<CurveMatch>[][] Array3Zig(CurveMatch[] array, int[] count)
	{
		if (array == null)
			return null;

		List<CurveMatch>[][] zig = new List<CurveMatch>[count[0]][];
		int ai = 0;
		int ci = 1;
		for (int i = 0; i < zig.Length; i++)
		{
			int c1 = count[ci];
			ci += 1;
			zig[i] = new List<CurveMatch>[c1];
			for (int j = 0; j < c1; j++)
			{
				int c2 = count[ci];
				ci += 1;
				zig[i][j] = new List<CurveMatch>();
				for (int k = 0; k < c2; k++)
				{
					zig[i][j].Add(array[ai]);
					ai += 1;
				}
			}
		}

		return zig;
	}

	private static float[] Vector3FloatSingle(Vector3 v)
	{
		return new float[] { v.x, v.y, v.z };
	}

	public static Vector3 FloatVector3Single(float[] f)
    {
		return new Vector3(f[0], f[1], f[2]);
    }

	private static float[] QuaternionFloatSingle(Quaternion q)
	{
		return new float[] { q.x, q.y, q.z, q.w };
	}

	public static Quaternion FloatQuaternionSingle(float[] f)
	{
		if (f == null)
			return Quaternion.identity;

		return new Quaternion(f[0], f[1], f[2], f[3]);
	}

	private static float[] QuaternionFloat(Quaternion[] q)
	{
		if (q == null)
			return null;

		float[] f = new float[q.Length * 4];
		int fi = 0;
		for (int qi = 0; qi < q.Length; qi++)
		{
			f[fi] = q[qi].x;
			f[fi + 1] = q[qi].y;
			f[fi + 2] = q[qi].z;
			f[fi + 3] = q[qi].w;
			fi += 4;
		}

		return f;
	}

	public static Quaternion[] FloatQuaternion(float[] f)
	{
		if (f == null)
			return null;

		Quaternion[] q = new Quaternion[f.Length / 4];
		int fi = 0;
		for (int qi = 0; qi < q.Length; qi++)
        {
			q[qi] = new Quaternion(f[fi], f[fi + 1], f[fi + 2], f[fi + 3]);
			fi += 4;
		}

		return q;
	}
}

[Serializable, ProtoContract]
[ProtoInclude(1, typeof(CurveMatch))]
public class CurveIndex
{
	[ProtoMember(2)]
	public string fragmentID;
	[ProtoMember(3)]
	public int face;
	[ProtoMember(4)]
	public int curve;

	public CurveIndex(string fragmentID, int face, int curve)
	{
		this.fragmentID = fragmentID;
		this.face = face;
		this.curve = curve;
	}

	// 为兼容protoBuf
	public CurveIndex()
    {
		this.fragmentID = "";
		this.face = 0;
		this.curve = 0;
	}

	public CurveIndex(bool clear)
    {
        if (clear)
			Clear();
        else
        {
			this.fragmentID = "";
			this.face = 0;
			this.curve = 0;
		}
	}

	public void Clear()
    {
		this.fragmentID = FragmentTypes.Unknown;
		this.face = -1;
		this.curve = -1;
	}

	public bool IsEmpty()
    {
		if (this.face == -1)
			return true;
		else
			return false;
    }

	public static bool operator ==(CurveIndex a, CurveIndex b)
    {
		if (a is null && b is null)
			return true;
		else if (a is null || b is null)
			return false;

		if (a.fragmentID == b.fragmentID && a.face == b.face && a.curve == b.curve)
			return true;
		else
			return false;
    }

	public static bool operator !=(CurveIndex a, CurveIndex b) => !(a == b);

	public override int GetHashCode() => (fragmentID, face, curve).GetHashCode();

	public override bool Equals(object obj)
    {
		return this == (CurveIndex)obj;
    }
}

[Serializable, ProtoContract]
public class CurveMatch : CurveIndex
{
	[ProtoMember(5)]
	public float matchScore;

	public CurveMatch(): base()
    {
		matchScore = 0;
	}

	public CurveMatch(string fragmentID, int face, int curve, float matchScore): base(fragmentID, face, curve)
    {
		this.matchScore = matchScore;
	}

	public CurveIndex GetCurve()
    {
		return new CurveIndex(this.fragmentID, this.face, this.curve);
    }
}

[Serializable, ProtoContract]
public class GroupMatch
{
	[ProtoMember(1)]
	public List<KeyValuePair<CurveIndex, CurveIndex>> targetLinks;
	[ProtoMember(2)]
	public List<KeyValuePair<CurveIndex, CurveIndex>> otherLinks;
	// potential match score
	[ProtoMember(3)]
	public float matchScore;    // avg match score of each face pair

	public GroupMatch()
    {
		targetLinks = null;
		otherLinks = null;
		matchScore = 0;
	}

	public GroupMatch(List<KeyValuePair<CurveIndex, CurveIndex>> targetLinks, 
		List<KeyValuePair<CurveIndex, CurveIndex>> otherLinks,
		float matchScore)
    {
		this.targetLinks = targetLinks;
		this.otherLinks = otherLinks;
		this.matchScore = matchScore;
    }
}