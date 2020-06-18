using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner.Objects.Models
{
    [Serializable()]
    public class Map
    {
        public List<CoreMap> Cores = new List<CoreMap>();

        public class CoreMap
        {
            public CoreMap()
            {

            }
            public int CoreId { get; set; }
            public int CpuId { get; set; }
            public long Duration { get; set; }
            public List<Event> events = new List<Event>();

            public class Event
            {
                public Event()
                {

                }

                public int Start { get; set; }
                public int End { get; set; }
                public int Duration { get; set; }
            }
        }
    }


}
