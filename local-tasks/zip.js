const
  gulp = requireModule("gulp"),
  promisify = requireModule("promisify-stream"),
  readCsProjVersion = requireModule("read-csproj-version"),
  target = "build/artifacts",
  zip = require("gulp-zip");

gulp.task("zip", [ "zip-binaries", "zip-source"], () => Promise.resolve());

gulp.task("zip-binaries", async () => {
  const version = await readVersion();
  return promisify(
    gulp.src(["build/Release/**/*", "LICENSE", "NOTICE"])
      .pipe(zip(`log4net-binaries-${version}.zip`))
      .pipe(gulp.dest(target))
  );
});

gulp.task("zip-source", async () => {
  const version = await readVersion();
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
      "!.idea/**/*"
    ])
    .pipe(zip(`log4net-source-${version}.zip`))
    .pipe(gulp.dest(target))
  );
});

function readVersion() {
  return readCsProjVersion("src/log4net/log4net.csproj");
}
