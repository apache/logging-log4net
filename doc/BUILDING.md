## Building log4net

Log4net provides support for the following targets
- net462
- netstandard2.0

TL;DR (Windows):
- install Visual Studio Build Tools (at least VS2019)
- install nodejs (at least v16)
- install dotnet (v8+) and the .NET SDK (current latest)
- in the project folder:
  - `npm i`
  - `npm run build`

TL;DR (Docker):
- install docker (if you haven't already)
  - https://docs.docker.com/engine/install/
- in logging/log4net run
  - `docker build -t log4net-builder .`
  - `docker run -it log4net-builder`
    - this will
      - install all dependencies in the container
      - build src/log4net.sln
  - inside the container run
    - `dotnet test /logging-log4net/src/log4net.sln`

TL;DR (!Windows):
- install the dotnet SDK - v8 or better
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
                the latest dotnet sdk (you will need at least v8)
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
- build via the vs2019 Windows AppVeyor image. There is an appveyor.yml file
    included which (should) build if you set up AppVeyer to track
    your fork. AppVeyer is free for open-source projects.
    (TODO: should have a link to the official AppVeyor build)

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
