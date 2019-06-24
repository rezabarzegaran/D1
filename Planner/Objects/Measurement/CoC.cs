using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    public class CoC : IMeasurement
    {
        // DO NOT use a queue. Otherwise we need to recreate it continously.
        private List<ChainInstance> _chainInstances;
        private int _chainLength;

        public CoC(string name)
        {
            Name = name;
            _chainInstances = new List<ChainInstance>();
            Tasks = new List<VirtualTask>();
            Period = -1;
        }

        public string Name { get; }
        public double Cost { get; set; }
        public int Period { get; private set; }
        public bool Failed => 1 > 0;
        public List<VirtualTask> Tasks { get; }

        public void Reset()
        {
            Cost = 0;
            _chainInstances.Clear();
            Tasks.ForEach(x => x.reset());
        }

        public void SetPeriod(int period)
        {
            Period = period;
        }
        public void AddTask(Job job)
        {
            Tasks.Add(new VirtualTask(job.Name));
            Period = job.Period;
            _chainLength++;
        }

        public void StartTask(Job job, int cycle)
        {
            foreach (var task in Tasks)
            {
                if (task.Name == job.Name)
                {
                    if (!task._started)
                    {
                        task._started = true;
                        task.SetStart(cycle);
                    }
                }
            }
        }
        public void EndTask(Job job, int cycle)
        {
            foreach (var task in Tasks)
            {
                if (task.Name == job.Name)
                {
                    if (task._started)
                    {
                        task._started = false;
                        task.SetEnd(cycle);
                    }
                }
            }
        }

        public class VirtualTask
        {
            public VirtualTask(string name)
            {
                _started = false;
                Name = name;
                StartAccess = new List<int>();
                EndAccess = new List<int>();
            }
            public bool _started;
            public string Name { get; }
            public List<int> StartAccess { get; set; }
            public List<int> EndAccess { get; set; }

            public void reset()
            {
                StartAccess.Clear();
                EndAccess.Clear();
                _started = false;
            }

            public void SetStart(int cycle)
            {
                StartAccess.Add(cycle);
            }

            public void SetEnd(int cycle)
            {
                EndAccess.Add(cycle);
            }

        }
        private class ChainInstance
        {
            public ChainInstance(int cycle)
            {
                Start = cycle;
                Progress = 0;
                CurrentTaskStarted = true;

            }

            public int Start { get; }
            public int ReadyAt { get; set; }
            public int Progress { get; set; }
            public bool CurrentTaskStarted { get; set; }
        }

    }
}
