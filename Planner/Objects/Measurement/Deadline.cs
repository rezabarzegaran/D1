using System;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    public class Deadline : IMeasurement
    {
        private bool _started;

        public Deadline(Job job)
        {
            _started = false;
            Period = job.Period;
            OwnerName = job.Name;
            OwnerCpu = job.CpuId;
            OwnerCore = job.CoreId;
        }
        public Deadline(int p, string ownername, int cpu, int core)
        {
            _started = false;
            Period = p;
            OwnerName = ownername;
            OwnerCpu = cpu;
            OwnerCore = core;
        }

        public int MaxDistance { get; set; }
        public bool Failed => MaxDistance > 0;
        public int Period { get; private set; }
        public string OwnerName { get; }
        public int OwnerCpu { get; private set; }
        public int OwnerCore { get; private set; }


        public void Reset()
        {
            MaxDistance = 0;
            _started = false;
        }
        public void StartTask(Job job, int cycle)
        {
            if (!_started)
            {
                _started = true;
            }
        }
        public void EndTask(Job job, int cycle)
        {
            if (_started)
            {
                MaxDistance = Math.Max(MaxDistance, cycle - job.AbsoluteDeadline);
                _started = false;
            }
        }
        public void SetEnvironment(int period, int cpuid, int coreid)
        {
            Period = period;
            OwnerCpu = cpuid;
            OwnerCore = coreid;
        }
    }
}
