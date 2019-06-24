using System.Collections.Generic;

namespace Planner.Objects
{
    /// <summary>
    /// This class represents a single core on a processor.
    /// As the solution requires Partitioned EDF scheduling, the core is the
    /// entity implementing the EDF schedule itself.
    /// The WaitingList, ReadyList are the collections containing the tasks
    /// that are either waiting to be released or waiting to be executed.
    /// </summary>
    public class Core
    {
        private readonly int _cpuId;
        public readonly int CPUID;

        public Core(int cpuId, int coreId, int macroTick, double cf)
        {
            _cpuId = cpuId;
            CPUID = cpuId;
            Id = coreId;
            ReadyList = new PriorityQueue<Job>(OrderByDeadline);
            WaitingList = new PriorityQueue<Job>(OrderByRelease);
            Tasks = new List<Job>();
            Hyperperiod = 1;
            MacroTick = macroTick;
            Event = new TaskEvent(this);
            _CF = cf;
        }
        public Core(int cpuId, int coreId, int macroTick, double cf, List<Job> tasks)
        {
            _cpuId = cpuId;
            CPUID = cpuId;
            Id = coreId;
            ReadyList = new PriorityQueue<Job>(OrderByDeadline);
            WaitingList = new PriorityQueue<Job>(OrderByRelease);
            Tasks = new List<Job>();
            Hyperperiod = 1;
            MacroTick = macroTick;
            Event = new TaskEvent(this);
            _CF = cf;



            foreach (var task in tasks)
            {
                Tasks.Add(task);
                task.SetEnvironment(Id, _CF);
                Utilization += task.Cost;
                Hyperperiod = (int)Extensions.LeastCommonMultiple(Hyperperiod, task.Period);
            }



        }

        public int Id { get; }
        public double _CF { get; set; }

        public PriorityQueue<Job> WaitingList { get; }
        public PriorityQueue<Job> ReadyList { get; }
        public List<Job> Tasks { get; }
        public double Utilization { get; private set; }
        public double Availability => 100.0d - Utilization;
        public Job Current { get; private set; }
        public int Hyperperiod { get; private set; }
        public int MacroTick { get; private set; }
        public TaskEvent Event { get; private set; }
        public void QueueJob(Job job)
        {
            Tasks.Add(job);
            job.SetEnvironment(Id, _CF);
            Utilization += job.Cost;
            Hyperperiod = (int)Extensions.LeastCommonMultiple(Hyperperiod, job.Period);
        }

        public void Initialize()
        {
            foreach (Job j in Tasks)
            {
                j.Reset();
                WaitingList.Enqueue(j);
            }
        }


        //TODO: Does this work correctly with an empty taskset?
        /// <summary>
        /// This method executes the current event if is set.
        /// It also sets the upcoming event based on the readylist/waiting list. 
        /// </summary>
        /// <param name="debug">Indicates if the ExecutionTrace should be built.</param>
        public void TriggerEvent(bool debug)
        {
            //Console.WriteLine($"**** Cycle: {Event.Cycle} ****");
            bool hasExecution = false; // TODO: remove this. replace check by Current!=null
            ReleaseTasks(Event.Cycle); // TODO: remove this, and activate lines 99 through 102. when unittesting is done.
            if (Current != null)
            {
                bool finalExecutionSlice = Current.FinalEvent(MacroTick);
                Current.Execute(Event.Cycle, debug, MacroTick);
                // If this is the last execution slice for this task
                // then insert it into the waiting list.
                if (finalExecutionSlice)
                {
                    WaitingList.Enqueue(Current);
                    Current = null;
                }
                // We have executed the current task, so release all ready tasks,
                // then assign/preempt a new task.
                ReleaseTasks(Event.Cycle);
                AssignTask();
                // Set the cycle for the next task.
                if (Current != null)
                {
                    Event.Cycle = Current.NextEvent(Event.Cycle, MacroTick);
                    hasExecution = true;
                }
            }
            //else
            //{
            //    ReleaseTasks(Event.Cycle);
            //}

            int next = hasExecution ? Event.Cycle : int.MaxValue;

            if (ReadyList.Count() > 0)
            {
                int nextPremption = ReadyList.Peek().NextEvent(Event.Cycle, MacroTick);
                if (nextPremption < next)
                {
                    next = nextPremption;
                    AssignTask();
                }
            }
            else if (!hasExecution && WaitingList.Count() > 0)
            {
                next = WaitingList.Peek().Release;
            }

            Event.Cycle = next;
        }
        public Core Clone()
        {
            //core.CanSwap = CanSwap;
            return new Core(_cpuId, Id, MacroTick , _CF);
        }
        public Core DeepClone()
        {
            return new Core(_cpuId, Id, MacroTick, _CF, Tasks);
        }

        private void ReleaseTasks(int currentCycle)
        {
            while (WaitingList.Count() > 0 && currentCycle >= WaitingList.Peek().Release)
            {
                ReadyList.Enqueue(WaitingList.Dequeue());
            }
        }
        private void AssignTask()
        {
            if (ReadyList.Count() > 0)
            {
                // Assign a task if we dont have one
                if (Current == null)
                {
                    Current = ReadyList.Dequeue();
                }
                // Preempt the current task if the awaiting task's deadline is lower
                //else if (ReadyList.Peek().PriorityDeadline < Current.PriorityDeadline)
                else if (ReadyList.Peek().PriorityDeadline < Current.PriorityDeadline)
                {
                    ReadyList.Enqueue(Current);
                    Current = ReadyList.Dequeue();
                }
            }
        }
        private int OrderByDeadline(Job j1, Job j2)
        {
            if (j1.PriorityDeadline > j2.PriorityDeadline) return 1;
            if (j1.PriorityDeadline == j2.PriorityDeadline) return 0;
            return -1;
        }
        private int OrderByRelease(Job j1, Job j2)
        {
            if (j1.Release > j2.Release) return 1;
            if (j1.Release == j2.Release) return 0;
            return -1;
        }
    }
}
