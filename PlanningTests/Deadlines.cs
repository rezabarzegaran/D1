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
    public class Deadlines
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
                Job j = new Job(1, "T1", period, deadline, executiontime, earliestactivation, 0, 0, 0, new Evaluator(1,1,1, 1), new List<int> { period });
                Deadline d = new Deadline(j);

                Assert.AreEqual(0, d.MaxDistance);
                d.StartTask(j, executiontime + earliestactivation);
                Assert.AreEqual(0, d.MaxDistance);
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
                Job j = new Job(1, "T1", period, deadline, executiontime, earliestactivation, 0, 0, 0, new Evaluator( 1, 1, 1, 1), new List<int> { period });
                Deadline d = new Deadline(j);

                Assert.AreEqual(0, d.MaxDistance);
                d.EndTask(j, executiontime + earliestactivation);
                Assert.AreEqual(0, d.MaxDistance);
            }
        }

        // Tests that the E2E matches the execution.
        [TestMethod]
        public void Success_Below_Deadline()
        {
            Job j = new Job(1, "T1", 0, 10, 0, 0, 0, 0, 0, new Evaluator( 1, 1, 1, 1), new List<int> { 0 });
            j.Reset();
            Deadline d = new Deadline(j);
            d.StartTask(j, 0);
            Assert.AreEqual(0, d.MaxDistance);
            d.EndTask(j, 8);
            Assert.AreEqual(0, d.MaxDistance);
        }

        // Tests that the E2E matches the execution.
        [TestMethod]
        public void Success_Above_Deadline()
        {
            Job j = new Job(1, "T1", 0, 10, 0, 0, 0, 0, 0, new Evaluator( 1, 1, 1, 1), new List<int> { 0 });
            j.Reset();
            Deadline d = new Deadline(j);
            d.StartTask(j, 0);
            Assert.AreEqual(0, d.MaxDistance);
            d.EndTask(j, 12);
            Assert.AreEqual(2, d.MaxDistance);
        }

        // Tests that the E2E matches the execution.
        [TestMethod]
        public void Success_RandomTasks()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                int executiontime = rng.Next(15) + 1;
                int period = 2 * executiontime;
                int deadline = rng.Next(period - executiontime) + executiontime;
                int earliestactivation = rng.Next(deadline - executiontime);
                Job j = new Job(1, "T1", period, deadline, executiontime, earliestactivation, 0, 0, 0, new Evaluator( 1, 1, 1, 1), new List<int> { period });
                j.Reset();
                Deadline d = new Deadline(j);

                int startCycle = earliestactivation;
                d.StartTask(j, startCycle);
                Assert.AreEqual(0, d.MaxDistance);
                int endCycle = executiontime + earliestactivation;
                d.EndTask(j, endCycle);
                Assert.AreEqual(Math.Max(0, endCycle - (startCycle + deadline)), d.MaxDistance);
            }
        }

        // Tests that the E2E matches the execution when increasing the execution time.
        [TestMethod]
        public void Success_IncrementalMaxDistance()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                Job j = new Job(1, "T1", 0, 0, 0, 0, 0, 0, 0, new Evaluator( 1, 1, 1, 1), new List<int> { 0 });
                Deadline deadline = new Deadline(j);
                int prevDeadline = 0;
                for (int incr = 1; incr < 50; incr++)
                {
                    int executiontime = 2 * incr;
                    j = new Job(1, "T1", 0, 0, executiontime, 0, 0, 0, 0, new Evaluator( 1, 1, 1, 1), new List<int> { 0 });
                    deadline.StartTask(j, 0);
                    Assert.AreEqual(prevDeadline, deadline.MaxDistance);

                    deadline.EndTask(j, executiontime);
                    Assert.AreEqual(executiontime, deadline.MaxDistance);
                    Assert.IsTrue(deadline.MaxDistance > prevDeadline);
                    prevDeadline = deadline.MaxDistance;
                }
            }
        }
    }
}
