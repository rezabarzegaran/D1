using System;
using System.Collections.Generic;

namespace Planner.Objects.Measurement
{
    public class AbsoluteJitter : JitterBase
    {
        private int _minStart;
        private int _maxStart;
        private int _minEnd;
        private int _maxEnd;
        private bool _initialStart;
        private bool _initialEnd;

        public AbsoluteJitter(Job job) : base(job)
        {
            _minStart = 0;
            _maxStart = 0;
            _minEnd = 0;
            _maxEnd = 0;
            _initialStart = true;
            _initialEnd = true;
            StartJitters = new List<int>();
            EndJitters = new List<int>();
        }

        public override int MaxJitter => Math.Max(_maxStart - _minStart, _maxEnd - _minEnd);
        public override List<int> StartJitters { get; }
        public override List<int> EndJitters { get; }

        public override void Reset()
        {
            _minStart = 0;
            _maxStart = 0;
            _minEnd = 0;
            _maxEnd = 0;
            _initialStart = true;
            _initialEnd = true;
            StartJitters.Clear();
            EndJitters.Clear();
        }
        public override void StartTask(Job job, int cycle)
        {
            if (_initialStart)
            {
                _maxStart = cycle - job.Release;
                _minStart = _maxStart;
                _initialStart = false;
                return;
            }
            _maxStart = Math.Max(_maxStart, cycle - job.Release);
            _minStart = Math.Min(_minStart, cycle - job.Release);
            StartJitters.Add(_maxStart - _minStart);
        }
        public override void EndTask(Job job, int cycle)
        {
            if (_initialEnd)
            {
                _maxEnd = cycle - job.Release;
                _minEnd = _maxEnd;
                _initialEnd = false;
                return;
            }
            _maxEnd = Math.Max(_maxEnd, cycle - job.Release);
            _minEnd = Math.Min(_minEnd, cycle - job.Release);
            EndJitters.Add(_maxEnd - _minEnd);
        }

    }
}
