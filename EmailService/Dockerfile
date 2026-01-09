# ======== BUILD STAGE ========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем все .csproj и восстанавливаем зависимости
COPY ["EmailService/EmailService.csproj", "EmailService/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "EmailService/EmailService.csproj"

# Копируем остальной код
COPY . .
WORKDIR "/src/EmailService"

RUN dotnet publish "EmailService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ======== RUNTIME STAGE ========
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EmailService.dll"]
