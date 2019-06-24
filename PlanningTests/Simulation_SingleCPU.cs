using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Planner.Objects;
using Planner.Objects.Models;
using System.Linq;

namespace PlanningTests
{
    // Single task on single cpu and single core.
    [TestClass]
    public class Simulation_SingleCPU
    {
        [TestClass]
        public class SingleCore
        {

            [TestClass]
            public class SingleTask
            {
                // Tests that all execution cycles are after the earliest activation time.
                [TestMethod]
                public void Success_EarliestActivation()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 50; i++)
                    {
                        int executiontime = rng.Next(15) + 1;
                        int period = 2 * executiontime;
                        int earliestactivation = rng.Next(period - executiontime);
                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                        Workload workload = WorkloadGenerator.CreateWorkload();
                        workload.CreateTask(0, 0, 0, executiontime, period, period, earliestactivation);

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        Job job = simulation.Tasks.First();
                        List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                        Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                        Assert.AreEqual(executiontime, trace[0].Count(), $"Failed to deliver the number of expected execution cycles for task: {job.Name}");
                        Assert.IsTrue(trace[0].First().Cycle == trace[0].First().EarliestActivation, $"Earliest activation exceed for task: {job.Name} at initial cycle");
                        Assert.IsTrue(trace[0].All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");
                    }
                }

                // Tests that given a single task, that all execution slices are consecutive, below the macro tick
                // and accumulated equals the execution time.
                // Consequently this verifies that the macrotick doesn't inadvertantly interfere with the execution.
                [TestMethod]
                public void Success_Macrotick()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 500; i++)
                    {
                        int executiontime = rng.Next(15) + 1;
                        int period = 2 * executiontime;
                        int macrotick = rng.Next(period) + 1;
                        int earliestactivation = rng.Next(period - executiontime);
                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                        Workload workload = WorkloadGenerator.CreateWorkload();
                        workload.CreateTask(0, 0, 0, executiontime, period, period, earliestactivation);

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        Job job = simulation.Tasks.First();
                        List<IGrouping<int, Job.Execution>> trace = job.ExecutionTrace.GroupBy(x => x.Iteration).ToList();
                        Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");

                        Assert.AreEqual(executiontime, trace[0].Select(x => x.ExecutionSize).Sum(), $"Expected cycles mismatch for task : {job.Name},0");
                        Assert.IsTrue(trace[0].All(x => x.ExecutionSize <= macrotick), $"Expected execution skips are too big for task: {job.Name},0");
                        Assert.IsTrue(Utility.VerifyMacrotickCompliance(simulation.Environment, trace[0].ToList(), job), $"Macrotick compliance failed for task: {job.Name},0");

                        Assert.AreEqual(executiontime, trace[1].Select(x => x.ExecutionSize).Sum(), $"Expected cycles mismatch for task : {job.Name},1");
                        Assert.IsTrue(trace[1].All(x => x.ExecutionSize <= macrotick), $"Expected execution skips are too big for task: {job.Name},1");
                        Assert.IsTrue(Utility.VerifyMacrotickCompliance(simulation.Environment, trace[1].ToList(), job), $"Macrotick compliance failed for task: {job.Name},1");
                        Console.WriteLine($"Test: {i}");
                    }
                }

