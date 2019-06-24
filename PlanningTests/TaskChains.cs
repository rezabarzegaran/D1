using Microsoft.VisualStudio.TestTools.UnitTesting;
using Planner.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using Planner.Objects.Measurement;

namespace PlanningTests
{
    // Single task on single cpu and single core.
    [TestClass]
    public class TaskChains
    {
        // Tests that it doesnt progress when only given start tasks.
        [TestMethod]
        public void Success_StartTask()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {                
                int executiontime = rng.Next(15) + 1;
                int period = 2 * executiontime;
                int deadline = rng.Next(period - executiontime) + executiontime;
                int earliestactivation = rng.Next(deadline - executiontime);
                TaskChain tc = new TaskChain(string.Empty, deadline - earliestactivation, 1.0);
                Job j = new Job(1, "T1", period, deadline, executiontime, earliestactivation, 0, 0, 0, null, new List<int> { period });
                tc.AddTask(j);

                Assert.AreEqual(0, tc.E2E);
                tc.StartTask(j, j.ExecutionTime + earliestactivation);
                Assert.AreEqual(0, tc.E2E);
            }
        }

        // Tests that it doesnt progress when only given end tasks.
        [TestMethod]
        public void Success_EndTask()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                int executiontime = rng.Next(15) + 1;
                int period = 2 * executiontime;
                int deadline = rng.Next(period - executiontime) + executiontime;
                int earliestactivation = rng.Next(deadline - executiontime);
                TaskChain tc = new TaskChain(string.Empty, deadline - earliestactivation, 1.0);
                Job j = new Job(1, "T1", period, deadline, executiontime, earliestactivation, 0, 0, 0, null, new List<int> { period });
                tc.AddTask(j);

