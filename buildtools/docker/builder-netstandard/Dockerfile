# Name of container: docker-ubuntu-latest-nant
FROM microsoft/dotnet

# set the maintainer of this docker image
MAINTAINER "dev@logging.apache.org"

# install dependencies
RUN apt-get update && apt-get install -y nant git

# add custom workspace that can be written by anyone
RUN mkdir -p /var/workspaces && chmod 0777 /var/workspaces

# add jenkins user that maps to the custom workspace
RUN groupadd -r -g 12040 jenkins-slave
RUN useradd --base-dir /var/workspaces --create-home --shell /bin/bash --uid 10025 --gid 12040 --groups 12040 -p -M jenkins

