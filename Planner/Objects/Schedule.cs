using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Planner.Objects
{
    /// <summary>
    /// This entity is not complete yet.
    /// It will make use of the execution trace to build a static schedule.
    /// However, it is very closely related to Simulation.
    /// </summary>
    public class Schedule
    {
        private SVGDump _svgdump;
        private int _endCycle;


        public Schedule(Simulation simulation)
        {
            Simulation = simulation;
            Tasks = simulation.Tasks.ToList();
            Environment = simulation.Environment;
        }

        public Simulation Simulation { get; }
        public Environment Environment { get; }
        public List<Job> Tasks { get; }
        public int[][] Table { get; set; }
        

        public void Run()
        {
            _svgdump = new SVGDump();
            Simulation.Run(true);

            foreach (Processor cpu in Simulation.Environment.Cpus)
            {
                _svgdump.AddCPU(cpu.Id, cpu.CoreCount);
            }

            _endCycle = Simulation.Tasks.Select(x => x.ExecutionTrace.Select(y => y.Cycle + y.ExecutionSize).Max()).Max();
            int nrColumns = Simulation.Environment.Cpus.SelectMany(x => x.Cores).Count();
            Table = new int[nrColumns][];
            Enumerable.Range(0, nrColumns).ForEach(x => Table[x] = new int[_endCycle]);
            _svgdump.SetScope(_endCycle);
        }

        public void Build(string schedule, string svg)
        {
            // Build table
            int idx = 0;
            foreach (Core core in Simulation.Environment.Cpus.SelectMany(x => x.Cores))
            {
                for (int cycle = 0; cycle < _endCycle; cycle++)
                {
                        Table[idx][cycle] = -1;
                    
                }
                foreach (Job job in core.Tasks)
                {

                    foreach (Job.Execution execution in job.ExecutionTrace)
                    {
                        for (int cycle = execution.Cycle; cycle < execution.Cycle + execution.ExecutionSize; cycle++)
                        {
                                Table[idx][cycle] = job.Id;
   
                        }
                    }
                }

                idx++;
            }

            StringBuilder output = new StringBuilder();
            foreach (Processor cpu in Simulation.Environment.Cpus)
            {
                output.Append($"CPU:{cpu.Id}\n");
                foreach (Core core in cpu.Cores)
                {
                    output.Append($"CORE:{core.Id}\n");
                    
                    int[] cycles = new int[_endCycle];
                    for (int cycle = 0; cycle < _endCycle; cycle++)
                    {
                            cycles[cycle] = -1;
                        
                    }
                    foreach (Job job in core.Tasks)
                    {
                        foreach (Job.Execution execution in job.ExecutionTrace)
                        {
                            for (int cycle = execution.Cycle; cycle < execution.Cycle + execution.ExecutionSize; cycle++)
                            {

                                    cycles[cycle] = job.Id;
                               
                            }
                        }
                    }

                    for (int cycle = 0; cycle < _endCycle; cycle++)
                    {
                        if ((cycle % 250)== 0)
                        {
                            output.Append($"Cycle:{cycle}, Task:{cycles[cycle]}\n");
                        }
                        
                    }
                }
                if (!string.IsNullOrEmpty(schedule)) File.WriteAllText(schedule, output.ToString());
                //if (!string.IsNullOrEmpty(schedule)) _svgdump.Generate(svg);
            }


            /*StringBuilder table = new StringBuilder();

            table.AppendLine("************************* Static Schedule *************************");
            foreach (Processor cpu in Simulation.Environment.Cpus)
            {
                table.AppendLine($"Result for CPU: {cpu.Id}");
                foreach (Core core in cpu.Cores)
                {
                    table.AppendLine($"\nSchedule for CORE: {core.Id}");
                    foreach (Job job in core.Tasks)
                    {
                        StringBuilder builder = new StringBuilder();
                        builder.Append($"TASK[NAME={job.Name}, WCET={job.ExecutionTime}, PERIOD={job.Period}, DEADLINE={job.Deadline}] : ");
                        Job.Execution initial = job.ExecutionTrace.First();
                        int startCycle = initial.Cycle;
                        int length = initial.ExecutionSize;
                        builder.Append($"[^{startCycle},");
                        foreach (Job.Execution execution in job.ExecutionTrace.Skip(1))
                        {
                            if (execution.Cycle != startCycle + length)
                            {
                                builder.Append(execution.RemainingET == execution.ExecutionTime ? $"{startCycle + length - 1}.]" : $"{startCycle + length - 1}]");
                                _svgdump.AddTask(startCycle, length, job.CpuId, job.CoreId, job.Id);
                                startCycle = execution.Cycle;
                                builder.Append(execution.RemainingET == execution.ExecutionTime ? $"[^{startCycle}," : $"[{startCycle},");
                                length = execution.ExecutionSize;
                            }
                            else
                            {
                                length += execution.ExecutionSize;
                            }
                        }

                        builder.Append($"{startCycle + length - 1}.]");
                        _svgdump.AddTask(startCycle, length, job.CpuId, job.CoreId, job.Id);
                        table.AppendLine(builder.ToString());
                    }
                }
            }
            if (!string.IsNullOrEmpty(schedule)) File.WriteAllText(schedule, table.ToString());
            if (!string.IsNullOrEmpty(svg)) _svgdump.Generate(svg);*/
        }
    }
}
