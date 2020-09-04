const
    gulp = requireModule("gulp"),
    spawn = requireModule("spawn"),
    env = requireModule("env"),
    os = require("os"),
    which = require("which");

gulp.task("build-site", async () => {
    let maven;
    try {
        maven = await which("mvn");
    } catch (e) {
        let extra;
        switch (os.platform()) {
            case "win32":
                extra = "You may install maven via scoop (https://scoop.sh/)";
                break;
            case "darwin":
                extra = "You may install maven via homebrew";
                break;
            default:
                extra = "You should install maven with your package manager";
                break;
        }
        throw new Error(`Unable to find mvn in your path. ${extra}`);
    }

    return spawn("mvn", [ "site" ]);
});

