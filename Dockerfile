FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
COPY ["Geno/bin/Localizations", "root/.net/Localizations"]
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Geno/Geno.csproj", "Geno/"]
COPY ["Database/Database.csproj", "Database/"]
COPY ["Localization/Localization.csproj", "Localization/"]
COPY ["ShikimoriService/ShikimoriService.csproj", "ShikimoriService/"]
COPY ["WaifuPicsApi/WaifuPicsApi.csproj", "WaifuPicsApi/"]
RUN dotnet restore "Geno/Geno.csproj"
COPY . .
WORKDIR "/src/Geno"
RUN dotnet build "Geno.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Geno.csproj" --os linux --arch x64 --sc -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./Geno"]
