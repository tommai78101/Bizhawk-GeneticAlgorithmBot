@echo off
set PATH=%PATH%;C:\Program Files (x86)\git\bin;C:\Program Files\git\bin
git --version > NUL
@if errorlevel 1 goto MISSINGGIT

dotnet build .\src\GeneticAlgorithmBot.sln -c Release --no-incremental > NUL
@if not errorlevel 0 goto DOTNETBUILDFAILED

dotnet build .\src\GeneticAlgorithmBot.sln -c Release
@if errorlevel 1 goto DOTNETBUILDFAILED

@cp .\src\bin\Release\net48\GeneticAlgorithmBot.dll .\BizHawk\ExternalTools\ 2>nul
@if errorlevel 1 copy .\src\bin\Release\net48\GeneticAlgorithmBot.dll .\BizHawk\ExternalTools\
goto END

:DOTNETBUILDFAILED
set ERRORLEVEL=1
@echo dotnet build failed. Usual cause: user committed broken code, or unavailable dotnet sdk
goto END

:MISSINGGIT
set ERRORLEVEL=1
@echo missing git.exe. can't make distro without that.

:END


