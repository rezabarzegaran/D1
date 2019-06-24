using System;
using System.Collections.Generic;
using Planner.Objects;

namespace Planner.Interfaces
{
    public interface IMetaHeuristic
    {
        Simulation BestSolution { get;  }
        FitnessScore Fitness { get;  }
        int ElapsedTime { get;  }
        void Run();
    }
}
