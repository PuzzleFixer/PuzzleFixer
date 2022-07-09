using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsynManager : MonoBehaviour
{
    [Header("options")]
    [SerializeField] private int threadNum = 8;

    private AsynRunner[] asynRunner;
    public delegate void AfterRun(Matrix4x4 transformMatrix, int targetidx, Transform objMesh1, Transform objMesh2);
    private AfterRun[] afterRun;

    private List<int> runningList;

    private void Start()
    {
        asynRunner = new AsynRunner[threadNum];
        for (int i = 0; i < asynRunner.Length; i++)
        {
            asynRunner[i] = new AsynRunner();
            asynRunner[i].Start();
        }

        afterRun = new AfterRun[threadNum];
        runningList = new List<int>();
    }

    private void Update()
    {
        for (int i = runningList.Count - 1; i >= 0; i--)
        {
            int id = runningList[i];
            if (!asynRunner[id].GetEventState())
            {
                afterRun[id](asynRunner[id].mat, asynRunner[id].targetidx,
                    asynRunner[id].objMesh1, asynRunner[id].objMesh2);
                runningList.RemoveAt(i);
            }
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < asynRunner.Length; i++)
        {
            asynRunner[i].SetRunning(false);
            asynRunner[i].AwakeRunning();
            asynRunner[i].Stop(false);
        }
    }

    public void RunGR(int id, Vector3[][] fPoints, Vector3[][] fNormals,
        Transform objMesh1, Transform objMesh2, AfterRun afterRun)
    {
        if (id < threadNum && !asynRunner[id].GetEventState())
        {
            asynRunner[id].fPoints = fPoints;
            asynRunner[id].fNormals = fNormals;
            asynRunner[id].objMesh1 = objMesh1;
            asynRunner[id].objMesh2 = objMesh2;
            asynRunner[id].AwakeRunning();

            if (afterRun != null)
            {
                this.afterRun[id] = afterRun;
                runningList.Add(id);
            }
        }
    }

    public bool isRunEnd(int id)
    {
        if (afterRun == null)
            return !asynRunner[id].GetEventState();
        else
            return !runningList.Exists(i => i == id);
    }
}
