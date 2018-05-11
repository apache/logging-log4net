# Name of container
FROM ubuntu:16.04

# set the maintainer of this docker image
MAINTAINER "dev@logging.apache.org"

# set the timezone
RUN ln -fs /usr/share/zoneinfo/Universal /etc/localtime
RUN echo "Universal" > /etc/timezone

# install dependencies
RUN apt-get update && apt-get install -y nant git mono-complete mono-reference-assemblies-2.0

