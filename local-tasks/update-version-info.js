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
  gulp = requireModule("gulp");

gulp.task("update-version-info", async () => {
  // the version as per the .csproj is the correct version, but there
  // are other places where the version string is set via [assembly]
  // attributes, so we need to re-align them all
  const
    Git = require("simple-git/promise"),
    readTextFile = requireModule("read-text-file"),
    writeTextFile = requireModule("write-text-file"),
    readCsProjVersion = requireModule("read-csproj-version"),
    currentVersion = await readCsProjVersion("src/log4net/log4net.csproj"),
    assemblyInfo = "src/log4net/AssemblyInfo.cs",
    assemblyVersionInfo = "src/log4net/AssemblyVersionInfo.cs",
    versionString = sanitiseVersion(currentVersion);

  await updateVersionsIn(assemblyInfo, versionString);
  await updateVersionsIn(assemblyVersionInfo, versionString);

  const git = new Git(".");
  await git.add([
    assemblyInfo,
    assemblyVersionInfo
  ]);
  await git.commit(`:bookmark: update versioning to ${versionString}`);

  async function updateVersionsIn(
    filePath,
    newVersion
  ) {
    const
      contents = await readTextFile(filePath),
      updated = contents
        // specific matches for "x.x.x.x"
        .replace(/"\d+\.\d+\.\d+\.\d+"/g, `"${newVersion}"`)
        // matches for "x.x.x.x- as found in AssemblyVersionInfo.cs
        .replace(/"\d+\.\d+\.\d+\.\d+-/g, `"${newVersion}-`);
    await writeTextFile(filePath, updated);
  }

  function sanitiseVersion(version) {
    const parts = version.split(".");
    while (parts.length < 4) {
      parts.push("0");
    }
    return parts.slice(0, 4).join(".");
  }
});


