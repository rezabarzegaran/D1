using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Planner.Objects;
using Planner.Objects.Models;

namespace PlanningTests
{
    [TestClass]
    public class Fitness
    {
        // Deadline is expected to fail for the second task due to excessive macrotick
        [TestMethod]
        public void Fitness_Deadline_Failed()
        {
            int macrotick = 4;

            Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
            Workload workload = WorkloadGenerator.CreateWorkload();
            workload.CreateTask(0, 0, 0, 5, 10, 10, 0);
            workload.CreateTask(1, 0, 0, 2, 4, 4, 0);

            Simulation simulation = new Simulation(workload, configuration, 1,1,1, 1);
            simulation.Run(true);
            Assert.IsTrue(simulation.Fitness.Score > simulation.Evaluator.MaxValidScore);
            Assert.IsTrue(simulation.Fitness.DeadlinePenalty > 0);
            Assert.AreEqual(0.0, simulation.Fitness.E2EPenalty);
            Assert.AreEqual(0.0, simulation.Fitness.JitterPenalty);
        }
        // Same case as before, but with a lower macrotick.
        // Deadline is not expected to fail for the second task as the macrotick has been lowered.
        [TestMethod]
        public void Fitness_Deadline_Succeeded()
        {
            int macrotick = 2;

            Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
            Workload workload = WorkloadGenerator.CreateWorkload();
            workload.CreateTask(0, 0, 0, 5, 10, 10, 0);
            workload.CreateTask(1, 0, 0, 2, 4, 4, 0);

            Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
            simulation.Run(true);
            Assert.IsTrue(simulation.Fitness.Score < simulation.Evaluator.MaxValidScore);
            Assert.AreEqual(0.0, simulation.Fitness.E2EPenalty);
            Assert.AreEqual(0.0, simulation.Fitness.JitterPenalty);
            Assert.AreEqual(0.0, simulation.Fitness.DeadlinePenalty);
        }

        // Maxjitter is set to 0
        [TestMethod]
        public void Fitness_Jitter_Succeeded()
        {
            int macrotick = 1;

            Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
            Workload workload = WorkloadGenerator.CreateWorkload();
            workload.CreateTask(0, 0, 0, 5, 10, 10, 0, 1);
            workload.CreateTask(1, 0, 0, 2, 4, 4, 0, 2);

            Simulation simulation = new Simulation(workload, configuration,1,1,1, 1);
            simulation.Run(true);
            Assert.IsTrue(simulation.Fitness.Score < simulation.Evaluator.MaxValidScore);
            Assert.AreEqual(0.0, simulation.Fitness.E2EPenalty);
            Assert.AreEqual(0.0, simulation.Fitness.JitterPenalty);
            Assert.AreEqual(0.0, simulation.Fitness.DeadlinePenalty);
        }

        [TestMethod]
        public void Fitness_Jitter_Failed()
        {
            int macrotick = 1;

            Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
            Workload workload = WorkloadGenerator.CreateWorkload();
            workload.CreateTask(0, 0, 0, 5, 10, 10, 0, 1);
            workload.CreateTask(1, 0, 0, 2, 4, 4, 0, 1);

            Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
            simulation.Run(true);
            Assert.IsTrue(simulation.Fitness.Score > simulation.Evaluator.MaxValidScore);
            Assert.IsTrue(simulation.Fitness.JitterPenalty > 0);
            Assert.AreEqual(0.0, simulation.Fitness.E2EPenalty);
            Assert.AreEqual(0.0, simulation.Fitness.DeadlinePenalty);
        }
    }
}
