using Planner.Objects.Measurement;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        public FitnessScore(double w1, double w2, double w3, double w4, double w5, double w6, List<Application> apps, List<Deadline> deadlines, List<JitterBase> jitters, List<CoreSchedule> schedules, MLApp.MLApp matlab)
        {
            Matlab = matlab;
            Dictionary<int, int> violationsCPUMap = new Dictionary<int, int>();
            double totalE2E = 0;
            int totalOrder = 0;
            double validScore = 0;
            double e2EPenalty = 0;
            double orderPenalty = 0;
            double deadlinePenalty = 0;
            double jitterPenalty = 0;

            double controlPenalty = 0;
            double controlDevPenalty = 0;
            double partitionPenalty = 0;

            int totale2es = 0;
            int totaljitters = 0;
            int totaldeadlines = 0;


            List<double> CocVal = new List<double>();
            int CAs = 0;

            foreach (Application app in apps)
            {
                validScore += Fitness(app);
                totalE2E += app.Threshold;
                e2EPenalty += Penalty(app);
                E2EViolations += app.E2EViolations;
                if(app.InOrder) OrderViolations += app.OrderViolation;
                if (app.InOrder) orderPenalty += app.OrderViolation;

                if (app.CA)
                {
                    CAs++;
                    double temp = CoCPenalty(app);
                    CocVal.Add(temp);
                    controlPenalty += temp;
                }



                totalOrder += (app._Instances.Count * app.Tasks.Count);
                totale2es += app._Instances.Count;




            }
            validScore = (totalE2E > 0.0) ? validScore / totalE2E : 0.0;
            if (apps.Count > 0)
            {
                e2EPenalty /= apps.Count;
                orderPenalty /= totalOrder;
            }
            if (CAs > 0) controlPenalty /= CAs;

            controlDevPenalty = 0.01 * Penalty(controlPenalty, CocVal);

            PossibleOrderViolation = totalOrder;
            PossibleE2EViolation = totale2es;

            // Calculate deadlinepenalty & determine the cpu's which own the failed tasks
            foreach (Deadline deadline in deadlines)
            {
                totaldeadlines += deadline.Map.Count;

                if (deadline.Failed)
                {
                    deadlinePenalty += Penalty(deadline);
                    if (!violationsCPUMap.ContainsKey(deadline.OwnerCpu))
                    {
                        violationsCPUMap.Add(deadline.OwnerCpu, 0);
                    }
                    violationsCPUMap[deadline.OwnerCpu]++;
                    DeadlineViolations += deadline.TotalViolation;
                }
            }
            if (deadlines.Count > 0) deadlinePenalty /= deadlines.Count;
            PossibleDeadlineViolation = totaldeadlines;


            // Calculate jitterpenalty & determine the cpu's which own the failed tasks

            foreach (JitterBase jitter in jitters)
            {
                totaljitters += jitter.EndJitters.Count;

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
            PossibleJitterViolation = totaljitters;


            foreach (CoreSchedule schedule in schedules)
            {
                schedule.Initiate();
                if (schedule.Failed)
                {
                    partitionPenalty += (schedule.PartitionCost);
                    PartitionViolations += schedule.PartitionViolations;
                }
                

            }

            if (schedules.Count > 0)
            {
                partitionPenalty /= schedules.Count;
            }

            ValidSolution = (E2EViolations == 0) && (DeadlineViolations == 0) && (OrderViolations == 0) &&
                            (JitterViolations == 0) && (PartitionViolations == 0);

            // Calculate static and weighted penalties.
            double staticPenalty = Math.Ceiling(Math.Min(1.0, e2EPenalty + deadlinePenalty + jitterPenalty + orderPenalty + controlPenalty + partitionPenalty + controlDevPenalty)) * w1;
            E2EPenalty = e2EPenalty * w2;
            DeadlinePenalty = deadlinePenalty * w3;
            JitterPenalty = jitterPenalty * w4;
            OrderPenalty = orderPenalty * w1;
            ControlPenalty = controlPenalty * w5;
            ControlDevPenalty = controlDevPenalty * w5;
            PartitionPenalty = partitionPenalty * w6;
            // Calculate score and combined penalty 
            MaxValidScore = w1;
            ValidScore = validScore * w1;

            TotalPenalty = E2EPenalty + DeadlinePenalty + JitterPenalty + OrderPenalty + ControlPenalty + PartitionPenalty + ControlDevPenalty + (ViolationCount * 10000);
            IsValid = ValidSolution;
            //Score = Math.Max(TotalPenalty, ValidScore);
            Score = TotalPenalty;
            UnweightedScore = Math.Max(staticPenalty + (e2EPenalty + deadlinePenalty + jitterPenalty + orderPenalty + controlPenalty + partitionPenalty + controlDevPenalty) * w1, ValidScore);
        }

        public double MaxValidScore { get; }
        public double ValidScore { get; }
        public double E2EPenalty { get; }
        public double DeadlinePenalty { get; }
        public double JitterPenalty { get; }
        public double OrderPenalty { get; }
        public double ControlPenalty { get; }
        public double ControlDevPenalty { get; }
        public double PartitionPenalty { get; }
        public double TotalPenalty { get; }
        public double Score { get; set; }
        public bool IsValid { get; }
        public bool ValidSolution { get; }
        public int WorstCpu { get; }
        public int PossibleOrderViolation { get; }
        public int PossibleJitterViolation { get; }
        public int PossibleE2EViolation { get; }
        public int PossibleDeadlineViolation { get; }
        public int E2EViolations { get; private set; }
        public int OrderViolations { get; private set; }
        public int DeadlineViolations { get; private set; }
        public int JitterViolations { get; private set; }
        public int PartitionViolations { get; private set; }
        public int ViolationCount => E2EViolations + DeadlineViolations + JitterViolations + OrderViolations + PartitionViolations;
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
        private double Penalty(Application chain)
        {
            if (chain.FailedE2E)
            {
                int limit = chain.Threshold;
                int violation = Math.Min(limit, chain.E2E - chain.Threshold);
                return (double)violation / limit;
            }

            return 0;
        }
        private double CoCPenalty(Application app)
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

        private double Penalty(double avg, List<double> cocVal)
        {
            double _sum = 0;
            foreach (double val in cocVal)
            {
                _sum += Math.Pow((val - avg) , 2);
            }

            double retVal = Math.Sqrt(_sum / cocVal.Count);
            return retVal;
        }
        private double Fitness(Application app)
        {
            return Math.Min(app.E2E, app.Threshold);
        }
    }
}
