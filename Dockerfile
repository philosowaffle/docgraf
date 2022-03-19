# Create final image base
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y \
	bash
	&& rm -rf /var/lib/apt/lists/*

# Create build image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

ARG TARGETPLATFORM
ARG VERSION

ENV VERSION=${VERSION}

COPY . /build
WORKDIR /build

SHELL ["/bin/bash", "-c"]

###################
# BUILD CONSOLE APP
###################
RUN echo $TARGETPLATFORM \
	&& echo $VERSION \
	&& \
	if [[ "$TARGETPLATFORM" = "linux/arm64" ]] ; then \
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

EXPOSE 4000

ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["console"]