using Microsoft.VisualStudio.TestTools.UnitTesting;
using Planner.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanningTests
{
    [TestClass]
    public class JobTests
    {
        [TestMethod]
        public void Success_NextEvent_1()
        {
            Evaluator evaluator = new Evaluator( 1, 1, 1, 1);

            int macrotick = 4;
            int period = 50;
            int executionTime = 10;
            Job job = new Job(0, "T0", period, period, executionTime, 0, 0, 0, 0, evaluator, new List<int> { period });
            job.Reset();

            int skipahead = job.NextEvent(0, macrotick);
            Assert.AreEqual(4, skipahead);
            job.Execute(skipahead, true, macrotick);

            skipahead = job.NextEvent(skipahead, macrotick);
            Assert.AreEqual(8, skipahead);
            job.Execute(skipahead, true, macrotick);

            skipahead = job.NextEvent(skipahead, macrotick);
            Assert.AreEqual(10, skipahead);
            job.Execute(skipahead, true, macrotick);
        }

    
    }
}
