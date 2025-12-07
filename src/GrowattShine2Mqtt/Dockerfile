FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
RUN apk add --upgrade --no-cache build-base clang zlib-dev
WORKDIR /src
COPY . .
WORKDIR "/src/src"
RUN dotnet publish "GrowattShine2Mqtt/GrowattShine2Mqtt.csproj" -r linux-musl-x64 -c Release -o /out
RUN rm -f /out/*.dbg /out/*.Development.json && chmod +x /out/GrowattShine2Mqtt

FROM alpine AS final
RUN apk add --no-cache tzdata
WORKDIR /app
COPY --from=build /out/* .
ENTRYPOINT ["/app/GrowattShine2Mqtt"]