using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpSchedule
{
    public abstract class ScheduleBase
    {
        /// <summary>
        /// Occurs when a job encounters an Error.
        /// </summary>
        public event EventHandler<Exception> OnError;

        /// <summary>
        /// Occurs when the scheduler missed the execution of a job outside the minimum precision specified.
        /// </summary>
        public event EventHandler<TimeSpan> OnRunningBehind;

        protected List<ScheduleItem> ScheduleItems = new List<ScheduleItem>();
        protected readonly object ScheduleKey = new object();

        /// <summary>
        /// The target precision the scheduler will try to uphold.
        /// </summary>
        public int Precision { get; set; } = 100;

        /// <summary>
        /// The minimum precision that should be upheld, will raise OnRunningBehind when not upheld.
        /// </summary>
        public int MinPrecision { get; set; } = 2000;

        protected void DoOnError(Exception e)
        {
            OnError?.Invoke(this, e);
        }

        protected bool DoSingleCheck()
        {
            var item = GetNextItem();
            if (item != null && item.NextRun < DateTime.Now.AddMilliseconds(Precision))
            {
                if (item.IsScheduled)
                {
                    var ts = DateTime.Now.Subtract(item.NextRun);
                    if (ts.TotalMilliseconds > MinPrecision)
                    {
                        OnRunningBehind?.Invoke(this, ts);
                    }
                    RunItem(item);
                }
                else
                {
                    Remove(item);
                }
                return true;
            }
            return false;
        }

        protected void RunItem(ScheduleItem item)
        {
            if (item.SynchronizationContext != null)
            {
                if (item.RunAsync)
                {
                    item.SynchronizationContext.Post((s) => { RunItemSync(item); }, null);
                }
                else
                {
                    item.SynchronizationContext.Send((s) => { RunItemSync(item); }, null);
                }
            }
            else if (item.RunAsync)
            {
                RunItemAsync(item);
            }
            else
            {
                RunItemSync(item);
            }
            lock (ScheduleKey)
            {
                if (item.Interval.HasValue)
                {
                    item.NextRun = GetNextTime(item.NextRun, item.Interval.Value, item.CanSkip);
                    ReloadScheduleInternal();
                }
                else
                {
                    item.IsScheduled = false;
                    ScheduleItems.Remove(item);
                }
            }
        }

        /// <summary>
        /// Removes a scheduled item from the schedule.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public void Remove(ScheduleItem item)
        {
            lock (ScheduleKey)
            {
                item.IsScheduled = false;
                ScheduleItems.Remove(item);
            }
        }

        /// <summary>
        /// Removes a series of items from the schedule.
        /// </summary>
        /// <param name="items">The series of items to remove.</param>
        public void RemoveRange(IEnumerable<ScheduleItem> items)
        {
            lock (ScheduleKey)
            {
                foreach (var item in items)
                {
                    item.IsScheduled = false;
                    ScheduleItems.Remove(item);
                }
            }
        }

        private void RunItemAsync(ScheduleItem item)
        {
            Task.Factory.StartNew(() => { RunItemSync(item); });
        }

        private void RunItemSync(ScheduleItem item)
        {
            try
            {
                item.ToRun?.Invoke();
                if (item.ToRunAsync != null)
                {
                    RunTaskSync(item.ToRunAsync());
                }
            }
            catch (Exception e)
            {
                DoOnError(e);
            }
        }

        private async void RunTaskSync(Task t)
        {
            try
            {
                await t;
            }
            catch (Exception e)
            {
                DoOnError(e);
            }
        }

        protected ScheduleItem GetNextItem()
        {
            ScheduleItem i;
            lock (ScheduleKey)
            {
                i = ScheduleItems.FirstOrDefault();
            }
            return i;
        }

        /// <summary>
        /// Schedules a synchronous item. Recommended for simple scenario's.
        /// </summary>
        /// <param name="ToRun">The Action to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem Schedule(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval));
        }

        /// <summary>
        /// Schedules a synchronous item.
        /// </summary>
        /// <param name="ToRun">The Task to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem Schedule(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval));
        }

        /// <summary>
        /// Schedules an asynchronous item. Recommended for compute-heavy or long running items.
        /// </summary>
        /// <param name="ToRun">The Action to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem ScheduleAsync(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { RunAsync = true });
        }

        /// <summary>
        /// Schedules an asynchronous item. Recommended for compute-heavy tasks.
        /// </summary>
        /// <param name="ToRun">The Task to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem ScheduleAsync(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { RunAsync = true });
        }

        /// <summary>
        /// Schedules a synchronous and synchronised item. This will use the current SynchronizationContext to run the item on.
        /// </summary>
        /// <param name="ToRun">The Action to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem ScheduleSynchronized(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { SynchronizationContext = SynchronizationContext.Current });
        }

        /// <summary>
        /// Schedules a synchronous and synchronised item. This will use the current SynchronizationContext to run the item on.
        /// </summary>
        /// <param name="ToRun">The Task to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem ScheduleSynchronized(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { SynchronizationContext = SynchronizationContext.Current });
        }

        /// <summary>
        /// Schedules an asynchronous and synchronised item. This will use the current SynchronizationContext to run the item on.
        /// </summary>
        /// <param name="ToRun">The Action to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem ScheduleSynchronizedAsync(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { RunAsync = true, SynchronizationContext = SynchronizationContext.Current });
        }

        /// <summary>
        /// Schedules an asynchronous and synchronised item. This will use the current SynchronizationContext to run the item on.
        /// </summary>
        /// <param name="ToRun">The Task to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem ScheduleSynchronizedAsync(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { RunAsync = true, SynchronizationContext = SynchronizationContext.Current });
        }

        /// <summary>
        /// Schedules a preconfigured item.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem Schedule(ScheduleItem item)
        {
            lock (ScheduleKey)
            {
                ScheduleItems.Add(PrepareScheduleInternal(item));
                ReloadScheduleInternal();
            }
            return item;
        }

        /// <summary>
        /// Schedules a range of preconfigured items.
        /// </summary>
        /// <param name="item">The items to add.</param>
        /// <returns>The added items.</returns>
        public IEnumerable<ScheduleItem> ScheduleRange(IEnumerable<ScheduleItem> items)
        {
            lock (ScheduleKey)
            {
                ScheduleItems.AddRange(items.Select(i => PrepareScheduleInternal(i)));
                ReloadScheduleInternal();
            }
            return items;
        }

        private ScheduleItem PrepareScheduleInternal(ScheduleItem item)
        {
            item.NextRun = item.Start;
            if (item.CanSkip && item.Interval.HasValue && item.Start < DateTime.Now)
            {
                item.NextRun = GetNextTime(item.Start, item.Interval.Value, item.CanSkip);
            }
            item.IsScheduled = true;
            return item;
        }

        private DateTime GetNextTime(DateTime last, TimeSpan interval, bool skip)
        {
            if (skip)
            {
                var now = DateTime.Now;
                return last.AddMilliseconds(Math.Max(Math.Floor((now - last).TotalMilliseconds / interval.TotalMilliseconds) + 1, 1) * interval.TotalMilliseconds);
            }
            else
            {
                return last.Add(interval);
            }
        }

        /// <summary>
        /// Schedules a synchronous item.
        /// </summary>
        /// <typeparam name="T">The type of the state object.</typeparam>
        /// <param name="ToRun">The Action to be run.</param>
        /// <param name="Start">The time the item should be run at.</param>
        /// <param name="State">The state to be delivered to the item.</param>
        /// <param name="Interval">(Optional) The interval between successive runs of the item.</param>
        /// <returns>The added item.</returns>
        public StateScheduleItem<T> Schedule<T>(Action<T> ToRun, DateTime Start, T State, TimeSpan? Interval = null)
        {
            return Schedule(new StateScheduleItem<T>(ToRun, Start, State, Interval));
        }

        /// <summary>
        /// Schedules a preconfigured item with state.
        /// </summary>
        /// <typeparam name="T">The type of the state object.</typeparam>
        /// <param name="item"></param>
        /// <returns>The added item.</returns>
        public StateScheduleItem<T> Schedule<T>(StateScheduleItem<T> item)
        {
            Schedule((ScheduleItem)item);
            return item;
        }

        /// <summary>
        /// Schedules a synchronous execution after the time elapsed.
        /// </summary>
        /// <param name="ToRun">The Action to be run.</param>
        /// <param name="timeSpan">The time to wait before execution.</param>
        /// <returns>The added item.</returns>
        public ScheduleItem RunIn(Action ToRun, TimeSpan timeSpan)
        {
            return Schedule(ToRun, DateTime.Now + timeSpan);
        }

        /// <summary>
        /// Schedules a synchronous execution after the time elapsed with state.
        /// </summary>
        /// <typeparam name="T">The type of the state object.</typeparam>
        /// <param name="ToRun">The Action to be run.</param>
        /// <param name="timeSpan">The time to wait before execution.</param>
        /// <param name="State">The state to be delivered to the item.</param>
        /// <returns>The added item.</returns>
        public StateScheduleItem<T> RunIn<T>(Action<T> ToRun, TimeSpan timeSpan, T State)
        {
            return Schedule(ToRun, DateTime.Now + timeSpan, State);
        }

        protected virtual void ReloadScheduleInternal()
        {
            ScheduleItems.Sort((x, y) => { return x.NextRun.CompareTo(y.NextRun); });
        }

        /// <summary>
        /// Forces a reload of the internal schedule. Should not be used.
        /// </summary>
        public void ReloadSchedule()
        {
            lock (ScheduleKey)
            {
                ReloadScheduleInternal();
            }
        }
    }
}