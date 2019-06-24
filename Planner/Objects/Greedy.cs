using Planner.Interfaces;

namespace Planner.Objects
{
    /// <summary>
    /// This class represents the typical simulated annealing implementation.
    /// </summary>
    public class Greedy : IMetaHeuristic
    {
        protected readonly Simulation _initialSolution;

        public Greedy(Simulation initialSolution)
        {
            _initialSolution = initialSolution;
        }

        public Simulation BestSolution { get; protected set; }
        public FitnessScore Fitness { get; protected set; }
        public int ElapsedTime { get; protected set; }

        public void Run()
        {
            long start = System.Environment.TickCount;
            BestSolution = _initialSolution;
            BestSolution.Run(false);
            Fitness = BestSolution.Fitness;
            ElapsedTime = (int)(System.Environment.TickCount - start);
        }
    }
}
