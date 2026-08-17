FROM node:22-alpine AS frontend-build
WORKDIR /frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ .
# Bundled into the same origin as the API (see runtime stage below), so the frontend's
# default dev-time API_BASE_URL ('http://localhost:5022') must be overridden to relative.
ENV VITE_API_BASE_URL=""
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/src/Shop.Api/Shop.Api.csproj backend/src/Shop.Api/
RUN dotnet restore backend/src/Shop.Api/Shop.Api.csproj

COPY backend/src/Shop.Api/ backend/src/Shop.Api/
RUN dotnet publish backend/src/Shop.Api/Shop.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
COPY --from=frontend-build /frontend/dist ./wwwroot
USER $APP_UID
ENTRYPOINT ["dotnet", "Shop.Api.dll"]
