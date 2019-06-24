using System;
using System.Collections.Generic;

namespace Planner.Objects
{
    /// <summary>
    /// Represents a Task instance.
    /// A task is defined by the Period, Deadline, ExecutionTime, EarliestActivation and processor mapping.
    /// Additionally, some tasks have jitter constraints, some have core assignment.
    /// </summary>
    public class Job
    {
        private readonly int _coreId;
        private int _iteration;
        private Evaluator _evaluator;

        public Job(int id, string name, int period, int deadline, int executionTime, int earliestActivation, int jitterThreshold, int cpuId, int coreId, double corecf, Evaluator evaluator, List<int> periods)
        {
            _iteration = 0;
            Id = id;
            Name = name;
            Period = period;
            
            ExecutionTime_base = executionTime;
            ExecutionTime = Convert.ToInt32(ExecutionTime_base * corecf);
            EarliestActivation = earliestActivation;
            JitterThreshold = jitterThreshold;
            _coreId = coreId;
            CpuId = cpuId;
            CoreId = coreId;
            CoreCF = corecf;
            Deadline = deadline;
            // TODO: remove following 3 lines when unittesting is setup correcly.
            Release = (Period * _iteration) + Offset + EarliestActivation;
            AbsoluteDeadline = Release + Deadline - EarliestActivation;
            RemainingET = ExecutionTime;
            Cost = (executionTime * 100.0d) / Period;
            ExecutionTrace = new List<Execution>();
            _evaluator = evaluator;
            Periods = new List<int>(periods);
        }

        public Job(int id, string name, int period, int deadline, int executionTime, int earliestActivation, int jitterThreshold, int cpuId, int coreId, double corecf, Evaluator evaluator, List<int> periods, List<Execution> _executionTrace)
        {
            _iteration = 0;
            Id = id;
            Name = name;
            Period = period;

            ExecutionTime_base = executionTime;
            ExecutionTime = Convert.ToInt32(ExecutionTime_base * corecf);
            EarliestActivation = earliestActivation;
            JitterThreshold = jitterThreshold;
            _coreId = coreId;
            CpuId = cpuId;
            CoreId = coreId;
            CoreCF = corecf;
            Deadline = deadline;
            // TODO: remove following 3 lines when unittesting is setup correcly.
            Release = (Period * _iteration) + Offset + EarliestActivation;
            AbsoluteDeadline = Release + Deadline - EarliestActivation;
            RemainingET = ExecutionTime;
            Cost = (executionTime * 100.0d) / Period;
            ExecutionTrace = new List<Execution>();
            foreach (var _trace in _executionTrace)
            {
                ExecutionTrace.Add(new Execution(_trace.Cycle, _trace.Iteration, _trace.Offset, _trace.Release, _trace.Deadline, _trace.Period, _trace.EarliestActivation, _trace.ExecutionTime, _trace.RemainingET, _trace.ExecutionSize));

            }
            _evaluator = evaluator;
            Periods = new List<int>(periods);
        }

        public int Id { get; }
        public string Name { get; }
        public int Offset { get; set; }
        public int Release { get; private set; }
        public int ExecutionTime { get; set; }
        public int ExecutionTime_base { get; }
        public int EarliestActivation { get; }
        public int JitterThreshold { get; }
        public int Deadline { get; }
        public int AbsoluteDeadline { get; private set; }
        public int PriorityDeadline { get; private set; }
        public int DeadlineAdjustment { get; set; }
        public int Period { get; set; }
        public double Cost { get; }
        public int CpuId { get; }
        public int CoreId { get; private set; }
        public double CoreCF { get; set; }
        public List<int> Periods { get; private set; }

