using Planner.Objects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Planner.Objects.Measurement;

namespace Planner.Objects
{
    /// <summary>
    /// This entity represents an optimized EDF schedule simulation.
    /// That is, it simulated a EDF schedule but skips cycles inbetween
    /// events.
    /// </summary>
    public class Simulation
    {
        public Simulation(Workload workload, Configuration configuration, double w2, double w3, double w4, double w5, double w6)
        {
            Environment = new Environment(configuration);
            SwappableTasks = new List<Job>();
            NonSwappableTasks = new List<Job>();
            Evaluator = new Evaluator(w2, w3, w4, w5, w6);
            Applications = new List<Application>();
            Matlab = new MLApp.MLApp();
            Matlab.Execute("cd " + matlabexecutefilename);

            Dictionary<string, Job> tasks = CreateJobsFromWorkload(workload);
            // Configure constraints.
            foreach (Workload.TaskChain chain in workload.Work.Chains)
            {
                Evaluator.EvalE2E(chain.Name, chain.Runnables.Select(x => tasks[x.Name]), chain.EndToEndDeadline, chain.Priority);
            }
            foreach (Job task in tasks.Values)
            {
                if (task.JitterThreshold != -1) Evaluator.EvalJitter(task);
                Evaluator.EvalDeadline(task);
                Evaluator.EvalTaskMap(task);
                if (task.HasCoreAffinity)
                {
                    NonSwappableTasks.Add(task);
                }
                else
                {
                    SwappableTasks.Add(task);
                }
            }
            int applicationcounter = 0;
            foreach (Workload.App app in workload.Work.Applications)
            {
                Applications.Add(new Application(applicationcounter, app.Name, app.CA, app.Inorder,
                    app.Runnables.Select(x => tasks[x.Name])));
                Evaluator.EvalAppMap(app.Name, app.Runnables.Select(x => tasks[x.Name]));
                if (app.Inorder) Evaluator.EvalOrder(app.Name, app.Runnables.Select(x => tasks[x.Name]));
                if (app.CA) Evaluator.EvalQoC(app.Name, app.Runnables.Select(x => tasks[x.Name]));

                applicationcounter++;
            }

            foreach (var CPU in Environment.Cpus)
            {
                foreach (var core in CPU.Cores)
                {
                    Evaluator.EvalCoreMap(CPU.Id, core.Id, Tasks.ToList());
                }
            }
        }
        public Simulation(Environment environment, List<Job> nonSwappable, List<Job> swappable, Evaluator evaluator, List<Application> applications, MLApp.MLApp matlab)
        {
            Environment = environment;
            NonSwappableTasks = nonSwappable;
            SwappableTasks = swappable;
            Evaluator = evaluator;
            Applications = applications;
            Matlab = matlab;
            Matlab.Execute("cd " + matlabexecutefilename);
        }

        public Simulation(Environment environment, List<Job> nonSwappable, List<Job> swappable, Evaluator evaluator, List<Application> applications, FitnessScore fitness, MLApp.MLApp matlab)
        {
            Environment = environment;
            NonSwappableTasks = nonSwappable;
            SwappableTasks = swappable;
            Evaluator = evaluator;
            Applications = applications;
            Fitness = fitness;
            Matlab = matlab;
            Matlab.Execute("cd " + matlabexecutefilename);
        }

        private string matlabexecutefilename = @"C:\Users\mohba\Documents\GitHub\JitterTime\Code";
        public List<Job> SwappableTasks { get; }
        public List<Job> NonSwappableTasks { get; }
        public IEnumerable<Job> Tasks => SwappableTasks.Concat(NonSwappableTasks);
        public Evaluator Evaluator { get; }
        public FitnessScore Fitness { get; private set; }
        public Environment Environment { get; }
        public List<Application> Applications { get; }
        public MLApp.MLApp Matlab = null;

