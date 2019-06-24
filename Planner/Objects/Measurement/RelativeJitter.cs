using System;
using System.Collections.Generic;

namespace Planner.Objects.Measurement
{
    public class RelativeJitter : JitterBase
    {
        private int _start;
        private int _end;
        private int _startJitter;
        private int _endJitter;
        private bool _initialStart;
        private bool _initialEnd;

        public RelativeJitter(Job job) : base(job)
        {
            _start = 0;
            _end = 0;
            _startJitter = 0;
            _endJitter = 0;
            _initialStart = true;
            _initialEnd = true;
            StartJitters = new List<int>();
            EndJitters = new List<int>();
        }
        public override int MaxJitter => Math.Max(_startJitter, _endJitter);
        public override List<int> StartJitters { get; }
        public override List<int> EndJitters { get; }

        public override void Reset()
        {
            _start = 0;
            _end = 0;
            _startJitter = 0;
            _endJitter = 0;
            _initialStart = true;
            _initialEnd = true;
            StartJitters.Clear();
            EndJitters.Clear();
        }
        public override void StartTask(Job job, int cycle)
        {
            if (_initialStart)
            {
                _start = cycle - job.Release;
                _initialStart = false;
                return;
            }
            int newStart = cycle - job.Release;
            _startJitter = Math.Max(_startJitter, Math.Abs(newStart - _start));
            _start = newStart;
            StartJitters.Add(_startJitter);
        }
        public override void EndTask(Job job, int cycle)
        {
            if (_initialEnd)
            {
                _end = cycle - job.Release;
                _initialEnd = false;
                return;
            }
            int newEnd = cycle - job.Release;
            _endJitter = Math.Max(_endJitter, Math.Abs(newEnd - _end));
            _end = newEnd;
            EndJitters.Add(_endJitter);
        }
    }
}
