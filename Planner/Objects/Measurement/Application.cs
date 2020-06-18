using System;
using System.Collections.Generic;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    /// <summary>
    /// This class represents a taskchain.
    /// </summary>
    public class Application : IMeasurement
    {
        // DO NOT use a queue. Otherwise we need to recreate it continously.
        public List<Events> _Instances;
        private int _chainLength;
        private long _hyperperiod;

        public Application(string name, int threshold, bool inorder, bool ca)
        {
            Name = name;
            Threshold = threshold;
            _Instances = new List<Events>();
            Tasks = new List<VirtualTask>();
            OrderViolation = 0;
            _hyperperiod = 1;
            E2EViolations = 0;
            Cost = 0;
            Period = -1;
            InOrder = inorder;
            CA = ca;
        }
        public Application(Application app)
        {
            Name = app.Name;
            Threshold = app.Threshold;
            _Instances = new List<Events>();
            InOrder = app.InOrder;
            CA = app.CA;
            Cost = app.Cost;
            Period = app.Period;
            foreach (var ev in app._Instances)
            {
                _Instances.Add(new Events(ev));
            }
            Tasks = new List<VirtualTask>();
            foreach (var task in app.Tasks)
            {
                Tasks.Add(new VirtualTask(task.Name));
            }
            OrderViolation = app.OrderViolation;
            _hyperperiod = app._hyperperiod;
            E2EViolations = app.E2EViolations;
            E2E = app.E2E;
            OrderViolation = app.OrderViolation;
            Completed = app.Completed;

        }

        public string Name { get; }
        public bool InOrder { get; }
        public bool CA { get; }
        public int E2E { get; set; }
        public int Threshold { get; }
        public int E2EViolations { get; private set; }
        public bool Failed => FailedE2E || FailedOrder;
        public bool FailedE2E => E2E > Threshold;
        public bool FailedOrder => OrderViolation > 0 && InOrder;
        public List<VirtualTask> Tasks { get; }
        public int OrderViolation { get; set; }
        public double Cost { get; set; }
        public int Period { get; private set; }
        public bool Completed { get; private set; }

        public void Reset()
        {
            Completed = false;
            E2E = 0;
            Cost = 0;
            OrderViolation = 0;
            E2EViolations = 0;
            Tasks.ForEach(x => x.reset());
            _Instances.Clear();
        }
        public void AddTask(Job job)
        {
            Tasks.Add(new VirtualTask(job.Name));
            Period = job.Period;
            _hyperperiod = Extensions.LeastCommonMultiple(_hyperperiod, job.Period); // TODO undo this after benchmarking
            _chainLength++;
        }

        public void StartTask(Job job, int cycle)
        {
            bool relatedInstance = false;
            if (Tasks[0].Name == job.Name)
            {
                _Instances.Add(new Events(cycle));
                relatedInstance = true;
            }
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

            foreach (Events instance in _Instances)
            {
                // Only start a given task in an instance, if 
                // - it hasn't been started already. 
                // - starting the task at 'cycle' is after the previous task's readiness cycle.
                // - the task being started, is the expected task in the instance progress.
                if (!instance.CurrentTaskStarted && cycle >= instance.ReadyAt && job.Name == Tasks[instance.Progress].Name && !instance.Complete)
                {
                    relatedInstance = true;
                    instance.CurrentTaskStarted = true;
                }
            }
            // Are we starting a new task chain instance?

            if (!relatedInstance)
            {
                OrderViolation++;
            }
        }
        public void EndTask(Job job, int cycle)
        {
            int unfinishedInstances = 0;

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

            foreach (Events instance in _Instances)
            {
                if (instance.CurrentTaskStarted && Tasks[instance.Progress].Name == job.Name && !instance.Complete)
                {
                    // If the chain instance is complete, then meassure the E2E
                    // Do not add it to the list.
                    if (instance.Progress == _chainLength - 1)
                    {
                        instance.SetEnd(cycle);
                        instance.Complete = true;
                        int InstanceE2E = instance.End - instance.Start;
                        E2E = Math.Max(E2E, InstanceE2E);
                        if (InstanceE2E > Threshold)
                        {
                            E2EViolations++;
                        }

                    }
                    // Otherwise, end the current task and set the chain instance ready
                    // to begin a new task. Also, add it to the list such that it can continue.
                    else
                    {
                        instance.ReadyAt = cycle;
                        instance.Progress++;
                        instance.CurrentTaskStarted = false;
                    }
                    continue;
                }


                if (!instance.Complete)
                {
                    unfinishedInstances++;
                }

            }

            //_Instances = chainInstances;
            if (cycle > _hyperperiod && unfinishedInstances <= 1) Completed = true;
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
            public VirtualTask(VirtualTask job)
            {
                _started = false;
                Name = job.Name;
                StartAccess = new List<int>();
                foreach (var s in job.StartAccess)
                {
                    SetStart(s);
                }

                foreach (var e in job.EndAccess)
                {
                    SetEnd(e);
                }

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
        public class Events
        {
            public Events(int cycle)
            {
                Start = cycle;
                Progress = 0;
                CurrentTaskStarted = true;
                Complete = false;

                End = -1;

            }
            public Events(Events ev)
            {
                Start = ev.Start;
                Progress = ev.Progress;
                CurrentTaskStarted = ev.CurrentTaskStarted;
                Complete = ev.Complete;

                End = ev.End;

            }


            public void SetEnd(int cycle)
            {
                End = cycle;
            }

            public int Start { get; }
            public int End { get; private set; }
            public bool Complete { get; set; }
            public int ReadyAt { get; set; }
            public int Progress { get; set; }
            public bool CurrentTaskStarted { get; set; }
        }
    }
}
