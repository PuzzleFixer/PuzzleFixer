using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageManager : MonoBehaviour
{
    [SerializeField] private bool autoStartServer = true;
    private Requester requester;
    public delegate IEnumerator AfterReceive(byte[] output);
    private AfterReceive afterReceive;

    static public class Command
    {
        public const string tSNE = "tSNE";
        public const string FindNodeByNodeNearest = "FindNodeByNodeNearest";
        public const string tSNECluster = "tSNECluster";
    }

    private void Start()
    {
        string serverPath = Application.dataPath + "/../PythonUtilities/server.py";
        if (autoStartServer)
            EXERunner.CallPythonScript(serverPath, true);

        requester = new Requester();
        requester.Start();
    }

    private void Update()
    {
        if (requester.getRecv)
        {
            StartCoroutine(afterReceive(requester.output));
            requester.getRecv = false;
        }
    }

    private void OnDestroy()
    {
        requester.SetRunning(false);
        requester.AwakeRunning();
        requester.Stop();

        if (autoStartServer)
            EXERunner.ExitPythonScript();
    }

    public void SendMessage(int num, byte[] data, string command, AfterReceive handleMessage)
    {
        requester.sendCmd = command;
        requester.input = data;
        requester.number = num;
        afterReceive = handleMessage;
        requester.AwakeRunning();
    }

}
