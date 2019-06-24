using System.Collections.Generic;
using System.Linq;
using Planner.Objects.Models;

namespace Planner.Objects
{
    /// <summary>
    /// This entity represents the instance of a processor with respect to modelling the architecture.
    /// Each processor contains one or more cores that each can queue tasks.
    /// </summary>
    public class Processor
    {
        public Processor(int cpuId, List<Configuration.Cpu.Core> cores)
        {
            Id = cpuId;
            Cores = new Core[cores.Count];
            int id = 0;
            foreach (Configuration.Cpu.Core core in cores)
            {
                Cores[id] = new Core(cpuId, id, core.MacroTick , core.CF);
                id++;
            }

            CoreCount = cores.Count;
        }
        public Processor(Core[] cores, int id)
        {
            Cores = cores;
            CoreCount = cores.Length;
            Id = id;
        }

        public int Hyperperiod => Cores.Max(x => x.Hyperperiod);
        public int CoreCount { get; }
        public Core[] Cores { get; }
        public int Id { get; }
        /// <summary>
        /// Initializes all cores.
        /// This method should be called before the simulation is run.
        /// </summary>
        public void Initialize() => Cores.ForEach(x => x.Initialize());
        /// <summary>
        /// Enqueue a job on the processor. Automatically distributes tasks without core affinity to the
        /// core with the most availability.
        /// </summary>
        /// <param name="job"></param>
        public void QueueJob(Job job)
        {
            if (job.CoreId >= 0 && job.CoreId < Cores.Length)
            {
                Cores[job.CoreId].QueueJob(job);
            }
            // Swappable tasks
            else
            {
                Cores.OrderByDescending(x => x.Availability).FirstOrDefault()?.QueueJob(job);
            }
        }

        public Processor Clone()
        {
            return new Processor(Cores.Select(x => x.Clone()).ToArray(), Id);
        }
        public Processor DeepClone()
        {
            return new Processor(Cores.Select(x => x.DeepClone()).ToArray(), Id);
        }
    }
}
