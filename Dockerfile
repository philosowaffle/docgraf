FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

COPY . /build
WORKDIR /build

SHELL ["/bin/bash", "-c"]

ARG TARGETPLATFORM
ARG VERSION

RUN echo $TARGETPLATFORM
RUN echo $VERSION
ENV VERSION=${VERSION}

###################
# BUILD CONSOLE APP
###################
RUN if [[ "$TARGETPLATFORM" = "linux/arm64" ]] ; then \
		dotnet publish /build/src/Console/Console.csproj -c Release -r linux-arm64 -o /build/published ; \
	else \
		dotnet publish /build/src/Console/Console.csproj -c Release -r linux-x64 -o /build/published ; \
	fi

###################
# FINAL
###################
FROM mcr.microsoft.com/dotnet/runtime:6.0

RUN apt-get update
RUN apt-get -y install bash

WORKDIR /app

COPY --from=build /build/published .
COPY --from=build /build/LICENSE ./LICENSE
COPY --from=build /build/configuration.example.json ./configuration.local.json
COPY ./entrypoint.sh .
RUN chmod 777 entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["console"]