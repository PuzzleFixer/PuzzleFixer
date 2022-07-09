using System;
using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
 
public class ObjExporter {
 
    public static string MeshToString(MeshFilter mf) {
        Mesh m = mf.mesh;
        Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
 
        StringBuilder sb = new StringBuilder();
 
        sb.Append("g ").Append(mf.name).Append("\n");
        foreach(Vector3 v in m.vertices) {
            sb.Append(string.Format("v {0} {1} {2}\n",v.x,v.y,v.z));
        }
        sb.Append("\n");
        foreach(Vector3 v in m.normals) {
            sb.Append(string.Format("vn {0} {1} {2}\n",v.x,v.y,v.z));
        }
        sb.Append("\n");
        foreach(Vector3 v in m.uv) {
            sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));
        }
        for (int material=0; material < m.subMeshCount; material ++) {
            sb.Append("\n");
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");
 
            int[] triangles = m.GetTriangles(material);
            for (int i=0;i<triangles.Length;i+=3) {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
                    triangles[i]+1, triangles[i+1]+1, triangles[i+2]+1));
            }
        }
        return sb.ToString();
    }
    
    public static string MeshGlobalToString(MeshFilter mf, 
	    bool normal = true, bool uv = true, bool triangle = true) 
    {
	    Mesh m = mf.mesh;
	    Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
 
	    StringBuilder sb = new StringBuilder();
 
	    sb.Append("g ").Append(mf.name).Append("\n");
	    foreach(Vector3 lv in m.vertices) {
		    Vector3 wv = mf.transform.TransformPoint(lv);
		    sb.Append(string.Format("v {0} {1} {2}\n",wv.x,wv.y,wv.z));
	    }
	    sb.Append("\n");
	    if (normal)
	    {
		    foreach(Vector3 ln in m.normals) {
                Vector3 wn = mf.transform.TransformDirection(ln);
                sb.Append(string.Format("vn {0} {1} {2}\n",wn.x,wn.y,wn.z));
            }
            sb.Append("\n");
	    }

	    if (uv)
		    foreach(Vector3 v in m.uv) 
			    sb.Append(string.Format("vt {0} {1}\n",v.x,v.y));

		for (int material=0; material < m.subMeshCount; material ++) 
		{
		    if (mats[material] != null)
		    {
			    sb.Append("\n");
	            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
	            sb.Append("usemap ").Append(mats[material].name).Append("\n");
		    }

		    if (triangle)
		    {
			    int[] triangles = m.GetTriangles(material);
                for (int i=0;i<triangles.Length;i+=3) {
                    sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
                	    triangles[i]+1, triangles[i+1]+1, triangles[i+2]+1));
                }
		    }
	    }
	    return sb.ToString();
    }
    
    public static string PointCloudGlobalToString(MeshFilter mf) 
    {
	    Mesh m = mf.mesh;
	    Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
 
	    StringBuilder sb = new StringBuilder();

	    sb.Append(m.vertexCount).Append("\n");
	    for (int i = 0; i < m.vertexCount; i++)
	    {
		    Vector3 wv = mf.transform.TransformPoint(m.vertices[i]); 
		    sb.Append(string.Format("{0} {1} {2}",wv.x,wv.y,wv.z));
		    Vector3 wn = mf.transform.TransformDirection(m.normals[i]);
		    sb.Append(string.Format(" {0} {1} {2}\n",wn.x,wn.y,wn.z));
	    }
	    return sb.ToString();
    }
 
    public static void MeshToObj(MeshFilter mf, string filename, 
	    bool normal = true, bool uv = true, bool triangle = true) 
    {
        using (StreamWriter sw = new StreamWriter(filename)) 
        {
            //sw.Write(MeshToString(mf));
            sw.Write(MeshGlobalToString(mf, normal, uv, triangle));
        }
    }
    
    public static void MeshToXYZ(MeshFilter mf, string filename) 
    {
	    using (StreamWriter sw = new StreamWriter(filename)) 
	    {
		    sw.Write(PointCloudGlobalToString(mf));
	    }
    }
}