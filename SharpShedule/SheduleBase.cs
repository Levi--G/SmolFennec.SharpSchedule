using System;
using System.Collections.Generic;
using System.Text;

namespace SharpShedule
{
    public class SheduleBase
    {
        public event EventHandler<Exception> OnError;
        protected List<SheduleItem> SheduleItems = new List<SheduleItem>();
        protected readonly object SheduleKey = new object();

        public int Precision { get; set; } = 100;

        protected void DoOnError(Exception e)
        {
            OnError?.Invoke(this, e);
        }

        public SheduleItem Shedule(Action ToRun, DateTime Start, TimeSpan? Interval = null)
        {
            return Shedule(new SheduleItem(ToRun, Start, Interval));
        }

        public SheduleItem Shedule(SheduleItem item)
        {
            lock (SheduleKey)
            {
                item.NextRun = item.Start;
                if (item.Interval.HasValue && item.Start < DateTime.Now)
                {
                    item.NextRun = item.Start.AddMilliseconds(((DateTime.Now - item.Start).TotalMilliseconds / item.Interval.Value.TotalMilliseconds + 1) * item.Interval.Value.TotalMilliseconds);
                }
                SheduleItems.Add(item);
                item.IsSheduled = true;
                ReloadShedule();
            }
            return item;
        }

        public StateSheduleItem<T> Shedule<T>(Action<T> ToRun, DateTime Start, T State, TimeSpan? Interval = null)
        {
            return Shedule(new StateSheduleItem<T>(ToRun, Start, State, Interval));
        }

        public StateSheduleItem<T> Shedule<T>(StateSheduleItem<T> item)
        {
            Shedule((SheduleItem)item);
            return item;
        }

        public SheduleItem RunIn(Action ToRun, TimeSpan timeSpan)
        {
            return Shedule(ToRun, DateTime.Now + timeSpan);
        }

        public StateSheduleItem<T> RunIn<T>(Action<T> ToRun, TimeSpan timeSpan, T State)
        {
            return Shedule(ToRun, DateTime.Now + timeSpan, State);
        }

        public void ReloadShedule()
        {
            SheduleItems.Sort((x, y) => { return x.NextRun.CompareTo(y.NextRun); });
        }
    }
}
