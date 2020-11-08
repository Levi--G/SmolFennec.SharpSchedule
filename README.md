# SharpSchedule
A C# Scheduler library that allows running code at specific times or intervals

## Examples

```cs
AsyncScheduler Sheduler = new AsyncScheduler() { Precision = 10000 };
Sheduler.Schedule(ExecutesEveryHour, DateTime.Now, TimeSpan.FromHours(1));
Sheduler.Schedule(ExecutesOnce, DateTime.Now.AddMinutes(10));
Sheduler.Schedule(StopAll, DateTime.Now.AddHours(5));
await Sheduler.Start(); // Blocks until Scheduler is disposed or stopped

void ExecutesOnce()
{
    Console.WriteLine("10 minutes have passed");
}

void ExecutesEveryHour()
{
    Console.WriteLine("1 hour has passed");
}

void StopAll()
{
    Sheduler.Stop();
}

```