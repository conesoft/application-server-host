using System.Diagnostics;

namespace Conesoft.Server_Host.Features.ActiveProcesses.Messages;

record OnProcessGettingKilled(string Name, Process Process);
