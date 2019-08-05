using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    public class CoreMap : IMeasurement
    {
        // DO NOT use a queue. Otherwise we need to recreate it continously.


        public CoreMap(int cpuid, int coreid)
        {

            CpuID = cpuid;
            CoreID = coreid;
            Tasks = new List<Job>();
            Events = new List<Event>();
            Separation = new Dictionary<int, int>();
            SliceEvents = new List<Event>();
            TaskEvents = new List<Event>();
            SepSpace = new List<SeprateSpace>();
            FailedSepSpace = new List<SeprateSpace>();
            TotalFails = 0;
        }

        public CoreMap(int cpuid, int coreid, List<Job> jobs, List<Event> events, Dictionary<int, int> separation)
        {
            CpuID = cpuid;
            CoreID = coreid;
            Tasks = new List<Job>();
            foreach (var job in jobs)
            {
                AddTask(job);
            }
            Events = new List<Event>();
            foreach (var _event in events)
            {
                Events.Add(new Event(_event.Cycle, _event.Size, _event.Ownername, _event.Cil)); ;
            }
            
            Separation = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> entry in separation)
            {
                Separation.Add(entry.Key, entry.Value);
            }

            SliceEvents = new List<Event>();
            SepSpace = new List<SeprateSpace>();
            FailedSepSpace = new List<SeprateSpace>();
            TaskEvents = new List<Event>();

            CreateMap();
            getNFailed();
        }

        public int CpuID { get; }
        public int CoreID { get; }
        public bool Failed => getValidSpace();
        public List<Job> Tasks { get; }
        public Dictionary<int,int> Separation;
        public List<Event> Events;
        public List<Event> SliceEvents;
        public List<Event> TaskEvents;
        public List<SeprateSpace> SepSpace;
        public List<SeprateSpace> FailedSepSpace;
        public int TotalFails;
        public void Reset()
        {
            //Separation.Clear();
            Events.Clear();
            SliceEvents.Clear();
            FailedSepSpace.Clear();
            SepSpace.Clear();
            TaskEvents.Clear();
            TotalFails = 0;
        }
        public void AddTask(Job job)
        {
            Tasks.Add(job);
        }


        public void StartTask(Job job, int cycle)
        {
        }
        public void EndTask(Job job, int cycle)
        {
        }

        public void CreateMap()
        {
            Events.Clear();
            List<Job> relatedJobs = Tasks.FindAll(x => ((x.CoreId == CoreID) && (x.CpuId == CpuID)));

            foreach (var job in relatedJobs)
            {
               
                foreach (var execution in job.ExecutionTrace)
                {
                    Events.Add(new Event(execution.Cycle, execution.ExecutionSize, job.Name, job.Cil));
                }
            }
            GFG gg = new GFG();
            Events.Sort(gg);
            CreateSeparateMap();
            CreateTaskMap();
            CreateSpaceMap();

        }

        public void CreateSeparateMap()
        {
            SliceEvents.Clear();
            int startIndex = 0;
            int counter = 0;
            int lastEnd = Events[Events.Count - 1].Cycle + Events[Events.Count - 1].Size;
            while (startIndex < Events.Count)
            {

                int startCycle = Events[startIndex].Cycle;
                int currCil = Events[startIndex].Cil;
                string currSlice = counter.ToString();
                int endCycle = startCycle + Events[startIndex].Size;
                for (int j = (startIndex + 1); j < Events.Count; j++)
                {
                    if (endCycle == Events[j].Cycle)
                    {
                        if (currCil == Events[j].Cil)
                        {
                            endCycle += Events[j].Size;
                        }
                        else
                        {
                            SliceEvents.Add(new Event(startCycle, (endCycle - startCycle), currSlice, currCil));
                            startIndex = j;
                            break;
                        }

                    }
                    else
                    {
                        SliceEvents.Add(new Event(startCycle, (endCycle - startCycle), currSlice, currCil));
                        startIndex = j;
                        break;
                    }
                }
                
                if ((startIndex == (Events.Count) - 1) || (endCycle == lastEnd))
                {
                    SliceEvents.Add(new Event(startCycle, (endCycle - startCycle), currSlice, currCil));
                    break;
                }

                counter++;

            }
        }

        public void CreateSpaceMap()
        {
            SepSpace.Clear();
            for (int i = 0; i < (SliceEvents.Count - 1); i++)
            {
                int _space = SliceEvents[i + 1].Cycle - SliceEvents[i].Cycle - SliceEvents[i].Size;
                if (_space < 0)
                {
                    ;
                }
                SepSpace.Add(new SeprateSpace(_space,SliceEvents[i + 1].Cil, Separation));
            }
        }
        public void CreateTaskMap()
        {
            TaskEvents.Clear();
            int startIndex = 0;
            int lastEnd = Events[Events.Count - 1].Cycle + Events[Events.Count - 1].Size;
            while (startIndex < Events.Count)
            {

                int startCycle = Events[startIndex].Cycle;
                int currCil = Events[startIndex].Cil;
                string currSlice = Events[startIndex].Ownername;
                int endCycle = startCycle + Events[startIndex].Size;
                for (int j = (startIndex + 1); j < Events.Count; j++)
                {
                    if (endCycle == Events[j].Cycle)
                    {
                        if (currSlice == Events[j].Ownername)
                        {
                            endCycle += Events[j].Size;
                        }
                        else
                        {
                            TaskEvents.Add(new Event(startCycle, (endCycle - startCycle), currSlice, currCil));
                            startIndex = j;
                            break;
                        }

                    }
                    else
                    {
                        TaskEvents.Add(new Event(startCycle, (endCycle - startCycle), currSlice, currCil));
                        startIndex = j;
                        break;
                    }
                }

                if ((startIndex == (Events.Count - 1)) || (endCycle == lastEnd))
                {
                    TaskEvents.Add(new Event(startCycle, (endCycle - startCycle), currSlice, currCil));
                    break;
                }


            }
        }

        public void CalcSeparation()
        {
            Separation.Clear();
            List<Job> relatedJobs = Tasks.FindAll(x => ((x.CoreId == CoreID) && (x.CpuId == CpuID)));
            foreach (var job in relatedJobs)
            {
                int currentsep = 0;
                if (Separation.TryGetValue(job.Cil, out currentsep))
                {
                    if (currentsep < (job.ExecutionTime / 20))
                    {
                        Separation[job.Cil] = (job.ExecutionTime / 20);
                    }
                }
                else
                {
                    Separation.Add(job.Cil, (job.ExecutionTime / 20));
                }
            }

        }

        public int getWorstCil()
        {
            Dictionary<int, int> FailedCil = new Dictionary<int, int>();
            foreach (var space in SepSpace)
            {
                if (space.Failed)
                {
                    int currFailed = 0;
                    if (FailedCil.TryGetValue(space.Cil, out currFailed))
                    {
                        FailedCil[space.Cil] = currFailed + 1;
                    }
                    else
                    {
                        FailedCil.Add(space.Cil, 1);
                    }


                }
            }

            int worstcil = -1;
            int Nfailed = 0;
            foreach (KeyValuePair<int, int> entry in FailedCil)
            {
                if (entry.Value > Nfailed)
                {
                    Nfailed = entry.Value;
                    worstcil = entry.Key;
                }
            }

            return worstcil;

        }
        private bool getValidSpace()
        {


            foreach (var space in SepSpace)
            {
                if (space.Failed)
                {
                    return true;
                }
            }

            return false;
        }

        public int getNSlices()
        {
            return SliceEvents.Count;
        }

        public int getNFailed()
        {
            FailedSepSpace.Clear();
            int counts = 0;
            foreach (var space in SepSpace)
            {
                if (space.Failed)
                {
                    counts++;
                    FailedSepSpace.Add(new SeprateSpace(space.Space, space.Cil, space.defaulSpace));
                }
            }

            TotalFails = counts;
            return counts;
            
        }

        public int getDuration()
        {
            return (Events[Events.Count - 1].Cycle + Events[Events.Count - 1].Size);
        }



        public class Event
        {
            public Event(int cycle, int size, string ownername, int cil)
            {
                Cycle = cycle;
                Size = size;
                Ownername = ownername;
                Cil = cil;
            }
            public int Cycle { get; }
            public int Size { get; }
            public string Ownername { get; }
            public int Cil { get; }
        }

        public class SeprateSpace
        {
            public SeprateSpace(int space, int cil, Dictionary<int, int> separation)
            {
                Space = space;
                Cil = cil;
                defaulSpace = new Dictionary<int, int>();
                foreach (KeyValuePair<int, int> entry in separation)
                {
                    defaulSpace.Add(entry.Key, entry.Value);
                }
            }
            public int Space { get; }
            public int Cil { get; }
            public bool Failed => Space < defaulSpace[Cil];
            public Dictionary<int, int> defaulSpace;

        }

        class GFG : IComparer<Event>
        {
            public int Compare(Event x, Event y)
            {
                return x.Cycle.CompareTo(y.Cycle);


            }
        }



    }
}
