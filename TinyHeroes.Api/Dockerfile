FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config .
COPY TinyHeroes.slnx .
COPY TinyHeroes.Domain/TinyHeroes.Domain.csproj TinyHeroes.Domain/
COPY TinyHeroes.Application/TinyHeroes.Application.csproj TinyHeroes.Application/
COPY TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj TinyHeroes.Infrastructure/
COPY TinyHeroes.Api/TinyHeroes.Api.csproj TinyHeroes.Api/
RUN dotnet restore TinyHeroes.Api/TinyHeroes.Api.csproj
COPY . .
RUN dotnet publish TinyHeroes.Api/TinyHeroes.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TinyHeroes.Api.dll"]
