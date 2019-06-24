using Microsoft.VisualStudio.TestTools.UnitTesting;
using Planner.Objects.Measurement;
using System;
using System.Collections.Generic;
using Planner.Objects;

namespace PlanningTests
{
    // Single task on single cpu and single core.
    [TestClass]
    public class RelativeJitters
    {
        [TestMethod]
        public void Success_StartTask_Once()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                Job j = new Job(1, "T1", 20, 0, 0, 0, 0, 0, 0, new Evaluator(1, 1, 1, 1), new List<int> { 20 });
                RelativeJitter jitter = new RelativeJitter(j);
                Assert.AreEqual(0, jitter.MaxJitter);
                jitter.StartTask(j, rng.Next(15) + 1);
                Assert.AreEqual(0, jitter.MaxJitter);
            }
        }

        [TestMethod]
        public void Success_RandomStartJitter()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                int executiontime = rng.Next(15) + 1;
                int earliestactivation = rng.Next(15) + 1;
                Job j = new Job(1, "T1", 0, 0, executiontime, earliestactivation, 0, 0, 0, new Evaluator(1, 1, 1, 1), new List<int> { 0 });
                RelativeJitter jitter = new RelativeJitter(j);

                jitter.StartTask(j, earliestactivation);
                Assert.AreEqual(0, jitter.MaxJitter);
                jitter.StartTask(j, earliestactivation + executiontime);
                Assert.AreEqual(executiontime, jitter.MaxJitter);
            }
        }

        [TestMethod]
        public void Success_IncrementalStartJitter()
        {
            Random rng = new Random(System.Environment.TickCount);
            Job j = new Job(1, "T1", 0, 0, 0, 0, 0, 0, 0, new Evaluator(1, 1, 1, 1), new List<int> { 0 });
            for (int i = 0; i < 50; i++)
            {
                RelativeJitter jitter = new RelativeJitter(j);
                int prevStartTime = 0;
                for (int startTimes = 0; startTimes < 50; startTimes += 2)
                {
                    j = new Job(1, "T1", 20, 0, 0, 0, 0, 0, 0, new Evaluator(1, 1, 1, 1), new List<int> { 20 });
                    jitter.StartTask(j, startTimes);
                    Assert.AreEqual(startTimes - prevStartTime, jitter.MaxJitter);
                    prevStartTime = startTimes;
                }
            }
        }

        [TestMethod]
        public void Success_EndTask_Once()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                Job j = new Job(1, "T1", 20, 0, 0, 0, 0, 0, 0, new Evaluator(1, 1, 1, 1), new List<int> { 20 });
                RelativeJitter jitter = new RelativeJitter(j);
                Assert.AreEqual(0, jitter.MaxJitter);
                jitter.EndTask(j, rng.Next(15) + 1);
                Assert.AreEqual(0, jitter.MaxJitter);
            }
        }

        [TestMethod]
        public void Success_RandomEndJitter()
        {
            Random rng = new Random(System.Environment.TickCount);
            for (int i = 0; i < 50; i++)
            {
                int executiontime = rng.Next(15) + 1;
                int earliestactivation = rng.Next(15) + 1;
                Job j = new Job(1, "T1", 0, 0, executiontime, earliestactivation, 0, 0, 0, new Evaluator(1, 1, 1, 1), new List<int> { 0 });
                RelativeJitter jitter = new RelativeJitter(j);

                jitter.EndTask(j, earliestactivation);
                Assert.AreEqual(0, jitter.MaxJitter);
                jitter.EndTask(j, earliestactivation + executiontime);
                Assert.AreEqual(executiontime, jitter.MaxJitter);
            }
        }

        [TestMethod]
        public void Success_IncrementalEndJitter()
        {
            Job j = new Job(1, "T1", 0, 0, 0, 0, 0, 0, 0, new Evaluator(1, 1, 1, 1), new List<int> { 0 });
            for (int i = 0; i < 50; i++)
            {
                RelativeJitter jitter = new RelativeJitter(j);
                int prevEndTime = 0;
                for (int endTimes = 0; endTimes < 50; endTimes += 2)
                {
                    j = new Job(1, "T1", 20, 0, 0, 0, 0, 0, 0, new Evaluator(1, 1, 1, 1), new List<int> { 20 });
                    jitter.EndTask(j, endTimes);
                    Assert.AreEqual(endTimes - prevEndTime, jitter.MaxJitter);
                    prevEndTime = endTimes;
                }
            }
        }
    }
}
