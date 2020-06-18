using System;
using System.Collections.Generic;
using System.Linq;
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
            WCET = job.ExecutionTime;
        }
        public Deadline(Deadline d)
        {
            _started = d._started;
            Period = d.Period;
            OwnerName = d.OwnerName;
            OwnerCpu = d.OwnerCpu;
            OwnerCore = d.OwnerCore;
            WCET = d.WCET;
            MaxDistance = d.MaxDistance;
            TotalViolation = d.TotalViolation;

            foreach (var map in d.Map)
            {
                Map.Add(new Events(map));
            }


        }

        public List<Events> Map = new List<Events>();
        public int MaxDistance { get; set; }
        public bool Failed => MaxDistance > 0;
        public int Period { get; private set; }
        public int TotalViolation { get; private set; }
        public string OwnerName { get; }
        public int OwnerCpu { get; private set; }
        public int OwnerCore { get; private set; }
        private int WCET;

        public void Reset()
        {
            MaxDistance = 0;
            TotalViolation = 0;
            _started = false;
            Map.Clear();
        }
        public void StartTask(Job job, int cycle)
        {
            if (!_started)
            {
                Map.Add(new Events(cycle, WCET));
                _started = true;
            }
        }
        public void EndTask(Job job, int cycle)
        {
            if (_started)
            {
                Map.Last().End = cycle;
                MaxDistance = Math.Max(MaxDistance, cycle - job.AbsoluteDeadline);
                if ((cycle - job.AbsoluteDeadline) > 0)
                {
                    TotalViolation++;
                }
                _started = false;
            }
        }
        public void SetEnvironment(int period, int cpuid, int coreid)
        {
            Period = period;
            OwnerCpu = cpuid;
            OwnerCore = coreid;
        }
        public class Events
        {
            public Events(int cycle, int wcet)
            {
                Start = cycle;
                End = -1;
                WCET = wcet;



            }
            public Events(Events ev)
            {
                Start = ev.Start;
                End = ev.End;
                WCET = ev.WCET;




            }

            public void SetEnd(int cycle)
            {
                End = cycle;
            }

            public int Start { get; }
            public int WCET { get; }
            public int End { get; set; }
            public bool hasPreemption => (End - Start) > WCET;
        }
    }
}
