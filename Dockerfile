FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app/
COPY . /app/.

RUN dotnet restore
COPY . /app/.

RUN dotnet build -c Release
COPY . /app/.

RUN dotnet publish ./Geno/Geno.csproj -o ./Geno/bin/Publish/linux-x64/ -c Release -r linux-x64 --sc /p:PublishReadyToRun=true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeAllContent=true /p:ForSelfExtract=true /p:IncludeNativeLibrariesForSelfExtract=true
COPY . /app

ENTRYPOINT ["./Geno"]
