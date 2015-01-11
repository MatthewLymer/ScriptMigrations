using System;
using System.Diagnostics;
using SystemWrappers.Interfaces.Diagnostics;

namespace SystemWrappers.Wrappers.Diagnostics
{
    public class StopwatchWrapper : IStopwatch
    {
        private readonly Stopwatch _stopwatch;

        public StopwatchWrapper(Stopwatch stopwatch)
        {
            _stopwatch = stopwatch;
        }

        public void Start()
        {
            _stopwatch.Start();
        }

        public void Reset()
        {
            _stopwatch.Reset();
        }

        public void Restart()
        {
            _stopwatch.Restart();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public bool IsRunning
        {
            get { return _stopwatch.IsRunning; }
        }

        public TimeSpan Elapsed
        {
            get { return _stopwatch.Elapsed; }
        }
    }
}
