FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Копируем только csproj (он в корне)
COPY ["DatingApp.Server.csproj", "."]
RUN dotnet restore "DatingApp.Server.csproj"
# Копируем всё остальное
COPY . .
RUN dotnet build "DatingApp.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DatingApp.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DatingApp.Server.dll"]