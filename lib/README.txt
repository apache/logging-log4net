Assemblies placed into the main subdirectory will be used when
compiling log4net itself, assemblies in the test directory will be
used when compiling the tests.

The only assembly required is nunit.framework.dll when compiling the
tests.

In each subdirectory it is possible to create a structure that mirrors
lognet's output directory to contain assemblies that are only used
when compiling for the matching target framework, i.e.

lib\main\net\2.0\foo.dll

would be used when targeting .NET 2.0 but not when targeting .NET 4.0
or Mono 2.0.