        public int RemainingET { get; private set; }
        public int NextEvent(int relativeCycle, int macrotick) => relativeCycle + (RemainingET < macrotick ? RemainingET : macrotick);
        public int NextEventLength(int macrotick) => RemainingET < macrotick ? RemainingET : macrotick;
        public bool FinalEvent(int macrotick) => RemainingET == NextEventLength(macrotick);
        public int MaxOffset => (((Period - RemainingET) >=0 )? (Period - RemainingET) : 0 );
        public int MaxDeadlineAdjustment => (Deadline - EarliestActivation);
        public bool HasCoreAffinity => _coreId != -1;
        public List<Execution> ExecutionTrace { get; }

        public void Reset()
        {
            Release = (Period * _iteration) + Offset + EarliestActivation;
            AbsoluteDeadline = Release + Deadline - EarliestActivation;
            RemainingET = ExecutionTime;
            PriorityDeadline = AbsoluteDeadline - DeadlineAdjustment;
        }
        public void SetEnvironment(int coreId, double CF)
        {
            ExecutionTime = Convert.ToInt32(ExecutionTime_base * CF);
            
            CoreId = coreId;
            CoreCF = CF;
        }
        /// <summary>
        /// Executes a execution slice from the cycle-skiplength.
        /// An execution slice is the smallest uninteruptable task fragment
        /// defined by the macrotick.
        /// </summary>
        public void Execute(int cycle, bool enableTrace, int macrotick)
        {
            int skiplength = NextEventLength(macrotick);
            enableTrace.Then(BuildSchedule, cycle - skiplength, skiplength);
            //Console.WriteLine(this);
            if (RemainingET == ExecutionTime)
            {
                _evaluator.StartTask(this, cycle - skiplength);
            }

            RemainingET -= skiplength;
            if (RemainingET == 0)
            {
                _evaluator.EndTask(this, cycle);
                // TODO: Use reset when unittests are complete
                Release = (Period * ++_iteration) + Offset + EarliestActivation;
                AbsoluteDeadline = Release + Deadline - EarliestActivation;
                RemainingET = ExecutionTime;
                PriorityDeadline = AbsoluteDeadline - DeadlineAdjustment;
            }
        }
        public Job Clone()
        {
            return new Job(Id, Name, Period, Deadline, ExecutionTime_base, EarliestActivation, JitterThreshold, CpuId, _coreId, CoreCF, _evaluator, Periods);
        }
        public Job DeepClone()
        {
            return new Job(Id, Name, Period, Deadline, ExecutionTime_base, EarliestActivation, JitterThreshold, CpuId, _coreId, CoreCF, _evaluator, Periods, ExecutionTrace);
        }
        public override string ToString()
        {
            string affinity = $", CPU: {CpuId:D}";
            affinity += $", Core: {CoreId:D}";
            return $@"Job{Id}[{Name}] - O:{Offset:D2}, DO:{DeadlineAdjustment}, R: {Release:D5}, D: {AbsoluteDeadline:D5}, P: {Period:D5}, EA:{EarliestActivation:D5}, ET: {ExecutionTime:D5}, RET: {RemainingET:D5}{affinity}";
        }

        private void BuildSchedule(int cycle, int executionSize)
        {
            ExecutionTrace.Add(new Execution(cycle, _iteration, Offset, Release, AbsoluteDeadline, Period, EarliestActivation, ExecutionTime, RemainingET, executionSize));
        }

        public struct Execution
        {
            public Execution(int cycle, int iteration, int offset, int release, int deadline, int period, int earliestActivation, int executionTime, int remainingEt, int executionSize)
            {
                Cycle = cycle;
                Iteration = iteration;
                Offset = offset;
                Release = release;
                Deadline = deadline;
                Period = period;
                EarliestActivation = earliestActivation;
                ExecutionTime = executionTime;
                RemainingET = remainingEt;
                ExecutionSize = executionSize;
            }
            public int Cycle { get; }
            public int Iteration { get; }
            public int Offset { get; }
            public int Release { get; }
            public int Deadline { get; }
            public int Period { get; }
            public int EarliestActivation { get; }
            public int ExecutionTime { get; }
            public int RemainingET { get; }
            public int ExecutionSize { get; }
        }
    }
}
