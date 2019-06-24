using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.IO;
using Accord.Math;
using Planner.Objects.Measurement;

namespace Planner.Objects
{
    public class QoC
    {
        private string filename;
        private string[] varnames;
        private Accord.IO.MatReader reader;
        private List<List<double>> qocDist = new List<List<double>>();
        public QoC()
        {
            filename = @"Data\matlab.mat";
            reader = new MatReader(filename);
            //Name of all variables in .mat file
            varnames = reader.FieldNames;
            
        }

        internal double calculateScore(List<JitterBase> jitters, List<TaskChain> chains)
        {
            
            foreach (var chain in chains)
            {
                int chainjitter = 0;
                double chainperiod = 0; 
                foreach (var virtask in chain.Tasks)
                {
                    foreach (var jitter in jitters)
                    {
                        if (virtask.Name == jitter.OwnerName)
                        {
                            chainjitter += jitter.MaxJitter;
                            chainperiod = jitter.Period;
                        }
                    }
                }

                if (loadinputdata(chain.Name))
                {
                    setQoCval(chain.Name,calcQoC(ustosec(chainjitter), chain.Name, chainperiod));
                }

                
            }

            return PenaltyCalculator();
        }

        private bool loadinputdata(string chainID)
        {
            return varnames.Contains(chainID);
        }
        //Takes Jitter in milisec
        private double calcQoC(double jitter, string chainname, double period)
        {
            double h = period / 1000000;
            var chain = reader[chainname];
            var currCF = chain["CF1"];

            var Jmat = currCF["Jmat"].GetValue<double[,]>();
            int JMATrow = Jmat.GetLength(0);
            int JMATcol = Jmat.GetLength(1);

            var starth = currCF["starth"].GetValue<double[,]>()[0,0];
            var steph = currCF["steph"].GetValue<double[,]>()[0,0];


            double J = 10;
            int Jitter_Percent = Convert.ToInt32(jitter / h * 100);
            if (Jitter_Percent > 100)
            {
                return J;
            }

            try
            {
                int h_index = Convert.ToInt32((h - starth) / steph);
                int J_index = Convert.ToInt32((Jitter_Percent) / (100.0 / (JMATcol - 1)));
                double J_min = Jmat[h_index, J_index];
                int nexth = h_index + 1;
                if (nexth >= JMATrow)
                {
                    nexth += -1;
                }

                int nextJ = J_index + 1;
                if (nextJ >= JMATcol)
                {
                    nextJ += -1;

                }

                double J_maxh = Jmat[nexth, J_index];
                double J_maxj = Jmat[h_index, nextJ];
                J = J_min + (J_maxh - J_min) * (h - (starth + h_index * steph)) / steph + (J_maxj - J_min) * (Jitter_Percent - (J_index * (100 / (JMATcol - 1)))) / (100 / (JMATcol - 1));

            }
            catch
            {
                return J;
            }


            return J;
        }

        private double ustosec(int time)
        {
            return time / 1000000.0;
        }


        private void setQoCval(string chainname, double qocval)
        {
            var chain = reader[chainname];
            var currCF = chain["CF1"];

            int typenumber = Convert.ToInt32(currCF["type"].GetValue<byte[,]>()[0, 0]);

            if (qocDist.Count >= typenumber)
            {
                qocDist[typenumber-1].Add(qocval);
            }
            else
            {
                for (int i = qocDist.Count; i < typenumber; i++)
                {
                    qocDist.Add(new List<double>());
                }
                qocDist[typenumber - 1].Add(qocval);
            }
        }

        private double PenaltyCalculator()
        {
            List<double> sums = new List<double>();
            List<double> disters = new List<double>();
            double penalty = 0;
            foreach (var type in qocDist)
            {
                double sum = 0;
                foreach (var qoc in type)
                {
                    sum += qoc;
                }

                double avr = sum / type.Count;
                double dister = 0;
                foreach (var qoc in type)
                {
                    dister += Math.Pow((avr - qoc),2);
                }

                dister = dister / type.Count;
                dister = Math.Pow(dister, 0.5);
                sums.Add(sum);
                
                disters.Add(dister);
                penalty += sum - dister;

            }

            if (penalty <= 0)
            {
                return 0;
            }
            return penalty;
        }
    }
}
