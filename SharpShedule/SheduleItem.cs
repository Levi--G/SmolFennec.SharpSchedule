using System;

namespace SharpShedule
{
    public class SheduleItem
    {
        public Action ToRun { get; set; }
        public DateTime Start { get; set; }
        public DateTime NextRun { get; set; }
        public TimeSpan? Interval { get; set; }
        public bool IsSheduled { get; set; } = false;

        public SheduleItem(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            this.ToRun = ToRun;
            this.Start = Start;
            this.Interval = Interval;
        }
    }

    public class StateSheduleItem<T> : SheduleItem
    {
        public T State { get; set; }

        public StateSheduleItem(Action<T> ToRun, DateTime Start, T State, TimeSpan? Interval = null) : base(() => { ToRun(State); }, Start, Interval)
        {
            this.State = State;
        }
    }
}