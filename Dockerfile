FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "SypherBi.Api.csproj"
RUN dotnet publish "SypherBi.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/appsettings.json .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SypherBi.Api.dll"]
