using Planner.Objects.Measurement;
using System;
using System.Collections.Generic;
using System.Linq;
using Accord.IO;

namespace Planner.Objects
{
    /// <summary>
    /// This entity represents the 'objective function'.
    /// It computes the solution fitness, and the associated penalities for
    /// not complying with the given constraints.
    /// </summary>
    public class FitnessScore
    {
        
        /// <summary>
        /// Returns a new instance of the FitnessScore class.
        /// </summary>
        /// <param name="w1">The relative weight used for both maximal score, and static penalty.</param>
        /// <param name="w2">Weight used for penalizing E2E violations. w2 = x*w1</param>
        /// <param name="w3">Weight used for penalizing deadline violations. w3 = x*w1</param>
        /// <param name="w4">Weight used for penalizing jitter violations. w4 = x*w1</param>
        /// <param name="chains">List of E2E values. </param>
        /// <param name="deadlines">List of deadline values.</param>
        /// <param name="jitters">List of jitter values.</param>
        public FitnessScore(double w1, double w2, double w3, double w4, double w5, List<TaskChain> chains, List<Deadline> deadlines, List<JitterBase> jitters, List<Order> orders, List<CoC> controlcost, MLApp.MLApp matlab)
        {
            Matlab = matlab;
            Dictionary<int, int> violationsCPUMap = new Dictionary<int, int>();
            double totalE2E = 0;
            double validScore = 0;
            double e2EPenalty = 0;
            double deadlinePenalty = 0;
            double jitterPenalty = 0;
            double orderPenalty = 0;
            double controlPenalty = 0;

            // Calculate valid score and penalites
            foreach (TaskChain chain in chains)
            {
                validScore += Fitness(chain);
                totalE2E += chain.Threshold * chain.Priority;
                e2EPenalty += Penalty(chain);
                if (chain.Failed) E2EViolations++;
            }
            validScore = (totalE2E > 0.0) ? validScore / totalE2E : 0.0;
            if (chains.Count > 0) e2EPenalty /= chains.Count;

            // Calculate deadlinepenalty & determine the cpu's which own the failed tasks
            foreach (Deadline deadline in deadlines)
            {
                if (deadline.Failed)
                {
                    deadlinePenalty += Penalty(deadline);
                    if (!violationsCPUMap.ContainsKey(deadline.OwnerCpu))
                    {
                        violationsCPUMap.Add(deadline.OwnerCpu, 0);
                    }
                    violationsCPUMap[deadline.OwnerCpu]++;
                    DeadlineViolations++;
                }
            }
            if (deadlines.Count > 0) deadlinePenalty /= deadlines.Count;
           
            // Calculate jitterpenalty & determine the cpu's which own the failed tasks
            
            foreach (JitterBase jitter in jitters)
            {
                if (jitter.Failed)
                {
                    jitterPenalty += Penalty(jitter);
                    if (!violationsCPUMap.ContainsKey(jitter.OwnerCpu))
                    {
                        violationsCPUMap.Add(jitter.OwnerCpu, 0);
                    }
                    violationsCPUMap[jitter.OwnerCpu]++;
                    JitterViolations++;
                }
            }
            if (jitters.Count > 0) jitterPenalty /= jitters.Count;
            WorstCpu = (violationsCPUMap.Count > 0) ? violationsCPUMap.Aggregate((x, y) => x.Value > y.Value ? x : y).Key : -1;

            foreach (Order app in orders)
            {
                    orderPenalty += Penalty(app);
                    if(app.Failed)
                    {
                        OrderViolations++;
                    }
            }

            foreach (CoC controlapp in controlcost)
            {
                controlPenalty += Penalty(controlapp);
            }
            if (controlcost.Count > 0) controlPenalty /= controlcost.Count;



            ValidSolution = (E2EViolations == 0) && (DeadlineViolations == 0) && (OrderViolations == 0) &&
                            (JitterViolations == 0);

            // Calculate static and weighted penalties.
            double staticPenalty = Math.Ceiling(Math.Min(1.0, e2EPenalty + deadlinePenalty + jitterPenalty + orderPenalty + controlPenalty)) * w1;
            E2EPenalty = e2EPenalty * w2;
            DeadlinePenalty = deadlinePenalty * w3;
            JitterPenalty = jitterPenalty * w4;
            OrderPenalty = orderPenalty * w1;
            ControlPenalty = controlPenalty * w5;
            // Calculate score and combined penalty 
            MaxValidScore = w1;
            ValidScore = validScore * w1;

            TotalPenalty = E2EPenalty + DeadlinePenalty + JitterPenalty + OrderPenalty + ControlPenalty + (ViolationCount * 1000);
            IsValid = ValidSolution;
            //Score = Math.Max(TotalPenalty, ValidScore);
            Score = TotalPenalty;
            UnweightedScore = Math.Max(staticPenalty + (e2EPenalty + deadlinePenalty + jitterPenalty + orderPenalty + controlPenalty) * w1, ValidScore);
        }

