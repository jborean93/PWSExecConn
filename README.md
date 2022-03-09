# PWSExecConn

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/jborean93/PWSExecConn/blob/main/LICENSE)

Proof of Concept custom PSSession transport for PowerShell.

## Documentation

Documentation for this module and details on the cmdlets included can be found [here](docs/en-US/PWSExecConn.md).

## Requirements

This module has the following requirements:

* PowerShell v7.3 or newer
    * Requires the changes in https://github.com/PowerShell/PowerShell/pull/16923
* Python
    * [pypsexec](https://github.com/jborean93/pypsexec)

To allow this module to work on Linux the Python [pypsexec module](https://github.com/jborean93/pypsexec) is used to do the communication work with the remote host.
This module implements all the functionality required to connect to a host over SMB, copy the service binary, and manage the input/output of the remote process.
Unfortunately this means that this module has a dependency on Python and this module.
To install this you typically just need to run

```bash
pip install pypsexec

# Skip this step if you don't care about Kerberos/implicit authentication
pip install smbprotocol[kerberos]
```

To verify this was installed correctly open up PowerShell then run:

```powershell
python -c "import pypsexec"
```

This verifies that the PowerShell process can find a valid `python` interpereter and that interpreter can import the `pypsexec` module.
If this step fails then the module will not be able to create the PSExec based connection.

## Installing

This module isn't meant to be uploaded to the PSGallery.
It must be built first and then imported using:

```powershell
./build.ps1 -Configuration Debug
Import-Module -Name ./output/PWSExecConn
```
