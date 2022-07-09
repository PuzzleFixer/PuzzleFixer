using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeVisualizer : MonoBehaviour
{
    [HideInInspector] public Vector3[] pointCloudPos;
    [HideInInspector] public Vector4[] pointColor;
    [SerializeField] public float pointSize = 0.2f;
    [SerializeField] string materialName = "Spray/Node/NodeMat";

    #region draw
    [Header("Draw")]
    Mesh nodeMesh = null;
    Material nodeMaterial;
    bool nodeMaterialCloned;
    MaterialPropertyBlock props;
    [SerializeField] int layer = 0;

    ComputeBuffer drawNodesBuffer;
    private ComputeBuffer nodeBuffer;
    ComputeShader nodeCompute;
    ComputeBuffer debugBuffer;

    const int kThreadCount = 128;
    int nodeCount;
    int NodeThreadGroupCount { get { return Mathf.CeilToInt((float)nodeCount / kThreadCount); } }

    public static class NodeGPU
    {
        public struct GPUStruct
        {
            public int id;
            public float size;
            public Vector3 pos;
            public Vector4 color;
        };

        public static GPUStruct[] GetNodesGPUStruct(Vector3[] pos, Vector4[] color, float size)
        {
            if (pos.Length != color.Length)
                return null;

            GPUStruct[] nodesGPU = new GPUStruct[pos.Length];
            for (int i = 0; i < nodesGPU.Length; i++)
            {
                nodesGPU[i].id = i;
                nodesGPU[i].size = size;
                nodesGPU[i].pos = pos[i];
                nodesGPU[i].color = color[i];
            }

            return nodesGPU;
        }
    }
    #endregion

    void Awake()
    {
        nodeMesh = Resources.LoadAll<Mesh>("Spray/Models/Shapes")[1];
        nodeMaterial = Resources.Load<Material>(materialName);
        nodeCompute = Resources.Load<ComputeShader>("Spray/Node/Nodes");

        ClearNodes();
    }

    void Update()
    {
        nodeMaterial.SetBuffer("nodeBuffer", nodeBuffer);
 
        Graphics.DrawMeshInstancedIndirect(nodeMesh, 0, nodeMaterial, new Bounds(Vector3.zero,
            new Vector3(50.0f, 50.0f, 50.0f)), drawNodesBuffer, 0, props, 
            UnityEngine.Rendering.ShadowCastingMode.On, false, layer);
    }

    void OnDestroy()
    {
        ReleaseAllBuffer();
    }

    private void ReleaseAllBuffer()
    {
        if (drawNodesBuffer != null) drawNodesBuffer.Release();
        if (nodeBuffer != null) nodeBuffer.Release();
        if (debugBuffer != null) debugBuffer.Release();
        if (nodeMaterialCloned)
        {
            Destroy(nodeMaterial);
            nodeMaterialCloned = false;
        }
    }


    private void NewPointsCloud(Vector3[] pointCloudPos, Vector4[] pointColor, float pointSize)
    {
        this.pointCloudPos = pointCloudPos;
        this.pointColor = pointColor;
        nodeCount = pointCloudPos.Length;

        ReleaseAllBuffer();


        drawNodesBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

        drawNodesBuffer.SetData(new uint[5] {
                nodeMesh.GetIndexCount(0), (uint)nodeCount, 0, 0, 0
            });

        nodeBuffer = new ComputeBuffer(nodeCount, sizeof(int) + sizeof(float) * (1 + 3 + 4));
        debugBuffer = new ComputeBuffer(nodeCount, sizeof(float) * 4);


        if (nodeBuffer != null)
            nodeBuffer.SetData(NodeGPU.GetNodesGPUStruct(pointCloudPos, pointColor, pointSize));

        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", UnityEngine.Random.value);

        nodeMaterial = new Material(nodeMaterial);
        nodeMaterial.name += " (cloned)";
        nodeMaterialCloned = true;
    }

    public void UpdatePointsCloud(Vector3[] pointCloudPos, Vector4[] pointColor, bool clear = false)
    {
        float drawPointSize = clear ? 0.001f : pointSize;
        if (this.pointCloudPos == null || pointCloudPos.Length != this.pointCloudPos.Length)
            NewPointsCloud(pointCloudPos, pointColor, drawPointSize);
        else
            nodeBuffer.SetData(NodeGPU.GetNodesGPUStruct(pointCloudPos, pointColor, drawPointSize));
    }

    public void ClearNodes()
    {
        UpdatePointsCloud(new Vector3[] { Vector3.zero }, new Vector4[] { Vector4.zero }, true);
    }
}
