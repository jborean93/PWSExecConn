using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Remoting;
using System.Management.Automation.Remoting.Client;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace PWSExecConn;

internal sealed class PSExecConnectionInfo
{
    public PSRemotingCryptoHelper? test { get; set; }
    //public ClientSessionTransportManagerBase? abc { get; set; }
}

/*
internal sealed class PSExecConnectionInfo : RunspaceConnectionInfo
{
    private RemoteSessionNamedPipeClient _clientPipe;
    private readonly string _computerName;

    /// <summary>
    /// Process Id to attach to.
    /// </summary>
    public int ProcessId
    {
        get;
        set;
    }

    /// <summary>
    /// ConnectingTimeout in Milliseconds
    /// </summary>
    public int ConnectingTimeout
    {
        get;
        set;
    }

    private PSExecConnectionInfo()
    { }

    /// <summary>
    /// Construct instance.
    /// </summary>
    public PSExecConnectionInfo(
        int processId,
        int connectingTimeout)
    {
        ProcessId = processId;
        ConnectingTimeout = connectingTimeout;
        _computerName = $"LocalMachine:{ProcessId}";
        _clientPipe = new RemoteSessionNamedPipeClient(
            procId: ProcessId,
            appDomainName: string.Empty);
    }

    /// <summary>
    /// ComputerName
    /// </summary>
    public override string ComputerName
    {
        get { return _computerName; }
        set { throw new NotImplementedException(); }
    }

    /// <summary>
    /// Credential
    /// </summary>
    public override PSCredential Credential
    {
        get { return null; }
        set { throw new NotImplementedException(); }
    }

    /// <summary>
    /// AuthenticationMechanism
    /// </summary>
    public override AuthenticationMechanism AuthenticationMechanism
    {
        get { return AuthenticationMechanism.Default; }
        set { throw new NotImplementedException(); }
    }

    /// <summary>
    /// CertificateThumbprint
    /// </summary>
    public override string CertificateThumbprint
    {
        get { return string.Empty; }
        set { throw new NotImplementedException(); }
    }

    /// <summary>
    /// Create shallow copy of NamedPipeInfo object.
    /// </summary>
    public override RunspaceConnectionInfo Clone()
    {
        var connectionInfo = new NamedPipeInfo(ProcessId, ConnectingTimeout);
        connectionInfo._clientPipe = _clientPipe;
        return connectionInfo;
    }

    /// <summary>
    /// Create an instance of ClientSessionTransportManager.
    /// </summary>
    public override BaseClientSessionTransportManager CreateClientSessionTransportManager(
        Guid instanceId,
        string sessionName,
        PSRemotingCryptoHelper cryptoHelper)
    {
        return new NamedPipeClientSessionTransportMgr(
            connectionInfo: this,
            runspaceId: instanceId,
            cryptoHelper: cryptoHelper);
    }

    /// <summary>
    /// Attempt to connect to process Id.
    /// If connection fails, is aborted, or times out, an exception is thrown.
    /// </summary>
    /// <param name="textWriter">Named pipe text stream writer.</param>
    /// <param name="textReader">Named pipe text stream reader.</param>
    /// <exception cref="TimeoutException">Connect attempt times out or is aborted.</exception>
    public void Connect(
        out StreamWriter textWriter,
        out StreamReader textReader)
    {
        // Wait for named pipe to connect.
        _clientPipe.Connect(
            timeout: ConnectingTimeout > -1 ? ConnectingTimeout : int.MaxValue);

        textWriter = _clientPipe.TextWriter;
        textReader = _clientPipe.TextReader;
    }

    /// <summary>
    /// Stops a connection attempt, or closes the connection that has been established.
    /// </summary>
    public void StopConnect()
    {
        _clientPipe?.AbortConnect();
        _clientPipe?.Close();
        _clientPipe?.Dispose();
    }
}

internal sealed class PsexecClientSessionTransportMgr : ClientSessionTransportManagerBase
{
    private readonly PSExecConnectionInfo _connectionInfo;
    private const string _threadName = "NamedPipeCustomTransport Reader Thread";

    internal PsexecClientSessionTransportMgr(PSExecConnectionInfo connectionInfo, Guid runspaceId,
        PSRemotingCryptoHelper cryptoHelper) : base(runspaceId, cryptoHelper)
    {
        if (connectionInfo == null) { throw new PSArgumentException("connectionInfo"); }

        _connectionInfo = connectionInfo;
    }

    /// <summary>
    /// Create a named pipe connection to the target process and set up
    /// transport reader/writer.
    /// </summary>
    public override void CreateAsync()
    {
        _connectionInfo.Connect(out StreamWriter pipeTextWriter, out StreamReader pipeTextReader);

        // Create writer for named pipe.
        _messageWriter = new OutOfProcessTextWriter(pipeTextWriter);

        // Create reader thread for named pipe.
        StartReaderThread(pipeTextReader);
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);

        if (isDisposing)
        {
            CloseConnection();
        }
    }

    protected override void CleanupConnection()
    {
        CloseConnection();
    }
}
*/