        public double MaxValidScore { get; }
        public double ValidScore { get; }
        public double E2EPenalty { get; }
        public double DeadlinePenalty { get; }
        public double JitterPenalty { get; }
        public double OrderPenalty { get; }
        public double ControlPenalty { get; }
        public double TotalPenalty { get; }
        public double Score { get; set; }
        public bool IsValid { get; }
        public bool ValidSolution { get; }
        public int WorstCpu { get; }
        public int E2EViolations { get; private set; }
        public int OrderViolations { get; private set; }
        public int DeadlineViolations { get; private set; }
        public int JitterViolations { get; private set; }
        public int ViolationCount => E2EViolations + DeadlineViolations + JitterViolations + OrderViolations;
        public double UnweightedScore { get; private set; }
        public MLApp.MLApp Matlab = null;

        private double Penalty(JitterBase jitter)
        {
            if (jitter.Failed)
            {
                int limit = (jitter.Period - jitter.ExecutionTime);
                int violation = Math.Min(limit, jitter.MaxJitter - jitter.Threshold);
                return (double)violation / limit;
            }

            return 0;
        }
        private double Penalty(Deadline deadline)
        {
            if (deadline.Failed)
            {
                int limit = deadline.Period;
                int violation = Math.Min(limit, deadline.MaxDistance);
                return (double)violation / limit;
            }

            return 0;
        }
        private double Penalty(TaskChain chain)
        {
            if (chain.Failed)
            {
                int limit = chain.Threshold;
                int violation = Math.Min(limit, chain.E2E - chain.Threshold);
                return (double)violation / limit;
            }

            return 0;
        }
        private double Penalty(Order order)
        {
            //order.CheckViolation();
            if (order.CheckViolation() != 0)
            {
                return (double)50;
            }

            return 0;
        }
        private double Penalty(CoC app)
        {
            double chainperiod = app.Period;
            int count = 0;
            List<int> itters = new List<int>();
            int[][] scheduletable = new int[app.Tasks.Count][];
            foreach (var virtask in app.Tasks)
            {
                scheduletable[count] = virtask.EndAccess.DeepClone().ToArray();
                itters.Add(virtask.EndAccess.Count);
                count++;
            }

            int tasknums = count;
            int itter = itters.Min();
            double[,] Runtable = new double[tasknums, itter];
            for (int i = 0; i < scheduletable.Length; i++)
            {
                for (int j = 0; j < itter; j++)
                {
                    Runtable[i, j] = scheduletable[i][j];
                }
            }

            double CoCval = 1;
            if (itter >= 5)
            {
                object result = null;
                Matlab.Feval("CoC", 2, out result, app.Name, chainperiod, Runtable);
                object[] res = result as object[];
                CoCval = Convert.ToDouble(res[0]);
                int chainType = Convert.ToInt32(res[1]);

            }



            app.Cost = CoCval;
            return CoCval;
        }
        private double Fitness(TaskChain chain)
        {
            return Math.Min(chain.E2E, chain.Threshold) * chain.Priority;
        }
    }
}
