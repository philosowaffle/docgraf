###################
# CREATE FINAL LAYER
###################
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y \
		bash \
		tzdata \
	&& apt-get purge -y -f --force-yes $EXT_BUILD_DEPS \
	&& apt-get autoremove -y \
	&& apt-get clean \
	&& rm -rf /var/lib/apt/lists/*

RUN groupadd -g 1019 docgraf && useradd -g docgraf -u 1019 docgraf

WORKDIR /app
RUN mkdir -m770 {output,data,working}

###################
# BUILD LAYER
###################
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

COPY . /build
WORKDIR /build

SHELL ["/bin/bash", "-c"]

ARG TARGETPLATFORM
ARG VERSION

RUN echo $TARGETPLATFORM \
	&& echo $VERSION
ENV VERSION=${VERSION}

###################
# BUILD CONSOLE APP
###################
RUN if [[ "$TARGETPLATFORM" = "linux/arm64" ]] ; then \
	dotnet publish /build/src/Console/Console.csproj -c Release -r linux-arm64 -o /build/published --version-suffix $VERSION --self-contained ; \
else \
	dotnet publish /build/src/Console/Console.csproj -c Release -r linux-x64 -o /build/published --version-suffix $VERSION --self-contained ; \
fi


###################
# FINAL
###################
FROM final

COPY --from=build /build/published .
COPY --from=build /build/LICENSE ./LICENSE
COPY --from=build /build/configuration.example.json ./configuration.local.json

COPY --chmod=770 ./docker/entrypoint.sh .

EXPOSE 4000

ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["console"]