using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MeshColor : MonoBehaviour
{    
    [Header("start button")]
    [SerializeField] bool showMeshColor = false;

    static private float attenuate = 1.4f / 4.0f;
    static private Vector4 colorAttenuate = new Vector4(attenuate, attenuate, attenuate, 1.0f);
    static public Color resetColor = new Color(255 / 255f, 244 / 255f, 214 / 255f);
    static public Color matchWorst = Color.blue * colorAttenuate;
    static public Color matchBest = new Color(0, 1, 0.5f) * colorAttenuate;

    static public List<KeyValuePair<Vector2Int, Color>> faceColor; 

    void Update()
    {
        if (showMeshColor)
        {
            DrawMeshColor(FragmentData.fragments);

            showMeshColor = false;
        }
    }


    static public void DrawMeshColor(Fragment[] fragmentRef)
    {
        List<float> matchScore = new List<float>();
        foreach (var fragi in fragmentRef)
        {
            foreach (var link in fragi.skeletonLink)
            {
                int facei = link.Key.face;
                int curvei = link.Key.curve;
                var fragj = FragmentData.GetFragmentByName(link.Value.fragmentID);
                int facej = link.Value.face;
                int curvej = link.Value.curve;

                matchScore.Add(FragmentData.GetPairCurveMatchScore(fragi, facei, curvei, fragj, facej, curvej));
            }
        }

        Mesh[] mesh = new Mesh[fragmentRef.Length];
        Color[][] meshColor = new Color[fragmentRef.Length][];
        for (int i = 0; i < fragmentRef.Length; i++)
        {
            mesh[i] = fragmentRef[i].objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh;
            meshColor[i] = new Color[mesh[i].vertexCount];
            for (int j = 0; j < meshColor[i].Length; j++)
                meshColor[i][j] = resetColor;
        }

        int matchScorei = 0;

        float maxScore = matchScore.Max();
        float minScore = matchScore.Where(x => x != 0).Min();
        faceColor = new List<KeyValuePair<Vector2Int, Color>>();

        for (int fragidx = 0; fragidx < fragmentRef.Length; fragidx++)
        {
            var fragi = fragmentRef[fragidx];
            foreach (var link in fragi.skeletonLink)
            {
                int facei = link.Key.face;
                int curvei = link.Key.curve;
                var fragjidx = FragmentData.GetFragmentIndexByName(link.Value.fragmentID);
                var fragj = FragmentData.GetFragmentByName(link.Value.fragmentID);
                int facej = link.Value.face;
                int curvej = link.Value.curve;
                Color lerpColor = Color.Lerp(matchWorst, matchBest, 
                    (matchScore[matchScorei] - minScore) / (maxScore - minScore));
                Color lerpColorFace = lerpColor / attenuate;
                lerpColorFace.a = 1;

                int[] piidx = fragi.faceIdxSet[facei];
                foreach (var pi in piidx)
                    meshColor[fragidx][pi] = lerpColorFace;

                faceColor.Add(new KeyValuePair<Vector2Int, Color>(new Vector2Int(fragidx, facei), lerpColor));

                if (fragj != null)
                {
                    piidx = fragj.faceIdxSet[facej];
                    foreach (var pi in piidx)
                        meshColor[fragjidx][pi] = lerpColorFace;
                    faceColor.Add(new KeyValuePair<Vector2Int, Color>(new Vector2Int(fragjidx, facej), lerpColor));
                }

                matchScorei += 1;
            }
        }

        for (int i = 0; i < mesh.Length; i++)
            mesh[i].colors = meshColor[i];
    }

    static public void OnlyOpenLinkFaceLeft(GroupMatch groupMatch, 
        Transform[] fragShapes, List<string> shapeName, List<Vector4> nodeColor)
    {
        Mesh[] mesh = new Mesh[fragShapes.Length];
        Color[][] meshColor = new Color[fragShapes.Length][];
        for (int i = 0; i < fragShapes.Length; i++)
        {
            mesh[i] = fragShapes[i].GetChild(0).GetChild(0).GetComponent<MeshFilter>().mesh;
            meshColor[i] = mesh[i].colors;
        }

        List<CurveIndex> openLinksInfo = groupMatch.targetLinks
                .Select(t => t.Key)
                .ToList();
        openLinksInfo.AddRange(groupMatch.otherLinks
            .Select(t => t.Key)
            .ToList());

        var openLinkInfoSet = new HashSet<KeyValuePair<string, int>>();
        foreach (var openlink in openLinksInfo)
            openLinkInfoSet.Add(new KeyValuePair<string, int>(openlink.fragmentID, openlink.face));

        for (int i = 0; i < shapeName.Count; i++)
        {
            var name = shapeName[i];
            var frag = FragmentData.GetFragmentByName(name);
            foreach (var link in frag.skeletonLink)
            {
                int faceID = link.Key.face;
                int[] pids = frag.faceIdxSet[faceID];
                if (openLinkInfoSet.Contains(new KeyValuePair<string, int>(name, faceID)) == false)
                    foreach (var pi in pids)
                        meshColor[i][pi] = resetColor;
                else
                {
                    Color faceColor = nodeColor[i];
                    faceColor.r /= colorAttenuate[0];
                    faceColor.g /= colorAttenuate[1];
                    faceColor.b /= colorAttenuate[2];
                    foreach (var pi in pids)
                        meshColor[i][pi] = faceColor;
                }
                    
            }
        }

        for (int i = 0; i < fragShapes.Length; i++)
            mesh[i].colors = meshColor[i];
    }


    static public void DrawFaceColor(Fragment frag, int faceidx, Color c)
    {
        Color faceColor = c / attenuate;
        faceColor.a = 1;
        Mesh mesh = frag.objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh;
        Color[] meshColor = mesh.colors;
        int[] piidx = frag.faceIdxSet[faceidx];
        foreach (var pi in piidx)
            meshColor[pi] = faceColor;
        mesh.colors = meshColor;
    }

    static public void ResetFaceColor(Fragment[] fragmentRef)
    {
        foreach (var fragi in fragmentRef)
        {
            Mesh meshi = fragi.objMesh.transform.Find(FragmentTypes.Default).GetComponent<MeshFilter>().mesh;
            Color[] meshColori = new Color[meshi.vertexCount];
            for (int i = 0; i < meshColori.Length; i++)
                meshColori[i] = resetColor;
            meshi.colors = meshColori;
        }
    }
}
