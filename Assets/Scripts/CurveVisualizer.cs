using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Curves;
using System;
using System.Linq;

public class CurveVisualizer : MonoBehaviour
{
    #region materials & properties
    Material edgeMaterial;
    bool materialCloned;
    static MaterialPropertyBlock props;

    TubeTemplate tubeTemplate;
    private int segmentCount    // how many segments do a complete output curve (between two end points) has
                                // can be changed in the tubeTemplate inspector
    {
        get { return tubeTemplate.segments + 1; }
        set { tubeTemplate.segments = value; }
    }
    [SerializeField] private float radius = 0.05f;
    #endregion

    #region buffers
    // draw
    ComputeBuffer drawEdgesBuffer = null;
    ComputeBuffer tangentBuffer = null;
    ComputeBuffer normalBuffer = null;
    ComputeBuffer colorBuffer = null;

    // curve
    ComputeBuffer curveBuffer = null;           // nodes position of each curve (input points)
    ComputeBuffer curvePointsBuffer = null;     // nodes position of each curve (output points)
    ComputeBuffer curvePointsNum = null;        // segment number of each edge, avoid padding
    ComputeBuffer curveEndColor = null;         // end point color of each curve

    ComputeBuffer debugBuffer = null;

    ComputeShader curveCompute;
    #endregion

    #region private variables
    private Vector4[] debug;
    [Header("shader path")]
    [SerializeField] string curveComputePath = "Curves/Shader/Curves";
    [SerializeField] string edgeMaterialPath = "Curves/Editor/Default Tube Material";
    #endregion


    const int kThreadCount = 128;
    int curvesCount;
    int curvesPointsNumMax;
    int EdgeThreadGroupCount { get { return Convert.ToInt32(Math.Ceiling((float)curvesCount / kThreadCount)); } }

    private void Awake()
    {
        curveCompute = Resources.Load<ComputeShader>(curveComputePath);
        tubeTemplate = Resources.Load<TubeTemplate>("Curves/Editor/New Tube Template");
        edgeMaterial = Resources.Load<Material>(edgeMaterialPath);

        segmentCount = 8;

        Initailize(1, new int[] { 1 }, new Vector3[] { Vector3.zero }, new Vector4[] { Vector4.zero });
    }

    private void FixedUpdate()
    {
        var kernel = curveCompute.FindKernel("CurvesReconstruct");
        curveCompute.SetInt("InstanceCount", curvesCount);
        curveCompute.SetInt("HistoryLength", segmentCount);
        curveCompute.SetBuffer(kernel, "TangentBuffer", tangentBuffer);
        curveCompute.SetBuffer(kernel, "NormalBuffer", normalBuffer);
        curveCompute.SetBuffer(kernel, "ColorBuffer", colorBuffer);
        curveCompute.SetBuffer(kernel, "CurveBuffer", curveBuffer);
        curveCompute.SetBuffer(kernel, "CurvePointsBuffer", curvePointsBuffer);
        curveCompute.SetBuffer(kernel, "CurvePointsNumRO", curvePointsNum);
        curveCompute.SetBuffer(kernel, "CurveEndColorRO", curveEndColor);
        curveCompute.SetBuffer(kernel, "debugBuffer", debugBuffer);
        curveCompute.Dispatch(kernel, EdgeThreadGroupCount, 1, 1);

        //debugBuffer.GetData(debug);
    }

    protected void Update()
    {
        // Draw the mesh with instancing.
        edgeMaterial.SetFloat("_Radius", radius);

        edgeMaterial.SetBuffer("_TangentBuffer", tangentBuffer);
        edgeMaterial.SetBuffer("_NormalBuffer", normalBuffer);
        edgeMaterial.SetBuffer("_ColorBuffer", colorBuffer);
        edgeMaterial.SetBuffer("_CurvePointsBuffer", curvePointsBuffer);
        edgeMaterial.SetInt("_InstanceCount", curvesCount);
        edgeMaterial.SetInt("_HistoryLength", segmentCount);

        Graphics.DrawMeshInstancedIndirect(
            tubeTemplate.mesh, 0, edgeMaterial,
            new Bounds(Vector3.zero, Vector3.one * 100),
            drawEdgesBuffer, 0, props
        );
    }

