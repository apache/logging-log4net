Log4Net Release procedure
---

This serves to document the release procedure for log4net, which is probably
more of interest to maintainers than anyone else, but I've found that there
are enough moving parts and time between releases to make the process more
difficult than it needs to be. Some parts are automated and others can be in
the future.

Assuming the code is in a place where a release can be made, for the imagined
release version 2.0.123:

1. Update the documentation under `src/site`
    - minimally, this means at least:
        - `src/site/xdoc/release/release-notes.xml`
            - copy an existing release section & think about:
                - what does this release change?
                - bug fixes?
                - enhancements
                - don't forget to mention contributors
                    - people who reported issues
                    - people who created pull requests
                    - people who suggested code that was implemented
        - `src/site/xdoc/download_log4net.xml`
            - you should be able to search & replace on the prior version
                for the new one you're about to create
2. Update the log4net.csproj file with this new version
3. Build release artifacts with `npm run release`
    - if this doesn't work, you may need to `npm ci` first!
    - currently, this _must_ happen on a windows machine because of older
        .net framework requirements which cannot be met on a Linux machine
        (or at least, I haven't figured out how - in particular CF)
4. Sign release artifacts (zips & nupkg) under `build/artifacts`
    - eg `gpg --argmor --output log4net-2.0.123.nupkg.asc --detach-sig log4net-2.0.123.nupkg`
    - there is an accompanying `sign-log4net-libraries.sh` which you could invoke if you cd
        into the `build/artifacts` folder
        - I build on Windows and sign on Linux as my build machine belongs to my company
            and I don't want to store keys there. Always protect your keys fervently!
5. Clone out the log4net doc repo (https://github.com/apache/logging-log4net-site)
    - check out the `asf-staging` branch
    - create a folder which includes the version, eg `log4net-2.0.123`
6. Copy the contents of `target/site` from this repo into the folder created in (5)
7. Update the symlinks in the base of the docs repo, ie:
    - 2.0.x -> 2.0.123
    - 2.x -> 2.0.123
8. update the `doap_log4net.rdf` to point to the new release 
    - (copy-paste-modify an existing release)
9. update the `.htaccess` file
    - the trailing RewriteRule should point to the new log4net-2.0.123 folder
10. push the `asf-staging` branch to github and wait a bit 
    - after a minute or two, check the updates at https://logging.staged.apache.org/log4net
        - are you seeing the correct releases page?
        - are you seeing the correct downloads page?
        - download links should (at this point) not work
11. create an rc-releasd at GitHub with a tag like `rc/2.0.123-rc1`
    - attach all the files from the build/artifacts folder, _including signatures_
12. get the artifacts in build/artifacts up to https://downloads.apache.org/logging/log4net/
    - `svn co https://dist.apache.org/repos/dist/dev/logging -N apache-dist-logging-dev`
    - `cd apache-dist-logging-dev`
    - `svn up log4net`
    - copy in source & binary artifacts to respective locations
    - `svn delete` old items
    - `svn commit`
13. raise a vote on the log4net mailing list (dev@logging.apache.org)
14. wait
15. when the vote has 3 or more +1's, it's time to go live!
16. copy the apache artifacts (binary and source) to the release svn repo and commit
    - `svn co https://dist.apache.org/repos/dist/release/logging -N apache-dist-logging-release`
    - `cd apache-dist-logging-release`
    - `svn up log4net`
    - copy in source & binary artifacts to respective locations
    - `svn delete` old items
    - `svn commit`
16. push the .nupkg to nuget.org
17. don't forget to make the docs live: in the logging-log4net-site folder:
    - `git checkout asf-site`
    - `git pull --rebase`
    - `git merge asf-staging`
18. rename the release at github, eg to `rel/2.0.123`
    - double-check that the `rel` tag is created

