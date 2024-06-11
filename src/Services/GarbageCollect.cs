using Serilog;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;

namespace Conesoft.Server_Host.Services
{
    public class GarbageCollect
    {
        public static async Task Every(TimeSpan period)
        {
            var timer = new PeriodicTimer(period);
            while(await timer.WaitForNextTickAsync())
            {
                var memoryBefore = Process.GetCurrentProcess().PrivateMemorySize64;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                var memoryAfter = Process.GetCurrentProcess().PrivateMemorySize64;
                Log.Information($"Running garbage collection cycle: {(memoryBefore - memoryAfter).Bytes()} released, {memoryAfter.Bytes()} in use");
            }
        }
    }
}
