using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Planner.Objects.Models
{
    /// <summary>
    /// This class facilitates loading various formats (Custom/TTech),
    /// returning a workload and configuration.
    /// In case of TTechs data format this class will convert the data.
    /// </summary>
    public class DataLoader
    {

        public static T Load<T>(string file)
        {
            object obj = null;
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                obj = serializer.Deserialize(fileStream);
            }

            return (T)obj;
        }
        public static void Save(string file, object data)
        {
            if (File.Exists(file)) File.Delete(file);
            XmlSerializer serializer = new XmlSerializer(data.GetType());
            using (FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate))
            {
                serializer.Serialize(fileStream, data);
            }
        }
        public static (Workload, Configuration) Load(params string[] path)
        {
            Workload workload = Load<Workload>(path[0]);
            Configuration configuration = Load<Configuration>(path[1]);
            return (workload, configuration);
        }
        public static void Unload(string _resultfile, string _taskmapfile, string _vmmapfile, List<Simulation> s)
        {
            System.IO.Directory.CreateDirectory(_resultfile);
            System.IO.Directory.CreateDirectory(_taskmapfile);
            System.IO.Directory.CreateDirectory(_vmmapfile);
            string file = _resultfile + @"\Results.xml";
            Evaluation _evaluation = new Evaluation();


            int count = 0;
            foreach (Simulation simulation in s)
            {
                Evaluation.Solution temp = new Evaluation.Solution();
                temp.Id = count;
                SetVMS(temp, simulation.Environment);
                SetCores(temp, simulation.Environment);
                SetTasks(temp, simulation.Tasks.ToList());
                SetScores(temp, simulation.Fitness);
                SetViolation(temp, simulation.Fitness);
                SetControlScore(temp, simulation.Evaluator);
                SetEvaluationScore(temp, simulation.Evaluator);

                SetTaskMap(_taskmapfile, count, simulation);

                SetVmMap(_vmmapfile, count, simulation);

                count++;
                _evaluation.Solutions.Add(temp);

            }


            Save(file, _evaluation);
        }

        public static void SetVMS(Evaluation.Solution temp, Environment env)
        {
            foreach (var cpu in env.Cpus)
            {
                foreach (var core in cpu.Cores)
                {
                    Evaluation.Solution.VM virtualmachine = new Evaluation.Solution.VM();
                    virtualmachine.Name = "Dyn_" + cpu.Id.ToString() + "_" + core.Id.ToString();
                    virtualmachine.CPUID = cpu.Id;
                    virtualmachine.CoreID = core.Id;
                    virtualmachine.Slices = 1;
                    virtualmachine.Violation = 0;
                    virtualmachine.Fixed = true;
                    temp.vms.Add(virtualmachine);


                }
            }

        }

        public static void SetCores(Evaluation.Solution temp, Environment env)
        {
            foreach (var cpu in env.Cpus)
            {
                foreach (var core in cpu.Cores)
                {
                    Evaluation.Solution.PE pe = new Evaluation.Solution.PE();
                    pe.CPUID = cpu.Id;
                    pe.CoreID = core.Id;
                    pe.Aviliability = core.Availability;
                    pe.Hyperperiod = core.Hyperperiod;

                    foreach (var job in core.Tasks)
                    {
                        pe.Task_Names.Add(job.Name);
                    }
                    pe.VM_Names.Add("Dyn_"+cpu.Id.ToString()+"_"+core.Id.ToString());
                    //foreach (var vm in core.VMs)
                    //{
                    //    pe.VM_Names.Add(vm.Name);
                    //}

                    temp.cores.Add(pe);

                }
            }
        }

        public static void SetTasks(Evaluation.Solution temp, List<Job> alljobs)
        {
            foreach (var job in alljobs)
            {
                Evaluation.Solution.SolutionTask task = new Evaluation.Solution.SolutionTask();

                task.Name = job.Name;
                task.CPUID = job.CpuId;
                task.CoreID = job.CoreId;
                task.Period = job.Period;
                task.WCET = job.ExecutionTime;
                task.Cost = job.Cost;

                temp.Tasks.Add(task);
            }
        }

        public static void SetScores(Evaluation.Solution temp, FitnessScore fitness)
        {
            temp.Scores.Total = fitness.Score;
            temp.Scores.E2E = fitness.E2EPenalty;
            temp.Scores.Deadline = fitness.DeadlinePenalty;
            temp.Scores.Control = fitness.ControlPenalty;
            temp.Scores.Instance = -1;
            temp.Scores.Jitter = fitness.JitterPenalty;
            temp.Scores.Order = fitness.OrderPenalty;
            temp.Scores.VM = -1;
            temp.Scores.valid = fitness.IsValid;


        }

        public static void SetControlScore(Evaluation.Solution temp, Evaluator eval)
        {
            foreach (var qoc in eval.ControlCost)
            {
                temp.Scores.CoC.Add(qoc.Cost);
            }


        }

        public static void SetEvaluationScore(Evaluation.Solution temp, Evaluator eval)
        {
            foreach (var chain in eval.Chains)
            {
                temp.Scores.Chains.Add(chain.E2E);
            }

            foreach (var deadline in eval.Deadlines)
            {
                temp.Scores.Deadlines.Add(deadline.MaxDistance);
            }


        }
        public static void SetViolation(Evaluation.Solution temp, FitnessScore fitness)
        {
            temp.Violations.Total = fitness.ViolationCount;
            temp.Violations.E2E = fitness.E2EViolations;
            temp.Violations.Deadline = fitness.DeadlineViolations;
            temp.Violations.Jitter = fitness.JitterViolations;
            temp.Violations.Order = fitness.OrderViolations;
            temp.Violations.Instance = -1;


        }

        public static void SetTaskMap(string _taskmapfile, int count, Simulation simulation)
        {
            string taskmapfile = _taskmapfile + @"\Sulotion_" + count.ToString() + ".xml";
            Map _taskmap = new Map();

            foreach (var taskmap in simulation.Evaluator.TaskMaps)
            {

                Map.Item _item = new Map.Item();
                _item.OwnerName = taskmap.OwnerName;
                _item.CpuId = taskmap.OwnerCpu;
                _item.CoreId = taskmap.OwnerCore;
                _item.Duration = -1;
                _item.Space = -1;
                int numevents = Math.Min(taskmap.Starts.Count,taskmap.Ends.Count);
                for (int i = 0; i < numevents; i++)
                {
                    Map.Item.Event t = new Map.Item.Event();
                    t.Start = taskmap.Starts[i];
                    t.End = taskmap.Ends[i];
                    _item.events.Add(t);
                }


                _taskmap.items.Add(_item);
            }
            Save(taskmapfile, _taskmap);
        }

        public static void SetVmMap(string _vmmapfile, int count, Simulation simulation)
        {
            Map _vmmap = new Map();
            string vmmapfile = _vmmapfile + @"\Sulotion_" + count.ToString() + ".xml";

            foreach (var cpu in simulation.Environment.Cpus)
            {
                foreach (var core in cpu.Cores)
                {
                    Map.Item _item = new Map.Item();
                    List<Slot> Slots = new List<Slot>();
                    _item.OwnerName = "Dyn_" + cpu.Id.ToString() + "_" + core.Id.ToString(); ;
                    _item.CpuId = cpu.Id;
                    _item.CoreId = core.Id;
                    _item.Space = 70;
                    foreach (var job in core.Tasks)
                    {
                        int nextStep = 0;

                        for (int i = 0; i < job.ExecutionTrace.Count; i++)
                        {
                            if (i == nextStep)
                            {
                                Slot temp = new Slot();
                                temp.start = job.ExecutionTrace[i].Cycle;
                                nextStep = job.ExecutionTrace.Count;
                                temp.end = job.ExecutionTrace[(job.ExecutionTrace.Count - 1)].Cycle;
                                temp.end += job.ExecutionTime - (temp.end - temp.start);
                                for (int j = i ; j < (job.ExecutionTrace.Count-1); j++)
                                {
                                    int dur = Math.Abs(job.ExecutionTrace[j].Cycle - job.ExecutionTrace[j + 1].Cycle);
                                    if (dur > core.MacroTick)
                                    {
                                        temp.end = job.ExecutionTrace[j].Cycle;
                                        temp.end += job.ExecutionTime - (temp.end - temp.start);
                                        nextStep = j+1;
                                        break;
                                    }
                                }
                                Slots.Add(temp);
                            }
                        }


                    }
                    //Sort SLots
                    Slots.Sort((x, y) => x.start.CompareTo(y.start));

                    foreach (var slot in Slots)
                    {
                        Map.Item.Event t = new Map.Item.Event();
                        t.Start = slot.start;
                        t.End = slot.end;
                        _item.events.Add(t);
                    }
                    _item.Duration = Slots.LastOrDefault().end;
                    _vmmap.items.Add(_item);
                }
            }
            Save(vmmapfile, _vmmap);
        }

        public class Slot
        {
            public Slot()
            {

            }

            public int start;
            public int end;
        }


    }
}
