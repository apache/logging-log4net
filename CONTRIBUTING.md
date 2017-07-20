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

# Getting started

* Make sure you have a [JIRA account](https://issues.apache.org/jira/).
* Make sure you have a [GitHub account](https://github.com/signup/free).
* If you're planning to implement a new feature it makes sense to discuss the changes on the [development mailing list](https://logging.apache.org/log4net/mail-lists.html) first. This way you can make sure you are not wasting your time on something that isn't considered to be in Apache log4net's scope.
* Submit a ticket for your issue, assuming one does not already exist.
  * Clearly describe the issue including steps to reproduce when it is a bug.
  * Make sure you fill in the earliest version that you know has the issue.
* Fork the repository on GitHub.

# Contributing changes

* Create a topic branch from where you want to base your work (this is usually the develop branch).
* Make commits of logical units.
* Respect the original code style:
  * Create minimal diffs - disable on save actions like reformat source code or organize imports. If you feel the source code should be reformatted create a separate PR for this change.
  * Check for unnecessary whitespace with git diff --check before committing.
* Make sure your commit messages are in the proper format. Your commit message should contain the key of the JIRA issue.
* Make sure you have added the necessary tests for your changes.
* Run all the tests with `nant` inside the `tests` directory to assure nothing else was accidentally broken. Please note that not all targets can be built on a single machine and therefore only a subset of the actual targets is built and tested.

## Trivial changes

For changes of a trivial nature to comments and documentation, it is not always necessary to create a new ticket in JIRA. In this case, it is appropriate to start the first line of a commit with '(doc)' instead of a ticket number.

## Submitting changes

* For non trivial changes, please sign the [Contributor License Agreement](https://www.apache.org/licences/#clas) if you haven't already.
* Push your changes to a topic branch in your fork of the repository.
* Submit a pull request to the repository in the apache organization.
* Update your JIRA ticket and include a link to the pull request in the ticket.
* A Jenkins [job](https://builds.apache.org/job/logging-log4net) should automatically attempt to merge, build and test your pull request and you can check the outcome [here](https://builds.apache.org/job/logging-log4net/view/change-requests/).

# Additional resources

* [Log4Net mailing lists](https://logging.apache.org/log4net/mail-lists.html)
* [Apache log4net JIRA project page](https://issues.apache.org/jira/browse/LOG4NET)
* [Contributor License Agreement](https://www.apache.org/licenses/#clas)
* [General GitHub documentation](https://help.github.com/)
* [GitHub pull request documentation](https://help.github.com/send-pull-requests/)

