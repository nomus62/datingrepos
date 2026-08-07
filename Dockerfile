# DatingApp.Server/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DatingApp.Server/DatingApp.Server.csproj", "DatingApp.Server/"]
RUN dotnet restore "DatingApp.Server/DatingApp.Server.csproj"
COPY . .
WORKDIR "/src/DatingApp.Server"
RUN dotnet build "DatingApp.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DatingApp.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DatingApp.Server.dll"]