                Assert.AreEqual(0, tc.E2E);
                tc.EndTask(j, j.ExecutionTime + earliestactivation);
                Assert.AreEqual(0, tc.E2E);
            }
        }

        // Tests that it only measures the registered tasks.
        [TestMethod]
        public void Success_RegisteredTask()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                int executiontime = rng.Next(15) + 1;
                int period = 2 * executiontime;
                int deadline = rng.Next(period - executiontime) + executiontime;
                int earliestactivation = rng.Next(deadline - executiontime);
                TaskChain tc = new TaskChain(string.Empty, deadline - earliestactivation, 1.0);
                Job j1 = new Job(1, "T1", period, deadline, executiontime, earliestactivation, 0, 0, 0, null, new List<int> { period });
                Job j2 = new Job(1, "T2", period, deadline, executiontime, earliestactivation, 0, 0, 0, null, new List<int> { period });
                tc.AddTask(j1);
                tc.Reset();
                int e2e = tc.E2E;
                tc.StartTask(j1, earliestactivation);
                Assert.AreEqual(e2e, tc.E2E);
                tc.EndTask(j1, j1.ExecutionTime + earliestactivation);
                Assert.AreEqual(j1.ExecutionTime, tc.E2E);

                e2e = tc.E2E;
                tc.StartTask(j2, earliestactivation + 1);
                tc.EndTask(j2, earliestactivation + 50);
                Assert.AreEqual(e2e, tc.E2E);

                tc.StartTask(j2, j1.Period + earliestactivation);
                tc.EndTask(j2, j1.Period + earliestactivation + j1.ExecutionTime);
                Assert.AreEqual(j1.ExecutionTime, tc.E2E);
            }
        }

        // Tests that the E2E matches the execution.
        [TestMethod]
        public void Success_E2E_TTechCase()
        {
            TaskChain tc = new TaskChain(string.Empty, 0, 1.0);
            Job j1 = new Job(1, "T1", 10, 0, 0, 0, 0, 0, 0, null, new List<int>{10});
            Job j2 = new Job(1, "T2", 20, 0, 0, 0, 0, 0, 0, null, new List<int>{20});
            Job j3 = new Job(1, "T3", 40, 0, 0, 0, 0, 0, 0, null, new List<int>{40});
            Job j4 = new Job(1, "T4", 80, 0, 0, 0, 0, 0, 0, null, new List<int> { 80 });
            tc.AddTask(j1);
            tc.AddTask(j2);
            tc.AddTask(j3);
            tc.AddTask(j4);
            tc.Reset();
            tc.StartTask(j2, 2);
            tc.EndTask(j2, 4);
            tc.StartTask(j3, 4);
            tc.EndTask(j3, 6);
            tc.StartTask(j1, 10);
            tc.EndTask(j1, 11);
            tc.StartTask(j2, 12);
            tc.EndTask(j2, 14);
            tc.StartTask(j3, 14);
            tc.EndTask(j3, 16);
            tc.StartTask(j2, 22);
            tc.EndTask(j2, 24);
            tc.StartTask(j3, 24);
            tc.EndTask(j3, 26);
            tc.StartTask(j1, 30);
            tc.EndTask(j1, 31);
            tc.StartTask(j2, 32);
            tc.EndTask(j2, 34);
            tc.StartTask(j3, 34);
            tc.EndTask(j3, 36);
            tc.StartTask(j4, 37);
            Assert.AreEqual(0, tc.E2E);
            tc.EndTask(j4, 40);
            Assert.AreEqual(30, tc.E2E);
        }

        // Tests that the E2E matches the execution.
        [TestMethod]
        public void Success_E2E_RandomTask()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                int executiontime = rng.Next(15) + 1;
                int period = 2 * executiontime;
                int deadline = rng.Next(period - executiontime) + executiontime;
                int earliestactivation = rng.Next(deadline - executiontime);
                TaskChain tc = new TaskChain(string.Empty, deadline - earliestactivation, 1.0);
                Job j = new Job(1, "T1", period, deadline, executiontime, earliestactivation, 0, 0, 0, null, new List<int> { period });
                tc.AddTask(j);
                tc.Reset();
                tc.StartTask(j, earliestactivation);
                Assert.AreEqual(0, tc.E2E);
                tc.EndTask(j, j.ExecutionTime + earliestactivation);
                Assert.AreEqual(j.ExecutionTime, tc.E2E);
            }
        }

        // Tests that the E2E matches the execution when increasing the execution time.
        [TestMethod]
        public void Success_E2E_IncrementalE2E()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                int executiontime = rng.Next(15) + 1;
                int period = 2 * executiontime;
                int deadline = rng.Next(period - executiontime) + executiontime;
                int earliestactivation = rng.Next(deadline - executiontime);
                TaskChain tc = new TaskChain(string.Empty, deadline - earliestactivation, 1.0);
                Job j = new Job(1, "T1", period, deadline, executiontime, earliestactivation, 0, 0, 0, null, new List<int> { period });
                tc.AddTask(j);
                tc.Reset();
                for (int incr = 0; incr < 50; incr++)
                {
                    int e2e = tc.E2E;
                    tc.StartTask(j, earliestactivation);
                    Assert.AreEqual(e2e, tc.E2E);
                    tc.EndTask(j, j.ExecutionTime + earliestactivation + incr);
                    Assert.AreEqual(j.ExecutionTime + incr, tc.E2E);
                }
            }
        }

        // Tests that the E2E matches the execution & that only task executions with correct precedence will succeed.
        [TestMethod]
        public void Success_E2E_Chain_Progressions_1()
        {
            TaskChain tc = new TaskChain(string.Empty, 50, 1.0);
            Job j1 = new Job(1, "T1", 10, 0, 0, 0, 0, 0, 0, null, new List<int> { 10 });
            tc.AddTask(j1);
            tc.AddTask(j1);
            tc.AddTask(j1);
            tc.Reset();
            // Activate first task in chain
            Assert.AreEqual(0, tc.E2E);
            tc.StartTask(j1, 0);
            tc.EndTask(j1, 3);
            Assert.AreEqual(0, tc.E2E);

            // Activate task present in chain, but do not progress because it is earlier
            tc.StartTask(j1, 1);
            tc.EndTask(j1, 5);
            Assert.AreEqual(0, tc.E2E);

            // Activate final task in chain and progress.
            tc.StartTask(j1, 6);
            tc.EndTask(j1, 9);
            Assert.AreEqual(0, tc.E2E);

            // Activate final task in chain and progress.
            tc.StartTask(j1, 12);
            tc.EndTask(j1, 15);
            Assert.AreEqual(15, tc.E2E);
        }

        // Tests that the E2E matches the execution & that only task executions with correct precedence will succeed.
        [TestMethod]
        public void Success_E2E_Chain_Progressions_2()
        {
            TaskChain tc = new TaskChain(string.Empty, 50, 1.0);
            Job j1 = new Job(1, "T1", 1, 0, 0, 0, 0, 0, 0, null, new List<int> { 1 });
            Job j2 = new Job(1, "T2", 5, 0, 0, 0, 0, 0, 0, null, new List<int> { 5 });
            tc.AddTask(j1);
            tc.AddTask(j2);
            tc.Reset();
            // Activate first task in chain
            Assert.AreEqual(0, tc.E2E);
            tc.StartTask(j1, 0);
            tc.EndTask(j1, 3);
            Assert.AreEqual(0, tc.E2E);

            // Activate task present in chain, but do not progress because it is earlier.
            tc.StartTask(j2, 1);
            tc.EndTask(j2, 5);
            Assert.AreEqual(0, tc.E2E);

            // Activate final task in chain and progress.
            tc.StartTask(j2, 12);
            tc.EndTask(j2, 15);
            Assert.AreEqual(15, tc.E2E);
        }

        // Tests that the E2E matches the execution & that only task executions with correct precedence will succeed.
        [TestMethod]
        public void Success_E2E_Chain_Progressions_3()
        {
            TaskChain tc = new TaskChain(string.Empty, 50, 1.0);
            Job j1 = new Job(1, "T1", 1, 0, 0, 0, 0, 0, 0, null, new List<int> { 1 });
            Job j2 = new Job(1, "T2", 5, 0, 0, 0, 0, 0, 0, null, new List<int> { 5 });
            tc.AddTask(j1);
            tc.AddTask(j1);
            tc.AddTask(j1);
            tc.AddTask(j2);
            tc.Reset();
            // Activate first task in chain
            Assert.AreEqual(0, tc.E2E);
            tc.StartTask(j1, 0);
            tc.EndTask(j1, 3);
            Assert.AreEqual(0, tc.E2E);

            // Activate second task in chain
            tc.StartTask(j1, 4);
            tc.EndTask(j1, 7);
            Assert.AreEqual(0, tc.E2E);

            // Activate task present in chain, but do not progress because it is earlier
            tc.StartTask(j2, 1);
            tc.EndTask(j2, 5);
            Assert.AreEqual(0, tc.E2E);

            // Activate third task in chain
            tc.StartTask(j1, 8);
            tc.EndTask(j1, 11);
            Assert.AreEqual(0, tc.E2E);

            // Activate task present in chain, but do not progress.
            tc.StartTask(j2, 1);
            tc.EndTask(j2, 5);
            Assert.AreEqual(0, tc.E2E);

            // Activate final task in chain and progress.
            tc.StartTask(j2, 12);
            tc.EndTask(j2, 15);
            Assert.AreEqual(15, tc.E2E);
        }

        // Tests that the E2E matches the execution & that only task executions with correct precedence will succeed.
        [TestMethod]
        public void Success_E2E_Chain_Precedence()
        {
            TaskChain tc = new TaskChain(string.Empty, 50, 1.0);
            Job j1 = new Job(1, "T1", 1, 0, 0, 0, 0, 0, 0, null, new List<int> { 1});
            Job j2 = new Job(1, "T2", 5, 0, 0, 0, 0, 0, 0, null, new List<int> { 5 });
            Job j3 = new Job(1, "T3", 10, 0, 0, 0, 0, 0, 0, null, new List<int> { 10 });
            tc.AddTask(j1);
            tc.AddTask(j2);
            tc.AddTask(j3);
            tc.Reset();
            // Activate final task in chain
            tc.StartTask(j3, 0);
            tc.EndTask(j3, 5);
            Assert.AreEqual(0, tc.E2E);

            // Activate second task in chain
            tc.StartTask(j2, 1);
            tc.EndTask(j2, 5);
            Assert.AreEqual(0, tc.E2E);

            // Activate first task in chain
            tc.StartTask(j1, 0);
            tc.EndTask(j1, 3);
            Assert.AreEqual(0, tc.E2E);

            // Activate second task in chain
            tc.StartTask(j2, 4);
            tc.EndTask(j2, 5);
            Assert.AreEqual(0, tc.E2E);

            // Activate final task in chain
            tc.StartTask(j3, 12);
            tc.EndTask(j3, 15);
            Assert.AreEqual(15, tc.E2E);
        }

        // Tests that the E2E matches the execution.
        [TestMethod]
        public void Success_E2E_RandomTaskchains()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                int taskcnt = rng.Next(50);
                List<Job> tasks = new List<Job>();
                long hyperperiod = 1;
                for (int taskCount = 0; taskCount < taskcnt; taskCount++)
                {
                    int executiontime = rng.Next(15) + 1;
                    int period = 2 * executiontime;
                    int deadline = rng.Next(period - executiontime) + executiontime;
                    int earliestactivation = rng.Next(deadline - executiontime);
                    Job j = new Job(taskCount, "T" + taskCount, period, deadline, executiontime, earliestactivation, 0, 0, 0, null, new List<int> { period });
                    tasks.Add(j);
                    hyperperiod = Extensions.LeastCommonMultiple((int)hyperperiod, j.Period);
                }

                int e2e = tasks.Sum(x => x.ExecutionTime);
                TaskChain tc = new TaskChain(string.Empty, 0, 1.0);
                tasks.ForEach(x => tc.AddTask(x));
                tc.Reset();
                int start = 0;
                foreach (Job j in tasks)
                {
                    tc.StartTask(j, start);
                    tc.EndTask(j, start + j.ExecutionTime);
                    start += j.ExecutionTime;
                }
                Assert.AreEqual(e2e, tc.E2E);
            }
        }
    }
}
