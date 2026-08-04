FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Shop.Api/Shop.Api.csproj src/Shop.Api/
RUN dotnet restore src/Shop.Api/Shop.Api.csproj

COPY src/Shop.Api/ src/Shop.Api/
RUN dotnet publish src/Shop.Api/Shop.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Shop.Api.dll"]
