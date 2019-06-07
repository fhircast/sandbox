@echo off
IF NOT EXIST %~dp0\Hub\ (
	echo Run batch file in sandbox level directory
)
IF NOT EXIST %~dp0\WebSubClient\ (
	echo Run batch file in sandbox level directory
)

start cmd.exe /k "dotnet run --project Hub"
start cmd.exe /k "dotnet run --project WebSubClient"
start "" http://localhost:5001