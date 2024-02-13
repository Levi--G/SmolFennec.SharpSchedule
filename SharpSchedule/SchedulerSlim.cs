using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpSchedule
{
    /// <summary>
    /// A general purpose scheduler with a single backing thread.
    /// </summary>
    public class SchedulerSlim
    {
        private Thread? Spinner;
        private bool Stopping = true;
        public bool IsRunning { get; private set; } = false;
        private int index = 0;
        private List<Action> Actions = new List<Action>();
        private List<Action> RunOnceActions = new List<Action>();

        public int NojobDelay { get; set; } = 5;

        /// <summary>
        /// Occurs when a job encounters an Error.
        /// </summary>
        public event EventHandler<Exception>? OnError;

        /// <summary>
        /// Starts a thread to run the scheduler and begins processing jobs.
        /// </summary>
        public void StartThread(ApartmentState apartmentState = ApartmentState.MTA)
        {
            StopAndBlock();
            Stopping = false;
            Spinner = new Thread(Wait);
            Spinner.TrySetApartmentState(apartmentState);
            Spinner.Start();
        }

        public void ScheduleOnce(Action torun)
        {
            lock (RunOnceActions)
            {
                RunOnceActions.Add(torun);
            }
        }

        public void ScheduleLoop(Action torun)
        {
            lock (Actions)
            {
                Actions.Add(torun);
            }
        }

        public void UnscheduleLoop(Action torun)
        {
            lock (Actions)
            {
                Actions.Remove(torun);
            }
        }

        public Task RunOnceAsTask(Action torun)
        {
#if NET6_0_OR_GREATER
            TaskCompletionSource tcs = new();
#else
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
#endif
            lock (RunOnceActions)
            {
                RunOnceActions.Add(() =>
                {
                    try
                    {
                        torun();
#if NET6_0_OR_GREATER
                        tcs.SetResult();
#else
                        tcs.SetResult(true);
#endif
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            }
            return tcs.Task;
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
                    Action? Torun = null;
                    lock (RunOnceActions)
                    {
                        if (RunOnceActions.Count > 0)
                        {
                            Torun = RunOnceActions.Last();
                            RunOnceActions.RemoveAt(RunOnceActions.Count - 1);
                        }
                    }
                    if (Torun == null)
                    {
                        lock (Actions)
                        {
                            if (index >= Actions.Count)
                            {
                                if (Actions.Count == 0)
                                {
                                    //no valid tasks found to run
                                    Thread.Sleep(NojobDelay);
                                    continue;
                                }
                                index = 0;
                            }
                            Torun = Actions[index];
                            index++;
                        }
                    }
                    try
                    {
                        Torun.Invoke();
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(this, ex);
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
        }

        /// <summary>
        /// Stops processing new jobs and wait until the last one is finished.
        /// </summary>
        public void StopAndBlock()
        {
            Stopping = true;
            if (Spinner != null && Spinner.IsAlive)
            {
                Spinner.Join();
            }
            Spinner = null;
        }
    }
}