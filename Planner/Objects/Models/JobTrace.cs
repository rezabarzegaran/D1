using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner.Objects.Models
{
    [Serializable()]
    public class JobTrace
    {
        public List<Item> items = new List<Item>();

        public class Item
        {
            public Item()
            {

            }

            public string OwnerName { get; set; }
            public int CoreId { get; set; }
            public int CpuId { get; set; }
            public List<Event> events = new List<Event>();

            public class Event
            {
                public Event()
                {

                }

                public int Cycle { get; set; }
                public int RemainingET { get; set; }
                public int StepSize { get; set; }
            }
        }
    }
}
