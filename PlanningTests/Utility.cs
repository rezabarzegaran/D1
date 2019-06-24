using Planner.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using Planner.Objects.Measurement;

namespace PlanningTests
{
    public static class Utility
    {


        public static bool VerifyPeriod(List<Job.Execution> executions, bool ignoreDeadline, params (int cycle, int executionsize)[] periods)
        {
            if (executions.Count == periods.Length)
            {
                bool result = true;
                for (int idx = 0; idx < periods.Length; idx++)
                {
                    result &= executions[idx].Cycle == periods[idx].cycle;
                    result &= executions[idx].ExecutionSize == periods[idx].executionsize;
                    result &= ignoreDeadline || executions[idx].Cycle + executions[idx].ExecutionSize <= executions[idx].Deadline;
                }

                return result;
            }

            return false;
        }

        public static IEnumerable<Job.Execution> BoundTrace(Planner.Objects.Environment e, Job j)
        {
            int nriterations = e.Hyperperiod / j.Period;
            return j.ExecutionTrace.TakeWhile(x => x.Iteration < nriterations);
        }

        public static bool VerifyDeadlines(Job.Execution execution, Job job)
        {
            return (execution.Cycle + execution.ExecutionSize ) <= execution.Deadline;
        }
        public static bool VerifyEarliestActivation(int cycle, int iteration, Job job)
        {
            int release = job.Offset + (iteration * job.Period) + job.EarliestActivation;
            return (cycle >= release);
        }
        public static bool VerifyMacrotickCompliance(Planner.Objects.Environment environment, List<Job.Execution> cycles, Job job)
        {
            int macrotick = environment.Cpus[job.CpuId].Cores[job.CoreId].MacroTick;
            int remainder = job.ExecutionTime % macrotick;

            Queue<int> consecutiveExecutions = new Queue<int>();
            Queue<int> executionTimes = new Queue<int>();

            foreach (Job.Execution execution in cycles)
            {
                if (execution.ExecutionSize <= 0 || execution.ExecutionSize > macrotick) return false;
                if (consecutiveExecutions.Count == 0)
                {
                    consecutiveExecutions.Enqueue(execution.Cycle + execution.ExecutionSize);
                    executionTimes.Enqueue(execution.ExecutionSize);
                    continue;
                }


                if (execution.ExecutionSize % macrotick == remainder)
                {
                    if (executionTimes.Peek() % macrotick != 0) return false;
                }

                int value = consecutiveExecutions.Dequeue();
                consecutiveExecutions.Enqueue(execution.Cycle + execution.ExecutionSize);
                int et = executionTimes.Dequeue();

                executionTimes.Enqueue(et + execution.ExecutionSize);

            }

            return executionTimes.All(x => x == job.ExecutionTime);
        }

        public static bool VerifyE2EConstraints(Evaluator evaluator)
        {
            bool failed = false;
            foreach (TaskChain chain in evaluator.Chains)
            {
                if (chain.Failed)
                {
                    return false;
                }
            }

            return true;
        }


        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (T item in enumerable) action(item);
        }

    }
}