        /// <summary>
        /// Run the simulation with the parameter indicating if the execution trace should be built.
        /// </summary>
        public void Run(bool debug)
        {

            PriorityQueue<TaskEvent> taskEvents = new PriorityQueue<TaskEvent>(OrderByCycle);
            AssignJobs();
            Initialize();
            Environment.Initialize();
            long hyperperiod = (long)2 * Environment.Hyperperiod;
            hyperperiod = SetHyperperiod(hyperperiod) + Tasks.Max(x => x.Offset);

            Evaluator.Reset();

            foreach (Processor cpu in Environment.Cpus)
            {
                foreach (Core core in cpu.Cores)
                {
                    taskEvents.Enqueue(core.Event);
                }
            }

            while (taskEvents.Count() > 0)
            {
                TaskEvent te = taskEvents.Dequeue();
                // Continue enqueuing events as longs as we are
                // within the hyperperiod, or we have unfinished chains
                if (te.Cycle < hyperperiod || Evaluator.Continue)
                {
                    te.Trigger(debug);
                    taskEvents.Enqueue(te);
                }
            }
            Fitness = Evaluator.GetScore(Matlab);
        }
        public Simulation Clone()
        {
            return new Simulation(Environment.Clone(), CopyJobs(NonSwappableTasks), CopyJobs(SwappableTasks), Evaluator, CopyApps(Applications), Matlab);
        }

        public Simulation DeepClone()
        {
            return new Simulation(Environment.DeepClone(), DeepCopyJobs(NonSwappableTasks), DeepCopyJobs(SwappableTasks), DeepCopyEvaluator(Evaluator), DeepCopyApps(Applications), Fitness, Matlab);

        }
        /// <summary>
        /// Returns an instance of a EDF schedule based on the current taskset configuration.
        /// </summary>
        /// <returns></returns>
        public Schedule GetSchedule()
        {
            return new Schedule(this);
        }
        public Workload GetWorkload()
        {
            Workload workload = new Workload();
            workload.Work = new Workload.WorkSchedule();
            workload.Work.Items = new List<Workload.Task>();
            workload.Work.Chains = new List<Workload.TaskChain>();

            foreach (Job task in Environment.Cpus.SelectMany(cpu => cpu.Cores.SelectMany(core => core.Tasks)))
            {
                workload.Work.Items.Add(new Workload.Task()
                {
                    Name = task.Name,
                    Id = task.Id,
                    BCET = task.ExecutionTime,
                    WCET = task.ExecutionTime,
                    CpuId = task.CpuId,
                    CoreId = task.CoreId,
                    EarliestActivation = task.EarliestActivation,
                    MaxJitter = task.JitterThreshold,
                    Deadline = task.Deadline,
                    Periods = task.Periods.Select(x => new Workload.Period { Value = x }).ToList(),
                    Offset = task.Offset,
                    DeadlineAdjustment = task.DeadlineAdjustment
                });
            }

            foreach (TaskChain chain in this.Evaluator.Chains)
            {
                Workload.TaskChain taskChain = new Workload.TaskChain();
                taskChain.Name = chain.Name;
                taskChain.EndToEndDeadline = chain.Threshold;
                taskChain.Priority = chain.Priority;
                taskChain.Runnables = new List<Workload.Runnable>();
                foreach (TaskChain.VirtualTask task in chain.Tasks)
                {
                    taskChain.Runnables.Add(new Workload.Runnable() { Name = task.Name });
                }
                workload.Work.Chains.Add(taskChain);
            }
            return workload;
        }
        public override string ToString()
        {
            return Tasks.Select(t => $"{t}\n").Aggregate((t1, t2) => t1 + t2);
        }

