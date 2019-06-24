using Planner.Interfaces;
using Planner.Objects.Measurement;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Planner.Objects
{
    /// <summary>
    /// This allows the simulation to evaluate the fitness and constraint violations iteratively.
    /// </summary>
    public class Evaluator
    {
        private readonly Dictionary<string, List<IMeasurement>> eventMap;
        private readonly double _w1;
        private readonly double _w2;
        private readonly double _w3;
        private readonly double _w4;
        private readonly double _w5;
        private int _completedChains;
        public bool validchains;

        public Evaluator(double w2, double w3, double w4, double w5)
        {
            _w1 = 1_0_000;
            _w2 = _w1 * w2;
            _w3 = _w1 * w3;
            _w4 = _w1 * w4;
            _w5 = _w1 * w5;
            eventMap = new Dictionary<string, List<IMeasurement>>();
            Chains = new List<TaskChain>();
            Deadlines = new List<Deadline>();
            TaskMaps = new List<TaskMap>();
            AppMaps = new List<AppMap>();
            Jitters = new List<JitterBase>();
            Orders = new List<Order>();
            ControlCost = new List<CoC>();

        }
        public Evaluator(List<TaskChain> chains, List<Deadline> deadlines, List<CoC> coc, List<TaskMap> taskmap )
        {

            //eventMap = new Dictionary<string, List<IMeasurement>>();
            Chains = new List<TaskChain>();
            foreach (var chain in chains)
            {
                TaskChain tc = new TaskChain(chain.Name, chain.Threshold, chain.Priority, chain.inOrder);
                tc.E2E = chain.E2E;
                Chains.Add(tc);
            }
            
            
            Deadlines = new List<Deadline>();
            foreach (var deadline in deadlines)
            {
                Deadline d = new Deadline(deadline.Period, deadline.OwnerName,deadline.OwnerCpu,deadline.OwnerCore);
                d.MaxDistance = deadline.MaxDistance;
                Deadlines.Add(d);
            }

            TaskMaps = new List<TaskMap>();
            foreach (var map in taskmap)
            {
                TaskMap m = new TaskMap(map.Period,map.OwnerName,map.OwnerCpu,map.OwnerCore, map.Starts, map.Ends);
                TaskMaps.Add(m);

            }


            //AppMaps = new List<AppMap>();
            //Jitters = new List<JitterBase>();
            //Orders = new List<Order>();


            ControlCost = new List<CoC>();

            foreach (var ctrl in coc)
            {
                CoC tc = new CoC(ctrl.Name);
                tc.Cost = ctrl.Cost;
                ControlCost.Add(tc);
            }

        }

        public List<TaskChain> Chains { get; }
        public List<Deadline> Deadlines { get; }
        public List<JitterBase> Jitters { get; }
        public List<TaskMap> TaskMaps { get; }
        public List<AppMap> AppMaps { get; }
        public List<Order> Orders { get; }
        public List<CoC> ControlCost { get; }
        public double MaxValidScore => _w1;
        public bool Continue { get; private set; }

        public FitnessScore GetScore(MLApp.MLApp matlab)
        {
            return new FitnessScore(_w1, _w2, _w3, _w4,  _w5, Chains, Deadlines, Jitters, Orders, ControlCost, matlab);
        }
        public void Reset()
        {
            Continue = Chains.Count > 0; // Should only be true if we have chains.
            _completedChains = 0;
            Chains.ForEach(x => x.Reset());
            Deadlines.ForEach(x => x.Reset());
            TaskMaps.ForEach(x => x.Reset());
            AppMaps.ForEach(x => x.Reset());
            Jitters.ForEach(x => x.Reset());
            Orders.ForEach(x => x.Reset());
            ControlCost.ForEach(x => x.Reset());
        }
        public void EvalE2E(string name, IEnumerable<Job> taskchain, int threshold, double priority, bool inorder)
        {
            TaskChain tc = new TaskChain(name, threshold, priority, inorder);
            HashSet<string> uniqueTaskNames = new HashSet<string>();
            foreach (Job task in taskchain)
            {
                tc.AddTask(task);
                if (!eventMap.ContainsKey(task.Name))
                {
                    eventMap.Add(task.Name, new List<IMeasurement>());
                }

                uniqueTaskNames.Add(task.Name);
            }
            uniqueTaskNames.ForEach(tName => eventMap[tName].Add(tc));
            Chains.Add(tc);
        }
        public void EvalDeadline(Job task)
        {
            Deadline deadline = new Deadline(task);
            if (!eventMap.ContainsKey(task.Name))
            {
                eventMap.Add(task.Name, new List<IMeasurement>());
            }
            Deadlines.Add(deadline);
            eventMap[task.Name].Add(deadline);
        }
        public void EvalTaskMap(Job task)
        {
            TaskMap taskmap = new TaskMap(task);
            if (!eventMap.ContainsKey(task.Name))
            {
                eventMap.Add(task.Name, new List<IMeasurement>());
            }
            TaskMaps.Add(taskmap);
            eventMap[task.Name].Add(taskmap);

        }
        public void EvalOrder(string name, IEnumerable<Job> tasks)
        {
            Order tc = new Order(name);
            HashSet<string> uniqueTaskNames = new HashSet<string>();
            foreach (Job task in tasks)
            {
                tc.AddTask(task);
                if (!eventMap.ContainsKey(task.Name))
                {
                    eventMap.Add(task.Name, new List<IMeasurement>());
                }

                uniqueTaskNames.Add(task.Name);
            }
            uniqueTaskNames.ForEach(tName => eventMap[tName].Add(tc));
            Orders.Add(tc);
        }
        public void EvalAppMap(string name, IEnumerable<Job> tasks)
        {
            AppMap tc = new AppMap(name);
            HashSet<string> uniqueTaskNames = new HashSet<string>();
            foreach (Job task in tasks)
            {
                tc.AddTask(task);
                if (!eventMap.ContainsKey(task.Name))
                {
                    eventMap.Add(task.Name, new List<IMeasurement>());
                }

                uniqueTaskNames.Add(task.Name);
            }
            uniqueTaskNames.ForEach(tName => eventMap[tName].Add(tc));
            AppMaps.Add(tc);
        }
        public void EvalQoC(string name, IEnumerable<Job> tasks)
        {
            CoC tc = new CoC(name);
            HashSet<string> uniqueTaskNames = new HashSet<string>();
            foreach (Job task in tasks)
            {
                tc.AddTask(task);
                if (!eventMap.ContainsKey(task.Name))
                {
                    eventMap.Add(task.Name, new List<IMeasurement>());
                }

                uniqueTaskNames.Add(task.Name);
            }
            uniqueTaskNames.ForEach(tName => eventMap[tName].Add(tc));
            ControlCost.Add(tc);
        }
        public void EvalJitter(Job task)
        {
            RelativeJitter jitter = new RelativeJitter(task);
            if (!eventMap.ContainsKey(task.Name))
            {
                eventMap.Add(task.Name, new List<IMeasurement>());
            }
            Jitters.Add(jitter);
            eventMap[task.Name].Add(jitter);
        }
        public void StartTask(Job job, int cycle)
        {
            if (eventMap.ContainsKey(job.Name))
            {
                eventMap[job.Name].ForEach(x => x.StartTask(job, cycle));
            }
        }
        public void EndTask(Job job, int cycle)
        {
            if (eventMap.ContainsKey(job.Name))
            {
                int completedChains = 0;
                foreach (IMeasurement measurement in eventMap[job.Name])
                {
                    if (measurement is TaskChain chain)
                    {
                        bool couldComplete = !chain.Completed;
                        measurement.EndTask(job, cycle);
                        if (couldComplete && chain.Completed)
                        {
                            completedChains++;
                        }
                    }
                    else
                    {
                        measurement.EndTask(job, cycle);
                    }
                }
                // Determine the number of unfinished chains
                if (completedChains > 0)
                {
                    _completedChains += completedChains;
                    if (_completedChains == Chains.Count) Continue = false;
                }
            }
        }

        public Evaluator clone()
        {
            return new Evaluator(Chains, Deadlines, ControlCost, TaskMaps);
        }

    }
}
