# Create final image base
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine AS final
WORKDIR /app

#RUN apt-get update
RUN apk update
RUN apk add bash

# Create build image
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

ARG TARGETPLATFORM
ARG VERSION

RUN echo $TARGETPLATFORM
RUN echo $VERSION

ENV VERSION=${VERSION}

COPY . /build
WORKDIR /build
RUN ls -la

SHELL ["/bin/bash", "-c"]

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
FROM final

COPY --from=build /build/published .
COPY --from=build /build/LICENSE ./LICENSE
COPY --from=build /build/configuration.example.json ./configuration.local.json
COPY ./entrypoint.sh .
RUN chmod 777 entrypoint.sh

ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["console"]