WebApp
======

Example web application using log4net.

To setup IIS to run this web application:

 - In IIS create a new Virtual Directory 
   in the root of the default web site.
 - Set the alias to 'WebApp<code language>', where <code language> 
   can be either CS (for the C# version of this tutorial) or 
   VB (for the VB.NET version of this tutorial)
   (e.g. WebAppCS)
 - Set the directory to an underlying 'src' directory
   (e.g. 'C:\log4net\examples\net\1.0\Tutorials\WebApp\cs\src')
 - Set the access permissions to Read & Run scripts

To run the test application

 - Visit: http://localhost/WebAppCS/WebForm1.aspx or http://localhost/WebAppVB/WebForm1.aspx
 - Calculate some sums

To setup logging

 - Examine the WebApp.dll.log4net file. 
 - The ASPNET user account must be given permissions
   to write to logfiles 
 - Once you have set the permissions try rerunning
   the test application.
