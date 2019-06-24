using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    public class TaskMap : IMeasurement
    {
        private bool _started;

        public TaskMap(Job job)
        {
            _started = false;
            Period = job.Period;
            OwnerName = job.Name;
            OwnerCpu = job.CpuId;
            OwnerCore = job.CoreId;
            Starts = new List<int>();
            Ends = new List<int>();
        }
        public TaskMap(int p, string name, int cpu, int core, List<int> _starts, List<int> _ends)
        {
            _started = false;
            Period = p;
            OwnerName = name;
            OwnerCpu = cpu;
            OwnerCore = core;
            Starts = new List<int>();
            Ends = new List<int>();
            foreach (var s in _starts)
            {
                Starts.Add(s);
            }

            foreach (var e in _ends)
            {
                Ends.Add(e);
            }

        }


        public bool Failed => 1 > 0;
        public int Period { get; private set; }
        public string OwnerName { get; }
        public int OwnerCpu { get; private set; }
        public int OwnerCore { get; private set; }
        public List<int> Starts;
        public List<int> Ends;

        public void Reset()
        {
            _started = false;
            Starts.Clear();
            Ends.Clear();

        }
        public void StartTask(Job job, int cycle)
        {
            if (!_started)
            {
                _started = true;
                Starts.Add(cycle);
                
            }
        }
        public void EndTask(Job job, int cycle)
        {
            if (_started)
            {
                _started = false;
                Ends.Add(cycle);
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
