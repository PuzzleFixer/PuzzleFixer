using System.Threading;
using UnityEngine;

public class AsynRunner : RunAbleThread
{
    private EventWaitHandle waitHandle = new ManualResetEvent(initialState: false);

    public Vector3[][] fPoints, fNormals;
    public Transform objMesh1, objMesh2;
    public int targetidx;
    public Matrix4x4 mat;

    protected override void Run()
    {
        while (true)
        {
            waitHandle.WaitOne();
            if (!Running)
                break;

            mat = PairRegister.MeshLabGP(fPoints, fNormals, out targetidx);

            waitHandle.Reset();
        }
    }

    public void AwakeRunning()
    {
        waitHandle.Set();
    }

    public bool GetEventState()
    {
        return waitHandle.WaitOne(0);
    }
}
