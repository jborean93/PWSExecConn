using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace PWSExecConn;

public sealed class PSExecConnectionInfo : RunspaceConnectionInfo
{
    public int ConnectingTimeout { get; set; }

    public override string ComputerName { get; set; }

    public override PSCredential? Credential { get; set; }

    public string Executable { get; set; }

    public string Arguments { get; set; }

    public PSCredential? ProcessCredential { get; set; }

    public bool RunAsSystem { get; set; }

    public override AuthenticationMechanism AuthenticationMechanism
    {
        get { return AuthenticationMechanism.Default; }
        set { throw new NotImplementedException(); }
    }

    public override string CertificateThumbprint
    {
        get { return string.Empty; }
        set { throw new NotImplementedException(); }
    }

    public PSExecConnectionInfo(string computerName, PSCredential? credential, int connectingTimeout,
        string executable, string arguments, PSCredential? processCredential, bool runAsSystem)
    {
        ComputerName = computerName;
        Credential = credential;
        ConnectingTimeout = connectingTimeout;
        Executable = executable;
        Arguments = arguments;
        ProcessCredential = processCredential;
        RunAsSystem = runAsSystem;
    }

    public override RunspaceConnectionInfo Clone()
        => new PSExecConnectionInfo(ComputerName, Credential, ConnectingTimeout, Executable, Arguments,
            ProcessCredential, RunAsSystem);

    public override BaseClientSessionTransportManager CreateClientSessionTransportManager(
        Guid instanceId,
        string sessionName,
        PSRemotingCryptoHelper cryptoHelper)
    {
        return new PsexecClientSessionTransportMgr(
            connectionInfo: this,
            runspaceId: instanceId,
            cryptoHelper: cryptoHelper);
    }
}

internal sealed class PsexecClientSessionTransportMgr : ClientSessionTransportManagerBase
{
    private readonly PSExecConnectionInfo _connectionInfo;

    private Process? _proc = null;

    internal PsexecClientSessionTransportMgr(PSExecConnectionInfo connectionInfo, Guid runspaceId,
        PSRemotingCryptoHelper cryptoHelper) : base(runspaceId, cryptoHelper)
    {
        _connectionInfo = connectionInfo;
    }

    /// <summary>
    /// Creates the transport manager and initiates the connection. This is called when the Runspace is first being
    /// opened and is designed for the implementation to initiate the connection and set up the pipes for communication.
    /// 3 things must happen in this setup:
    ///     _messageWriter must be set to an input stream to the server
    ///     SendOneItem must be called to start the message exchange
    ///     Some thread or event handler that calls HandleOutputDataReceived or HandleErrorDataReceived based on the
    ///         output.
    /// </summary>
    public override void CreateAsync()
    {
        PSExecManifest manifest = new()
        {
            Hostname = _connectionInfo.ComputerName,
            UserName = _connectionInfo.Credential?.UserName,
            Password = _connectionInfo.Credential?.GetNetworkCredential().Password,
            ProcessUserName = _connectionInfo.ProcessCredential?.UserName,
            ProcessPassword = _connectionInfo.ProcessCredential?.GetNetworkCredential().Password,
            RunAsSystem = _connectionInfo.RunAsSystem,
            Executable = _connectionInfo.Executable,
            Arguments = _connectionInfo.Arguments,
        };

        Process proc = new();
        _proc = proc;

        string assemblyLocation = typeof(PsexecClientSessionTransportMgr).Assembly.Location;
        string psexecScript = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "psexec.py"));
        proc.StartInfo = new()
        {
            FileName = "python",
            Arguments = psexecScript,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
        };

        // Sets up the transport to read the output data from the process and handle that accordingly.
        proc.ErrorDataReceived += ErrorDataReceived;
        proc.OutputDataReceived += OutputDataReceived;
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // Writes the initial configuration data for use by the pypsexec process.
        proc.StandardInput.WriteLine(JsonSerializer.Serialize(manifest));
        proc.StandardInput.Flush();

        // Used by ClientSessionTransportManagerBase to send input data to the target. SendOneItem is needed to kick
        // off the message input.
        _messageWriter = new OutOfProcessTextWriter(proc.StandardInput);
        SendOneItem();
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);
        if (isDisposing)
        {
            CleanupConnection();
        }
    }

    /// <summary>
    /// Called after the session is closed and provides a way for the implementation to cleanup any resources it has
    /// allocated for the PSSession.
    /// </summary>
    protected override void CleanupConnection()
    {
        _proc?.WaitForExit();
        _proc?.Dispose();
        _proc = null;
    }

    private void ErrorDataReceived(object? sender, DataReceivedEventArgs args)
    {
        HandleErrorDataReceived(args.Data);
    }

    private void OutputDataReceived(object? sender, DataReceivedEventArgs args)
    {
        HandleOutputDataReceived(args.Data);
    }
}

internal class PSExecManifest
{
    public string Hostname { get; set; } = "";
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? ProcessUserName { get; set; }
    public string? ProcessPassword { get; set; }
    public bool RunAsSystem { get; set; }
    public string Executable { get; set; } = "";
    public string Arguments { get; set; } = "";
}
