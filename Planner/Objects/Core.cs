using System;
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
            Separation = new Dictionary<int, int>();
            _CF = cf;
        }
        public Core(int cpuId, int coreId, int macroTick, double cf, List<Job> tasks, Dictionary<int, int> separation)
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
            Separation = new Dictionary<int, int>();
            _CF = cf;



            foreach (var task in tasks)
            {
                Tasks.Add(task);
                task.SetEnvironment(Id, _CF);
                Utilization += task.Cost;
                Hyperperiod = (int)Extensions.LeastCommonMultiple(Hyperperiod, task.Period);
            }
            Separation.Clear();
            foreach (var item in separation)
            {
                
                Separation.Add(item.Key, item.Value);
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
        public Dictionary<int, int> Separation;
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

            CalcSeparation();
        }


        //TODO: Does this work correctly with an empty taskset?
        /// <summary>
        /// This method executes the current event if is set.
        /// It also sets the upcoming event based on the readylist/waiting list. 
        /// </summary>
        /// <param name="debug">Indicates if the ExecutionTrace should be built.</param>
        public void TriggerEvent(bool debug)
        {
            int next = Int32.MaxValue;
            ReleaseTasks(Event.Cycle);

            if (Current == null)
            {
                if (ReadyList.Count() > 0)
                {
                    if ((Event.Cycle % Hyperperiod) == 0)
                    {
                        int offset = Separation[ReadyList.Peek().Cil];
                        next = Event.Cycle + offset;
                    }
                    else
                    {
                        Current = ReadyList.Dequeue();
                        next = Current.NextEvent(Event.Cycle, MacroTick);
                    }
                }
                else if (WaitingList.Count() > 0)
                {
                    next = WaitingList.Peek().Release;
                }
            }
            else
            {
                bool finalExecutionSlice = Current.FinalEvent(MacroTick);
                Current.Execute(Event.Cycle, debug, MacroTick);
                next = Current.NextEvent(Event.Cycle, MacroTick);
                int offset = 0;

                if (ReadyList.Count() > 0)
                {
                    Job possibleJob = ReadyList.Peek();
                    if (possibleJob.Cil != Current.Cil)
                    {
                        offset = Separation[possibleJob.Cil];
                    }
                    if (finalExecutionSlice)
                    {
                        WaitingList.Enqueue(Current);
                        Current = null;
                        next = Event.Cycle + offset;
                    }else if ((possibleJob.PriorityDeadline + offset) < Current.PriorityDeadline)
                    {
                        ReadyList.Enqueue(Current);
                        Current = null;
                        next = Event.Cycle + offset;
                    }

                }
                else
                {
                    if (finalExecutionSlice)
                    {
                        WaitingList.Enqueue(Current);
                        Current = null;

                        foreach (var item in Separation)
                        {
                            if (offset < item.Value)
                            {
                                offset = item.Value;
                            }
                        }
                        next = Event.Cycle + offset;
                    }

                }




                ReleaseTasks(Event.Cycle);




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
            return new Core(_cpuId, Id, MacroTick, _CF, Tasks, Separation);
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

        private Job PredictAssignTask()
        {
            Job temp = null;
            if (Current != null)
            {
                temp = Current.Clone();
            }
            
            if (ReadyList.Count() > 0)
            {
                // Assign a task if we dont have one
                if (temp == null)
                {
                    temp = ReadyList.Peek().Clone();
                }
                // Preempt the current task if the awaiting task's deadline is lower
                //else if (ReadyList.Peek().PriorityDeadline < Current.PriorityDeadline)
                else if (ReadyList.Peek().PriorityDeadline < temp.PriorityDeadline)
                {
                    //ReadyList.Enqueue(Current);
                    temp = ReadyList.Peek().Clone();
                }
            }

            return temp;
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
        public void CalcSeparation()
        {
            Separation.Clear();
            foreach (var job in Tasks)
            {
                int currentsep = 0;
                if (Separation.TryGetValue(job.Cil, out currentsep))
                {
                    if (currentsep < (job.ExecutionTime / 10))
                    {
                        Separation[job.Cil] = (job.ExecutionTime / (40/4));
                    }
                }
                else
                {
                    Separation.Add(job.Cil, (job.ExecutionTime / (40 / 4)));
                }
            }

        }
    }
}
