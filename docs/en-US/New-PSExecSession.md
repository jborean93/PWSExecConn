---
external help file: PWSExecConn.dll-Help.xml
Module Name: PWSExecConn
online version: https://www.github.com/jborean93/PWSExecConn/blob/main/docs/en-US/New-PSSExecSession.md
schema: 2.0.0
---

# New-PSExecSession

## SYNOPSIS
Create a PSExec based PSSession.

## SYNTAX

### RunAsCredential (Default)
```
New-PSExecSession [-ComputerName] <String[]> [-Credential <PSCredential>] [-RunAsCredential <PSCredential>]
 [-Executable <String>] [-Arguments <String>] [-Name <String>] [<CommonParameters>]
```

### RunAsSystem
```
New-PSExecSession [-ComputerName] <String[]> [-Credential <PSCredential>] [-RunAsSystem] [-Executable <String>]
 [-Arguments <String>] [-Name <String>] [<CommonParameters>]
```

## DESCRIPTION
Creates a PSSession targeting the remote computer using the same protocols as PSExec.
PSExec operates over SMB (port 445) rather than WinRM.
This is an example module that demonstrates how to create a custom PSRemoting transport protocol based on the newly implement custom transport feature in PowerShell 7.3.
It is a completely experimental feature and not designed for production use, use at your own risk.

## EXAMPLES

### Example 1: Creates PSExec session using implicit auth
```powershell
PS C:\> $session = New-PSExecSession -ComputerName win-host
PS C:\> Invoke-Command $session { echo "from remote host" }
```

Creates a PSExec based connection to `win-host` and invokes a scriptblock on that target host.

### Example 2: Creates PSExec session with explicit username/password
```powershell
PS C:\> $cred = Get-Credential
PS C:\> $session = New-PSExecSession -ComputerName win-host -Credential $cred
```

Creates a PSExec based connection to `win-host` authenticating with the credential specified.
This will authenticate over SMB using Kerberos (if available) and falling back to NTLM.
The remote session will be running as the credentials specified just like a typical WinRM based session.

### Example 3: Creates PSExec session which runs as another user
```powershell
PS C:\> $cred = Get-Credential
PS C:\> $session = New-PSExecSession -ComputerName win-host -ProcessCredential $cred
```

Creates a PSExec based connection to `win-host` and authenticating as the current running user.
This authentication relies on Kerberos being available to the Python `pypsexec` module.
While the authentication is based on the current user context the remote session will be running as an `Interactive` logon session with the credentials specified.

### Example 4: Creates PSExec session which connects as one user but runs as another user
```powershell
PS C:\> $cred = Get-Credential
PS C:\> $procCred = Get-Credential
PS C:\> $session = New-PSExecSession -ComputerName win-host -Credential $cred -ProcessCredential $procCred
```

Creates a PSExec based connection to `win-host` and authenticating as the explicit credential specified.
This will authenticate over SMB using Kerberos (if available) and falling back to NTLM.
The remote session will run as the user specified by `-ProcessCredential` rather than the connection credential.
The remote session will also be running as an `Interactive` logon session.

### Example 5: Creates PSExec session with runs as SYSTEM
```powershell
PS C:\> $session = New-PSExecSession -ComputerName win-host -RunAsSystem
```

Creates a PSExec based connection to `win-host` and authenticating as the current running user.
The remote session will run as the `SYSTEM` account rather than the connection credential.

### Example 6: Creates PSExec session that runs with Windows PowerShell (5.1)
```powershell
PS C:\> $session = New-PSExecSession -ComputerName win-host -Executable powershell -Arguments "-Version 5.1 -NoLogo -ServerMode
```

Creates a PSExec based connection to `win-host` and authenticating as the current running user.
The remote session will be based on Windows PowerShell (5.1) rather than PowerShell (7+).

## PARAMETERS

### -Arguments
Arguments used to start the remote PowerShell session.
This defaults to `-NoLogo -ServerMode` which works for PowerShell.
If changing `-Executable` to `powershell` to target Windows PowerShell this should be set to `-Version 5.1 -NoLogo -ServerMode`.
The value for `-Version` in the argument should reflect what Windows PowerShell version is installed.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: -NoLogo -ServerMode
Accept pipeline input: False
Accept wildcard characters: False
```

### -ComputerName
The hostname to connect to.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Credential
The credential used for connection authentication.
On Windows if omitted this will use the current user for authentication if available.
On non-Windows it can be omitted if Kerberos has been set up and the Kerberos Python libs for `pypsexec` have been install `pip install smbprotocol[kerberos]`.
If Kerberos is unavailable on Linux then this must be set for the authentication to work.

```yaml
Type: PSCredential
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Executable
The executable to start that hosts the remote PowerShell session.
This defaults to `pwsh` which relies on a working `PATH` environment to find.
To use Windows PowerShell change this to `powershell` and also set `-Arguments "-Version 5.1 -NoLogo -ServerMode"`

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
A name to apply to the Runspace created.
If not set then a random name is chosen.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RunAsCredential
Run the remote PowerShell session with these credentials rather than the connection username.
If set then the remote PowerShell session will be running in an `Interactive` logon session which can delegate it's credentials to further hosts.
Because it is run in an `Interactive` session the user specified must have the right to log on interactively.

```yaml
Type: PSCredential
Parameter Sets: RunAsCredential
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RunAsSystem
Runs the remote PowerShell session as the `SYSTEM` account rather than the connection username.

```yaml
Type: SwitchParameter
Parameter Sets: RunAsSystem
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String[]
The computer name to create the session on.

## OUTPUTS

### System.Management.Automation.Runspaces.PSSession
A PSSession for each host connected to.

## NOTES
This is very experimental with little error handling and input validation.
The exchange between the pwsh process and spawning the Python `pypsexec` process is also quite rudimentary.
It is likely failures will be swallowed and the error messages returned back to the caller being generic messages.

Due to time constraints the PSExec handler is using a Python library that is spawned by the custom transport code.
This requires a working `python` interpreter to be present and that interpreter must be able to resolve the `pypsexec` process.
If Kerberos auth is required then `pip install smbprotocol[kerberos]` must also be run to install the optional Kerberos libraries required by `pypsexec`.

## RELATED LINKS

[Custom Remote Transport PR](https://github.com/PowerShell/PowerShell/pull/16923)
[pypsexec](https://github.com/jborean93/pypsexec)
