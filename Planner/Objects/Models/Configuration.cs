using System.Collections.Generic;
using System.Xml.Serialization;

namespace Planner.Objects.Models
{
    /// <summary>
    /// Custom for loading custom configations not adhering to TTech's data format.
    /// Only used to load the architecture.
    /// </summary>
    [XmlRoot("Configuration")]
    public class Configuration
    {
        [XmlElement("Cpu")]
        public List<Cpu> Cpus { get; set; }

        public class Cpu
        {
            public Cpu()
            {
            }

            [XmlAttribute("Id")]
            public int Id { get; set; }

            [XmlElement("Core")]
            public List<Core> Cores { get; set; }

            public class Core
            {
                public Core()
                {
                }

                [XmlAttribute("Id")]
                public int Id { get; set; }

                [XmlAttribute("MacroTick")]
                public int MacroTick { get; set; }

                [XmlAttribute("CF")]
                public double CF { get; set; }
            }
        }
    }
}
