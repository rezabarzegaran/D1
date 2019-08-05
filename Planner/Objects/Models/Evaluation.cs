using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planner.Objects.Models
{
    [Serializable()]
    public class Evaluation
    {
        public List<Solution> Solutions = new List<Solution>();

        public class Solution
        {
            public Solution()
            {
            }
            public int Id { get; set; }
            public List<VM> vms = new List<VM>();
            public List<PE> cores = new List<PE>();

            public List<SolutionTask> Tasks = new List<SolutionTask>();
            public Score Scores = new Score();
            public Violation Violations = new Violation();





            public class VM
            {
                public VM()
                {

                }
                public string Name { get; set; }
                public int CPUID { get; set; }
                public int CoreID { get; set; }
                public int Slices { get; set; }
                public int Violation { get; set; }
                public bool Fixed { get; set; }

            }

            public class PE
            {
                public PE()
                {

                }
                public int CPUID { get; set; }
                public int CoreID { get; set; }
                public double Aviliability { get; set; }
                public int Hyperperiod { get; set; }
                public List<string> VM_Names = new List<string>();
                public List<string> Task_Names = new List<string>();
            }

            public class SolutionTask
            {
                public SolutionTask()
                {

                }
                public string Name { get; set; }
                public int CoreID { get; set; }
                public int CPUID { get; set; }
                public int Period { get; set; }
                public int WCET { get; set; }
                public double Cost { get; set; }

            }

            public class Score
            {
                public Score()
                {

                }
                public double Total { get; set; }
                public double E2E { get; set; }
                public double Jitter { get; set; }
                public double Deadline { get; set; }
                public double Order { get; set; }
                public double Control { get; set; }
                public double DevControl { get; set; }
                public double Separation { get; set; }
                public double Instance { get; set; }
                public double VM { get; set; }
                public bool valid { get; set; }
                public List<double> CoC = new List<double>();
                public List<double> Chains = new List<double>();
                public List<double> Deadlines = new List<double>();

            }
            public class Violation
            {
                public Violation()
                {

                }
                public int Total { get; set; }
                public int E2E { get; set; }
                public int Deadline { get; set; }
                public int Jitter { get; set; }
                public int Order { get; set; }
                public int Instance { get; set; }
                public int Separation { get; set; }

            }

        }
    }
}
