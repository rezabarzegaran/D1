using System;
using System.Collections.Generic;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    public abstract class JitterBase : IMeasurement
    {
        protected JitterBase(Job job)
        {
            Threshold = job.JitterThreshold;
            Period = job.Period;
            ExecutionTime = job.ExecutionTime;
            OwnerName = job.Name;
            OwnerCpu = job.CpuId;
            OwnerCore = job.CoreId;
        }
        public abstract int MaxJitter { get; }
        public abstract List<int> StartJitters { get; }
        public abstract List<int> EndJitters { get; }
        public int Threshold { get; }
        public bool Failed => MaxJitter > Threshold;
        public int Period { get; }
        public int ExecutionTime { get; }
        public string OwnerName { get; }
        public int OwnerCpu { get; }
        public int OwnerCore { get; }

        public abstract void Reset();
        public abstract void StartTask(Job job, int cycle);
        public abstract void EndTask(Job job, int cycle);
    }
}
