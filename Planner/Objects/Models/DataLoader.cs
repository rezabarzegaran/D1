using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Planner.Objects.Models
{
    /// <summary>
    /// This class facilitates loading various formats (Custom/TTech),
    /// returning a workload and configuration.
    /// In case of TTechs data format this class will convert the data.
    /// </summary>
    public class DataLoader
    {
        private static bool enableExecSlides = true;
        private static bool enableIdleSlides = false;
        private static bool enablePartitionSlides = true;
        
        public enum SVGTypes
        {
            exec,
            idle,
            tasks
        }

        private static SVGTypes svgOut = SVGTypes.exec;
        public static T Load<T>(string file)
        {
            object obj = null;
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                obj = serializer.Deserialize(fileStream);
            }

            return (T)obj;
        }
        public static void Save(string file, object data)
        {
            if (File.Exists(file)) File.Delete(file);
            XmlSerializer serializer = new XmlSerializer(data.GetType());
            using (FileStream fileStream = new FileStream(file, FileMode.OpenOrCreate))
            {
                serializer.Serialize(fileStream, data);
            }
        }
        public static (Workload, Configuration) Load(params string[] path)
        {
            Workload workload = Load<Workload>(path[0]);
            Configuration configuration = Load<Configuration>(path[1]);
            return (workload, configuration);
        }
        public static void Unload(string _resultfile, List<Simulation> s)
        {
            string TaskFolder = _resultfile + @"\Tasks";
            string CoreFolder = _resultfile + @"\Cores";
            System.IO.Directory.CreateDirectory(_resultfile);
            System.IO.Directory.CreateDirectory(TaskFolder);
            System.IO.Directory.CreateDirectory(CoreFolder);
            string file = _resultfile + @"\Results.xml";

            GenerateReport(file, s);
            GenerateCoreMap(CoreFolder, s);
            //GenerateTaskMap(CoreFolder, s);
            string filenameFirst = _resultfile + @"\table_first.svg";
            string filenameLast = _resultfile + @"\table_last.svg";
            //GenerateSVG(s.First(), filenameFirst, svgOut);
            //GenerateSVG(s.Last(), filenameLast, svgOut);
        }

        private static void GenerateReport(string _resultfile, List<Simulation> s)
        {
            Evaluation _evaluation = new Evaluation();

            int count = 0;
            foreach (Simulation simulation in s)
            {
                if (true)
                {
                    Evaluation.Solution temp = new Evaluation.Solution();
                    temp.Id = count;
                    temp.Hyperperiod = simulation.Environment.Hyperperiod;
                    SetCores(temp, simulation.Environment);
                    SetScores(temp, simulation.Fitness, simulation.Evaluator);

                    count++;
                    _evaluation.Solutions.Add(temp);
                }
            }
            Save(_resultfile, _evaluation);
        }

        public static void SetCores(Evaluation.Solution temp, Environment env)
        {
            foreach (var cpu in env.Cpus)
            {
                foreach (var core in cpu.Cores)
                {
                    Evaluation.Solution.PE pe = new Evaluation.Solution.PE();
                    pe.CPUID = cpu.Id;
                    pe.CoreID = core.Id;
                    pe.Aviliability = core.Availability;
                    

                    List<int> SILs = new List<int>();
                    foreach (var job in core.Tasks)
                    {
                        pe.Task_Names.Add(job.Name);
                        if (!SILs.Contains(job.Cil))
                        {
                            SILs.Add(job.Cil);
                        }
     
                    }

                    pe.Total_Partitions = SILs.Count;
                    temp.Cores.Add(pe);

                }
            }
        }

        public static void SetScores(Evaluation.Solution temp, FitnessScore fitness, Evaluator ev)
        {
            temp.Scores.Total = fitness.Score;
            temp.Scores.Validity = fitness.ValidSolution;
            temp.Scores.ValidScore = fitness.MaxValidScore;

            temp.Scores.E2EScore = fitness.E2EPenalty;
            temp.Scores.E2EViolation = fitness.E2EViolations;
            temp.Scores.E2EPossibleViolation = fitness.PossibleE2EViolation;

            temp.Scores.DeadlineScore = fitness.DeadlinePenalty;
            temp.Scores.DeadlineViolation = fitness.DeadlineViolations;
            temp.Scores.DeadlinePossibleViolation = fitness.PossibleDeadlineViolation;

            temp.Scores.JitterScore = fitness.JitterPenalty;
            temp.Scores.JitterViolation = fitness.JitterViolations;
            temp.Scores.JitterPossibleViolation = fitness.PossibleJitterViolation;

            temp.Scores.OrderScore = fitness.OrderPenalty;
            temp.Scores.OrderViolation = fitness.OrderViolations;
            temp.Scores.OrderPossibleViolation = fitness.PossibleOrderViolation;
            temp.Scores.QOCScore = fitness.ControlPenalty;
            temp.Scores.DevControl = fitness.ControlDevPenalty;

            temp.Scores.PartitionScore = fitness.PartitionPenalty;
            temp.Scores.PartitionViolation = fitness.PartitionViolations;

            foreach (var qoc in ev.Apps)
            {
                if (qoc.CA)
                {
                    temp.Scores.CoC.Add(qoc.Cost);
                }
                
            }
            foreach (var chain in ev.Apps)
            {
                temp.Scores.Chains.Add(chain.E2E);
            }

            foreach (var deadline in ev.Deadlines)
            {
                temp.Scores.Deadlines.Add(deadline.MaxDistance);
            }


            //temp.Scores.ExtensibilityScore = fitness.ExtensibilityPenalty;






        }



        public static void GenerateSVG(Simulation s, string filename, SVGTypes ouType)
        {
            SVGDump svg = new SVGDump();
            svg.SetScope(s.Environment.Hyperperiod);
            foreach (var cpu in s.Environment.Cpus)
            {
                svg.AddCPU(cpu.Id, cpu.CoreCount);
            }

            foreach (var schedule in s.Evaluator.Schedules)
            {
                switch (ouType)
                {
                    case SVGTypes.tasks:
                        foreach (var slice in schedule.TaskSlices)
                        {
                            svg.AddTask(slice.Start, slice.Duration, schedule.CpuId, schedule.CoreId, Convert.ToInt32(slice.Owner));
                        }
                        break;
                    case SVGTypes.exec:
                        foreach (var slice in schedule.ExecSlices)
                        {
                            svg.AddTask(slice.Start, slice.Duration, schedule.CpuId, schedule.CoreId, 1);
                        }
                        break;
                    default:
                        foreach (var slice in schedule.ExecSlices)
                        {
                            svg.AddTask(slice.Start, slice.Duration, schedule.CpuId, schedule.CoreId, 1);
                        }
                        break;
                }

            }

            svg.Generate(filename);


        }

        private static void GenerateCoreMap(string folder, List<Simulation> s)
        {
            string Execfoldername = folder + @"\ExecutionSlices";
            System.IO.Directory.CreateDirectory(Execfoldername);

            string Idlefoldername = folder + @"\IdleSlices";
            System.IO.Directory.CreateDirectory(Idlefoldername);

            string Partitionfoldername = folder + @"\PartitionSlices";
            System.IO.Directory.CreateDirectory(Partitionfoldername);

            int count = 0;
            foreach (Simulation simulation in s)
            {
                if (true)
                {
                    string Execfilename = Execfoldername + @"\Solution" + count + ".xml";
                    Map exemaps = new Map();
                    string Idlefilename = Idlefoldername + @"\Solution" + count + ".xml";
                    Map idlemap = new Map();

                    string Partitionfilename = Partitionfoldername + @"\Solution" + count + ".xml";
                    Map partitionmap = new Map();


                    foreach (var schedule in simulation.Evaluator.Schedules)
                    {
                        Map.CoreMap exectemp = new Map.CoreMap();
                        Map.CoreMap idletemp = new Map.CoreMap();
                        Map.CoreMap partitiontemp = new Map.CoreMap();
                        exectemp.CoreId = schedule.CoreId;
                        idletemp.CoreId = schedule.CoreId;
                        partitiontemp.CoreId = schedule.CoreId;
                        exectemp.CpuId = schedule.CpuId;
                        idletemp.CpuId = schedule.CpuId;
                        partitiontemp.CpuId = schedule.CpuId;
                        idletemp.Duration = schedule._Hyperperiod;
                        exectemp.Duration = schedule._Hyperperiod;
                        partitiontemp.Duration = schedule._Hyperperiod;
                        if (enableExecSlides)
                        {
                            foreach (var slice in schedule.ExecSlices)
                            {
                                Map.CoreMap.Event tempevent = new Map.CoreMap.Event();
                                tempevent.Start = slice.Start;
                                tempevent.End = slice.End;
                                tempevent.Duration = slice.Duration;
                                exectemp.events.Add(tempevent);

                            }
                            exemaps.Cores.Add(exectemp);

                        }

                        if (enablePartitionSlides)
                        {
                            foreach (var slice in schedule.PartitionSlices)
                            {
                                Map.CoreMap.Event tempevent = new Map.CoreMap.Event();
                                tempevent.Start = slice.Start;
                                tempevent.End = slice.End;
                                tempevent.Duration = slice.Duration;
                                partitiontemp.events.Add(tempevent);

                            }
                            partitionmap.Cores.Add(exectemp);

                        }

                        if (enableIdleSlides)
                        {
                            foreach (var slice in schedule.IdleSlices)
                            {
                                Map.CoreMap.Event tempevent = new Map.CoreMap.Event();
                                tempevent.Start = slice.Start;
                                tempevent.End = slice.End;
                                tempevent.Duration = slice.Duration;
                                idletemp.events.Add(tempevent);
                            }
                            idlemap.Cores.Add(idletemp);

                        }




                    }

                    count++;
                    if (enableExecSlides)
                    {
                        Save(Execfilename, exemaps);
                    }

                    if (enableIdleSlides)
                    {
                        Save(Idlefilename, idlemap);
                    }
                    if (enablePartitionSlides)
                    {
                        Save(Partitionfilename, partitionmap);
                    }



                }


            }
        }
    }
}
