# Build image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore && dotnet publish ./Geno/Geno.csproj -o ./Geno/bin/Publish/linux-x64/ -c Release -r linux-x64 --sc /p:PublishReadyToRun=true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeAllContent=true /p:ForSelfExtract=true /p:IncludeNativeLibrariesForSelfExtract=true

# Runtime image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/Geno/bin/Publish/linux-x64 /app
ENTRYPOINT ["./Geno"]
