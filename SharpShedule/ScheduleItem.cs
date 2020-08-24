using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpSchedule
{
    public class ScheduleItem : IDisposable
    {
        private DateTime start;

        public Action ToRun { get; }
        public Func<Task> ToRunAsync { get; }
        public DateTime Start
        {
            get => start;
            set
            {
                if (IsScheduled) { throw new Exception("Can't modify a scheduled item"); }
                start = value;
            }
        }
        public DateTime NextRun { get; internal set; }
        public TimeSpan? Interval { get; set; }
        public bool IsScheduled { get; internal set; } = false;
        public bool CanSkip { get; set; } = true;
        public SynchronizationContext SynchronizationContext { get; set; }
        public bool RunAsync { get; set; }

        public ScheduleItem(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            this.ToRun = ToRun;
            this.Start = Start;
            this.Interval = Interval;
        }

        public ScheduleItem(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            this.ToRunAsync = ToRun;
            this.Start = Start;
            this.Interval = Interval;
        }

        public void Dispose()
        {
            IsScheduled = false;
        }
    }

    public class StateScheduleItem<T> : ScheduleItem
    {
        public T State { get; set; }

        public StateScheduleItem(Action<T> ToRun, DateTime Start, T State, TimeSpan? Interval = null) : base(() => ToRun(State), Start, Interval)
        {
            this.State = State;
        }

        public StateScheduleItem(Func<T, Task> ToRun, DateTime Start, T State, TimeSpan? Interval = null) : base(() => ToRun(State), Start, Interval)
        {
            this.State = State;
        }
    }
}