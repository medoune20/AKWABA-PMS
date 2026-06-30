# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Akwaba.Web/Akwaba.Web.csproj -c Release -o /app

# Runtime (écoute par défaut sur 8080, convention LP2M)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/Data /app/keys
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Akwaba.Web.dll"]
