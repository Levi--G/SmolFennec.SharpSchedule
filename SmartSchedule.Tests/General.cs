using System;
using Xunit;
using SharpSchedule;
using System.Threading;

namespace SmartSchedule.Tests
{
    public class General
    {
        /// <summary>
        /// With InterruptWhenReloading tasks should be run immediately after adding
        /// </summary>
        [Fact]
        public void FastExecution()
        {
            var s = new Scheduler() { Precision = 10000 };
            s.StartThread();
            int i = 0;
            Thread.Sleep(50);
            s.Schedule(() => i++, DateTime.Now);
            Thread.Sleep(50);
            s.StopAndBlock();
            Assert.Equal(1, i);
        }

        /// <summary>
        /// Without InterruptWhenReloading tasks will be delayed
        /// </summary>
        [Fact]
        public void SlowExecution()
        {
            var s = new Scheduler() { Precision = 10000, InterruptWhenReloading = false };
            s.StartThread();
            int i = 0;
            Thread.Sleep(50);
            s.Schedule(() => i++, DateTime.Now);
            Thread.Sleep(50);
            s.StopAndBlock();
            Assert.Equal(0, i);
        }

        /// <summary>
        /// Should run the exact amount of times
        /// </summary>
        [Fact]
        public void RepeatsNoSkip()
        {
            var s = new Scheduler();
            int i = 0;
            s.Schedule(new ScheduleItem(() => i++, DateTime.Now, TimeSpan.FromMilliseconds(200)) { CanSkip = false });
            s.StartThread();
            Thread.Sleep(500);
            s.StopAndBlock();
            Assert.Equal(3, i);
        }

        /// <summary>
        /// Should run the right number of times 500/200 = 2
        /// </summary>
        [Fact]
        public void Repeats()
        {
            var s = new Scheduler();
            int i = 0;
            s.Schedule(() => i++, DateTime.Now, TimeSpan.FromMilliseconds(200));
            s.StartThread();
            Thread.Sleep(500);
            s.StopAndBlock();
            Assert.Equal(2, i);
        }

        /// <summary>
        /// Should run twice due to long running
        /// </summary>
        [Fact]
        public void RepeatsSkipped()
        {
            var s = new Scheduler();
            int i = 0;
            s.Schedule(() => { Thread.Sleep(225); i++; }, DateTime.Now, TimeSpan.FromMilliseconds(50));
            s.StartThread();
            Thread.Sleep(400);
            s.StopAndBlock();
            Assert.Equal(2, i);
        }

        /// <summary>
        /// When first task blocks second should not run
        /// </summary>
        [Fact]
        public void NoAsync()
        {
            var s = new Scheduler();
            int i = 0;
            s.Schedule(() => Thread.Sleep(500), DateTime.Now);
            s.Schedule(() => i++, DateTime.Now.AddMilliseconds(1));
            s.StartThread();
            Thread.Sleep(200);
            s.StopAndBlock();
            Assert.Equal(0, i);
        }

        /// <summary>
        /// First task does not block, second task should still run
        /// </summary>
        [Fact]
        public void Async()
        {
            var s = new Scheduler() { Precision = 1000 };
            int i = 0;
            s.ScheduleAsync(() => Thread.Sleep(500), DateTime.Now);
            s.Schedule(() => i++, DateTime.Now.AddMilliseconds(1));
            s.StartThread();
            Thread.Sleep(200);
            s.StopAndBlock();
            Assert.Equal(1, i);
        }

        /// <summary>
        /// Checks threadid's for the synchronised task
        /// </summary>
        [Fact]
        public void Synchronised()
        {
            var cs = new Scheduler() { Precision = 1000 };
            var c = new SchedulerSynchronizationContext(cs);
            cs.StartThread();
            var s = new Scheduler() { Precision = 1000 };
            s.StartThread();

            var context = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(c);

            int? csti = null;
            cs.Schedule(() => csti = Thread.CurrentThread.ManagedThreadId, DateTime.Now);

            int? cti = null;
            c.Send((s) => cti = Thread.CurrentThread.ManagedThreadId, null);

            int? i = null;
            s.Schedule(() => i = Thread.CurrentThread.ManagedThreadId, DateTime.Now);

            int? syti = null;
            s.ScheduleSynchronized(() => syti = Thread.CurrentThread.ManagedThreadId, DateTime.Now);

            while (s.GetScheduledItems().Count > 0 && cs.GetScheduledItems().Count > 0)
            {
                Thread.Sleep(20);
            }
            cs.StopAndBlock();
            s.StopAndBlock();

            SynchronizationContext.SetSynchronizationContext(context);

            Assert.True(csti.HasValue && cti.HasValue && i.HasValue && syti.HasValue);
            Assert.Equal(csti, cti);
            Assert.Equal(syti, cti);
            Assert.NotEqual(i, syti);
        }
    }
}
