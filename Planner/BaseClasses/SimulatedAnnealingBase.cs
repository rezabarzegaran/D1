using Planner.Interfaces;
using Planner.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Planner.Objects.Measurement;

namespace Planner.BaseClasses
{
    public abstract class SimulatedAnnealingBase : IAnnealing
    {
        private static Stopwatch _seed = Stopwatch.StartNew();
        protected readonly double _initialTemperature;
        protected readonly Simulation _initialSolution;
        protected readonly Random _rng;
        protected readonly int _maxDuration;
        protected int _endTime;
        protected readonly DebugLevel _debugLevel;

        protected SimulatedAnnealingBase(Simulation initialSolution, int duration, double initialTemperature, DebugLevel debugLevel)
        {
            _initialTemperature = initialTemperature;
            _maxDuration = duration;
            _debugLevel = debugLevel;
            _initialSolution = initialSolution;
            _rng = new Random((int)_seed.ElapsedTicks);
            Temperatures = new List<(TimeSpan, double)>();
            GeneratedNeighborValues = new List<(TimeSpan, double)>();
            AcceptedNeighborValues = new List<(TimeSpan, double)>();
            BestNeighborValues = new List<(TimeSpan, double)>();
        }

        public Simulation BestSolution { get; protected set; }
        public List<(TimeSpan,double)> Temperatures { get; protected set; }
        public List<(TimeSpan, double)> GeneratedNeighborValues { get; protected set; }
        public List<(TimeSpan, double)> AcceptedNeighborValues { get; protected set; }
        public List<(TimeSpan, double)> BestNeighborValues { get; protected set; }

        public FitnessScore Fitness { get; protected set; }
        public int ElapsedTime { get; protected set; }


        public abstract void Run();

        public void DumpOutput()
        {
            Console.WriteLine(" **** Best fitness values **** ");
            BestNeighborValues.ForEach(x=>Console.WriteLine($"{x.Item1} - {x.Item2}"));
            Console.WriteLine(" **** Current fitness values **** ");
            GeneratedNeighborValues.ForEach(x => Console.WriteLine($"{x.Item1} - {x.Item2}"));
            Console.WriteLine(" **** Temperature **** ");
            Temperatures.ForEach(x => Console.WriteLine($"{x.Item1} - {x.Item2:N1}"));
        }

        protected double AcceptanceProbability(double energy, double newEnergy, double temperature)
        {
            if (newEnergy < energy)
            {
                return 1.0;
            }

            return Math.Exp((energy - newEnergy) / temperature);
        }

        protected void AdjustDeadline(Simulation candidate)
        {
            string failed = candidate.Evaluator.Jitters.Where(x => x.Failed).Select(x => x.OwnerName).FirstOrDefault();
            if (failed != null)
            {
                Job j = candidate.Tasks.FirstOrDefault(x => x.Name == failed);
                j.DeadlineAdjustment = _rng.Next(0, j.MaxDeadlineAdjustment) + j.EarliestActivation;
            }

        }
        protected void AdjustPeriod(Simulation candidate)
        {
            List<Job> NonSwappable = candidate.NonSwappableTasks;
            List<Job> Swappable = candidate.SwappableTasks;

            int cnt = NonSwappable.Count + Swappable.Count;
            int idx = _rng.Next(cnt);

            Job j  = idx < NonSwappable.Count ? NonSwappable[idx] : Swappable[idx - NonSwappable.Count];
            j.Period = j.Periods[_rng.Next(0, j.Periods.Count)];

            string related_chainname = null;
            foreach (var chain in candidate.Evaluator.Chains)
            {
                foreach (var task in chain.Tasks)
                {
                    if (j.Name == task.Name)
                    {
                        related_chainname = chain.Name;
                        break;
                    }
                }

            }

            foreach (var chain in candidate.Evaluator.Chains)
            {
                if (chain.Name == related_chainname)
                {
                    if (chain.inOrder)
                    {
                        foreach (var task in chain.Tasks)
                        {
                            foreach (var job in candidate.NonSwappableTasks)
                            {
                                if (job.Name == task.Name)
                                {
                                    job.Period = j.Period;
                                }
                            }
                            foreach (var job in candidate.SwappableTasks)
                            {
                                if (job.Name == task.Name)
                                {
                                    job.Period = j.Period;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void AdjustOffset(Simulation candidate, int targetCpu = -1)
        {
            List<Job> NonSwappable = candidate.NonSwappableTasks;
            List<Job> Swappable = candidate.SwappableTasks;

            int cnt = NonSwappable.Count + Swappable.Count;
            int idx = _rng.Next(cnt);

            Job j = null;
            if (targetCpu >= 0)
            {
                j = candidate.Tasks.Where(x => x.CpuId == targetCpu).Randomize(_rng).FirstOrDefault();
            }
            if (j == null)
            {
                j = idx < NonSwappable.Count ? NonSwappable[idx] : Swappable[idx - NonSwappable.Count];
            }
            j.Offset = _rng.Next(0, j.MaxOffset);
        }
        protected void SwapTasks(Simulation candidate)
        {
            if (candidate.SwappableTasks.Count > 0)
            {
                Job j2 = null;
                Job j1 = candidate.SwappableTasks[_rng.Next(candidate.SwappableTasks.Count)];
                if ((j2 = SwapWith(candidate.SwappableTasks, j1)) != null)
                {
                    int coreId1 = j1.CoreId;
                    double coreCF1 = j1.CoreCF;
                    j1.SetEnvironment(j2.CoreId, j2.CoreCF);
                    j2.SetEnvironment(coreId1 , coreCF1);
                    j1.Offset = 0;
                    j2.Offset = 0;
                    j1.DeadlineAdjustment = 0;
                    j2.DeadlineAdjustment = 0;
                }
            }
        }
        protected Job SwapWith(List<Job> candidates, Job job)
        {
            return candidates.FirstOrDefault(j => j != job && j.CoreId != job.CoreId && j.CpuId == job.CpuId);
        }

        protected void Debug(DebugLevel level, double value, bool log = true)
        {
            if (!log) return;
            if (_debugLevel != DebugLevel.None && (_debugLevel & level) == level)
            {
                switch (level)
                {
                    case DebugLevel.Temperature:
                        Temperatures.Add((_seed.Elapsed, value));
                        break;
                    case DebugLevel.GeneratedNeighbor:
                        GeneratedNeighborValues.Add((_seed.Elapsed, value));
                        break;
                    case DebugLevel.AcceptedNeighbor:
                        AcceptedNeighborValues.Add((_seed.Elapsed, value));
                        break;
                    case DebugLevel.BestNeighbor:
                        BestNeighborValues.Add((_seed.Elapsed, value));
                        break;
                }                
            }
        }
        public enum DebugLevel
        {
            None = 0,
            Temperature = 1,
            GeneratedNeighbor = 2,
            AcceptedNeighbor = 4,
            BestNeighbor = 8,
            All = 15
        };
    }
}
