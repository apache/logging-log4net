@ECHO OFF

REM We are going to change the environment variables, so protect the current settings.
SETLOCAL ENABLEDELAYEDEXPANSION

REM Figure out the path to the log4net directory
call :ComputeBase %~f0
set LOG4NET_DIR=%RESULT%

echo LOG4NET_DIR is %LOG4NET_DIR%

REM Get path to NAnt.exe
set NANTEXE_PATH=C:\net\nant-20031003\bin\nant.exe
echo NANTEXE_PATH is %NANTEXE_PATH%


REM Setup the build file
IF EXIST nant.build (
	set BUILD_FILE=nant.build
) ELSE (
	set BUILD_FILE=%LOG4NET_DIR%\log4net.build
)

echo BUILD_FILE is %BUILD_FILE%

REM Check for Mono install, update path to include mono libs
IF EXIST %WINDIR%\monobasepath.bat (
	CALL %WINDIR%\monobasepath.bat
	
	REM Remove quotes from path
	SET CLEAN_MONO_BASEPATH=!MONO_BASEPATH:"=!

	SET PATH="!CLEAN_MONO_BASEPATH!\bin\;!CLEAN_MONO_BASEPATH!\lib\;%path%"
)

IF "%1"=="package" GOTO Package

REM echo PATH is %PATH%

"%NANTEXE_PATH%" "-buildfile:%BUILD_FILE%" %1 %2 %3 %4 %5 %6 %7 %8
GOTO End

:Package
IF "%2"=="" GOTO NoProjectVersion

"%NANTEXE_PATH%" "-buildfile:%BUILD_FILE%" package "-D:package.version=%2" %3 %4 %5 %6 %7 %8
GOTO End

:NoProjectVersion
ECHO Please specify the version number of log4net that you want to package.
GOTO CommandLineOptions

:CommandLineOptions
ECHO The following commandline is valid :
ECHO usage: build.cmd [target]
ECHO When no target is specified, debug build configuration for the .NET Framework 1.0 is built.
echo When the target is "package", the version number of log4net that you want to package has to be specified.
ECHO eg. build.cmd package 1.3.0
GOTO End


REM ------------------------------------------
REM Expand a string to a full path
REM ------------------------------------------
:FullPath
set RESULT=%~f1
goto :EOF

REM ------------------------------------------
REM Compute the current directory
REM given a path to this batch script.
REM ------------------------------------------
:ComputeBase
set RESULT=%~dp1
REM Remove the trailing \
set RESULT=%RESULT:~0,-1%
Call :FullPath %RESULT%
goto :EOF


:End
ENDLOCAL
EXIT /B 0

