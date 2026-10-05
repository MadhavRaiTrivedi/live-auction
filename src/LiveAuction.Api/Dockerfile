FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/LiveAuction.Domain/LiveAuction.Domain.csproj src/LiveAuction.Domain/
COPY src/LiveAuction.Application/LiveAuction.Application.csproj src/LiveAuction.Application/
COPY src/LiveAuction.Infrastructure/LiveAuction.Infrastructure.csproj src/LiveAuction.Infrastructure/
COPY src/LiveAuction.Api/LiveAuction.Api.csproj src/LiveAuction.Api/
RUN dotnet restore src/LiveAuction.Api/LiveAuction.Api.csproj

COPY src/ src/
RUN dotnet publish src/LiveAuction.Api/LiveAuction.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "LiveAuction.Api.dll"]
