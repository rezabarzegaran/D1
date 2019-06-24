namespace Planner.Objects
{
    /// <summary>
    /// Entity listing the upcoming event cycle, and provides a trigger to execute the appropiate
    /// core event.
    /// </summary>
    public class TaskEvent
    {
        private Core _core;

        public TaskEvent(Core core)
        {
            _core = core;
        }
 
        public int Cycle { get; set; }

        public void Trigger(bool debug)
        {
            _core.TriggerEvent(debug);
        }
    }
}
