using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.IO;

public class EXERunner
{
    static private string meshlabserverPath = "C:/Program Files/VCG/MeshLab/meshlabserver.exe";
    static private Process pythonServer = null;

    static public void CallExternalExe(string exePath, string arg, bool showRegisterWindow)
    {
        if (exePath == "meshlab")
            exePath = meshlabserverPath;

        Process process = new Process
        {
            StartInfo =
            {
                UseShellExecute = showRegisterWindow,
                RedirectStandardOutput = !showRegisterWindow,
                RedirectStandardError = !showRegisterWindow,
                CreateNoWindow = !showRegisterWindow,
                FileName = exePath,
                Arguments = arg
            }
        };
        process.Start();
        string output = "";
        if (!showRegisterWindow)
            output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (!showRegisterWindow && process.HasExited)
            Debug.Log(output);
    }

    static public void CallPythonScript(string arg, bool showRegisterWindow)
    {
        string pyPathTxt = Application.dataPath + "/../PythonUtilities/pyPath.txt";
        string pyPath = File.ReadAllText(pyPathTxt);
        pythonServer = new Process
        {
            StartInfo =
            {
                UseShellExecute = showRegisterWindow,
                RedirectStandardOutput = !showRegisterWindow,
                RedirectStandardError = !showRegisterWindow,
                CreateNoWindow = !showRegisterWindow,
                FileName = pyPath,
                Arguments = arg
            }
        };
        pythonServer.Start();
    }

    static public void ExitPythonScript()
    {
        pythonServer.Kill();
    }
}
