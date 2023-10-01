#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-preview AS base
WORKDIR /app
EXPOSE 5080
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        clang \
        gcc-aarch64-linux-gnu \
        llvm \
        zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["src/GrowattShine2Mqtt/GrowattShine2Mqtt.csproj", "src/GrowattShine2Mqtt/"]
RUN dotnet restore -r linux-musl-x64 "src/GrowattShine2Mqtt/GrowattShine2Mqtt.csproj"
COPY . .
WORKDIR "/src/src/GrowattShine2Mqtt"
RUN dotnet publish "GrowattShine2Mqtt.csproj" -r linux-musl-x64 --no-restore -c Release -o /app
RUN rm /app/*.dbg /app/*.Development.json

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-alpine-aot AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./GrowattShine2Mqtt"]