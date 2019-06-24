using Planner.BaseClasses;
using Planner.Objects.Models;
using System;
using System.Collections.Generic;

namespace Planner.Objects
{
    public static class HeuristicFactory
    {
        public const double InitialTemperatureScaling = 6;
        public const double E2EWeight = 1;
        public const double DeadlineWeight = 1;
        public const double JitterWeight = 1;
        public const double QoCWeight = 0;
        public static int MinutesToMiliSeconds(int min) => min * 60 * 1000;
        public static int SecondsToMiliSeconds(int sec) => sec * 1000;

        public static class SA
        {
            private const double window = 70000;
            public const double CR = 0.0739;
            public const int NrIterations = 10;
            private static double Scale(int durationMiliSeconds, int nrIterations) => (window / durationMiliSeconds);
            public static double CoolingRate(int durationMiliSeconds, int nrIterations, double coolingRate) => coolingRate * Scale(durationMiliSeconds, nrIterations);
        }
        

        public static SimulatedAnnealing CreateSA(string file, string config, int durationMiliseconds, SimulatedAnnealingBase.DebugLevel debugLevel)
        {
            Simulation simulation = Load(file, config);
            return CreateSA(simulation, durationMiliseconds, debugLevel);
        }
        public static SimulatedAnnealing CreateSA(Simulation simulation, int durationMiliseconds, SimulatedAnnealingBase.DebugLevel debugLevel)
        {
            double initialTemperature = simulation.Evaluator.MaxValidScore * InitialTemperatureScaling;
            double cr = SA.CoolingRate(durationMiliseconds, SA.NrIterations, SA.CR);
            return new SimulatedAnnealing(simulation, durationMiliseconds, initialTemperature, cr, SA.NrIterations, debugLevel);
        }

        public static IEnumerable<SimulatedAnnealing> CreateSASet(string file, string config, int durationMiliseconds, SimulatedAnnealingBase.DebugLevel debugLevel)
        {
            while (true)
            {
                Simulation s = Load(file, config);
                yield return CreateSA(s, durationMiliseconds, debugLevel);
            }
        }
        public static IEnumerable<SimulatedAnnealing> CreateSASet(Func<Simulation> simulation, int durationMiliseconds, SimulatedAnnealingBase.DebugLevel debugLevel)
        {
            while (true)
            {
                yield return CreateSA(simulation(), durationMiliseconds, debugLevel);
            }
        }

        public static IEnumerable<Greedy> CreateGreedySet(Func<Simulation> simulation)
        {
            while (true)
            {
                yield return new Greedy(simulation());
            }
        }
        public static Simulation Load(string tasks, string architechture)
        {
            var config = DataLoader.Load(tasks, architechture);
            return Load(config.Item1, config.Item2);
        }
        public static Simulation Load(Workload workload, Configuration configuration)
        {
            return new Simulation(workload, configuration, E2EWeight, DeadlineWeight, JitterWeight, QoCWeight);
        }
    }
}