                // Tests that given a single task, that the deadline is met given that T=D.
                // Expect to succeed as no other task are present to preempt the task.
                [TestMethod]
                public void Success_Deadline_1()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 50; i++)
                    {
                        int executiontime = rng.Next(15) + 1;
                        int period = 2 * executiontime;
                        int earliestactivation = rng.Next(period - executiontime);
                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                        Workload workload = WorkloadGenerator.CreateWorkload();
                        workload.CreateTask(0, 0, 0, executiontime, period, period, earliestactivation);

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        Job job = simulation.Tasks.First();
                        List<IGrouping<int, Job.Execution>> trace = job.ExecutionTrace.GroupBy(x => x.Iteration).ToList();
                        Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");

                        Assert.AreEqual(executiontime, trace[0].Count(), $"Failed to deliver the number of expected execution cycles for task: {job.Name}");
                        Assert.AreEqual(period, trace[0].Last().Deadline, $"Failed to adhere to the deadline for task: {job.Name},0");
                        Assert.IsTrue(trace[0].Last().Cycle < trace[0].Last().Deadline, $"Failed to meet expected deadline to be met for task: {job.Name},0 at last cycle.");
                        Assert.IsTrue(Utility.VerifyDeadlines(trace[0].Last(), job), $"Deadline failed for task: {job.Name},0.");

                        Assert.AreEqual(executiontime, trace[1].Count(), $"Failed to deliver the number of expected execution cycles for task: {job.Name}");
                        Assert.AreEqual(period * 2, trace[1].Last().Deadline, $"Failed to adhere to the deadline for task: {job.Name},1");
                        Assert.IsTrue(trace[1].Last().Cycle < trace[1].Last().Deadline, $"Failed to meet expected deadline to be met for task: {job.Name},1 at last cycle.");
                        Assert.IsTrue(Utility.VerifyDeadlines(trace[1].Last(), job), $"Deadline failed for task: {job.Name},1.");
                    }
                }

                // Tests that given a single task, that the deadline is met given that T<D.
                [TestMethod]
                public void Success_Deadline_2()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 50; i++)
                    {
                        int executiontime = rng.Next(15) + 1;
                        int period = 2 * executiontime;
                        int deadline = rng.Next(period - executiontime) + executiontime;
                        int earliestactivation = rng.Next(deadline - executiontime);
                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                        Workload workload = WorkloadGenerator.CreateWorkload();
                        workload.CreateTask(0, 0, 0, executiontime, period, deadline, earliestactivation);

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        Job job = simulation.Tasks.First();
                        List<IGrouping<int, Job.Execution>> trace = job.ExecutionTrace.GroupBy(x => x.Iteration).ToList();
                        Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");

                        Assert.AreEqual(executiontime, trace[0].Count(), $"Failed to deliver the number of expected execution cycles for task: {job.Name}");
                        Assert.AreEqual(deadline, trace[0].Last().Deadline, $"Failed to adhere to the deadline for task: {job.Name},0");
                        Assert.IsTrue(trace[0].Last().Cycle < trace[0].Last().Deadline, $"Failed to meet expected deadline to be met for task: {job.Name},0 at last cycle.");
                        Assert.IsTrue(Utility.VerifyDeadlines(trace[0].Last(), job), $"Deadline failed for task: {job.Name},0.");

                        Assert.AreEqual(executiontime, trace[1].Count(), $"Failed to deliver the number of expected execution cycles for task: {job.Name}");
                        Assert.AreEqual(period + deadline, trace[1].Last().Deadline, $"Failed to adhere to the deadline for task: {job.Name},1");
                        Assert.IsTrue(trace[1].Last().Cycle < trace[1].Last().Deadline, $"Failed to meet expected deadline to be met for task: {job.Name},1 at last cycle.");
                        Assert.IsTrue(Utility.VerifyDeadlines(trace[1].Last(), job), $"Deadline failed for task: {job.Name},1.");
                    }
                }

                // Tests that given a single task, that the release times are coherent.
                [TestMethod]
                public void Success_Release()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 50; i++)
                    {
                        int executiontime = rng.Next(15) + 1;
                        int period = 2 * executiontime;
                        int earliestactivation = rng.Next(period - executiontime);
                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                        Workload workload = WorkloadGenerator.CreateWorkload();
                        workload.CreateTask(0, 0, 0, executiontime, period, period, earliestactivation);

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        Job job = simulation.Tasks.First();
                        List<IGrouping<int, Job.Execution>> trace = job.ExecutionTrace.GroupBy(x => x.Iteration).ToList();
                        Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");

                        Assert.AreEqual(executiontime, trace[0].Count(), $"Failed to deliver the number of expected execution cycles for task: {job.Name}");
                        Assert.AreEqual(earliestactivation, trace[0].First().Cycle, $"Failed to release task: {job.Name},0 at last expected cycle.");

                        Assert.AreEqual(executiontime, trace[1].Count(), $"Failed to deliver the number of expected execution cycles for task: {job.Name}");
                        Assert.AreEqual(period + earliestactivation, trace[1].First().Cycle, $"Failed to release task: {job.Name},1 at last expected cycle.");
                    }
                }

                // Macrotick < ExecutionTime < Period
                [TestMethod]
                public void Success_ConcreteTask_1()
                {
                    int executiontime = 14;
                    int period = 20;
                    int deadline = 20;
                    int earliestactivation = 2;
                    int macrotick = 6;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, executiontime, period, deadline, earliestactivation);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (2, 6), (8, 6), (14, 2)));
                    Assert.IsTrue(trace[0].All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");
                }

                // ExecutionTime < Macrotick < Period
                [TestMethod]
                public void Success_ConcreteTask_2()
                {
                    int executiontime = 14;
                    int period = 20;
                    int deadline = 20;
                    int earliestactivation = 2;
                    int macrotick = 16;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, executiontime, period, deadline, earliestactivation);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (2, 14)));
                    Assert.IsTrue(trace[0].All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");
                }

                // Macrotick < ExecutionTime < Period
                [TestMethod]
                public void Success_ConcreteTask_3()
                {
                    int executiontime = 4;
                    int period = 10;
                    int deadline = 10;
                    int earliestactivation = 2;
                    int macrotick = 1;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, executiontime, period, deadline, earliestactivation);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (2, 1), (3, 1), (4, 1), (5, 1)));
                    Assert.IsTrue(trace[0].All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");
                }

                // Tests that both deadlines, activationtimes, macroticks and E2E constraints are met.
                [TestMethod]
                public void Success_Accumulated()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 50; i++)
                    {
                        int executiontime = rng.Next(15) + 1;
                        int period = 2 * executiontime;

                        int deadline = rng.Next(period - executiontime) + executiontime;
                        int earliestactivation = rng.Next(deadline - executiontime);

                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                        Workload workload = WorkloadGenerator.CreateWorkload();
                        workload.CreateTask(0, 0, 0, executiontime, period, deadline, earliestactivation);

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        Job job = simulation.Tasks.First();
                        List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                        Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                        Assert.IsTrue(trace[0].All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name}.");
                        Assert.IsTrue(trace[0].All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");
                        Assert.IsTrue(Utility.VerifyE2EConstraints(simulation.Evaluator));
                    }
                }

                // Expected to fail due to a too high earliest activation.
                [TestMethod]
                public void Fail_EarliestActivation()
                {
                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 5, 10, 10, 7);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsFalse(trace[0].All(x => Utility.VerifyDeadlines(x, job)), $"Deadline should have failed for task: {job.Name}.");
                }

                // Expected to fail as utilization is to high. 
                [TestMethod]
                public void Fail_ExecutionTime()
                {
                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 11, 10, 10, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsFalse(trace[0].All(x => Utility.VerifyDeadlines(x, job)), $"Deadline should have failed for task: {job.Name}.");
                }
            }

            [TestClass]
            public class MultipleTasks
            {
                // Deadline is expected to fail for the second task due to excessive macrotick
                [TestMethod]
                public void Success_ConcreteTasks_1()
                {
                    int macrotick = 4;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 5, 10, 10, 0);
                    workload.CreateTask(1, 0, 0, 2, 4, 4, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (2, 4), (8, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (11, 4), (17, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(5, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");

                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (6, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (9, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[3].ToList(), true, (15, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[4].ToList(), false, (18, 2)));

                    Assert.IsFalse(trace[3].All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name}.");
                }
                // Same case as before, but with a lower macrotick.
                // Deadline is not expected to fail for the second task as the macrotick has been lowered.
                [TestMethod]
                public void Success_ConcreteTasks_2()
                {
                    int macrotick = 2;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 5, 10, 10, 0);
                    workload.CreateTask(1, 0, 0, 2, 4, 4, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (2, 2), (6, 2), (8, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (11, 2), (15, 2), (17, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(5, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");

                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (4, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (9, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[3].ToList(), false, (13, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[4].ToList(), false, (18, 2)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_3()
                {
                    int macrotick = 6;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 6, 18, 18, 0);
                    workload.CreateTask(1, 0, 0, 3, 6, 6, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (3, 6)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(3, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");

                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (9, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (12, 3)));
                }

                // 3 tasks. No expected failures.
                [TestMethod]
                public void Success_ConcreteTasks_4()
                {
                    int macrotick = 1;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 3, 6, 6, 0);
                    workload.CreateTask(1, 0, 0, 2, 8, 8, 0);
                    workload.CreateTask(2, 0, 0, 3, 12, 12, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(4, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 1), (1, 1), (2, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (8, 1), (9, 1), (10, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (13, 1), (14, 1), (15, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[3].ToList(), false, (21, 1), (22, 1), (23, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(3, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (3, 1), (4, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (11, 1), (12, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (19, 1), (20, 1)));

                    // Third task
                    job = simulation.Tasks.Skip(2).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (5, 1), (6, 1), (7, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (16, 1), (17, 1), (18, 1)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_5()
                {
                    int macrotick = 2;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 3, 6, 6, 0);
                    workload.CreateTask(1, 0, 0, 2, 8, 8, 0);
                    workload.CreateTask(2, 0, 0, 3, 12, 12, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(4, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2), (2, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (8, 2), (10, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (13, 2), (15, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[3].ToList(), false, (21, 2), (23, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(3, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (3, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (11, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (19, 2)));

                    // Third task
                    job = simulation.Tasks.Skip(2).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (5, 2), (7, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (16, 2), (18, 1)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_6()
                {
                    int macrotick = 3;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 3, 6, 6, 0);
                    workload.CreateTask(1, 0, 0, 2, 8, 8, 0);
                    workload.CreateTask(2, 0, 0, 3, 12, 12, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(4, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (8, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (13, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[3].ToList(), false, (21, 3)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(3, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (3, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (11, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (19, 2)));

                    // Third task
                    job = simulation.Tasks.Skip(2).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (5, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (16, 3)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_7()
                {
                    int macrotick = 3;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 1, 6, 6, 0);
                    workload.CreateTask(1, 0, 0, 4, 10, 10, 0);
                    workload.CreateTask(2, 0, 0, 5, 15, 15, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(5, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (8, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (14, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[3].ToList(), false, (19, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[4].ToList(), false, (26, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(3, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (1, 3), (4, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (11, 3), (15, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (22, 3), (25, 1)));

                    // Third task
                    job = simulation.Tasks.Skip(2).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (5, 3), (9, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (16, 3), (20, 2)));
                }

                // Tests that given a single task, that all execution slices are consecutive, below the macro tick
                // and accumulated equals the execution time.
                // Consequently this verifies that the macrotick doesn't inadvertantly interfere with the execution.
                [TestMethod]
                public void Success_Macrotick()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 500; i++)
                    {
                        int executiontime = rng.Next(15) + 10;
                        int period1 = 3 * executiontime;
                        int earliestactivation = rng.Next(2);
                        Workload workload = WorkloadGenerator.CreateWorkload();
                        workload.CreateTask(0, 0, 0, executiontime, period1, period1, earliestactivation);

                        executiontime = rng.Next(5) + 15;
                        int period2 = 3 * executiontime;
                        earliestactivation = rng.Next(2);
                        workload.CreateTask(1, 0, 0, executiontime, period2, period2, earliestactivation);
                        int macrotick = rng.Next(9) + 1;

                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        foreach (Job job in simulation.Tasks)
                        {
                            foreach (var trace in Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration))
                            {
                                Assert.IsTrue(trace.All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name}.");
                                Assert.IsTrue(trace.All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");

                                Assert.AreEqual(job.ExecutionTime, trace.Select(x => x.ExecutionSize).Sum(), $"Expected cycles mismatch for task : {job.Name},0");
                                Assert.IsTrue(trace.All(x => x.ExecutionSize <= macrotick), $"Expected slices are too big for task: {job.Name},0");
                                Assert.IsTrue(Utility.VerifyMacrotickCompliance(simulation.Environment, trace.ToList(), job), $"Macrotick compliance failed for task: {job.Name},0");
                            }
                        }
                    }
                }

                // Two tasks on single cpu and single core.
                // Expect to succeed as U=1.0
                [TestMethod]
                public void Success_TwoTasks_1()
                {
                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 5, 10, 10, 0);
                    workload.CreateTask(1, 0, 0, 4, 8, 8, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    foreach (Job job in simulation.Tasks)
                    {
                        foreach (var traceFragment in Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration))
                        {
                            Assert.IsTrue(traceFragment.All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name}.");
                            Assert.IsTrue(traceFragment.All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");
                        }
                    }

                    Assert.IsTrue(Utility.VerifyE2EConstraints(simulation.Evaluator));
                }

                [TestMethod]
                public void Success_TwoTasks_2()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 500; i++)
                    {
                        int macrotick = rng.Next(9) + 1;
                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                        Workload workload = WorkloadGenerator.CreateWorkload();
                        workload.CreateTask(0, 0, 0, 10, 60, 60, 0);
                        workload.CreateTask(1, 0, 0, 10, 20, 20, 0);

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        foreach (Job job in simulation.Tasks)
                        {
                            foreach (var traceFragment in Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration))
                            {
                                Assert.IsTrue(traceFragment.All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name}.");
                                Assert.IsTrue(traceFragment.All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");
                            }
                        }

                        Assert.IsTrue(Utility.VerifyE2EConstraints(simulation.Evaluator));
                    }
                }

                // Tests that given multiple tasks, that the deadlines are met given that T=D.
                // Expect to succeed as U<0.85
                [TestMethod]
                public void Success_Accumulated()
                {
                    Random rng = new Random(System.Environment.TickCount);
                    for (int i = 0; i < 500; i++)
                    {
                        int macrotick = 1;
                        Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick } });
                        Workload workload = WorkloadGenerator.CreateWorkload();

                        int utilization = 100;
                        int id = 0;
                        int iter = 0;
                        while (utilization > 15)
                        {
                            int period = rng.Next(3, utilization / 3);
                            int et = rng.Next(1, period);
                            int deadline = period;
                            if ((utilization - ((et * 100) / period)) > 5)
                            {
                                workload.CreateTask(id++, 0, 0, et, period, deadline, 0);
                                utilization -= 1 + ((et * 100) / period);
                            }
                            else
                            {
                                if (++iter > 5) break;
                            }
                        }

                        Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                        simulation.Run(true);

                        foreach (Job job in simulation.Tasks)
                        {
                            foreach (var traceFragment in Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration))
                            {
                                Assert.IsTrue(traceFragment.All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name}.");
                                Assert.IsTrue(traceFragment.All(x => Utility.VerifyEarliestActivation(x.Cycle, x.Iteration, job)), $"Earliest activation exceed for task: {job.Name}");

                                Assert.AreEqual(1 + ((job.ExecutionTime - 1) / macrotick) + (((job.ExecutionTime - 1) % macrotick) > 0 ? 1 : 0), traceFragment.Count(), $"Failed to deliver the number of expected execution cycles for task: {job.Name}");
                                Assert.AreEqual(job.ExecutionTime, traceFragment.Select(x => x.ExecutionSize).Sum(), $"Expected cycles mismatch for task : {job.Name},0");
                                Assert.IsTrue(traceFragment.All(x => x.ExecutionSize <= macrotick), $"Expected slices are too big for task: {job.Name},0");
                            }
                        }
                    }
                }

                // Two tasks on single cpu and single core.
                // Expected to fail as utilization is to high. 
                [TestMethod]
                public void Fail_Excessive_Utilization()
                {
                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { 1 } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 3, 4, 4, 0);
                    workload.CreateTask(1, 0, 0, 6, 12, 10, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(3, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(trace[0].All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name},0.");
                    Assert.IsTrue(trace[1].All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name},1.");
                    Assert.IsFalse(trace[2].All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name},2.");

                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsFalse(trace[0].All(x => Utility.VerifyDeadlines(x, job)), $"Deadline failed for task: {job.Name}.");
                }
            }
        }

        [TestClass]
        public class MultiCore
        {
            [TestClass]
            public class MultipleTasks
            {
                [TestMethod]
                public void Success_ConcreteTasks_1()
                {
                    int macrotick = 4;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick, macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 5, 10, 10, 0);
                    workload.CreateTask(1, 0, 1, 2, 4, 4, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 4), (4, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (4, 2)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_2()
                {
                    int macrotick = 2;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick, macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 5, 10, 10, 0);
                    workload.CreateTask(1, 0, 1, 2, 4, 4, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2), (2, 2), (4, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");

                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (4, 2)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_3()
                {
                    int macrotick = 6;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick, macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 6, 18, 18, 0);
                    workload.CreateTask(1, 0, 1, 3, 6, 6, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 6)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(3, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (6, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[2].ToList(), false, (12, 3)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_4()
                {
                    int macrotick = 1;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick, macrotick, macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 3, 6, 6, 0);
                    workload.CreateTask(1, 0, 1, 2, 8, 8, 0);
                    workload.CreateTask(2, 0, 2, 3, 12, 12, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 1), (1, 1), (2, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (6, 1), (7, 1), (8, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 1), (1, 1)));

                    // Third task
                    job = simulation.Tasks.Skip(2).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 1), (1, 1), (2, 1)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_5()
                {
                    int macrotick = 2;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick, macrotick, macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 3, 6, 6, 0);
                    workload.CreateTask(1, 0, 1, 2, 8, 8, 0);
                    workload.CreateTask(2, 0, 2, 3, 12, 12, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2), (2, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (6, 2), (8, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2)));

                    // Third task
                    job = simulation.Tasks.Skip(2).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2), (2, 1)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_6()
                {
                    int macrotick = 3;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick, macrotick, macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 3, 6, 6, 0);
                    workload.CreateTask(1, 0, 1, 2, 8, 8, 0);
                    workload.CreateTask(2, 0, 2, 3, 12, 12, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 3)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (6, 3)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 2)));

                    // Third task
                    job = simulation.Tasks.Skip(2).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 3)));
                }

                [TestMethod]
                public void Success_ConcreteTasks_7()
                {
                    int macrotick = 3;

                    Configuration configuration = WorkloadGenerator.SetupConfiguration(new List<int[]> { new int[] { macrotick, macrotick, macrotick } });
                    Workload workload = WorkloadGenerator.CreateWorkload();
                    workload.CreateTask(0, 0, 0, 1, 6, 6, 0);
                    workload.CreateTask(1, 0, 1, 4, 10, 10, 0);
                    workload.CreateTask(2, 0, 2, 5, 15, 15, 0);

                    Simulation simulation = new Simulation(workload, configuration, 1, 1, 1, 1);
                    simulation.Run(true);

                    // First task
                    Job job = simulation.Tasks.First();
                    List<IGrouping<int, Job.Execution>> trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(2, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 1)));
                    Assert.IsTrue(Utility.VerifyPeriod(trace[1].ToList(), false, (6, 1)));

                    // Second task
                    job = simulation.Tasks.Skip(1).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 3), (3, 1)));

                    // Third task
                    job = simulation.Tasks.Skip(2).First();
                    trace = Utility.BoundTrace(simulation.Environment, job).GroupBy(x => x.Iteration).ToList();
                    Assert.AreEqual(1, trace.Count, $"Number of expected periods mistmatch for task: {job.Name}.");
                    Assert.IsTrue(Utility.VerifyPeriod(trace[0].ToList(), false, (0, 3), (3, 2)));
                }

            }
        }
    }
}
