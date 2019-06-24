using System;
using System.Linq;
using Planner.Objects.Models;

namespace Planner.Objects
{
    /// <summary>
    /// This entity represents the combined architecture.
    /// It containts all the processors, and allows one to access them individually.
    /// </summary>
    public class Environment
    {
        private readonly int _numCPUs;

        public Environment(Configuration configuration)
        {
            _numCPUs = configuration.Cpus.Count;
            Cpus = new Processor[_numCPUs];
            for (int i = 0; i < _numCPUs; i++)
            {
                Cpus[i] = new Processor(i, configuration.Cpus[i].Cores);
            }
        }
        public Environment(Processor[] cpus)
        {
            _numCPUs = cpus.Length;
            Cpus = cpus;
        }

        public Processor[] Cpus { get; }
        public int Hyperperiod => Cpus.Max(x => x.Hyperperiod);

        public void Initialize() => Cpus.ForEach(x => x.Initialize());
        public void QueueJob(Job job)
        {
            if (job.CpuId >= 0 && job.CpuId < _numCPUs)
            {
                Cpus[job.CpuId].QueueJob(job);
            }
            else
            {
                throw new Exception("All tasks should have specified their CPU.");
            }
        }
        public Environment Clone()
        {
            return new Environment(Cpus.Select(x=>x.Clone()).ToArray());
        }
        public Environment DeepClone()
        {
            return new Environment(Cpus.Select(x => x.DeepClone()).ToArray());
        }
    }
}
