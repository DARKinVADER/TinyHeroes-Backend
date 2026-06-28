FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY nuget.config .
COPY TinyHeroes.slnx .
COPY TinyHeroes.Domain/TinyHeroes.Domain.csproj TinyHeroes.Domain/
COPY TinyHeroes.Application/TinyHeroes.Application.csproj TinyHeroes.Application/
COPY TinyHeroes.Infrastructure/TinyHeroes.Infrastructure.csproj TinyHeroes.Infrastructure/
COPY TinyHeroes.Api/TinyHeroes.Api.csproj TinyHeroes.Api/
RUN dotnet restore TinyHeroes.Api/TinyHeroes.Api.csproj -r linux-musl-x64
COPY . .
RUN dotnet publish TinyHeroes.Api/TinyHeroes.Api.csproj -c Release -r linux-musl-x64 --self-contained false -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
RUN apk add --no-cache krb5-libs
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TinyHeroes.Api.dll"]
