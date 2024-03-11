## Building log4net

Log4net provides support for a wide array of targets, including
- older .net 2 and 3.5 (including client profile for net-40)
- more modern net40/net45
- netstandard1.3/2.0

As such, it does require a relatively full .net environment on Windows to build.
I have recently found a fairly freshly-installed win11 machine to work adequately,
after adding the "windows feature" for ".NET Framework 3.5 (includes .NET 2.0 and 3.0)"

TL;DR (Windows):
- install Visual Studio Build Tools (at least VS2019)
- install nodejs (at least v16)
- install dotnet (v7+) and the .NET SDK (current latest)
- install dotnet 2, 3, 3.5 via "add windows features"
- in the project folder:
  - `npm i`
  - `npm run build`

TL;DR (!Windows):
- install the dotnet SDK - v7 or better (at time of writing, v8 is current but
  I've had some issues elsewhere, so I'm waiting a bit before recommending it
  everywhere
- install Mono (you're going to need it to target certain versions of .NET)
- install nodejs 16+
  - in the project folder:
  - `npm i`
  - `export DOTNET_CORE=1 npm run build`
    - we force using `dotnet` on non-windows targets for now. At some point,
      this should become automatic

## The full story

Options:
- build locally. Suggested environment:
    - Pre-requisites:
        - Visual Studio 2019 Build Tools
            - include desktop targets at least
            - include dotnet core targets or download and install
                the latest dotnet sdk (you will need at least v2.1)
            - note that build is possible with VS2022 build tools, but I had to
                install VS2019 build tools _as well_ to get msbuild to recognise 
                the legacy net35-profile target
        - Ensure you have .NET Framework 3.5 SP1 installed
            - on Win10+, this can only be installed via Add/Remove Windows Components
            - on other platforms, see https://dotnet.microsoft.com/download/dotnet-framework/net35-sp1
            - Building against .net 2/3.5, especially Client Profile, is not supported on Mono
        - Log4Net supports some older, out-of-support .net SDKs, particularly
          dotnet core 1.1 and .net framework client profiles for 3.5 and 4.0.
          There are helper powershell scripts in the root of this
          repository to assist with obtaining and installing these
          SDKs from Microsoft servers. Please see:
            - [install-dotnet-core-sdk-1.1.ps1](install-dotnet-core-sdk-1.1.ps1)
            - [install-net-framework-sdk-3.5.ps1](install-net-framework-sdk-3.5.ps1)
    - Binaries can be built with a Visual Studio or Rider installation
    - Binaries, packages and a release zip can be built via commandline
        - Ensure that you have a reasonably modern NodeJS installed (at least version 8+)
            - `npm ci`
            - `npm run build`
            - optionally `npm test` to run all tests
            - optionally `npm run release` to generate release artifacts
- build locally (CLI edition)
  - install nodejs (at least v16)
  - `npm i`
  - `npm run build`
- build via docker for windows, using the `build-with-docker-for-windows.bat` script
- build via the vs2019 Windows AppVeyer image. There is an appveyer.yml file
    included which (should) build if you set up AppVeyer to track
    your fork. AppVeyer is free for open-source projects.
    (TODO: should have a link to the official AppVeyer build)

## Updating the site

Log4Net uses Maven to build the site. Source artifacts can be found under `src/site`.
Building the site can be accomplished with `npm run build-site`. You should have maven
installed:
- Windows: get it from Scoop
- OSX: get it from Homebrew
- Linux: use your package manager

The site will be generated in `target/site`, and can be viewed locally. Updates should
be pushed to the `asf-staging` branch of [https://github.com/apache/logging-log4net-site](https://github.com/apache/logging-log4net-site])

Once the site has been pushed to the `asf-staging` branch, it can be viewed at
[http://logging.staged.apache.org/log4net](http://logging.staged.apache.org/log4net)
