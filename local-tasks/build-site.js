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

const
  gulp = requireModule("gulp"),
  spawn = requireModule("spawn"),
  env = requireModule("env"),
  os = require("os"),
  which = require("which");

gulp.task("build-site", async () => {
  const { rm } = require("yafs");
  let maven;
  try {
    maven = await which("mvn");
  } catch (e) {
    let extra;
    switch (os.platform()) {
      case "win32":
        extra = "You may install maven via chocolatey (https://chocolatey.org)";
        break;
      case "darwin":
        extra = "You may install maven via homebrew";
        break;
      default:
        extra = "You should install maven with your package manager";
        break;
    }
    throw new Error(`Unable to find mvn in your path. ${ extra }`);
  }
  await rm("target");
  return spawn("mvn", [ "site" ]);
});

