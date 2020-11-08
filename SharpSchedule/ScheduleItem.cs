using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpSchedule
{
    /// <summary>
    /// Keeps track of scheduled jobs
    /// </summary>
    public class ScheduleItem : IDisposable
    {
        private DateTime start;

        /// <summary>
        /// An action to be run.
        /// </summary>
        public Action ToRun { get; }

        /// <summary>
        /// A function returning a task to be run.
        /// </summary>
        public Func<Task> ToRunAsync { get; }

        /// <summary>
        /// The first time this item should run.
        /// </summary>
        public DateTime Start
        {
            get => start;
            set
            {
                if (IsScheduled) { throw new Exception("Can't modify a scheduled item"); }
                start = value;
            }
        }

        /// <summary>
        ///The next time the item will be run.
        /// </summary>
        public DateTime NextRun { get; internal set; }

        /// <summary>
        /// The interval between runs. Leave null for single run trigger.
        /// </summary>
        public TimeSpan? Interval { get; set; }

        /// <summary>
        /// Returns if the item is already scheduled to run on a scheduler.
        /// </summary>
        public bool IsScheduled { get; internal set; } = false;

        /// <summary>
        /// Sets if the item can skip a run if the scheduler is running behind (true) or should be run the exact number of times (false).
        /// </summary>
        public bool CanSkip { get; set; } = true;

        /// <summary>
        /// When set will run the job synchronised on this context.
        /// </summary>
        public SynchronizationContext SynchronizationContext { get; set; }

        /// <summary>
        /// Sets the job to run asynchronous from the scheduler.
        /// </summary>
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
        /// <summary>
        /// A state object to be passed to the job when it is run.
        /// </summary>
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