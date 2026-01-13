using System.Diagnostics;

namespace ConsoleProject.Utils
{
    public class Time
    {
        private double _prevTime;
        private double _currentTime;

        public static double DeltaTime { get; private set; }

        private static Stopwatch _stopwatch = null!;

        public Time() => Init();

        public void Init()
        {
            _stopwatch = Stopwatch.StartNew();
            _currentTime = _stopwatch.Elapsed.TotalSeconds;
            _prevTime = _currentTime;
            DeltaTime = 0.0;
        }

        public void Tick()
        {
            _prevTime = _currentTime;
            _currentTime = _stopwatch.Elapsed.TotalSeconds;

            DeltaTime = _currentTime - _prevTime;
        }
    }
}
