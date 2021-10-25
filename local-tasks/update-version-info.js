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


