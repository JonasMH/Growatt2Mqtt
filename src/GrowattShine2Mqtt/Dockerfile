#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-preview AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0-preview AS build
WORKDIR /src
COPY ["src/GrowattShine2Mqtt/GrowattShine2Mqtt.csproj", "src/GrowattShine2Mqtt/"]
RUN dotnet restore "src/GrowattShine2Mqtt/GrowattShine2Mqtt.csproj"
COPY . .
WORKDIR "/src/src/GrowattShine2Mqtt"
RUN dotnet build "GrowattShine2Mqtt.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GrowattShine2Mqtt.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GrowattShine2Mqtt.dll"]