        private Dictionary<string, Job> CreateJobsFromWorkload(Workload workload)
        {
            Dictionary<string, Job> jobs = new Dictionary<string, Job>();
            Random rng = new Random(System.Environment.TickCount);
            foreach (Workload.Task ti in workload.Work.Items)
            {
                double CORECF = 1;
                foreach (var cpu in Environment.Cpus)
                {
                    if (cpu.Id == ti.CpuId)
                    {
                        foreach (var core in cpu.Cores)
                        {
                            if (core.Id == ti.CoreId)
                            {
                                CORECF = core._CF;
                            }
                        }
                    }

                }



                
                int et = rng.Next(ti.BCET, ti.WCET);
                List<int> periods = ti.Periods.Select(x => x.Value).ToList();
                Job j = new Job(ti.Id, ti.Name, periods.First(), ti.Deadline, et, ti.EarliestActivation, ti.MaxJitter, ti.CpuId, ti.CoreId, CORECF, Evaluator, periods, ti.Cil);
                j.Offset = ti.Offset;
                j.DeadlineAdjustment = ti.DeadlineAdjustment;
                jobs.Add(ti.Name, j);
            }

            return jobs;
        }
        private void AssignJobs()
        {
            NonSwappableTasks.ForEach(x => Environment.QueueJob(x));
            SwappableTasks.ForEach(x => Environment.QueueJob(x));
        }
        private List<Job> CopyJobs(IEnumerable<Job> joblist)
        {
            return joblist.Select(t =>
            {
                Job j = t.Clone();
                j.Offset = t.Offset;
                j.DeadlineAdjustment = t.DeadlineAdjustment;
                j.SetEnvironment(t.CoreId , t.CoreCF);
                return j;
            }).ToList();
        }
        private List<Job> DeepCopyJobs(IEnumerable<Job> joblist)
        {
            return joblist.Select(t =>
            {
                Job j = t.DeepClone();
                j.Offset = t.Offset;
                j.DeadlineAdjustment = t.DeadlineAdjustment;
                j.SetEnvironment(t.CoreId, t.CoreCF);
                return j;
            }).ToList();
        }
        private Evaluator DeepCopyEvaluator(Evaluator evaluator)
        {
            return evaluator.clone();
        }

        private List<Application> CopyApps(IEnumerable<Application> applist)
        {
            return applist.Select(t =>
            {
                Application j = t.Clone();
                return j;
            }).ToList();
        }
        private List<Application> DeepCopyApps(IEnumerable<Application> applist)
        {
            return applist.Select(t =>
            {
                Application j = t.DeepClone();
                return j;
            }).ToList();
        }
        private int OrderByCycle(TaskEvent te1, TaskEvent te2)
        {
            if (te1.Cycle > te2.Cycle) return 1;
            if (te1.Cycle == te2.Cycle) return 0;
            return -1;
        }
        public void Initialize()
        {
            List<Job> AllTasks = Tasks.ToList();
            foreach (Deadline deadline in Evaluator.Deadlines)
            {
                Job current = AllTasks.First(x => x.Name == deadline.OwnerName);
                deadline.SetEnvironment(current.Period, current.CpuId, current.CoreId);
            }

            foreach (TaskMap map in Evaluator.TaskMaps)
            {
                Job current = AllTasks.First(x => x.Name == map.OwnerName);
                map.SetEnvironment(current.Period, current.CpuId, current.CoreId);
            }

            foreach (AppMap map in Evaluator.AppMaps)
            {
                foreach (var task in map.Tasks)
                {
                    Job current = AllTasks.First(x => x.Name == task.Name);
                    map.SetEnvironment(current.Name, current.CpuId, current.CoreId);
                }

            }

            foreach (var coreMap in Evaluator.CoreMaps)
            {
                coreMap.CalcSeparation();
            }


        }

        public long SetHyperperiod(long current)
        {
            long Controlhyperperiod = 1;
            int minperiod = Int32.MaxValue;
            foreach (Application app in Applications)
            {
                if (app.CA)
                {
                    Job foundtask = Tasks.First(x => x.Name == app.Tasks[0].Name);
                    int period = foundtask.Period;
                    if (minperiod > period)
                    {
                        minperiod = period;
                    }

                    //foreach (Application.ApplicationTasks task in app.Tasks)
                    //{
                    //    Job foundtask = Tasks.First(x => x.Name == task.Name);
                    //    if (foundtask != null)
                    //    {
                    //        int period = foundtask.Period;
                    //        Controlhyperperiod = Extensions.LeastCommonMultiple(Controlhyperperiod, period);

                    //    }
                    //}
                    
                }
                
            }

            int mintimes = Convert.ToInt32((current / minperiod));
            int coefficient = 1 + 160/mintimes;

            return coefficient * current;
            //Controlhyperperiod *= 45;

            //return Extensions.LeastCommonMultiple(Controlhyperperiod, current);




        }
    }
}
