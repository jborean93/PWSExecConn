using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;

namespace PWSExecConn;

[Cmdlet(VerbsCommon.New, "PSExecSession")]
[OutputType(typeof(PSSession))]
public sealed class NewPSExecSession : PSCmdlet
{
    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true
    )]
    public string[] ComputerName { get; set; } = Array.Empty<string>();

    [Parameter()]
    public int Port { get; set; } = 22;

    [Parameter()]
    public PSCredential? Credential { get; set; }

    protected override void ProcessRecord()
    {
        foreach (string computer in ComputerName)
        {
            WriteVerbose($"Starting SSH connection to {computer}");

            PSExecManifest manifest = new()
            {
                Hostname = computer,
                UserName = Credential?.UserName,
                Password = Credential?.GetNetworkCredential().Password,
                Executable = "pwsh",
                Arguments = "-sshs -NoLogo",
            };

            using Process proc = new Process();
            proc.StartInfo = new()
            {
                FileName = "python",
                Arguments = "/home/jborean/dev/pwsh-ssh-conn/psexec.py",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };
            proc.OutputDataReceived += DataReceived;
            proc.ErrorDataReceived += DataReceived;
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.StandardInput.AutoFlush = true;
            proc.StandardInput.WriteLine(JsonSerializer.Serialize(manifest));

            proc.WaitForExit();

            /*
        // Convert ConnectingTimeout value from seconds to milliseconds.
        _connectionInfo = new NamedPipeInfo(
            processId: ProcessId,
            connectingTimeout: (ConnectingTimeout == Timeout.Infinite) ? Timeout.Infinite : ConnectingTimeout * 1000);

        _runspace = RunspaceFactory.CreateRunspace(
            connectionInfo: _connectionInfo,
            host: Host,
            typeTable: TypeTable.LoadDefaultTypeFiles(),
            applicationArguments: null,
            name: Name);

        _openAsync = new ManualResetEvent(false);
        _runspace.StateChanged += HandleRunspaceStateChanged;

        try
        {
            _runspace.OpenAsync();
            _openAsync.WaitOne();

            WriteObject(
                PSSession.Create(
                    runspace: _runspace,
                    transportName: "PSNPTest",
                    psCmdlet: this));
        }
        finally
        {
            _openAsync.Dispose();
        }
            */
        }
    }

    private void DataReceived(object? sender, DataReceivedEventArgs args)
    {
        Console.WriteLine(args.Data);
    }
}


internal class PSExecManifest
{
    public string Hostname { get; set; } = "";
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string Executable { get; set; } = "";
    public string Arguments { get; set; } = "";
}
