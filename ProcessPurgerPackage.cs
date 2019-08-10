using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ProcessPurger.Extensions;
using Task = System.Threading.Tasks.Task;

namespace ProcessPurger
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(ProcessPurgerPackage.PackageGuidString)]
    public sealed class ProcessPurgerPackage : AsyncPackage, IVsDebuggerEvents
    {
        public const string PackageGuidString = "8372a177-5b80-4468-a172-5e7bea6e74ab";

        private DTE dte;
        private IVsDebugger debugService;
        private int DebuggedProcessId;
        private uint debugCookie;
        private ProcessMonitor processMonitor;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            dte = await GetServiceAsync(typeof(DTE)) as DTE;
            string msg = "The current Output Window object belongs to the ";
            Assumes.Present(dte);
            debugService = (IVsDebugger)GetGlobalService(typeof(SVsShellDebugger));
            debugService.AdviseDebuggerEvents(this, out debugCookie);
        }

        int IVsDebuggerEvents.OnModeChange(DBGMODE dbgmodeNew)
        {
            switch (dbgmodeNew)
            {
                case DBGMODE.DBGMODE_Run:
                    if (DebuggedProcessId == 0)
                    {
                        // switch mainthread

                        Process debuggedProcess = dte.Debugger.DebuggedProcesses.Cast<Process>().FirstOrDefault(proc => System.Diagnostics.Process.GetProcessById(proc.ProcessID).GetParent().ProcessName.Contains("VsDebugConsole"));
                        //if (System.Diagnostics.Process.GetProcessById(debuggedProcess.ProcessID).GetParent().ProcessName.Contains("VsDebugConsole"))
                        if (debuggedProcess != null)
                        {
                            DebuggedProcessId = debuggedProcess.ProcessID;
                            processMonitor = new ProcessMonitor(DebuggedProcessId);
                            processMonitor.StartWatch();
                        }
                    }
                    break;

                case DBGMODE.DBGMODE_Design:
                    Process debuggedProcesses = dte.Debugger.DebuggedProcesses.Cast<Process>().FirstOrDefault(process => process.ProcessID == DebuggedProcessId);
                    if (DebuggedProcessId != 0 && debuggedProcesses is null)
                    {
                        processMonitor.StopWatch();
                        processMonitor.KillChildProcesses();
                        processMonitor = null;
                        DebuggedProcessId = 0;
                    }
                    break;
            }
            return (int)dbgmodeNew;
        }
    }
}