using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharpShedule
{
    public class AsyncSheduler : SheduleBase
    {
        private bool Stopping = true;
        public bool RunAsync { get; set; } = false;

        public Task Start()
        {
            Stopping = false;
            return Wait();
        }

        private async Task Wait()
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
                    if (RunAsync)
                    {
                        Task.Factory.StartNew(() => { try { SI.ToRun(); } catch (Exception e) { DoOnError(e); } });
                    }
                    else
                    {
                        try
                        {
                            SI.ToRun();
                        }
                        catch (Exception e)
                        {
                            DoOnError(e);
                        }
                    }
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
                    await Task.Delay(Precision);
                }
            }
        }

        public void Stop()
        {
            Stopping = true;
        }
    }
}