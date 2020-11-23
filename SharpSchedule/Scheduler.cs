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
            Waiter.SetApartmentState(apartmentState);
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
            while (!Stopping)
            {
                if (!DoSingleCheck())
                {
                    NewWorkAvailable.WaitOne(Precision);
                    NewWorkAvailable.Reset();
                }
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