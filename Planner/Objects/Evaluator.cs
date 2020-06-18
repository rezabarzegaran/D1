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
        private readonly double _w6;
        private int _completedChains;
        public bool validchains;

        public Evaluator(double w2, double w3, double w4, double w5, double w6)
        {
            _w1 = 1_000;
            _w2 = _w1 * w2;
            _w3 = _w1 * w3;
            _w4 = _w1 * w4;
            _w5 = _w1 * w5;
            _w6 = _w1 * w6;
            eventMap = new Dictionary<string, List<IMeasurement>>();

            Deadlines = new List<Deadline>();
            Jitters = new List<JitterBase>();
            Apps = new List<Application>();
            Schedules = new List<CoreSchedule>();

        }
        public Evaluator(double w2, double w3, double w4, double w5, double w6, List<Application> apps, List<Deadline> deadlines, List<JitterBase> jitters, List<CoreSchedule> schedules)
        {

            _w1 = 1_0_000;
            _w2 = w2;
            _w3 = w3;
            _w4 = w4;
            _w5 = w5;
            _w6 = w6;
            eventMap = new Dictionary<string, List<IMeasurement>>();
            Apps = new List<Application>();

            foreach (var app in apps)
            {
                Apps.Add(new Application(app));
            }

            Deadlines = new List<Deadline>();
            foreach (var d in deadlines)
            {
                Deadlines.Add(new Deadline(d));
            }



            Jitters = new List<JitterBase>();

            Schedules = new List<CoreSchedule>();
            foreach (var s in schedules)
            {
                Schedules.Add(new CoreSchedule(s));
            }

        }

        public List<Application> Apps { get; }
        public List<Deadline> Deadlines { get; }
        public List<JitterBase> Jitters { get; }
        public List<CoreSchedule> Schedules { get; }
        public double MaxValidScore => _w1;
        public bool Continue { get; private set; }

        public FitnessScore GetScore(MLApp.MLApp matlab)
        {
            return new FitnessScore(_w1, _w2, _w3, _w4, _w5, _w6, Apps, Deadlines, Jitters, Schedules, matlab);
        }
        public void Reset()
        {
            Continue = Apps.Where(x => x.InOrder).ToList().Count > 0; // Should only be true if we have chains.
            _completedChains = 0;
            Apps.ForEach(x => x.Reset());
            Deadlines.ForEach(x => x.Reset());
            Jitters.ForEach(x => x.Reset());
            Schedules.ForEach(x => x.Reset());
        }
        public void EvalApplication(string name, IEnumerable<Job> tasks, int threshold, bool inorder, bool ca)
        {
            Application tc = new Application(name, threshold, inorder, ca);
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
            Apps.Add(tc);
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
        public void EvalPartitions(int cpuid, int coreid, IEnumerable<Job> tasks)
        {
            CoreSchedule tc = new CoreSchedule(cpuid, coreid);
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
            Schedules.Add(tc);
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
                    if (measurement is Application app)
                    {
                        if (app.InOrder)
                        {
                            bool couldComplete = !app.Completed;
                            measurement.EndTask(job, cycle);
                            if (couldComplete && app.Completed)
                            {
                                completedChains++;
                            }
                        }

                    }
                    else
                    {
                        measurement.EndTask(job, cycle);
                    }
                }
                // Determine the number of unfinished chains
                if (completedChains >= 0)
                {
                    _completedChains += completedChains;
                    if (_completedChains == Apps.Where(x => x.InOrder).ToList().Count) Continue = false;
                }
            }
        }

        public Evaluator DeepClone()
        {
            return new Evaluator(_w2, _w3, _w4, _w5, _w6, Apps, Deadlines, Jitters, Schedules);
        }

    }
}
