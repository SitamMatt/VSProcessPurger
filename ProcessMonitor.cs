using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ProcessPurger
{
    public class ProcessMonitor
    {
        private readonly int ProcessId;
        private TraceEventSession KernelSession;
        private readonly List<int> ChildrenProcessesIds;
        private readonly CancellationTokenSource cancellationTokenSource;
        private Task watcherTask;

        public ProcessMonitor(int processId)
        {
            ProcessId = processId;
            ChildrenProcessesIds = new List<int>();
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void StartWatch()
        {
            KernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
            KernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);
            KernelSession.Source.Kernel.ProcessStart += OnProcessStart;
            watcherTask = new Task(Watch, cancellationTokenSource.Token);
            watcherTask.Start();
        }

        public void Watch()
        {
            KernelSession.Source.Process();
        }

        public void StopWatch()
        {
            cancellationTokenSource.Cancel();
            KernelSession.Source.StopProcessing();
        }

        private void OnProcessStart(ProcessTraceData obj)
        {
            if (obj.ParentID == ProcessId || ChildrenProcessesIds.Contains(obj.ParentID))
            {
                ChildrenProcessesIds.Add(obj.ProcessID);
            }
        }

        public void KillChildProcesses()
        {
            foreach (var process in ChildrenProcessesIds)
            {
                try
                {
                    Process.GetProcessById(process).Kill();
                }
                catch { }
            }
        }
    }
}