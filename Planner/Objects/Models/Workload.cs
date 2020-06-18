using System.Collections.Generic;
using System.Xml.Serialization;

namespace Planner.Objects.Models
{
    /// <summary>
    /// Custom for loading custom configations not adhering to TTech's data format.
    /// Only used to load the taskset/taskchains.
    /// </summary>
    [XmlRoot("graphml")]
    public class Workload
    {
        [XmlElement("Graph")]
        public WorkSchedule Work { get; set; }

        public class WorkSchedule
        {
            [XmlElement("Node")]
            public List<Task> Items { get; set; }

            [XmlElement("Application")]
            public List<Application> Apps { get; set; }
        }

        public class Task
        {
            public Task()
            {
                CpuId = -1;
                CoreId = -1;
                MaxJitter = -1;
                Offset = 0;
                Cil = 1;
            }

            [XmlAttribute("Id")]
            public int Id { get; set; }

            [XmlAttribute("Name")]
            public string Name { get; set; }

            [XmlAttribute("WCET")]
            public int WCET { get; set; }

            [XmlAttribute("BCET")]
            public int BCET { get; set; }

            [XmlElement("Period")]
            public List<Period> Periods { get; set; }

            [XmlAttribute("Deadline")]
            public int Deadline { get; set; }

            [XmlAttribute("EarliestActivation")]
            public int EarliestActivation { get; set; }

            [XmlAttribute("MaxJitter")]
            public int MaxJitter { get; set; }

            [XmlAttribute("Offset")]
            public int Offset { get; set; }

            [XmlAttribute("DeadlineAdjustment")]
            public int DeadlineAdjustment { get; set; }

            [XmlAttribute("CpuId")]
            public int CpuId { get; set; }

            [XmlAttribute("CoreId")]
            public int CoreId { get; set; }

            [XmlAttribute("CIL")]
            public int Cil { get; set; }
        }

        public class Period
        {
            [XmlAttribute("Value")]
            public int Value { get; set; }
        }

        public class Runnable
        {
            [XmlAttribute("Name")]
            public string Name { get; set; }
        }
        public class Application
        {
            public Application()
            {
                CA = false;
                Inorder = false;
                EndToEndDeadline = -1;
            }
            [XmlAttribute("E2E")]
            public int EndToEndDeadline { get; set; }

            [XmlAttribute("Name")]
            public string Name { get; set; }

            [XmlAttribute("Inorder")]
            public bool Inorder { get; set; }

            [XmlAttribute("CA")]
            public bool CA { get; set; }

            [XmlElement("Runnable")]
            public List<Runnable> Runnables { get; set; }
        }
    }
}
