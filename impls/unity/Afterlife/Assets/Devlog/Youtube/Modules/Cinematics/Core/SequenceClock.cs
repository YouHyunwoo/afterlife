using System;

namespace Afterlife.Devlog.Cinematics
{
    // 프레임 및 Unity 수명과 무관한 누적 시간 모델. 컷 경계는 [start,end)이다.
    public sealed class SequenceClock
    {
        private readonly double[] _ends;
        public double Time { get; private set; }
        public double Duration => _ends.Length == 0 ? 0 : _ends[_ends.Length - 1];
        public bool Playing { get; private set; }
        public int Index
        {
            get { for (int i=0; i<_ends.Length; i++) if (Time < _ends[i]) return i; return _ends.Length-1; }
        }
        public double LocalTime => Index < 0 ? 0 : Time - Start(Index);
        public double Start(int index) => index <= 0 ? 0 : _ends[Math.Min(index,_ends.Length)-1];
        public SequenceClock(double[] durations)
        {
            if (durations == null || durations.Length == 0) throw new ArgumentException("Sequence requires shots.");
            _ends = new double[durations.Length];
            for (int i=0;i<durations.Length;i++)
            {
                if (double.IsNaN(durations[i]) || double.IsInfinity(durations[i]) || durations[i] <= 0)
                    throw new ArgumentException("Shot duration must be finite and positive.");
                _ends[i] = (i == 0 ? 0 : _ends[i-1]) + durations[i];
            }
        }
        public void SetPlaying(bool value) { Playing = value && Time < Duration; }
        public void Seek(double time) { Time = Math.Max(0,Math.Min(Duration,time)); if (Time >= Duration) Playing=false; }
        public void Tick(double delta) { if (Playing && delta > 0 && !double.IsNaN(delta)) Seek(Time + delta); }
    }
}
