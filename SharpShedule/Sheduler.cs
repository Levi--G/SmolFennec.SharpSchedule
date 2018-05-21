using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace SharpShedule
{
    public class Sheduler
    {
        List<SheduleItem> SheduleItems = new List<SheduleItem>();
        object SheduleKey = new object();
        Thread Waiter;
        bool Stopping = true;

        public int Precision { get; set; } = 100;

        public void Start()
        {
            Stop();
            Stopping = false;
            Waiter = new Thread(Wait);
            Waiter.Start();
        }

        void Wait()
        {
            while (!Stopping)
            {
                SheduleItem SI;
                lock (SheduleKey)
                {
                    SI = SheduleItems.First();
                }
                if (SI.NextRun < DateTime.Now.AddMilliseconds(Precision))
                {
                    Task.Factory.StartNew(SI.ToRun);
                    lock (SheduleKey)
                    {
                        if (SI.Interval.HasValue)
                        {
                            SI.NextRun += SI.Interval.Value;
                            ReloadShedule();
                        }
                        else
                        {
                            SheduleItems.Remove(SI);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(Precision);
                }
            }
        }

        public void Stop()
        {
            Stopping = true;
            if (Waiter != null && Waiter.IsAlive)
            {
                Waiter.Join();
            }
            Waiter = null;
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
