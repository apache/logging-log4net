const
  gulp = requireModule("gulp-with-help"),
  promisify = requireModule("promisify-stream"),
  readNuspecVersion = requireModule("read-nuspec-version"),
  zip = require("gulp-zip");

gulp.task("zip", async () => {
  const version = await readNuspecVersion("log4net.nuspec");
  return promisify(
    gulp.src("build/Release/**/*")
      .pipe(zip(`log4net-binaries-${version}.zip`))
      .pipe(gulp.dest("build/artifacts"))
  );
});