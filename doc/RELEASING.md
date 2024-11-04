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
2. Build release artifacts with `npm run release`
    - if this doesn't work, you may need to `npm ci` first!
3. Sign release artifacts (zips & nupkg) under `build/artifacts`
    - `./scripts/sign-log4net-libraries.sh/ps1`
4. Clone out the log4net doc repo (https://github.com/apache/logging-log4net-site)
    - check out the `asf-staging` branch
    - create a folder which includes the version, eg `log4net-2.0.123`
5. Copy the contents of `target/site` from this repo into the folder created in (5)
    - remember to either update or link in sdk docs from a prior release
6. Update the symlinks in the base of the docs repo, ie:
    - 2.0.x -> 2.0.123
    - 2.x -> 2.0.123
7. update the `doap_log4net.rdf` to point to the new release 
    - (copy-paste-modify an existing release)
8. update the `.htaccess` file
    - the trailing RewriteRule should point to the new log4net-2.0.123 folder
9. push the `asf-staging` branch to github and wait a bit 
   - after a minute or two, check the updates at https://logging.staged.apache.org/log4net
      - are you seeing the correct releases page?
      - download links should (at this point) not work
10. create an rc-releasd at GitHub with a tag like `rc/2.0.123-rc1`
    - attach all the files from the build/artifacts folder, _including signatures_
11. get the artifacts in build/artifacts up to https://downloads.apache.org/logging/log4net/
    - `svn co https://dist.apache.org/repos/dist/dev/logging -N apache-dist-logging-dev`
    - `cd apache-dist-logging-dev`
    - `svn up log4net`
    - `svn delete *`
    - `mkdir 2.0.123`
    - copy all artifacts to the new folder
    - `svn add *`
    - `svn commit -m 'log4net 2.0.123'`
    - check https://dist.apache.org/repos/dist/dev/logging/log4net/2.0.123/
12. raise a vote on the log4net mailing list (dev@logging.apache.org) - see MailTemplate.txt
13. wait
14. when the vote has 3 or more +1's, it's time to go live!
15. copy the apache artifacts (binary and source) to the release svn repo and commit
    - `svn co https://dist.apache.org/repos/dist/release/logging -N apache-dist-logging-release`
    - `cd apache-dist-logging-release`
    - `svn up log4net`
    - copy in source & binary artifacts to respective locations
    - `svn delete` old items
    - `svn commit`
16. push the .nupkg to nuget.org
    - via `dotnet`: `dotnet nuget push <path to package> -s nuget.org -k <your nuget api key>`
    - via `nuget`: `nuget push <path to package> -Source nuget.org -ApiKey <your nuget api key>`
17. don't forget to make the docs live: in the logging-log4net-site folder:
    - `git checkout asf-site`
    - `git pull --rebase`
    - `git merge asf-staging`
18. rename the release at github, eg to `rel/2.0.123`
    - double-check that the `rel` tag is created
19. apply the next version by calling `./scripts/update-version.ps1 2.0.123 2.0.124`