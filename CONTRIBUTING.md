<!---
 Licensed to the Apache Software Foundation (ASF) under one or more
 contributor license agreements.  See the NOTICE file distributed with
 this work for additional information regarding copyright ownership.
 The ASF licenses this file to You under the Apache License, Version 2.0
 (the "License"); you may not use this file except in compliance with
 the License.  You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->
# Contributing

You have found a bug or you have an idea for a cool new feature? Contributing code is a great way to give something back to the open source community. Before you dig right into the code there are a few guidelines that we need contributors to follow so that we can have a chance of keeping on top of things.

## Code of conduct

### Our pledge

In the interest of fostering an open and welcoming environment, we as contributors and maintainers pledge to making participation in our project and our community a harassment-free experience for everyone, regardless of age, body size, disability, ethnicity, gender identity and expression, level of experience, nationality, personal appearance, race, religion, or sexual identity and orientation.

### Our standards

Examples of behavior that contributes to creating a positive environment include:

* Using welcoming and inclusive language
* Being respectful of differing viewpoints and experiences
* Gracefully accepting constructive criticism
* Focusing on what is best for the community
* Showing empathy towards each other

Examples of unacceptable behavior by participants include:

* The use of sexualized language or imagery and unwelcome sexual attention or advances
* Trolling, insulting/derogatory comments, and personal or political attacks
* Public or private harassment
* Publishing others' private information, such as physical or electronic address, without explicit permission
* Other conduct which could reasonably be considered inappropriate in a professional setting

### Our responsibilities

Project maintainers are responsible for clarifying the standards of acceptable behavior and are expected to take appropriate and fair corrective action in response to any instances of unacceptable behavior.

Project maintainers have the right and responsibility to remove, edit, or reject comments, commits, code, wiki edits, issues, and other contributions that are not aligned to this Code of Conduct, or to ban temporarily or permanently any contributor for other behaviors that they deem inappropriate, threatening, offensive, or harmful.

### Scope

This Code of Conduct applies both within project spaces and in public spaces when an individual is representing the project or its community. Examples of representing a project or community include using an official project e-mail address, posting via an official social media account, or acting as an appointed representative at an online or offline event. Representation of a project may be further defined and clarified by project maintainers.

### Attribution

