FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

ARG project_folder=./src/DnsTlsProxy

# Copy csproj and restore as distinct layers
COPY $project_folder/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY $project_folder ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
WORKDIR /app
COPY --from=build-env /app/out .
COPY --from=build-env /app/appsettings.json .
ENTRYPOINT ["dotnet", "DnsTlsProxy.dll"]
