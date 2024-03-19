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
        public bool IsRunning { get; private set; } = false;
        public bool IsSynchronized { get; private set; } = false;
        public int JobsWaiting { get { lock (RunOnceActions) { return RunOnceActions.Count; } } }

        private Thread? Spinner;
        private bool Stopping = true;
        private bool hasLoopActions = false;
        private ManualResetEventSlim ActionAdded = new ManualResetEventSlim();
        private int index = 0;
        private List<Action> Actions = new List<Action>();
        private Queue<Action> RunOnceActions = new Queue<Action>();
        private SynchronizationContext? originalContext;

        public int NojobDelay { get; set; } = 5;

        /// <summary>
        /// Occurs when a job encounters an Error.
        /// </summary>
        public event EventHandler<Exception>? OnError;

        /// <summary>
        /// Creates a new schedulerslim
        /// </summary>
        /// <param name="setSynchronizationContext">set true to set a SynchronizationContext invoking the scheduler on continuation</param>
        public SchedulerSlim(bool setSynchronizationContext = false)
        {
            IsSynchronized = setSynchronizationContext;
        }

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
                RunOnceActions.Enqueue(torun);
                ActionAdded.Set();
            }
        }

        public void ScheduleOnce(Func<Task> torun)
        {
            lock (RunOnceActions)
            {
                RunOnceActions.Enqueue(async () => await torun());
                ActionAdded.Set();
            }
        }

        public void ScheduleLoop(Action torun)
        {
            lock (Actions)
            {
                Actions.Add(torun);
                hasLoopActions = true;
                ActionAdded.Set();
            }
        }

        public void UnscheduleLoop(Action torun)
        {
            lock (Actions)
            {
                Actions.Remove(torun);
                hasLoopActions = Actions.Count > 0;
            }
        }

        /// <summary>
        /// Runs a method on the scheduler as a task
        /// </summary>
        /// <param name="torun"></param>
        /// <returns></returns>
        public Task RunOnceAsTask(Action torun)
        {
#if NET6_0_OR_GREATER
            TaskCompletionSource tcs = new();
#else
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
#endif
            ScheduleOnce(() =>
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
            return tcs.Task;
        }

        /// <summary>
        /// Runs a Task on the scheduler, setSynchronizationContext is recommended
        /// </summary>
        public Task RunOnceAsTask(Func<Task> torun)
        {
#if NET6_0_OR_GREATER
            TaskCompletionSource tcs = new();
#else
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
#endif
            ScheduleOnce(async () =>
            {
                try
                {
                    await torun();
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
            return tcs.Task;
        }

        /// <summary>
        /// Runs a method on the scheduler as a task and returns its results
        /// </summary>
        public Task<T> RunOnceAsTask<T>(Func<T> torun)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            ScheduleOnce(() =>
            {
                try
                {
                    var res = torun();
                    tcs.SetResult(res);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Runs a Task on the scheduler, setSynchronizationContext is recommended
        /// </summary>
        public Task<T> RunOnceAsTask<T>(Func<Task<T>> torun)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            ScheduleOnce(async () =>
            {
                try
                {
                    var res = await torun();
                    tcs.SetResult(res);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Starts the scheduler on the current thread and begins processing jobs. This is a blocking call.
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
                if (IsSynchronized)
                {
                    originalContext = SynchronizationContext.Current;
                    SynchronizationContext.SetSynchronizationContext(new SchedulerSlimSynchronizationContext(this));
                }
                IsRunning = true;
                while (!Stopping)
                {
                    Action? Torun = null;
                    lock (RunOnceActions)
                    {
                        ActionAdded.Reset();
#if NET48 || NETSTANDARD2_0
                        if (RunOnceActions.Count > 0)
                        {
                            Torun = RunOnceActions.Dequeue();
                        }
#else
                        RunOnceActions.TryDequeue(out Torun);
#endif
                    }
                    if (Torun == null)
                    {
                        if (hasLoopActions)
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
                        else
                        {
                            ActionAdded.Wait();
                            continue;
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
                if (IsSynchronized)
                {
                    SynchronizationContext.SetSynchronizationContext(originalContext);
                }
            }
        }

        private void RunOnceThenStop()
        {
            Stopping = true;
            lock (RunOnceActions)
            {
                while (RunOnceActions.Count > 0)
                {
                    var Torun = RunOnceActions.Dequeue();
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
        }

        /// <summary>
        /// Stops processing new jobs.
        /// </summary>
        public void SignalStop(bool finishjobsonce = false)
        {
            if (finishjobsonce)
            {
                ScheduleOnce(RunOnceThenStop);
            }
            else
            {
                Stopping = true;
                ActionAdded.Set();
            }
        }

        /// <summary>
        /// Stops processing new jobs and wait until the last one is finished.
        /// </summary>
        public void StopAndBlock(bool finishjobsonce = false)
        {
            SignalStop(finishjobsonce);
            if (Spinner != null && Spinner.IsAlive)
            {
                Spinner.Join();
            }
            Spinner = null;
        }

        /// <summary>
        /// Stops processing new jobs and wait until the last one is finished.
        /// </summary>
        public async Task StopAndBlockAsync(bool finishjobsonce = false)
        {
            if (finishjobsonce)
            {
                await RunOnceAsTask(RunOnceThenStop).ConfigureAwait(false);
            }
            else
            {
                Stopping = true;
                ActionAdded.Set();
                if (Spinner != null && Spinner.IsAlive)
                {
                    //not ideal but no alternative for now
                    await Task.Run(Spinner.Join).ConfigureAwait(false);
                }
            }
            if (Spinner != null && Spinner.IsAlive)
            {
                await Task.Run(Spinner.Join).ConfigureAwait(false);
            }
            Spinner = null;
        }
    }
}