Windows Killed Process Canary
======================

A Windows Service to performantly produce telemetry on new or modified Windows memory pages that are now executable every 30 seconds.

Released as open source by NCC Group Plc - http://www.nccgroup.com/

Developed by Ollie Whitehouse, ollie dot whitehouse at nccgroup dot com

https://github.com/nccgroup/KilledProcessCanary

Released under AGPL see LICENSE for more information

Blog
-------------
https://research.nccgroup.com/2020/10/03/tool-windows-executable-memory-page-delta-reporter/

Hypothesis
-------------
Certain threat actors stop a number of services / kill a number of processes prior to encrypting with their ransomware. We deploy a number of canary processes which keep track of each other. If these services are stopped (via net stop or similar) and not during a process shutdown we fire a Canary DNS token and hibernate the host.

Compatibility
-------------
Should work on any version of Windows

What it does
-------------
Simply:
* Creates a mutex and shares the number of running instances
* net stop on a service will cause it decrement
* if that falls below 2 we fire a DNS canary token and hibernate the host

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