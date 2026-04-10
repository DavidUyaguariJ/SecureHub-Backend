# Fase 1: Imagen de docker
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Fase 2: Compilar  Aplicacion
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar y restaurar dependencias
COPY ["SecureHub-Backend.csproj", "."]
RUN dotnet restore "./SecureHub-Backend.csproj"

# Copiar el resto del proyecto
COPY . .

# Construir el proyecto
RUN dotnet build "./SecureHub-Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Fase 3: Publicar la aplicación
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SecureHub-Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Fase 4: Crear la imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Development

# ENTRYPOINT
ENTRYPOINT ["dotnet", "SecureHub-Backend.dll"]