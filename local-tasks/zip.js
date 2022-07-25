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
  promisify = requireModule("promisify-stream"),
  readCsProjVersion = requireModule("read-csproj-version"),
  target = "build/artifacts",
  debug = require("gulp-debug"),
  rename = require("gulp-rename"),
  zip = require("gulp-zip");

gulp.task("zip", ["zip-binaries", "zip-source"], () => Promise.resolve());

gulp.task("zip-binaries", async () => {
  const
    version = await readVersion(),
    baseDir = `apache-log4net-binaries-${version}`;
  return promisify(
    gulp.src(["build/Release/**/*", "LICENSE", "NOTICE"])
      .pipe(rename(path => {
        path.dirname = `${baseDir}/${path.dirname}`
      }))
      .pipe(zip(`${baseDir}.zip`))
      .pipe(gulp.dest(target))
  );
});

gulp.task("zip-source", async () => {
  const
    version = await readVersion(),
    baseDir = `apache-log4net-source-${version}`;

  return promisify(
    gulp.src([
      "**/*",
      "!**/obj/**/*",
      "!**/bin/**/*",
      "!node_modules",
      "!node_modules/**/*",
      "!build-tools",
      "!build-tools/**/*",
      "!build",
      "!build/**/*",
      "!.idea",
      "!.idea/**/*",
      "!*.exe"
    ])
      .pipe(rename(path => {
        path.dirname = `${baseDir}/${path.dirname}`
      }))
      .pipe(zip(`${baseDir}.zip`))
      .pipe(gulp.dest(target))
  );
});

function readVersion() {
  return readCsProjVersion("src/log4net/log4net.csproj");
}
