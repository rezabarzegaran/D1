using System;
using System.Collections.Generic;
using System.Linq;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    /// <summary>
    /// This class represents a taskchain.
    /// </summary>
    public class CoreSchedule : IMeasurement
    {
        // DO NOT use a queue. Otherwise we need to recreate it continously.
        public List<Events> TaskSlices;
        public List<Events> ExecSlices;
        public List<Events> IdleSlices;
        public List<Events> PartitionSlices;
        public List<Partition> Partitions;
        public long _Hyperperiod = 1;

        public CoreSchedule(int cpuid, int coreid)
        {
            CpuId = coreid;
            CoreId = coreid;
            TaskSlices = new List<Events>();
            ExecSlices = new List<Events>();
            IdleSlices = new List<Events>();
            PartitionSlices = new List<Events>();
            Partitions = new List<Partition>();
            Tasks = new List<VirtualTask>();
            PartitionViolations = 0;
            PartitionCost = 0;
        }
        public CoreSchedule(CoreSchedule c)
        {
            CpuId = c.CpuId;
            CoreId = c.CoreId;
            PartitionViolations = c.PartitionViolations;
            PartitionCost = c.PartitionCost;
            TaskSlices = new List<Events>();
            foreach (var tasksclice in c.TaskSlices)
            {
                TaskSlices.Add(new Events(tasksclice));
            }
            ExecSlices = new List<Events>();
            foreach (var exeslice in c.ExecSlices)
            {
                ExecSlices.Add(new Events(exeslice));
            }
            PartitionSlices = new List<Events>();
            foreach (var partitionslice in c.PartitionSlices)
            {
                PartitionSlices.Add(new Events(partitionslice));
            }
            IdleSlices = new List<Events>();
            foreach (var idleslices in c.IdleSlices)
            {
                IdleSlices.Add(new Events(idleslices));
            }
            Partitions = new List<Partition>();
            foreach (var part in c.Partitions)
            {
                Partitions.Add( new Partition(part));
            }
            Tasks = new List<VirtualTask>();
            foreach (var task in c.Tasks)
            {
                Tasks.Add(new VirtualTask(task));
            }


        }
        public int CpuId { get; }
        public int CoreId { get; }
        public double TotalPartitions => Partitions.Count;
        public bool Failed => PartitionViolations > 0;
        public List<VirtualTask> Tasks { get; }
        public int PartitionViolations;
        public int PartitionCost;


        public void Reset()
        {
            PartitionViolations = 0;
            PartitionCost = 0;
            TaskSlices.Clear();
            ExecSlices.Clear();
            PartitionSlices.Clear();
            Partitions.Clear();
            IdleSlices.Clear();
            Tasks.ForEach(x => x.executionTrace.Clear());
            Tasks.ForEach(x => x._started = false);
        }
        public void AddTask(Job job)
        {
            Tasks.Add(new VirtualTask(job));
            _Hyperperiod = Extensions.LeastCommonMultiple(_Hyperperiod, job.Period);
        }

        public void StartTask(Job job, int cycle)
        {
            VirtualTask task = Tasks.FirstOrDefault(x => x.Id == job.Id);

            if (task != null)
            {
                if (!task._started)
                {
                    task.executionTrace.Add(new Events(cycle, -1, task.Id.ToString(), task.CIL));
                    task._started = true;
                }
            }


        }
        public void EndTask(Job job, int cycle)
        {
            VirtualTask task = Tasks.FirstOrDefault(x => x.Id == job.Id);

            if (task != null)
            {
                if (task._started)
                {
                    task.executionTrace.Last().End = cycle;
                    task._started = false;
                }
                else
                {
                    task.executionTrace.RemoveAt(task.executionTrace.Count - 1);
                    task._started = false;
                }

            }


        }

        public class VirtualTask
        {
            public VirtualTask(Job job)
            {
                Id = job.Id;
                Name = job.Name;
                _started = false;
                CIL = job.Cil;
                executionTrace = new List<Events>();
                WCET = job.ExecutionTime;
            }
            public VirtualTask(VirtualTask job)
            {
                Id = job.Id;
                Name = job.Name;
                _started = job._started;
                CIL = job.CIL;
                executionTrace = new List<Events>();
                foreach (var trace in job.executionTrace)
                {
                    executionTrace.Add(new Events(trace));
                }

                WCET = job.WCET;
            }

            public string Name { get; }
            public int Id { get; }
            public bool _started { get; set; }
            public int CIL { get; }
            public int WCET { get; }
            public List<Events> executionTrace;
        }

        private void GeneratTable()
        {
            PriorityQueue<Events> tempexeslices = new PriorityQueue<Events>(OrderByStart);
            foreach (var task in Tasks)
            {
                foreach (var slice in task.executionTrace)
                {
                    if (slice.End != -1)
                    {
                        tempexeslices.Enqueue(new Events(slice.Start, slice.End, slice.Owner, slice.CIL));
                    }
                }
            }

            while (tempexeslices.Count() > 0)
            {
                TaskSlices.Add(new Events(tempexeslices.Dequeue()));
            }

            ExecSlices = GetExecSlices();
            PartitionSlices = GetPartitionSlices();
            IdleSlices = GetIdleSlices();

        }
        public void Initiate()
        {
            GeneratTable();
            MapTasksToPartitions();

            GeneratePartitions();

            if(Partitions.Count != 0) PartitionCost /= Partitions.Count;



        }
        
        private void MapTasksToPartitions()
        {
            foreach (var task in Tasks)
            {
                int crr_cil = task.CIL;
                bool found_partition = false;
                foreach (var p in Partitions)
                {
                    if (p.CIL == crr_cil)
                    {
                        p.AddTask(task);
                        found_partition = true;
                    }
                }
                if (!found_partition)
                {
                    Partition tc = new Partition(crr_cil);
                    tc.AddTask(task);
                    Partitions.Add(tc);
                }
            }
            
        }
        private void GeneratePartitions()
        {
            int pre_dist = 0;
            foreach (var slice in PartitionSlices)
            {
                int dist = Math.Abs(pre_dist - slice.Start);


                

                int crr_cil = slice.CIL;
                bool found_partition = false;
                foreach (var p in Partitions)
                {
                    if (p.CIL == crr_cil)
                    {
                        p.ExecSlices.Add(new Events(slice));
                        if (pre_dist != 0)
                        {
                            if (pre_dist < p.Overhead)
                            {
                                PartitionViolations++;
                                PartitionCost += Math.Abs(pre_dist - p.Overhead);
                            }
                        }
                        found_partition = true;
                    }
                }

                if (!found_partition)
                {
                    Partition tc = new Partition(crr_cil);
                    tc.ExecSlices.Add(new Events(slice));
                    if (pre_dist != 0)
                    {
                        if (pre_dist < tc.Overhead)
                        {
                            PartitionViolations++;
                            PartitionCost += Math.Abs(pre_dist - tc.Overhead);
                        }
                    }
                    Partitions.Add(tc);
                }

                pre_dist = slice.End;
            }
        }
        private int OrderByStart(Events j1, Events j2)
        {
            if (j1.Start > j2.Start) return 1;
            if (j1.Start == j2.Start) return 0;
            return -1;
        }
        private List<Events> GetIdleSlices()
        {
            List<Events> Slices = new List<Events>();
            int start = 0;
            foreach (var slice in ExecSlices)
            {

                if (slice.Start > start)
                {
                    Slices.Add(new Events(start, slice.Start));
                    start = slice.End;
                }
                else
                {
                    start = slice.End;
                }
            }

            if (ExecSlices.Last().End < _Hyperperiod)
            {
                Slices.Add(new Events(ExecSlices.Last().End, (int)_Hyperperiod));
            }


            Slices.OrderBy(x => x.Start);

            return Slices;
        }

        private List<Events> GetExecSlices()
        {
            List<Events> Slices = new List<Events>();

            foreach (var taskslice in TaskSlices)
            {
                Slices.Add(new Events(taskslice));
            }

            while (true)
            {
                int presize = Slices.Count;
                Slices = MergeSlices(Slices);
                if (Slices.Count == presize)
                {
                    break;
                }
            }

            Slices.OrderBy(x => x.Start);




            return Slices;
        }
        private List<Events> GetPartitionSlices()
        {
            List<Events> Slices = new List<Events>();

            foreach (var taskslice in TaskSlices)
            {
                Slices.Add(new Events(taskslice));
            }

            while (true)
            {
                int presize = Slices.Count;
                Slices = MergeSameCILSlices(Slices);
                if (Slices.Count == presize)
                {
                    break;
                }
            }

            Slices.OrderBy(x => x.Start);




            return Slices;
        }

        private List<Events> MergeSameCILSlices(List<Events> _slices)
        {
            List<Events> Slices = new List<Events>();
            bool changed = false;

            for (int i = 0; i < _slices.Count; i++)
            {
                if (changed)
                {
                    changed = false;
                    continue;
                }
                if (i != (_slices.Count - 1))
                {
                    if (_slices[i].End >= _slices[i + 1].Start)
                    {
                        if (_slices[i].CIL == _slices[i + 1].CIL)
                        {
                            int end = (_slices[i].End >= _slices[i + 1].End) ? _slices[i].End : _slices[i + 1].End;
                            Slices.Add(new Events(_slices[i].Start, end, "Taken", _slices[i].CIL));
                            changed = true;
                        }

                    }
                    else
                    {
                        Slices.Add(new Events(_slices[i].Start, _slices[i].End, "Taken", _slices[i].CIL));

                    }

                }
                else
                {
                    Slices.Add(new Events(_slices[i].Start, _slices[i].End, "Taken", _slices[i].CIL));
                    //Slices.Add(new Events(_slices[i + 1].Start, _slices[i + 1].End, "Taken"));
                }
            }

            return Slices;

        }

        private List<Events> MergeSlices(List<Events> _slices)
        {
            List<Events> Slices = new List<Events>();
            bool changed = false;

            for (int i = 0; i < _slices.Count; i++)
            {
                if (changed)
                {
                    changed = false;
                    continue;
                }
                if (i != (_slices.Count - 1))
                {
                    if (_slices[i].End >= _slices[i + 1].Start)
                    {
                        if (_slices[i].CIL == _slices[i + 1].CIL)
                        {
                            int end = (_slices[i].End >= _slices[i + 1].End) ? _slices[i].End : _slices[i + 1].End;
                            Slices.Add(new Events(_slices[i].Start, end, "Taken", _slices[i].CIL));
                            changed = true;
                        }

                    }
                    else
                    {
                        Slices.Add(new Events(_slices[i].Start, _slices[i].End, "Taken", _slices[i].CIL));

                    }

                }
                else
                {
                    Slices.Add(new Events(_slices[i].Start, _slices[i].End, "Taken", _slices[i].CIL));
                    //Slices.Add(new Events(_slices[i + 1].Start, _slices[i + 1].End, "Taken"));
                }
            }

            return Slices;

        }

        public class Partition
        {
            public Partition(int cil)
            {
                CIL = cil;
                Overhead = Int32.MaxValue;
            }
            public Partition(Partition partition)
            {
                CIL = partition.CIL;

                foreach (var ev in partition.ExecSlices)
                {
                    ExecSlices.Add(ev);
                }

                foreach (var t in partition.Tasks)
                {
                    Tasks.Add(t);
                }

                Overhead = partition.Overhead;
            }
            public int CIL { get; }
            public List<Events> ExecSlices = new List<Events>();
            public List<string> Tasks = new List<string>();
            public int Overhead { get; set; }

            private bool HasTask(string name)
            {
                foreach (var task in Tasks)
                {
                    if (task == name)
                    {
                        return true;
                    }
                }

                return false;
            }

            public void AddTask(VirtualTask t)
            {
                if(!HasTask(t.Name))
                {
                    Tasks.Add(t.Name);
                    if (Overhead >= (0.05 * t.WCET))
                    {
                        Overhead = (int) (0.05 * t.WCET);
                    }
                }
            }
        }

        public class Events
        {
            public Events(int Startcycle, int Endcycle, string owner = "Idle", int _cil = -1)
            {
                Start = Startcycle;
                End = Endcycle;
                Owner = owner;
                Duration = Endcycle - Startcycle;
                CIL = _cil;

            }
            public Events(Events ev)
            {
                Start = ev.Start;
                End = ev.End;
                Owner = ev.Owner;
                Duration = End - Start;
                CIL = ev.CIL;


            }

            public int Start { get; }
            public int End { get; set; }
            public string Owner { get; set; }
            public int Duration { get; set; }
            public int CIL { get; set; }
        }
    }


}
