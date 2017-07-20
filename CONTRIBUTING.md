# Contributing to Apache log4net

You have found a bug or you have an idea for a cool new feature? Contributing code is a great way to give something back to the open source community. Before you dig right into the code there are a few guidelines that we need contributors to follow so that we can have a chance of keeping on top of things.

# Getting started

* Make sure you have a [JIRA account](https://issues.apache.org/jira/).
* Make sure you have a [GitHub account](https://github.com/signup/free).
* If you're planning to implement a new feature it makes sense to discuss the changes on the [development mailing list](https://logging.apache.org/log4net/mail-lists.html) first. This way you can make sure you are not wasting your time on something that isn't considered to be in Apache log4net's scope.
* Submit a ticket for your issue, assuming one does not already exist.
** Clearly describe the issue including steps to reproduce when it is a bug.
** Make sure you fill in the earliest version that you know has the issue.
* Fork the repository on GitHub.

# Contributing changes

* Create a topic branch from where you want to base your work (this is usually the develop branch).
* Make commits of logical units.
* Respect the original code style:
** Create minimal diffs - disable on save actions like reformat source code or organize imports. If you feel the source code should be reformatted create a separate PR for this change.
** Check for unnecessary whitespace with git diff --check before committing.
* Make sure your commit messages are in the proper format. Your commit message should contain the key of the JIRA issue.
* Make sure you have added the necessary tests for your changes.
* Run all the tests with `nant` inside the `tests` directory to assure nothing else was accidentally broken. Please note that not all targets can be built on a single machine and therefore only a subset of the actual targets is built and tested.

## Trivial changes

For changes of a trivial nature to comments and documentation, it is not always necessary to create a new ticket in JIRA. In this case, it is appropriate to start the first line of a commit with '(doc)' instead of a ticket number.

## Submitting changes

* For non trivial changes, please sign the [Contributor License Agreement][https://www.apache.org/licences/#clas] if you haven't already.
* Push your changes to a topic branch in your fork of the repository.
* Submit a pull request to the repository in the apache organization.
* Update your JIRA ticket and include a link to the pull request in the ticket.
* The build system should automatically attempt to merge, build and test your pull request. Please navigate to [jenkins](https://builds.apache.org/job/logging-log4net) to check the outcome.

# Additional resources

* [Log4Net mailing lists](https://logging.apache.org/log4net/mail-lists.html)
* [Apache log4net JIRA project page](https://issues.apache.org/jira/browse/LOG4NET)
* [Contributor License Agreement][https://www.apache.org/licenses/#clas]
* [General GitHub documentation](https://help.github.com/)
* [GitHub pull request documentation](https://help.github.com/send-pull-requests/)

