const { renameSync } = require("fs");

const gulp = requireModule("gulp");

gulp.task("prefix-build-artifacts", async () => {
    // prefixes build artifacts with 'apache-'
    const
        { ls, rename, FsEntities } = require("yafs"),
        path = require("path"),
        artifactsFolder = path.join("build/artifacts"),
        contents = await ls(artifactsFolder, { fullPaths: true, entities: FsEntities.files });
    for (let item of contents) {
        const basename = path.basename(item);
        if (basename.match(/^apache-/)) {
            continue;
        }
        const newName = path.join(
            path.dirname(item),
            `apache-${basename}`
        );
        await rename(item, newName, true);
    }
});