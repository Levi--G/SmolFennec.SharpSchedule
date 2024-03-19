using System;
using System.Threading;

namespace SharpSchedule
{
    public class SchedulerSlimSynchronizationContext : SynchronizationContext
    {
        private SchedulerSlim scheduler;

        public SchedulerSlimSynchronizationContext(SchedulerSlim scheduler)
        {
            this.scheduler = scheduler;
        }

        public override SynchronizationContext CreateCopy()
        {
            return new SchedulerSlimSynchronizationContext(scheduler);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            scheduler.ScheduleOnce(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            using (var s = new ManualResetEventSlim(false))
            {
                scheduler.ScheduleOnce(() =>
                {
                    try
                    {
                        d(state);
                    }
                    finally
                    {
                        s.Set();
                    }
                });
                s.Wait();
            }
        }
    }
}