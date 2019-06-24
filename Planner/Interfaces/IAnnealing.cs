using System;
using System.Collections.Generic;
using Planner.Objects;

namespace Planner.Interfaces
{
    public interface IAnnealing : IMetaHeuristic
    {
        List<(TimeSpan, double)> Temperatures { get;  }
        List<(TimeSpan, double)> GeneratedNeighborValues { get; }
        List<(TimeSpan, double)> AcceptedNeighborValues { get;  }
        List<(TimeSpan, double)> BestNeighborValues { get;  }
    }
}
