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
            public long Hyperperiod { get; set; }
            public List<PE> Cores = new List<PE>();
            public Score Scores = new Score();

            public class PE
            {
                public PE()
                {

                }
                public int CPUID { get; set; }
                public int CoreID { get; set; }
                public double Aviliability { get; set; }
                public int Total_Partitions { get; set; }
                public List<string> Task_Names = new List<string>();
            }

            public class Score
            {
                public Score()
                {

                }
                public double Total { get; set; }
                public bool Validity { get; set; }
                public double ValidScore { get; set; }

                public double E2EScore { get; set; }
                public int E2EViolation { get; set; }
                public int E2EPossibleViolation { get; set; }

                public double JitterScore { get; set; }
                public int JitterViolation { get; set; }
                public int JitterPossibleViolation { get; set; }

                public double DeadlineScore { get; set; }
                public int DeadlineViolation { get; set; }
                public int DeadlinePossibleViolation { get; set; }

                public double OrderScore { get; set; }
                public int OrderViolation { get; set; }
                public int OrderPossibleViolation { get; set; }

                public double QOCScore { get; set; }
                public double DevControl { get; set; }

                public double PartitionScore { get; set; }
                public int PartitionViolation { get; set; }

                public double Instance { get; set; }
                
                public List<double> CoC = new List<double>();
                public List<double> Chains = new List<double>();
                public List<double> Deadlines = new List<double>();

                public int TotalViolation { get; set; }
                
                   

            }

        }
    }
}
