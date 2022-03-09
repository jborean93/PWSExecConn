using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace PWSExecConn;

[Cmdlet(VerbsCommon.New, "PSExecSession", DefaultParameterSetName = "RunAsCredential")]
[OutputType(typeof(PSSession))]
public sealed class NewPSExecSession : PSCmdlet
{
    private ManualResetEvent? _openEvent = null;
    private PSExecConnectionInfo? _connInfo = null;
    private Runspace? _runspace = null;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    public string[] ComputerName { get; set; } = Array.Empty<string>();

    [Parameter()]
    public PSCredential? Credential { get; set; }

    [Parameter(
        ParameterSetName = "RunAsCredential"
    )]
    public PSCredential? RunAsCredential { get; set; }

    [Parameter(
        ParameterSetName = "RunAsSystem"
    )]
    public SwitchParameter RunAsSystem { get; set; }

    [Parameter()]
    public string Executable { get; set; } = "pwsh";

    [Parameter()]
    public string Arguments { get; set; } = "-NoLogo -ServerMode";

    [Parameter()]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = "";

    protected override async void ProcessRecord()
    {
        foreach (string computer in ComputerName)
        {
            WriteVerbose($"Starting SSH connection to {computer}");

            PSExecConnectionInfo connInfo = new PSExecConnectionInfo(
                computer,
                Credential,
                0,
                Executable,
                Arguments,
                RunAsCredential,
                RunAsSystem);
            _connInfo = connInfo;

            Runspace runspace = RunspaceFactory.CreateRunspace(
                connectionInfo: connInfo,
                host: Host,
                typeTable: TypeTable.LoadDefaultTypeFiles(),
                applicationArguments: null,
                name: Name);
            _runspace = runspace;

            using (_openEvent = new ManualResetEvent(false))
            {
                runspace.StateChanged += HandleRunspaceStateChanged;
                runspace.OpenAsync();
                _openEvent?.WaitOne();

                if (runspace.RunspaceStateInfo.State == RunspaceState.Broken)
                {
                    // Reason message here is most likely useless but it's better than nothing.
                    ErrorRecord err = new(
                        runspace.RunspaceStateInfo.Reason,
                        "PSExecFailedConnection",
                        ErrorCategory.ConnectionError,
                        computer);

                    WriteError(err);
                    continue;
                }

                WriteObject(PSSession.Create(
                    runspace: runspace,
                    transportName: "PSExec",
                    psCmdlet: this));
            }
        }
    }

    protected override void StopProcessing()
    {
        // FUTURE: Should somehow cancel the open process/kill it
        SetOpenEvent();
    }

    private void HandleRunspaceStateChanged(object? source, RunspaceStateEventArgs stateEventArgs)
    {
        switch (stateEventArgs.RunspaceStateInfo.State)
        {
            case RunspaceState.Opened:
            case RunspaceState.Closed:
            case RunspaceState.Broken:
                if (_runspace != null)
                {
                    _runspace.StateChanged -= HandleRunspaceStateChanged;
                }

                SetOpenEvent();
                break;
        }
    }

    private void SetOpenEvent()
    {
        try
        {
            _openEvent?.Set();
        }
        catch (ObjectDisposedException) {}
    }
}
