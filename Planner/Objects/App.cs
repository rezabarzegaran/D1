using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner.Objects
{
    public class App
    {

        public App(string name, bool ca, bool inorder, IEnumerable<Job> taskchain)
        {
            Name = name;
            CA = ca;
            InOrder = inorder;
            Tasks = new List<ApplicationTasks>();
            foreach (Job task in taskchain)
            {
                Tasks.Add(new ApplicationTasks(task.Name));
            }
        }
        public App(string name, bool ca, bool inorder, IEnumerable<ApplicationTasks> taskchain)
        {
            Name = name;
            CA = ca;
            InOrder = inorder;
            Tasks = new List<ApplicationTasks>();
            foreach (ApplicationTasks task in taskchain)
            {
                Tasks.Add(new ApplicationTasks(task.Name));
            }
        }

        public string Name { get; }
        public bool CA { get; }
        public bool InOrder { get; }
        public List<ApplicationTasks> Tasks { get; }

        public App Clone()
        {
            return new App(Name, CA, InOrder, Tasks);
        }
        public App DeepClone()
        {
            return new App(Name, CA, InOrder, Tasks);
        }

        public class ApplicationTasks
        {
            public ApplicationTasks(string name)
            {
                Name = name;
            }
            public string Name { get; }
        }

    }
}
