using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planner.Objects.Measurement;
using Planner.Objects.Models;

namespace PlanningTests
{
    public static class WorkloadGenerator
    {
        public static Configuration SetupConfiguration(List<int[]> macroticks)
        {
            Configuration config = new Configuration();
            config.Cpus = new List<Configuration.Cpu>();
            for (int cpuid = 0; cpuid < macroticks.Count; cpuid++)
            {
                Configuration.Cpu cpu = new Configuration.Cpu();
                cpu.Id = cpuid;
                cpu.Cores = new List<Configuration.Cpu.Core>();
                for (int coreid = 0; coreid < macroticks[cpuid].Length; coreid++)
                {
                    Configuration.Cpu.Core core = new Configuration.Cpu.Core();
                    core.Id = coreid;
                    core.MacroTick = macroticks[cpuid][coreid];
                    cpu.Cores.Add(core);
                }
                config.Cpus.Add(cpu);
            }

            return config;
        }

        public static Workload CreateWorkload()
        {
            Workload workload = new Workload();
            workload.Work = new Workload.WorkSchedule();
            workload.Work.Items = new List<Workload.Task>();
            workload.Work.Chains = new List<Workload.TaskChain>();
            return workload;
        }

        public static void CreateTask(this Workload workload, int id, int cpu, int core, int et, int t, int d, int ea, int maxJitter = -1)
        {
            string name = $"T{id}";
            Workload.Task task = new Workload.Task()
            {
                Id = id,
                Name = name,
                CpuId = cpu,
                CoreId = core,
                BCET = et,
                WCET = et,
                Periods = new List<Workload.Period>() { new Workload.Period { Value = t } },
                Deadline = d,
                EarliestActivation = ea,
                MaxJitter = maxJitter
            };
            workload.Work.Items.Add(task);
        }
    }
}
