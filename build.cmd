@ECHO OFF

REM We are going to change the environment variables, so protect the current settings.
SETLOCAL ENABLEDELAYEDEXPANSION

IF "%1"=="-?" GOTO CommandLineOptions

REM Figure out the path to the log4net directory
CALL :ComputeBase %~f0
SET LOG4NET_DIR=%RESULT%
ECHO LOG4NET_DIR is %LOG4NET_DIR%

REM Get path to NAnt.exe

REM Try and determine if NAnt is in the PATH
SET NANTEXE_PATH=nant.exe
"%NANTEXE_PATH%" -help >NUL: 2>NUL:
IF NOT ERRORLEVEL 1 goto FoundNAnt

REM Try hard coded path for NAnt
SET NANTEXE_PATH=C:\Program Files\NAnt\nant-0.85-nightly-2006-03-06\bin\nant.exe
"%NANTEXE_PATH%" -help >NUL: 2>NUL:
IF NOT ERRORLEVEL 1 goto FoundNAnt

REM We have not found NAnt
ECHO.
ECHO NAnt does not appear to be installed. NAnt.exe failed to execute.
ECHO Please ensure NAnt is installed and can be found in the PATH.
GOTO EndError


:FoundNAnt
ECHO NANTEXE_PATH is %NANTEXE_PATH%

REM Setup the build file
IF EXIST nant.build (
	SET BUILD_FILE=nant.build
) ELSE (
	SET BUILD_FILE=%LOG4NET_DIR%\log4net.build
)

ECHO BUILD_FILE is %BUILD_FILE%


IF "%1"=="package" GOTO Package

"%NANTEXE_PATH%" "-buildfile:%BUILD_FILE%" %1 %2 %3 %4 %5 %6 %7 %8
GOTO EndOk

:Package
IF "%2"=="" GOTO NoProjectVersion

"%NANTEXE_PATH%" "-buildfile:%BUILD_FILE%" package "-D:package.version=%2" %3 %4 %5 %6 %7 %8
GOTO EndOk

:NoProjectVersion
ECHO.
ECHO SYNTAX ERROR: Missing Version String.
ECHO Please specify the version number of log4net that you want to package.
GOTO CommandLineOptions

:CommandLineOptions
ECHO.
ECHO Use the following command line syntax:
ECHO.
ECHO     build.cmd -?
ECHO     build.cmd -projecthelp
ECHO     build.cmd [nant target]
ECHO     build.cmd package [version string]
ECHO.
ECHO To get a list of all NAnt build targets run build.cmd with the -projecthelp option.
ECHO If no NAnt target is specified then the default target is 'compile-all'. This will compile all configurations on all available frameworks.
ECHO When using the 'package' command the version label for the package must be specified.
ECHO.
ECHO     Examples:
ECHO.
ECHO     build.cmd compile-mono-1.0
ECHO     build.cmd compile-all
ECHO     build.cmd package 1.3.0
ECHO     build.cmd package 2.1.0-alpha
ECHO.
GOTO EndError


REM ------------------------------------------
REM Expand a string to a full path
REM ------------------------------------------
:FullPath
SET RESULT=%~f1
GOTO :EOF

REM ------------------------------------------
REM Compute the current directory
REM given a path to this batch script.
REM ------------------------------------------
:ComputeBase
SET RESULT=%~dp1
REM Remove the trailing \
SET RESULT=%RESULT:~0,-1%
CALL :FullPath %RESULT%
GOTO :EOF


:EndOk
ENDLOCAL
EXIT /B 0

:EndError
ENDLOCAL
EXIT /B 1

