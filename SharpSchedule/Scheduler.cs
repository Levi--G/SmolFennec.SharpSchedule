using System;
using System.Threading;

namespace SharpSchedule
{
    /// <summary>
    /// A general purpose scheduler with a single backing thread.
    /// </summary>
    public class Scheduler : ScheduleBase
    {
        private Thread Waiter;
        private bool Stopping = true;
        ManualResetEvent NewWorkAvailable = new ManualResetEvent(false);

        public bool InterruptWhenReloading { get; set; } = true;
        public bool UseBetterPrecision { get; set; } = true;
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Starts a thread to run the scheduler and begins processing jobs.
        /// </summary>
#if NETSTANDARD2_0
        public void StartThread(ApartmentState apartmentState = ApartmentState.MTA)
#else

        public void StartThread()
#endif
        {
            StopAndBlock();
            Stopping = false;
            Waiter = new Thread(Wait);
#if NETSTANDARD2_0
            Waiter.TrySetApartmentState(apartmentState);
#endif
            Waiter.Start();
        }

        /// <summary>
        /// Starts the scheduler on the current thread and begins processing jobs.
        /// </summary>
        public void Start()
        {
            StopAndBlock();
            Stopping = false;
            Wait();
        }

        private void Wait()
        {
            try
            {
                IsRunning = true;
                while (!Stopping)
                {
                    if (!DoSingleCheck())
                    {
                        var delay = Precision;
                        if (UseBetterPrecision)
                        {
                            var i = GetNextItem();
                            if (i != null)
                            {
                                delay = (int)i.NextRun.Subtract(DateTime.Now).TotalMilliseconds - Precision;
                                delay = Math.Min(delay, Precision * 128);
                                delay = Math.Max(delay, Precision / 128);
                            }
                            else
                            {
                                delay = Precision * 128;
                            }
                        }
                        NewWorkAvailable.WaitOne(delay);
                        NewWorkAvailable.Reset();
                    }
                }
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Stops processing new jobs.
        /// </summary>
        public void SignalStop()
        {
            Stopping = true;
            NewWorkAvailable.Set();
        }

        /// <summary>
        /// Stops processing new jobs and wait until the last one is finished.
        /// </summary>
        public void StopAndBlock()
        {
            Stopping = true;
            NewWorkAvailable.Set();
            if (Waiter != null && Waiter.IsAlive)
            {
                Waiter.Join();
            }
            Waiter = null;
        }

        protected override void ReloadScheduleInternal()
        {
            base.ReloadScheduleInternal();
            if (InterruptWhenReloading)
            {
                NewWorkAvailable.Set();
            }
        }
    }
}