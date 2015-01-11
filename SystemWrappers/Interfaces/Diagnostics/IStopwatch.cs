using System;

namespace SystemWrappers.Interfaces.Diagnostics
{
    public interface IStopwatch
    {
        void Start();
        void Reset();
        void Restart();
        void Stop();

        bool IsRunning { get; }
        TimeSpan Elapsed { get; }
    }
}