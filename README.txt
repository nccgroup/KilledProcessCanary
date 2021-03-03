Installation
-------------

1) Create the Directories

2) Copy the Service Binaries

3) Install the first instance
New-Service -Name "MSSQL" -BinaryPathName "C:\Program Files\Microsoft SQL Server\sqlserver.exe"

4) Install the second instance
New-Service -Name "MySQL" -BinaryPathName "C:\Program Files\MySQL\mysqld.exe"

5) Start the Services
net start MSSQL
net start MySQL
