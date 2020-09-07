const 
    gulp = requireModule("gulp");

gulp.task("prepare-build-artifacts", gulp.series(
    "zip",
    "prefix-build-artifacts",
    "hash-build-artifacts"
));
