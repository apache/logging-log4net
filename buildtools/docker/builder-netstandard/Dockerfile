# Name of container: docker-ubuntu-latest-nant
FROM microsoft/dotnet

# set the maintainer of this docker image
MAINTAINER "dev@logging.apache.org"

# install dependencies
RUN apt-get update && apt-get install -y nant git sudo

# add custom workspace that can be written by anyone
RUN mkdir -p /var/workspaces && chmod 0777 /var/workspaces

# allow every user to run groupadd and useradd
RUN echo "ALL ALL=NOPASSWD: /usr/sbin/groupadd" >> /etc/sudoers
RUN echo "ALL ALL=NOPASSWD: /usr/sbin/useradd" >> /etc/sudoers

