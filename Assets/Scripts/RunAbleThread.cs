using System.Threading;
using NetMQ;


public abstract class RunAbleThread
{
    private readonly Thread _runnerThread;

    protected RunAbleThread()
    {
        _runnerThread = new Thread(Run);
    }

    protected bool Running { get; private set; }

    protected abstract void Run();

    public void Start()
    {
        Running = true;
        _runnerThread.Start();
    }

    public void SetRunning(bool stage)
    {
        Running = stage;
    }

    public void Stop(bool cleanZMQ = true)
    {
        Running = false;
        _runnerThread.Join(1000);
        if (cleanZMQ)
            NetMQConfig.Cleanup();
    }
}