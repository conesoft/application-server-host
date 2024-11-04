using System.Diagnostics;

namespace Conesoft.Server_Host.Features.ActiveProcesses.Messages;

record OnNewProcessLaunched(string Name, Process Process);
