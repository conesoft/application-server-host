using System.Diagnostics;

namespace Conesoft.Server_Host.Features.ActiveProcesses.Helpers;
// From https://stackoverflow.com/a/37034966/1528847

class ProcessTracker(bool closeOnExit = true)
{
    ChildProcessTracker? current = closeOnExit ? new() : null;
    readonly HashSet<Process> tracked = [];

    public Process? Track(Process? p)
    {
        if (p != null)
        {
            tracked.Add(p);
            current?.Track(p);
        }
        return p;
    }

    public bool CloseTrackedOnExit
    {
        get => current != null;
        set
        {
            if (current != null && value == false)
            {
                current.DeactivateTracking();
                current.Dispose();
                current = null;
            }

            if (current == null && value == true)
            {
                current = new();
                foreach (var p in tracked)
                {
                    current.Track(p);
                }
            }
        }
    }
}
