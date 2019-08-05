using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planner.Interfaces;

namespace Planner.Objects.Measurement
{
    public class Order : IMeasurement
    {
        public Order(string name)
        {
            Name = name;
            Tasks = new List<InorderTasks>();
            Length = 0;
            BadCycle = new List<int>();
        }
        public Order(string name, List<InorderTasks> tasks, List<int> badcycle )
        {
            Name = name;
            Tasks = new List<InorderTasks>();
            foreach (var item in tasks)
            {
                Tasks.Add(new InorderTasks(item.Name));
            }
            Length = Tasks.Count;
            BadCycle = new List<int>();
            foreach (var item in badcycle)
            {
                BadCycle.Add(item);
            }
        }
        public string Name { get; }
        public int Violations { get; private set; }
        public List<int> BadCycle { get; set; }
        public bool Failed => Violations > 0;
        public List<InorderTasks> Tasks { get; }
        public int Length;


        public void Reset()
        {
            Violations = 0;
            BadCycle.Clear();
            Tasks.ForEach(x => x.reset());
        }
        public void AddTask(Job job)
        {
            Tasks.Add(new InorderTasks(job.Name));
            Length++;
        }

        public void StartTask(Job job, int cycle)
        {
            foreach (var task in Tasks)
            {
                if (task.Name == job.Name)
                {
                    if (!task._started)
                    {
                        task._started = true;
                        task.Start(cycle);
                    }
                }
            }

        }
        public void EndTask(Job job, int cycle)
        {

            foreach (var task in Tasks)
            {
                if (task.Name == job.Name)
                {
                    if (task._started)
                    {
                        task._started = false;
                        task.End(cycle);
                    }
                }
            }
        }

        public int CheckViolation()
        {
            int instances = Int32.MaxValue;
            foreach (var task in Tasks)
            {
                int startnums = task.Starts.Count;
                int endnums = task.Ends.Count;
                if (instances > endnums)
                {
                    instances = endnums;
                }
                //instances = endnums;
                if (startnums != endnums)
                {
                    Violations++;
                }
            }
            for (int i = 0; i < (Length-1); i++)
            {
                if (Tasks[i].Ends.Count != Tasks[i + 1].Ends.Count)
                {
                    Violations++;
                }
            }

            Violations = 0;
            if (Violations == 0)
            {
                for (int i = 0; i < instances; i++)
                {
                    for (int j = 0; j < (Length - 1); j++)
                    {
                        if (Tasks[j].Ends[i] > Tasks[j + 1].Starts[i])
                        {
                            Violations++;
                        }
                    }
                }
            }
            else
            {
                Violations += instances;
            }
            return Violations;

        }

        public class InorderTasks
        {
            public InorderTasks(string name)
            {
                Name = name;
                _started = false;
                Starts = new List<int>();
                Ends = new List<int>();
            }
            public string Name { get; }
            public bool _started;
            public List<int> Starts;
            public List<int> Ends;

            public void reset()
            {
                _started = false;
                Starts.Clear();
                Ends.Clear();
            }

            public void Start(int cycle)
            {
                Starts.Add(cycle);

            }
            public void End(int cycle)
            {
                Ends.Add(cycle);
            }


        }
    }
}
