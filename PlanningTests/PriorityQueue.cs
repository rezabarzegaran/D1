using Microsoft.VisualStudio.TestTools.UnitTesting;
using Planner.Objects;
using System;
using System.Collections.Generic;

namespace PlanningTests
{
    [TestClass]
    public class PriorityQueue
    {
        // Tests that it doesnt progress when only given a start task.
        [TestMethod]
        public void Consistent()
        {
            Random rand = new Random(0);
            Planner.Objects.PriorityQueue<Job> pq = new Planner.Objects.PriorityQueue<Job>(OrderByDeadline);

            for (int op = 0; op < 100000; ++op)
            {
                int opType = rand.Next(0, 2);

                if (opType == 0)
                {
                    double priority = (100.0 - 1.0) * rand.NextDouble() + 1.0;
                    int period = rand.Next(100000);
                    pq.Enqueue(new Job(period, string.Empty, rand.Next(100000), rand.Next(100000), rand.Next(100000), rand.Next(100000), rand.Next(100000), rand.Next(100000), rand.Next(100000), null, new List<int> { period }));
                    Assert.IsTrue(pq.IsConsistent(), "Test fails after enqueue operation # " + op);
                }
                else
                {
                    if (pq.Count() > 0)
                    {
                        Job e = pq.Dequeue();
                        Assert.IsTrue(pq.IsConsistent(), "Test fails after dequeue operation # " + op);
                    }
                }
            }
        }

        private int OrderByDeadline(Job j1, Job j2)
        {
            if (j1.AbsoluteDeadline > j2.AbsoluteDeadline) return 1;
            if (j1.AbsoluteDeadline == j2.AbsoluteDeadline) return 0;
            return -1;
        }
    }
}
