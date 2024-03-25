/// <reference path="../node_modules/zarro/types.d.ts" />
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
(function () {
    const
      debug = requireModule<DebugFactory>("debug")(__filename),
      gulp = requireModule<Gulp>("gulp"),
      env = requireModule<Env>("env"),
      nugetSourceName = process.env.NUGET_SOURCE || "nuget.org",
      installLocalTools = requireModule<InstallLocalTools>("install-local-tools"),
      isDotnetCore = env.resolveFlag("DOTNET_CORE"),
      tools = isDotnetCore
        ? [] // currently, only dotnet targets are used for dotnet test/build
        : [
          `${ nugetSourceName }/nunit.consolerunner`
        ];
  
    env.associate("default-tools-installer", [ "BUILD_TOOLS_FOLDER", "DOTNET_CORE" ]);
  
    gulp.task(
      "default-tools-installer",
      `Installs the default toolset: ${ tools.join(", ") }`,
      () => {
        if (env.resolveFlag("DOTNET_CORE")) {
          debug(`DOTNET_CORE builds currently have ${ tools.length } default tools to install`);
        }
        if (tools.length === 0) {
          // don't waste time calling into the installer
          // when there are no tools to install
          return Promise.resolve();
        }
        return installLocalTools.install(tools);
      }
    );
  
    gulp.task(
      "clean-tools-folder",
      "Cleans out folders under the tools folder (will always be done as part of tool installation)",
      () => {
        return installLocalTools.clean();
      }
    );
  })();
  