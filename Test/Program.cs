using Planner.BaseClasses;
using Planner.Objects;
using Planner.Objects.Models;
using System;
using System.Collections.Generic;

namespace Test
{
    class Program
    {
        static List<Simulation> RunSimulatedAnnealing(Simulation s, int duration)
        {
            SimulatedAnnealing SA = HeuristicFactory.CreateSA(s, duration, SimulatedAnnealingBase.DebugLevel.All);
            SA.Run();
            return SA.BestSolutions;
        }



        static void Main(string[] args)
        {
            int duration = 2200000;
            duration = 5000;
            string file = @"Data\Tasks.xml";
            string config = @"Data\Config.xml";
            string outFile = @"Data\Results";


            (Workload work, Configuration cfg) scheme = DataLoader.Load(file, config);
            Simulation s = HeuristicFactory.Load(scheme.work, scheme.cfg);
            Console.WriteLine(s);
            //Simulation s2 = RunSimulatedAnnealing(s, 1500000);
            List<Simulation> bestSolutions = RunSimulatedAnnealing(s, duration);
            DataLoader.Unload(outFile, bestSolutions);
            //Schedule schedule = s2.GetSchedule();
            //schedule.Run();
            //schedule.Build("schedule.txt", "test.svg");
            //Console.Write(schedule.Output);
            //Console.WriteLine(s2);
            //Console.WriteLine(s2.Fitness.Score);
            Console.WriteLine("Hit a key to Exit");
            Console.ReadKey();
        }


    }
}
