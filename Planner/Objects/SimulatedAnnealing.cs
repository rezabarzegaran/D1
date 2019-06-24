using Planner.BaseClasses;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using Accord.IO;

namespace Planner.Objects
{
    /// <summary>
    /// This class represents the typical simulated annealing implementation.
    /// </summary>
    public class SimulatedAnnealing : SimulatedAnnealingBase
    {
        private readonly double _coolingRate;
        private readonly int _iterations;

        /// <summary>
        /// Returns an instance of the entity, allowing several parameters to be set that alters the execution of the algorithm.
        /// </summary>
        /// <param name="initialSolution">The initial solution from which the SA algorithm starts the optimization from.</param>
        /// <param name="duration">Time in miliseconds before the algorithm terminates.</param>
        /// <param name="initialTemperature">The initial temperature.</param>
        /// <param name="coolingRate">The cooling rate r which defines the exponential decay rate of the temperature: a(1-r)^n </param>
        /// <param name="iterations">This parameter allows the algorithm to run n number of iterations before the temperature is lowered.</param>
        /// <param name="debugLevel">This parameter allows for logging various values during the run of the algorithm. This enum is a flag.</param>
        public SimulatedAnnealing(Simulation initialSolution, int duration, double initialTemperature, double coolingRate, int iterations, DebugLevel debugLevel) : base(initialSolution, duration, initialTemperature, debugLevel)
        {
            _coolingRate = coolingRate;
            _iterations = iterations;
            BestSolutions = new List<Simulation>();
        }

        public List<Simulation> BestSolutions { get; set; }

        public override void Run()
        {
            double temp = _initialTemperature;
            _endTime = System.Environment.TickCount + _maxDuration;
            long start = System.Environment.TickCount;
            Simulation currentSolution = _initialSolution;
            currentSolution.Run(true);
            Simulation best = currentSolution;
            BestSolutions.Add(best.DeepClone());
            Console.WriteLine($"Base : {best.Fitness.ControlPenalty} - {best.Fitness.Score} - {best.Fitness.ValidSolution} - {best.Fitness.ViolationCount}");
            Debug(DebugLevel.GeneratedNeighbor, currentSolution.Fitness.Score);
            Debug(DebugLevel.AcceptedNeighbor, currentSolution.Fitness.Score);
            Debug(DebugLevel.BestNeighbor, currentSolution.Fitness.Score);

            //best.Fitness.Score = 49000;
            int TotalItter = 1;
            while (temp > 1 && _endTime > System.Environment.TickCount)
            {
                Debug(DebugLevel.Temperature, temp);
                for (int tries = 0; tries < _iterations; tries++)
                {
                    Simulation candidate = GetNeighbor(currentSolution);
                    TotalItter++;
                    Debug(DebugLevel.GeneratedNeighbor, candidate.Fitness.Score);
                    if (candidate.Fitness.Score < currentSolution.Fitness.Score)
                    {
                        currentSolution = candidate;
                        Debug(DebugLevel.AcceptedNeighbor, candidate.Fitness.Score);
                        if (currentSolution.Fitness.Score < best.Fitness.Score)
                        {
                            best = currentSolution;
                            Console.WriteLine($" Best {TotalItter} : {best.Fitness.ControlPenalty / 50000} - {best.Fitness.Score} - {best.Fitness.ValidSolution} - {best.Fitness.ViolationCount}");
                            BestSolutions.Add(best.DeepClone());
                            Debug(DebugLevel.BestNeighbor, best.Fitness.Score);
                        }
                    }
                    //else if (AcceptanceProbability(currentSolution.Fitness.Score, candidate.Fitness.Score, temp) > _rng.NextDouble())
                    //{
                    //    currentSolution = candidate;
                    //    Debug(DebugLevel.AcceptedNeighbor, candidate.Fitness.Score);
                    //}
                }

                temp *= 1 - _coolingRate;
            }
            BestSolution = best;
            Fitness = best.Fitness;
            ElapsedTime = (int)(System.Environment.TickCount - start);
          

        }

        private Simulation GetNeighbor(Simulation candidate)
        {
                Simulation newCandidate = candidate.Clone();
                switch (_rng.Next(66))
                {
                    case int n when n >= 0 && n < 33:     
                        AdjustDeadline(newCandidate);
                        break;
                    case int n when n >= 33 && n < 66:
                        AdjustOffset(newCandidate, candidate.Fitness.WorstCpu);
                        break;
                    case int n when n >= 66 && n < 100:
                        SwapTasks(newCandidate);
                        break;
                    case int n when n >= 100 && n < 133:
                        AdjustPeriod(newCandidate);
                        break;

                }
                newCandidate.Run(true);

            return newCandidate;


        }
        public bool checkvalidity(Simulation candidate)
        {
            bool inOrder = true;
            
            foreach (var chain in candidate.Evaluator.Chains)
            {
                if (chain.inOrder)
                {
                    int previous_t_finished = -1;
                    int current_t_finished = 0;
                    int current_t_start = 0;
                    foreach (var task in chain.Tasks)
                    {
                        foreach (var job in candidate.SwappableTasks)
                        {
                            if (task.Name == job.Name)
                            {
                                current_t_finished = job.Offset + job.Deadline;
                                current_t_start = job.Offset;
                            }
                        }
                        foreach (var job in candidate.NonSwappableTasks)
                        {
                            if (task.Name == job.Name)
                            {
                                current_t_finished = job.Offset + job.Deadline;
                                current_t_start = job.Offset;
                            }
                        }

                        if ((previous_t_finished >= current_t_finished) || (previous_t_finished >= current_t_start))
                        {
                            inOrder = false;
                        }

                        previous_t_finished = current_t_finished;
                        current_t_finished = 0;
                        current_t_start = 0;


                    }

                }


            }

            if (inOrder)
            {
                return true;
            }
            return inOrder;
        }
    }

}
