FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# (Опционально) Устанавливаем Google DNS для надёжного разрешения имён
RUN echo "nameserver 8.8.8.8" > /etc/resolv.conf

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем только .csproj для восстановления зависимостей
COPY ["DatingApp.Server.csproj", "."]
RUN dotnet restore "DatingApp.Server.csproj"

# Копируем всё остальное
COPY . .

# Сборка и публикация
RUN dotnet build "DatingApp.Server.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "DatingApp.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Запуск приложения
ENTRYPOINT ["dotnet", "DatingApp.Server.dll"]