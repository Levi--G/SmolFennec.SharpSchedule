using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpSchedule
{
    public class ScheduleBase
    {
        public event EventHandler<Exception> OnError;
        public event EventHandler<TimeSpan> OnRunningBehind;

        protected List<ScheduleItem> ScheduleItems = new List<ScheduleItem>();
        protected readonly object ScheduleKey = new object();

        public int Precision { get; set; } = 100;
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
                    item.NextRun = GetNextTime(item.Start, item.Interval.Value, item.CanSkip);
                    ReloadSchedule();
                }
                else
                {
                    item.IsScheduled = false;
                    ScheduleItems.Remove(item);
                }
            }
        }

        public void Remove(ScheduleItem item)
        {
            lock (ScheduleKey)
            {
                item.IsScheduled = false;
                ScheduleItems.Remove(item);
            }
        }

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

        public ScheduleItem Schedule(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval));
        }

        public ScheduleItem Schedule(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval));
        }

        public ScheduleItem ScheduleAsync(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { RunAsync = true });
        }

        public ScheduleItem ScheduleAsync(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { RunAsync = true });
        }

        public ScheduleItem ScheduleSynchronized(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { SynchronizationContext = SynchronizationContext.Current });
        }

        public ScheduleItem ScheduleSynchronized(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { SynchronizationContext = SynchronizationContext.Current });
        }

        public ScheduleItem ScheduleSynchronizedAsync(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { RunAsync = true, SynchronizationContext = SynchronizationContext.Current });
        }

        public ScheduleItem ScheduleSynchronizedAsync(Func<Task> ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Schedule(new ScheduleItem(ToRun, Start, Interval) { RunAsync = true, SynchronizationContext = SynchronizationContext.Current });
        }

        public ScheduleItem Schedule(ScheduleItem item)
        {
            lock (ScheduleKey)
            {
                ScheduleItems.Add(PrepareScheduleInternal(item));
                ReloadSchedule();
            }
            return item;
        }

        public IEnumerable<ScheduleItem> ScheduleRange(IEnumerable<ScheduleItem> items)
        {
            lock (ScheduleKey)
            {
                ScheduleItems.AddRange(items.Select(i => PrepareScheduleInternal(i)));
                ReloadSchedule();
            }
            return items;
        }

        private ScheduleItem PrepareScheduleInternal(ScheduleItem item)
        {
            item.NextRun = item.Start;
            if (item.Interval.HasValue && item.Start < DateTime.Now)
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
                return last.AddMilliseconds(((DateTime.Now - last).TotalMilliseconds / interval.TotalMilliseconds + 1) * interval.TotalMilliseconds);
            }
            else
            {
                return last.Add(interval);
            }
        }

        public StateScheduleItem<T> Schedule<T>(Action<T> ToRun, DateTime Start, T State, TimeSpan? Interval = null)
        {
            return Schedule(new StateScheduleItem<T>(ToRun, Start, State, Interval));
        }

        public StateScheduleItem<T> Schedule<T>(StateScheduleItem<T> item)
        {
            Schedule((ScheduleItem)item);
            return item;
        }

        public ScheduleItem RunIn(Action ToRun, TimeSpan timeSpan)
        {
            return Schedule(ToRun, DateTime.Now + timeSpan);
        }

        public StateScheduleItem<T> RunIn<T>(Action<T> ToRun, TimeSpan timeSpan, T State)
        {
            return Schedule(ToRun, DateTime.Now + timeSpan, State);
        }

        private void ReloadScheduleInternal()
        {
            ScheduleItems.Sort((x, y) => { return x.NextRun.CompareTo(y.NextRun); });
        }

        public void ReloadSchedule()
        {
            lock (ScheduleKey)
            {
                ReloadScheduleInternal();
            }
        }
    }
}