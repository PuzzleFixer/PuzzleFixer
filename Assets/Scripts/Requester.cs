using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using UnityEngine;

public class Requester : RunAbleThread
{
    public string sendCmd;
    public string recvMsg;
    public int number;
    public byte[] input = new byte[0];
    public byte[] output = new byte[0];
    private EventWaitHandle waitHandle = new ManualResetEvent(initialState: false);
    public bool getRecv = false;

    protected override void Run()
    {
        ForceDotNet.Force();
        using (RequestSocket client = new RequestSocket())
        {
            client.Connect("tcp://localhost:15555");

            while (true)
            {
                waitHandle.WaitOne();
                if (!Running)
                    break;

                client.SendMoreFrame(sendCmd);
                client.SendMoreFrame(number.ToString());
                client.SendFrame(input);
                output = client.ReceiveFrameBytes();
                getRecv = true;
                Debug.Log("ReceiveFrameBytes");

                waitHandle.Reset();
            }
        }
    }

    public void AwakeRunning()
    {
        waitHandle.Set();
    }
}
