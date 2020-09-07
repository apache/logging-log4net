const
    gulp = requireModule("gulp");

gulp.task("hash-build-artifacts", async () => {
    const
        path = require("path"),
        crypto = require("crypto"),
        { readFile, writeFile, ls, FsEntities } = require("yafs"),
        artifactsFolder = path.join("build", "artifacts");
    const
        buildArtifacts = await ls(artifactsFolder, {
            fullPaths: true,
            entities: FsEntities.files
        }),
        nupkg = buildArtifacts.find(p => p.match(/\.nupkg$/)),
        binaries = buildArtifacts.find(p => p.match(/apache-log4net-binaries-\d+\.\d+\.\d+.zip$/)),
        source = buildArtifacts.find(p => p.match(/apache-log4net-source-\d+\.\d+\.\d+.zip$/));

    if (!nupkg) {
        throw new Error(`apache-log4net nupkg not found in ${artifactsFolder}`);
    }
    if (!binaries) {
        throw new Error(`apache-log4net binaries zip not found in ${artifactsFolder}`);
    }
    if (!source) {
        throw new Error(`apache-log4net source zip not found in ${artifactsFolder}`);
    }

    await writeSHA512For(nupkg);
    await writeSHA512For(binaries);
    await writeSHA512For(source);

    function writeSHA512For(filepath) {
        return new Promise(async (resolve, reject) => {
            try {
                const
                    hash = crypto.createHash("sha512"),
                    data = await readFile(filepath);
                hash.update(data);
                const
                    outfile = `${filepath}.sha512`,
                    hex = hash.digest("hex"),
                    contents = `${hex} *${path.basename(filepath)}`;
                await writeFile(outfile, contents);
                resolve();
            } catch (e) {
                reject(e);
            }
        });
    }
});