    protected void OnDestroy()
    {
        ReleaseAllBuffers();
    }


    /// <summary>
    /// Initialize the buffers
    /// </summary>
    /// <param name="curvesNum">numbers of curves</param>
    /// <param name="curvesPointsNum">number of points on each curve</param>
    /// <param name="curvePoints">all points on all curves, oneCurvePointsMaxNum * curvesNum</param>
    /// <param name="curveColor">two end point color of each curve, 2 * curvesNum</param>
    private void Initailize(int curvesNum, int[] curvesPointsNum, Vector3[] curvePoints, Vector4[] curveColor)
    {
        curvesCount = curvesNum;
        curvesPointsNumMax = curvesPointsNum.Max();

        if (curvesPointsNumMax > segmentCount)
        {
            segmentCount = curvesPointsNumMax + 1;
        }

        ReleaseAllBuffers();

        drawEdgesBuffer = new ComputeBuffer(
           1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments
        );

        drawEdgesBuffer.SetData(new uint[5] {
                tubeTemplate.mesh.GetIndexCount(0), (uint)curvesCount, 0, 0, 0
            });

        // Allocate compute buffers.
        tangentBuffer = new ComputeBuffer(segmentCount * curvesCount, sizeof(float) * 4);
        normalBuffer = new ComputeBuffer(segmentCount * curvesCount, sizeof(float) * 4);
        colorBuffer = new ComputeBuffer(segmentCount * curvesCount, sizeof(float) * 4);

        debug = new Vector4[segmentCount];
        debugBuffer = new ComputeBuffer(debug.Length, sizeof(float) * 4);

        // points group from a end point to another end point is count as one curve
        curveBuffer = new ComputeBuffer(curvesPointsNumMax * curvesCount, sizeof(float) * 3);
        curvePointsBuffer = new ComputeBuffer(segmentCount * curvesCount, sizeof(float) * 3);
        curvePointsNum = new ComputeBuffer(curvesCount, sizeof(int));
        curveEndColor = new ComputeBuffer(2 * curvesCount, sizeof(float) * 4);

        curveBuffer.SetData(curvePoints);
        curvePointsNum.SetData(curvesPointsNum);
        curveEndColor.SetData(curveColor);

        // Invoke the initialization kernel.
        var kernel = curveCompute.FindKernel("CurvesInit");
        curveCompute.SetInt("InstanceCount", curvesCount);
        curveCompute.SetInt("HistoryLength", segmentCount);
        curveCompute.SetInt("IfHighlight", 0);
        curveCompute.SetBuffer(kernel, "TangentBuffer", tangentBuffer);
        curveCompute.SetBuffer(kernel, "NormalBuffer", normalBuffer);
        curveCompute.SetBuffer(kernel, "ColorBuffer", colorBuffer);
        curveCompute.SetBuffer(kernel, "CurvePointsBuffer", curvePointsBuffer);
        curveCompute.SetBuffer(kernel, "debugBuffer", debugBuffer);
        curveCompute.Dispatch(kernel, EdgeThreadGroupCount, 1, 1);

        // This property block is used only for avoiding an instancing bug.
        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", UnityEngine.Random.value);

        // Clone the given material before using.
        edgeMaterial = new Material(edgeMaterial);
        edgeMaterial.name += " (cloned)";
        materialCloned = true;
    }

    void ReleaseAllBuffers()
    {
        ReleaseBuffer(drawEdgesBuffer);
        ReleaseBuffer(tangentBuffer);
        ReleaseBuffer(normalBuffer);
        ReleaseBuffer(colorBuffer);

        ReleaseBuffer(curveBuffer);
        ReleaseBuffer(curvePointsBuffer);
        ReleaseBuffer(curvePointsNum);
        ReleaseBuffer(curveEndColor);
        ReleaseBuffer(debugBuffer);

        if (materialCloned)
        {
            Destroy(edgeMaterial);
            materialCloned = false;
        }
    }

