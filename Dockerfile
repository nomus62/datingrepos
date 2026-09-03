FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV PORT=8080

FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DatingApp.Server.csproj", "."]
RUN dotnet restore "DatingApp.Server.csproj"
COPY . .
RUN dotnet build "DatingApp.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DatingApp.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY *.json ./
RUN mkdir -p /app/wwwroot/photos/original \
             /app/wwwroot/photos/medium \
             /app/wwwroot/photos/thumb
RUN chmod -R 755 /app/wwwroot/photos
ENTRYPOINT ["dotnet", "DatingApp.Server.dll"]