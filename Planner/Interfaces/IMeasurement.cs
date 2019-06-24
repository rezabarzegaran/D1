using Planner.Objects;

namespace Planner.Interfaces
{
    /// <summary>
    /// This entity represents a single measurement type during a simulation.
    /// That is for each task a measurement is possible at the given cycle
    /// for starting and ending a task.
    /// </summary>
    public interface IMeasurement
    {
        bool Failed { get; }
        void Reset();
        void StartTask(Job job, int cycle);
        void EndTask(Job job, int cycle);
    }
}
