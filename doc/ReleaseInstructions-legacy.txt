THIS DOCUMENT NEEDS TO BE ADAPTED NOW WE'VE MOVED TO GIT

This document is a work in progress and was created in parallel to the
release of Apache log4net 1.2.11 and adapted for 2.0.6.

Prereqs
=======

* make sure you have all the required software around.  For the 2.0.6
  release this meant

  - you may need a couple of Windows or Linux boxes in order to be
    able to build all target frameworks.

  - Things you need

    o a Subversion command line client

    o NAnt 0.92 or better http://nant.sourceforge.net/

      Make sure to unblock the ZIP before you extract it.

    o Some recent version of a Java runtime environment (Java5 at
      minimum)

    o Apache Maven 3.x http://maven.apache.org/

      You may want to set some environment variables like Path (to
      include Maven's and NAnt's bin directories), M2_HOME and
      JAVA_HOME to make things easier.

    o .NET Framework 3.5SP1 and SDK 2.0

      As of October 2011 this is available from
      <http://msdn.microsoft.com/en-us/netframework/>

    o .NET Framework and SDK 4.0

      As of October 2011 this is available from
      <http://msdn.microsoft.com/en-us/netframework/>

    o .NET Core Developer Kit available from https://www.microsoft.com/net/core

    o Mono available from http://www.mono-project.com/download/

      Unfortunately NAnt 0.92 has issues with Mono that is too recent,
      I used a Ubuntu 14.04 installation and installed NAnt and Mono
      via the Ubuntu Debian packages. It comes with Mono 3.2.8.

    o Sandcastle Help File Builder with its Dependencies

      As of November 2015 this is available from
      <https://github.com/EWSoftware/SHFB>

      Install Html Help 1 Compiler by follwoing the instructions
      during the SGFB installation. It may tell you there'd already be
      a newer version but SHFB needs this installation anyway.

* Make sure your PGP key is in
  <https://svn.apache.org/repos/asf/logging/log4net/trunk/KEYS> and
  copy that file to
  https://dist.apache.org/repos/dist/release/logging/log4net/ .

  You should also upload your key to the keyservers.

* Make sure you have decrypted old-log4net.snk.gpg to old-log4net.snk

Preparing the Stage
===================

* Make sure the correct version number (2.0.6 right now) is in all
  the required places.  [yes, there is a lot of duplication]

  - log4net.build: <property name="package.version" value="2.0.6"/>
  - pom.xml: <version>2.0.6</version>
  - log4net.nuspec: <version>2.0.6</version>
  - log4net.shfbproj: <HtmlHelpName>log4net-sdk-2.0.6</HtmlHelpName>
  - src/AssemblyInfo.cs: many, many AssemblyInformationalVersionAttribute
  - src/AssemblyVersionInfo.cpp as well as .cs, .vb and .js - twice in
    each file
    These files also hold the assembly's copyright statement.  Make
    sure it includes the current year.
  - src/Log4netAssemblyInfo.cs: public const string Version = "2.0.6"
  - src/site/xdocs/download_log4net.xml - many times
  - netstandard/log4net/project.json -  "version": "2.0.6",
  - netstandard/log4net.tests/project.json -  "version": "2.0.6",

* Create the site using "nant generate-site" in order to create the
  RAT report as a side-effect and fix all files that don't have the
  proper license header.

