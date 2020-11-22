using System;
using System.Threading;

namespace SharpSchedule
{
    public class SchedulerSynchronizationContext : SynchronizationContext
    {
        private Scheduler scheduler;

        public SchedulerSynchronizationContext(Scheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public override SynchronizationContext CreateCopy()
        {
            return new SchedulerSynchronizationContext(scheduler);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            scheduler.Schedule(() => d(state), DateTime.Now);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            using (var s = new SemaphoreSlim(0))
            {
                scheduler.Schedule(() =>
                {
                    try
                    {
                        d(state);
                    }
                    finally
                    {
                        s.Release();
                    }
                }, DateTime.Now);
                s.Wait();
            }
        }
    }
}