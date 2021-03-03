Windows Killed Process Canary
======================

A Windows Service to detect if it has been shutdown and hibernate if multiple instances are. 

Released as open source by NCC Group Plc - http://www.nccgroup.com/

Developed by Ollie Whitehouse, ollie dot whitehouse at nccgroup dot com

https://github.com/nccgroup/KilledProcessCanary

Released under AGPL see LICENSE for more information

Hypothesis
-------------
Certain threat actors stop a number of services / kill a number of processes prior to encrypting with their ransomware. We deploy a number of canary processes which keep track of each other. If these services are stopped (via net stop or similar) and not during a host shutdown we fire a Canary DNS token and hibernate the host. By doing this we will:
* Minimize the impact / likelihood of successful encryption
* Give the best chance of key recory from RAM

Compatibility
-------------
Should work on any version of Windows

What it does
-------------
Simply:
* Creates a mutex and shares the number of running instances
* net stop on a Canary service will cause it decrement
* if that falls below 2 we fire a DNS canary token with the hostname and hibernate the host

How to Use
-------------
* Replace the Canary Token URL
* Compile SWOLLENRIVER
* Deploy into typical paths for at least TWO targetted Windows Services
* Install the Windows Services e.g.
```
New-Service -Name "MSSQL" -BinaryPathName "C:\Program Files\Microsoft SQL Server\sqlserver.exe"
```
* Start
* ... profit

History
-------------
The genesis of the idea came from a discussion with Harry..

TTP Examples for Ryuk
-------------
* https://www.carbonblack.com/blog/vmware-carbon-black-tau-ryuk-ransomware-technical-analysis/
* https://www.crowdstrike.com/blog/big-game-hunting-with-ryuk-another-lucrative-targeted-ransomware/

e.g.

```
net stop avpsus /y
net stop McAfeeDLPAgentService /y
net stop mfewc /y
net stop BMR Boot Service /y
net stop NetBackup BMR MTFTP Service /y
```

Example DNS Canary Fire
-------------
This uses a Canary Token (https://www.canarytokens.org/) DNS token to encode the hostname and the fact it is hibernating via the auxiliary information mechanism documented here https://docs.canarytokens.org/guide/dns-token.html#creating-a-dns-token.

For this to work you will need to replace the REPLACEME here:
https://github.com/nccgroup/KilledProcessCanary/blob/master/Program.cs#L241

An example alert seen is as follows:
![DNS Canary Token Firing](https://github.com/nccgroup/KilledProcessCanary/blob/master/Screenshots/DNSCanaryScreenShot.png)