* Make sure NOTICE corresponds to the general format of

  ,----
  |Apache log4net
  |Copyright 2004-{latest} The Apache Software Foundation
  |
  |This product includes software developed by
  |The Apache Software Foundation (http://www.apache.org/).
  `----

  in particular, check that {latest} is the current year.

* Update the release notes

  - go to the Roadmap View in JIRA
    <https://issues.apache.org/jira/browse/LOG4NET#selectedTab=com.atlassian.jira.plugin.system.project%3Aroadmap-panel>

  - follow the Release Notes link for the release you are going to
    create

  - Copy the text to src/site/xdocs/release/release-notes.xml in a new
    section for the new release and massage it to your liking.

Create the Release
==================

* Tag the source tree that makes up the release

  $ svn cp -r 1775235 \
        https://svn.apache.org/repos/asf/logging/log4net/trunk \
        https://svn.apache.org/repos/asf/logging/log4net/tags/2.0.6RC1

* Create a fresh working copy of the new tag on the Linux box and copy
  old-log4net.snk to the root directory.

* Run "nant"

* zip up the bin/mono directory and copy it to the Windows machine

* Create a fresh working copy of the new tag on the Windows box and copy
  old-log4net.snk as well as nuget.exe to the root directory.

* Extract the contents of the Mono zip to bin/mono

* Run "nant"

* Run SHFB

* Run "nant package"

* sign the distribution files, I used

  $ for i in log4net?2.0.6*; do \
        md5sum $i > $i.md5 \
        sha1sum $i > $i.sha1 \
        sha256sum $i > $i.sha256 \
        gpg --detach-sign --armor $i; done

* commit the distribution files to dest area of the svn dist
  repository, i.e to
  https://dist.apache.org/repos/dist/dev/logging/log4net
  also create a README.html based on the release-notes.

* publish the site build somewhere convenient for you -
  home.apache.org is a good option

* if you've got set up a myget feed, publish the nuget package

* call for a vote on the log4net-dev list.  It may be a good idea to
  copy the general list of Logging Services in order to reach more PMC
  members.

  The following is based on the template used in Commons

  ,----
  | log4net 2.0.6 RC1 is available for review here:
  |   https://dist.apache.org/repos/dist/dev/logging/log4net
  |   (revision 17495)
  | 
  | Details of changes since 1.2.15 are in the release notes:
  |   http://stefan.samaflost.de/staging/log4net-2.0.6/release/release-notes.html
  | 
  | I have tested this with Mono and several .NET frameworks using NAnt.
  | 
  | The tag is here:
  |   https://svn.apache.org/repos/asf/logging/log4net/tags/2.0.6RC1
  |   (revision 1775236)
  | 
  | Site:
  |   http://stefan.samaflost.de/staging/log4net-2.0.6/
  | 
  | RAT Report:
  |   http://stefan.samaflost.de/staging/log4net-2.0.6/rat-report.html
  | 
  | Nuget Package:
  |   https://www.myget.org/feed/log4net-test/package/nuget/log4net
  | 
  | Votes, please.  This vote will close in 72 hours, 1000 GMT 24-Dec 2016
  | 
  | [ ] +1 Release these artifacts
  | [ ] +0 OK, but...
  | [ ] -0 OK, but really should fix...
  | [ ] -1 I oppose this release because...
  | 
  | Thanks!
  `----

* If the vote doesn't pass, adapt trunk and start over with tagging
  the next release candidate

* Once the vote passes:

Publish the Release
===================

* svn mv the ZIPs, hashes and signatures to
  https://dist.apache.org/repos/dist/release/logging/log4net/source and
  https://dist.apache.org/repos/dist/release/logging/log4net/binaries
  respectively.
  You can do so using a local working copy of
  https://dist.apache.org/repos/dist - which tends to be huge, so
  check out selectively - or using svnmucc.

* create a file README.html holding just the latest news from
  release-notes.html and svn commit it to
  https://dist.apache.org/repos/dist/release/logging/log4net

* create a svn tag for the release from the release candidate tag that
  has been accepted

  $ svn cp -m "1.2.14 release has been accepted" \
        https://svn.apache.org/repos/asf/logging/log4net/tags/1.2.14RC1 \
        https://svn.apache.org/repos/asf/logging/log4net/tags/1.2.14

* publish the nuget package

* wait for the mirrors to catch up before proceeding, this takes
  several hours

Announce the Release
====================

* publish the website by copying it to
  https://svn.apache.org/repos/infra/websites/production/logging/content/log4net
  by convention create a directory named after the release, e.g. copy
  site to
  https://svn.apache.org/repos/infra/websites/production/logging/content/log4net/log4net-1.2.14
  Ensure line-feeds are consistent and that the cgi script has the
  svn:executable property set.
  Change the .htaccess files and 2.x/2.0.x symlinks in
  https://svn.apache.org/repos/infra/websites/production/logging/content/log4net
  so they point at your new directory.
  Copy the download_log4net.html file from the new directory to
  https://svn.apache.org/repos/infra/websites/production/logging/content/log4net

* send an announcement mail at least to

  log4net-dev@logging.apache.org
  log4net-user@logging.apache.org
  general@logging.apache.org
  announce@apache.org

  using your @apache.org email address.  PGP sign the announcement mail.

* mark the version as released in JIRA and create a new version for
  the next release if necessary

* Add the new version to the DOAP file at
  https://svn.apache.org/repos/asf/logging/log4net/doap_log4net.rdf

* Add the new version to the reporter tool
  https://reporter.apache.org/addrelease.html?logging

Post-Release Cleanup
====================

* Delete all artifacts of old releases from
  https://dist.apache.org/repos/dist/release/logging/log4net
