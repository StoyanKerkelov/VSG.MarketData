
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VSG.MarketData.slnx ./
COPY VSG.MarketData.Api/*.csproj VSG.MarketData.Api/
COPY VSG.MarketData.Console/*.csproj VSG.MarketData.Console/
COPY VSG.MarketData.Core/*.csproj VSG.MarketData.Core/
COPY VSG.MarketData.Infrastructure/*.csproj VSG.MarketData.Infrastructure/
COPY VSG.MarketData.Tests/*.csproj VSG.MarketData.Tests/

RUN dotnet restore VSG.MarketData.slnx

COPY . .

RUN dotnet publish VSG.MarketData.Api/VSG.MarketData.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VSG.MarketData.Api.dll"]
