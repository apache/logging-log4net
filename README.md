Introduction
===========

Apache log4net is a sub project of the Apache Logging Services project. 
Apache log4net graduated from the Apache Incubator in February 2007.
Web site: http://logging.apache.org/log4net


Documentation
=============

For the latest documentation see the log4net web site at:
http://logging.apache.org/log4net

Contributing
============

log4net development happens on the logging dev mailing list, see
https://logging.apache.org/log4net/mail-lists.html.  Please join the
mailing list and discuss bigger changes before working on them.

For bigger changes we must ask you to sign a Contributor License
Agreement http://www.apache.org/licenses/#clas.

Github pull requests are one way to contribute, The Apache issue
tracker is no longer accepting new issues but is available at 
https://issues.apache.org/jira/browse/LOG4NET for access to 
previous issues. 

Developing
==========

log4net targets a wide array of .net platforms, including some
which are out of support from Microsoft, making it difficult to
install relevant SDKs and build for those targets. In particular,
older Client Profile .NET Framework targets and dotnet core 1.1
may be installed by using the bundled helper scripts:

- [install-net-framework-sdk-3.5.ps1]()
- [install-dotnet-core-sdk-1.1.ps1]()

These scripts download the relevant installers from Microsoft servers,
but you run them at your own risk.

Please see 
- [CONTRIBUTING.md](doc/CONTRIBUTING.md)
- [BUILDING.md](doc/BUILDING.md)
- [RELEASING.md](doc/RELEASING.md)
