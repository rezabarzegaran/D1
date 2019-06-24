using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    public class AppMap : IMeasurement
    {
        // DO NOT use a queue. Otherwise we need to recreate it continously.
        private List<AppInstance> _appInstances;
        private int _chainLength;
        public List<Event> _events;

        public AppMap(string name)
        {
            Name = name;
            _appInstances = new List<AppInstance>();
            Tasks = new List<VirtualTask>();
            _events = new List<Event>();
            CpuID = new List<int>();
            CoreID = new List<int>();
        }

        public string Name { get; }
        public List<int> CpuID;
        public List<int> CoreID;
        public bool Failed => 1 > 0;
        public List<VirtualTask> Tasks { get; }

        public void Reset()
        {
            _appInstances.Clear();
            _events.Clear();
        }
        public void AddTask(Job job)
        {
            CpuID.Add(job.CpuId);
            CoreID.Add(job.CoreId);
            Tasks.Add(new VirtualTask(job.Name));
            _chainLength++;
        }

        public void SetEnvironment(string name, int cpuid, int coreid)
        {
            int index = Tasks.FindIndex(x => x.Name == name);
            CpuID[index] = cpuid;
            CoreID[index] = coreid;
        }

        public void StartTask(Job job, int cycle)
        {
            foreach (AppInstance instance in _appInstances)
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
            if (Tasks[0].Name == job.Name)
            {
                int ID = _events.Count;
                _events.Add(new Event(ID, cycle));
                _appInstances.Add(new AppInstance(ID, cycle));

            }
        }
        public void EndTask(Job job, int cycle)
        {
            List<AppInstance> appInstances = new List<AppInstance>();
            foreach (AppInstance instance in _appInstances)
            {
                if (instance.CurrentTaskStarted && Tasks[instance.Progress].Name == job.Name)
                {
                    // If the chain instance is complete, then meassure the E2E
                    // Do not add it to the list.
                    if (instance.Progress == _chainLength - 1)
                    {
                        _events.Last(x => x.Instance == instance.ID).End = cycle;
                    }
                    // Otherwise, end the current task and set the chain instance ready
                    // to begin a new task. Also, add it to the list such that it can continue.
                    else
                    {
                        instance.Progress++;
                        instance.ReadyAt = cycle;
                        instance.CurrentTaskStarted = false;
                        appInstances.Add(instance);
                    }
                    continue;
                }
                appInstances.Add(instance);
            }

            _appInstances = appInstances;
        }

        public class VirtualTask
        {
            public VirtualTask(string name)
            {
                Name = name;
            }
            public string Name { get; }
        }
        private class AppInstance
        {
            public AppInstance(int _id, int cycle)
            {
                ID = _id;
                Start = cycle;
                Progress = 0;
                CurrentTaskStarted = true;

            }
            public int ID { get; }
            public int Start { get; }
            public int ReadyAt { get; set; }
            public int Progress { get; set; }
            public bool CurrentTaskStarted { get; set; }
        }

        public class Event
        {
            public Event(int instance, int cycle)
            {
                Instance = instance;
                Start = cycle;
            }
            public int Instance { get; }
            public int Start { get; set; }
            public int End { get; set; }


        }


    }
}
