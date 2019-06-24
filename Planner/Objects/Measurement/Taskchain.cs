using System;
using System.Collections.Generic;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    /// <summary>
    /// This class represents a taskchain.
    /// </summary>
    public class TaskChain : IMeasurement
    {
        // DO NOT use a queue. Otherwise we need to recreate it continously.
        private List<ChainInstance> _chainInstances;
        private int _chainLength;
        private long _hyperperiod;

        public TaskChain(string name, int threshold, double priority, bool _inorder)
        {
            Name = name;
            Threshold = threshold;
            Priority = priority;
            _chainInstances = new List<ChainInstance>();
            Tasks = new List<VirtualTask>();
            _hyperperiod = 1;
            inOrder = _inorder;
        }

        public string Name { get; }
        public int E2E { get; set; }
        public int Threshold { get; }
        public bool Failed => E2E > Threshold;
        public double Priority { get; }
        public List<VirtualTask> Tasks { get; }
        public bool Completed { get; private set; }
        public bool inOrder { get; set; }

        public void Reset()
        {
            Completed = false;
            E2E = 0;
            _chainInstances.Clear();
        }
        public void AddTask(Job job)
        {
            int communicationDelay = 0; // should be based on speed of bus and message size
            _hyperperiod = Extensions.LeastCommonMultiple(_hyperperiod, job.Period); // TODO undo this after benchmarking
            Tasks.Add(new VirtualTask(job.Name, communicationDelay));
            _chainLength++;
        }

        public void StartTask(Job job, int cycle)
        {
            foreach (ChainInstance instance in _chainInstances)
            {
                // Only start a given task in an instance, if 
                // - it hasn't been started already. 
                // - starting the task at 'cycle' is after the previous task's readiness cycle.
                // - the task being started, is the expected task in the instance progress.
                if (!instance.CurrentTaskStarted && cycle >= instance.ReadyAt && job.Name == Tasks[instance.Progress].Name)
                {
                    instance.CurrentTaskStarted = true;
                }
            }
            // Are we starting a new task chain instance?
            if (cycle < _hyperperiod && Tasks[0].Name == job.Name)
            {
                _chainInstances.Add(new ChainInstance(cycle));
            }
        }
        public void EndTask(Job job, int cycle)
        {
            List<ChainInstance> chainInstances = new List<ChainInstance>();
            foreach (ChainInstance instance in _chainInstances)
            {
                if (instance.CurrentTaskStarted && Tasks[instance.Progress].Name == job.Name)
                {
                    // If the chain instance is complete, then meassure the E2E
                    // Do not add it to the list.
                    if (instance.Progress == _chainLength - 1)
                    {
                        E2E = Math.Max(E2E, cycle - instance.Start);
                    }
                    // Otherwise, end the current task and set the chain instance ready
                    // to begin a new task. Also, add it to the list such that it can continue.
                    else
                    {
                        instance.ReadyAt = cycle + Tasks[instance.Progress++].CommunicationDelay;
                        instance.CurrentTaskStarted = false;
                        chainInstances.Add(instance);
                    }
                    continue;
                }
                chainInstances.Add(instance);
            }

            _chainInstances = chainInstances;
            if (cycle > _hyperperiod && chainInstances.Count == 0) Completed = true;
        }

        public class VirtualTask
        {
            public VirtualTask(string name, int communicationDelay)
            {
                Name = name;
                CommunicationDelay = communicationDelay;
            }
            public string Name { get; }
            public int CommunicationDelay { get; }
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