    void ReleaseBuffer(ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            buffer.Release();
            buffer = null;
        }
    }

    public void UpdateCurve(Vector3[][] curveEndPoints, Vector4[] color = null, int lerpNum = 64)
    {
        Vector3[] curvePoints;
        int[] dataLineLength;
        LerpCurvePoints(curveEndPoints, lerpNum, out curvePoints, out dataLineLength);

        int num = curveEndPoints.Length;

        // convert color 
        Vector4[] curveEndPointColor;
        Vector4[] initialColor;
        if (color == null)
        {
            curveEndPointColor = new Vector4[2 * num];
            initialColor = new Vector4[] { UnityEngine.Random.ColorHSV(), UnityEngine.Random.ColorHSV() };
            for (int i = 0; i < num; i++)
            {
                curveEndPointColor[i * 2] = initialColor[0];
                curveEndPointColor[i * 2 + 1] = initialColor[1];
            }
        }
        else if (color.Length == 1)
        {
            curveEndPointColor = new Vector4[2 * num];
            initialColor = new Vector4[] { color[0], color[0] };
            for (int i = 0; i < num; i++)
            {
                curveEndPointColor[i * 2] = initialColor[0];
                curveEndPointColor[i * 2 + 1] = initialColor[1];
            }
        }
        else if (color.Length == 2)
        {
            curveEndPointColor = new Vector4[2 * num];
            initialColor = new Vector4[] { color[0], color[1] };
            for (int i = 0; i < num; i++)
            {
                curveEndPointColor[i] = initialColor[0];
                curveEndPointColor[i + num] = initialColor[1];
            }
        }
        else if (color.Length == num)
        {
            curveEndPointColor = new Vector4[2 * num];
            for (int i = 0; i < num; i++)
                curveEndPointColor[i] = curveEndPointColor[i + num] = color[i];
        }
        else
        {
            int clen = color.Length;
            curveEndPointColor = new Vector4[clen];
            for (int ci = 0; ci < clen; ci++)
                curveEndPointColor[ci] = color[(ci * 2) % clen];
        }

        if (curvesCount != num || curvesPointsNumMax != dataLineLength.Max())
            Initailize(num, dataLineLength, curvePoints, curveEndPointColor);
        else
        {
            curveBuffer.SetData(curvePoints);
            curvePointsNum.SetData(dataLineLength);
            curveEndColor.SetData(curveEndPointColor);
        }
    }

    private void LerpCurvePoints(Vector3[][] curveEndPoints, int lerpNum, 
        out Vector3[] curvePoints, out int[] dataLineLength)
    {
        List<Vector3> curvePointsList = new List<Vector3>();
        int[] lengthArr = new int[curveEndPoints.Length];
        for (int i = 0; i < curveEndPoints.Length; i++)
        {
            Vector3[] linePoints = new Vector3[lerpNum];
            for (int si = 0; si < lerpNum; si++)
                linePoints[si] = Vector3.Lerp(curveEndPoints[i][0], curveEndPoints[i][1], (float)si / (lerpNum - 1.0f));
            curvePointsList.AddRange(linePoints);
            lengthArr[i] = linePoints.Length;
        }
        Vector3[] data = curvePointsList.ToArray();
        dataLineLength = lengthArr;
        int num = dataLineLength.Length;

        int dataLineLenMax = dataLineLength.Max();
        curvePoints = new Vector3[dataLineLenMax * num];
        int dataIdx = 0;
        for (int i = 0; i < num; i++)
        {
            for (int j = 0; j < dataLineLength[i]; j++)
            {
                curvePoints[j * num + i] = data[dataIdx];
                dataIdx += 1;
            }
            // padding
            dataIdx -= 1;
            for (int j = dataLineLength[i]; j < dataLineLenMax; j++)
                curvePoints[j * num + i] = data[dataIdx];
            dataIdx += 1;
        }
    }

    public void ClearCurve()
    {
        Initailize(1, new int[] { 1 }, new Vector3[] { Vector3.zero }, new Vector4[] { Vector4.zero });
    }

}
