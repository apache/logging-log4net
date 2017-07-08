# Name of container: docker-ubuntu-latest-nant
FROM microsoft/dotnet

# set the maintainer of this docker image
MAINTAINER "dev@logging.apache.org"

# install dependencies
RUN apt-get update && apt-get install -y nant git

# install gosu
# entrypoint.sh requires this tool
#RUN apt-get update && apt-get -y --no-install-recommends install \
#    ca-certificates \
#    curl
#RUN curl -o /usr/local/bin/gosu -SL "https://github.com/tianon/gosu/releases/download/1.4/gosu-$(dpkg --print-architecture)" \
#    && curl -o /usr/local/bin/gosu.asc -SL "https://github.com/tianon/gosu/releases/download/1.4/gosu-$(dpkg --print-architecture).asc" \
#    && gpg --verify /usr/local/bin/gosu.asc \
#    && rm /usr/local/bin/gosu.asc \
#    && chmod +x /usr/local/bin/gosu
#RUN curl -o /usr/local/bin/gosu -SL "https://github.com/tianon/gosu/releases/download/1.4/gosu-$(dpkg --print-architecture)" \
#    && chmod +x /usr/local/bin/gosu

# set up an entry point for the image that allows customization
# before anything else is run on the container
RUN bash -c entrypoint.sh
#ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
