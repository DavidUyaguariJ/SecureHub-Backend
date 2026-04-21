FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/Api/Api.csproj",                         "src/Api/"]
COPY ["src/Application/Application.csproj",         "src/Application/"]
COPY ["src/Domain/Domain.csproj",                   "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj",   "src/Infrastructure/"]

# Restore desde el proyecto de entrada
RUN dotnet restore "src/Api/Api.csproj"

# Copiar todo el código fuente
COPY . .

# Build desde el proyecto Api
WORKDIR "/src/src/Api"
RUN dotnet build "Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ARG ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}

ENTRYPOINT ["dotnet", "Api.dll"]