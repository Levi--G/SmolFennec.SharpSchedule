# SmolFennec.SharpSchedule
**SmolFennec** is a collective name for small libraries and tools released by [Levi](https://github.com/Levi--G) licensed under MIT and hosted on github.
**SharpSchedule** is a DateTime scheduler for .NET that allows running code at specific times or intervals.

<img src="https://raw.githubusercontent.com/Levi--G/SmolFennec.SpeCLI/master/SpeCLI/SmolFennec.png" width="300" height="300">

[![NuGet version (SmolFennec.SharpSchedule)](https://img.shields.io/nuget/v/SmolFennec.SharpSchedule.svg)](https://www.nuget.org/packages/SmolFennec.SharpSchedule/)

## Support

Supported platforms: 
- .Net Standard 1.3+ (limited features)
- .Net Standard 2.0+

When in trouble:
[Submit an issue](https://github.com/Levi--G/SmolFennec.SharpSchedule/issues)

## Usage

There are currently 2 types:
- Scheduler: The original synchronous scheduler using threads.
- AsyncScheduler: A scheduler based on async delays

There are multiple ways to schedule an execution:
- `Schedule(Action/Func<Task>, DateTime, (optional)TimeSpan)`: Schedules an Action or Task to run synchronous with the scheduler for a specified time and optionally repeats this every x time. **Recommended for console applications** or when actions should be executed in order.
- `ScheduleAsync(Action/Func<Task>, DateTime, (optional)TimeSpan)`: Schedules an Action or Task to run asynchronous with the scheduler allowing multiple scheduled items to run at the same time. Recommended when the time is more important than the thread its executed on.
- `ScheduleSynchronized(Action/Func<Task>, DateTime, (optional)TimeSpan)`: Schedules an Action or Task to run on the current SynchronisationContext, handy for applications with a UI/Message pump.
- `ScheduleSynchronizedAsync(Action/Func<Task>, DateTime, (optional)TimeSpan)`: Async version of the Synchronised call. **Recommended for most UI applications.**
- `Schedule(ScheduleItem)`: Schedules a fully configured item as you want it.

```cs
Scheduler Scheduler = new Scheduler() { Precision = 10000 };
Scheduler.Schedule(ExecutesEveryHour, DateTime.Now, TimeSpan.FromHours(1));
Scheduler.Schedule(ExecutesOnce, DateTime.Now.AddMinutes(10));
Scheduler.Schedule(StopAll, DateTime.Now.AddHours(5).AddMinutes(1));
Scheduler.StartThread(); // Starts a thread to run the scheduled items

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
    Scheduler.Stop();
}

```