using System;
using Xunit;
using SharpSchedule;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
        /// Without BetterPrecision tasks will be run in multiples of Precision
        /// </summary>
        [Fact]
        public void NoBetterPrecisionExecution()
        {
            var s = new Scheduler() { Precision = 2000, UseBetterPrecision = false };
            s.StartThread();
            int i = 0;
            Thread.Sleep(50);
            s.Schedule(() => i++, DateTime.Now.AddMilliseconds(2100));
            Thread.Sleep(200);
            Assert.Equal(0, i);
            Thread.Sleep(2000);
            s.StopAndBlock();
            Assert.Equal(1, i);
        }

        /// <summary>
        /// With BetterPrecision tasks will be run as soon as possible
        /// </summary>
        [Fact]
        public void BetterPrecisionExecution()
        {
            var s = new Scheduler() { Precision = 2000 };
            s.StartThread();
            int i = 0;
            Thread.Sleep(50);
            s.Schedule(() => i++, DateTime.Now.AddMilliseconds(2100));
            Thread.Sleep(200);
            Assert.Equal(1, i);
            Thread.Sleep(2000);
            s.StopAndBlock();
            Assert.Equal(1, i);
        }

        /// <summary>
        /// With BetterPrecision there will be less spinning
        /// </summary>
        [Fact]
        public void BetterPrecisionLongerPolling()
        {
            var s = new Scheduler() { Precision = 50, InterruptWhenReloading = false, UseBetterPrecision = true };
            s.StartThread();
            int i = 0;
            Thread.Sleep(50);
            s.Schedule(() => i++, DateTime.Now);
            Thread.Sleep(200);
            s.StopAndBlock();
            Assert.Equal(0, i);
        }

        /// <summary>
        /// Without BetterPrecision there will be more spinning
        /// </summary>
        [Fact]
        public void NoBetterPrecisionLongerPolling()
        {
            var s = new Scheduler() { Precision = 50, InterruptWhenReloading = false, UseBetterPrecision = false };
            s.StartThread();
            int i = 0;
            Thread.Sleep(50);
            s.Schedule(() => i++, DateTime.Now);
            Thread.Sleep(200);
            s.StopAndBlock();
            Assert.Equal(1, i);
        }

        /// <summary>
        /// Rest IsRunning property
        /// </summary>
        [Fact]
        public void IsRunning()
        {
            var s = new Scheduler() { Precision = 2000 };
            Assert.False(s.IsRunning);
            s.StartThread();
            Thread.Sleep(50);
            Assert.True(s.IsRunning);
            s.StopAndBlock();
            Thread.Sleep(50);
            Assert.False(s.IsRunning);
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
            Thread.Sleep(450);
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
            Thread.Sleep(450);
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

        /// <summary>
        /// Should run the exactly once
        /// </summary>
        [Fact]
        public void SlimRunOnce()
        {
            var s = new SchedulerSlim();
            int i = 0;
            s.ScheduleOnce(() => i++);
            s.StartThread();
            Thread.Sleep(50);
            Assert.Equal(1, i);
            s.ScheduleOnce(() => i++);
            Thread.Sleep(50);
            s.StopAndBlock();
            Assert.Equal(2, i);
        }

        /// <summary>
        /// Should run the exactly once
        /// </summary>
        [Fact]
        public void SlimRunLoop()
        {
            var s = new SchedulerSlim();
            int i = 0;
            Action a = () => i++;
            s.ScheduleLoop(a);
            s.StartThread();
            Thread.Sleep(50);
            Assert.True(i > 0);
            s.StopAndBlock();
            s.UnscheduleLoop(a);
            var temp = i;
            s.StartThread();
            Thread.Sleep(50);
            s.StopAndBlock();
            Assert.Equal(temp, i);
        }

        [Fact]
        public void SlimSync()
        {
            var s = new SchedulerSlim();
            var ctx = new SchedulerSlimSynchronizationContext(s);
            s.StartThread();

            var old = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null);

            async Task<int> GetThreadID()
            {
                int i = 0;
                await Task.Run(() => { i++; Thread.Sleep(1000); });
                return Thread.CurrentThread.ManagedThreadId + i;
            }

            Assert.NotEqual(GetThreadID().Result, GetThreadID().Result);
            SynchronizationContext.SetSynchronizationContext(ctx);
            Assert.Equal(GetThreadID().Result, GetThreadID().Result);
            s.StopAndBlock();
            SynchronizationContext.SetSynchronizationContext(old);
        }

        [Fact]
        public void SlimTasks()
        {
            var s = new SchedulerSlim(true);
            s.StartThread();
            int i = 0;
            s.ScheduleOnce(() => i++);
            s.ScheduleOnce(async () => { await Task.Delay(10); i++; });
            s.RunOnceAsTask(() => i++);
            s.RunOnceAsTask(async () => { await Task.Delay(10); i++; });
            Thread.Sleep(50);
            s.RunOnceAsTask(() => i++);
            var a = s.RunOnceAsTask(async () => { await Task.Delay(10); i++; return i; });
            Thread.Sleep(50);
            var b = s.RunOnceAsTask(() => i);
            //due to how async works stopandblock does not guarantee tasks completing unless they are synchronous methods
            s.StopAndBlock(true);
            Assert.True(a.IsCompleted);
            Assert.True(b.IsCompleted);
            Assert.Equal(i, a.Result);
            Assert.Equal(i, b.Result);
            Assert.Equal(6, i);
        }
    }
}
