# Name of container: docker-ubuntu-latest-nant
FROM microsoft/dotnet

# define arguments passed in as environment variables
ARG JENKINS_UID
ARG JENKINS_GID

# set the maintainer of this docker image
MAINTAINER "dev@logging.apache.org"

# add custom workspace that can be written by anyone
RUN mkdir -p /var/workspaces && chmod 0777 /var/workspaces

# add jenkins user that maps to the custom workspace
RUN groupadd -r -g $JENKINS_GID jenkins-slave
RUN useradd --base-dir /var/workspaces --create-home --shell /bin/bash --uid $JENKINS_UID --gid $JENKINS_GID --groups $JENKINS_GID -p -M jenkins

# HACK: add apt source to make nant available;
# the docker image is now based on debian stretch which no longer includes the nant package
RUN echo "deb http://ftp.de.debian.org/debian sid main" >> /etc/apt/sources.list

# install dependencies
RUN apt-get update && apt-get install -y nant git

