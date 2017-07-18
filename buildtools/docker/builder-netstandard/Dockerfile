# Name of container: docker-ubuntu-latest-nant
FROM microsoft/dotnet

# define arguments passed in as environment variables
ARG JENKINS_UID
ARG JENKINS_GID

# set the maintainer of this docker image
MAINTAINER "dev@logging.apache.org"

RUN echo $JENKINS_UID
RUN echo $JENKINS_GID

# add custom workspace that can be written by anyone
RUN mkdir -p /var/workspaces && chmod 0777 /var/workspaces

# add jenkins user that maps to the custom workspace
RUN groupadd -r -g $JENKINS_GID jenkins-slave
RUN useradd --base-dir /var/workspaces --create-home --shell /bin/bash --uid $JENKINS_UID --gid $JENKINS_GID --groups $JENKINS_GID -p -M jenkins

# install dependencies
RUN apt-get update && apt-get install -y nant git

