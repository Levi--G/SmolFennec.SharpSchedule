﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpShedule
{
    public class Sheduler : SheduleBase
    {
        private Thread Waiter;
        private bool Stopping = true;

        public void Start()
        {
            Stop();
            Stopping = false;
            Waiter = new Thread(Wait);
            Waiter.Start();
        }

        private void Wait()
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
                    Task.Factory.StartNew(() => { try { SI.ToRun(); } catch (Exception e) { DoOnError(e); } });
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
    }
}