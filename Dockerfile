FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/SwiftPos.Domain/SwiftPos.Domain.csproj src/SwiftPos.Domain/
COPY src/SwiftPos.Application/SwiftPos.Application.csproj src/SwiftPos.Application/
COPY src/SwiftPos.Infrastructure/SwiftPos.Infrastructure.csproj src/SwiftPos.Infrastructure/
COPY src/SwiftPos.Api/SwiftPos.Api.csproj src/SwiftPos.Api/
RUN dotnet restore src/SwiftPos.Api/SwiftPos.Api.csproj

COPY . .
RUN dotnet publish src/SwiftPos.Api/SwiftPos.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SwiftPos.Api.dll"]