This Code of Conduct is adapted from the Contributor Covenant, version 1.4, available [here](http://contributor-covenant.org/version/1/4).

## Mailing lists

The major part of discussion happens on the mailing lists. The mailing lists are documented [here](https://logging.apache.org/log4net/mail-lists.html).

## Issues

Please use the official JIRA issue tracker. It can be found [here](https://issues.apache.org/jira/projects/LOG4NET/). Even though it is possible to track issues on GitHub we prefer to have them all there. To add, update or comment on issues you will need a [JIRA account](https://issues.apache.org/jira/secure/Signup!default.jspa).

Check if there exists already another issue that matches your situation. Only write a new issue if you cannot find any. Doing so the maintainers of the project spend less time in managing issues and therefore have more time to actually work on the issues.

Please backup your issues with as much information as you have. The more screenshots, logfiles or steps to reproduce a bug there are, the easier it is to track down the actual culprit.

Please try to be as clear as possible when submitting a new issue or updating an existing issue. Consider yourself to be a story teller and you are telling your story to someone who has never even heard of it. The easier it is to follow the story, the better it is to the reader of the story and the more likely it is that a reader of the story will invest time to work on it. The less time the reader of an issue spends with figuring out what an issue is about, the more time he has to fix the issue.

When an issue is about a regression, please try to find out in which release the regression was introduced. This again greatly helps the maintainers to hunt down the actual culprit.

Often it is wise to write to one of the mailing lists about a problem before you file an issue. You reach a much larger audience by sending an email to one of the mailing list and therefore it is more likely for you to receive helpful feedback faster. Further the maintainers of the project are subscribed to those mailing lists too and they can guide you on what to do next.

## Patches

There are many ways to contribute changes to the codebase. One is to file a pull request on GitHub, another is to attach a patch to a JIRA issue. While both work fine, pull requests provide ways to review modifications and are built and tested automatically. Therefore we prefer code contributions to go through GitHub pull requests. This can be done by following these steps:

* Make sure you have a [Github account](https://github.com/signup/free).
* If you're planning to implement a new feature, it makes sense to discuss the changes first on the [development mailing list](https://logging.apache.org/log4net/mail-lists.html). This way you can make sure you are not wasting your time on something that isn't considered to be in Apache Log4Net's scope.
* Eventually file a new ticket if there is and assign an the issue to you to make clear that you are working on it.
* Fork the repository on GitHub.
* If the modifications will take a considerable amount of time, it could make sense to create a feature branch that branches from `develop`. This allows you to separate your modifications from future modifications in the ´develop´ branch.
* Make your modifications in the form of commits. Please read the section about commit messages for further information about how commit messages should be written.
* Please check for unnecessary whitespace with git diff --check before committing.
* Make sure you write tests for your changes!
* Please note that not all targets can be built on a single machine and therefore only a subset of the actual targets can be built and tested easily on the machine of a developer. The good thing is, all branches and pull requests are built by the continuous deployment pipeline. Please continue reading [that section](#continuous-deployment-pipeline) for further information.

### Contributor license agreement

It is ok to contribute trivial patches without signing a contributor license agreement. Patches are considered to be trivial if they could be repeated trivially by anyone. Non trivial modifications however require you to sign a contributor license agreement. More information about this topic can be found [here](https://www.apache.org/licenses/#clas).

### Code style

Respect the original code style and whitespace rules. If you think that a file needs whitespace cleanup do so in separate commits.

* Run all the tests with `nant` inside the `tests` directory to assure nothing else was accidentally broken. Please note that not all targets can be built on a single machine and therefore only a subset of the actual targets is built and tested.

### Commit messages

Each commit should be an atomar modification and the commit message is the story that backs it up. The commit message explains the changes that the commit is about. This story greatly helps to understand what a modification is about, why it is there and can include further considerations and decisions that would not fit into the codebase as comments. Doing this further allows easier reviews of a modification. The easier reviews are, the faster they can be completed and the more time is spent with actually fixing stuff rather than only trying to understand a modification.

A commit messages first line represents the summary. The summary should start with the module where the modification is made and the issue(s) a modification is made should be appended to the summary. Any line in a commit message should not exceed about 80 characters. If you would like to provide more information to the commit, add an empty line after the summary and write more about the modification.

If you have a hard time to write a summary, your commit is probably too large and you should consider to split up your modifications into several commits, each doing more atomar modifications.

The following are good examples of great commit messages:

```
Readme: add documentation to new methods [LOG4NET-404]

The following methods are now well documented:
* method1
* method2
* method3
```

```
Submodule1: removed a few trailing whitespaces and fixed a typo in a comment
```

```
RollingFileAppender: fixed rolling to work on Sundays [LOG4NET-404]

This patch fixes a race condition that happened only on Sundays. In that case
the rolling did not work and an internal error log message "Error writing the
log message" was written.

We considered fixing the race condition by injecting a strategy class but
did not implement that because it was too much effort for this easy fix.

Note that the fix has a performance impact of 0.1ms each time a log message
is written.
```

### Pull requests

Pull requests are typically a set of commits. As with commit messages, a pull request should have a one liner summary and a longer description explaining what the pull request is about, what it improves and how it does so. If possible, back it up with background information and considerations that the author had thought about but decided to implement otherwise.

### Continuous deployment pipeline

We use Jenkins to build our codebase and the pipeline configuration is checked in into the repository codebase. The job can be found [here](https://builds.apache.org/job/logging-log4net).

#### Branches

All branches are built and tested automatically by our continuous deployment pipeline. Some branches however have a special meaning to the pipeline. All branches build and test assemblies and further provide the built artifacts as downloadable resources.

#### Branch: master

A commit on the master branch triggers the pipeline to publish the codebase as a release. This updates the site, publishes the assemblies, ..

__REMARK: this part of the pipeline is still work in progress.__

#### Branch: release/$version

A commit on a release branch triggers the pipeline to publish the codebase as a release candidate. This gives the possibility to review and do the last few steps to get a release done. Typical changes made in this branch are updates of the version tags.

__REMARK: this part of the pipeline is still work in progress.__

#### Pull requests

Pull requests are built and tested automatically by our continuous deployment pipeline. All pull request build jobs are listed [here](https://builds.apache.org/job/logging-log4net/view/change-requests/). When a build passes, the built assemblies can be downloaded from the Jenkins job. Use this feature to test your changes against our build server.

# Additional resources

* [Log4Net mailing lists](https://logging.apache.org/log4net/mail-lists.html)
* [Apache log4net JIRA project page](https://issues.apache.org/jira/projects/LOG4NET)
* [Contributor License Agreement](https://www.apache.org/licenses/#clas)
* [General GitHub documentation](https://help.github.com/)
* [GitHub pull request documentation](https://help.github.com/send-pull-requests/)
