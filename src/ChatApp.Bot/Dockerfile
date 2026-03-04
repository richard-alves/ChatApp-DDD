FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ChatApp.Bot/ChatApp.Bot.csproj", "src/ChatApp.Bot/"]
COPY ["src/ChatApp.Application/ChatApp.Application.csproj", "src/ChatApp.Application/"]
COPY ["src/ChatApp.Domain/ChatApp.Domain.csproj", "src/ChatApp.Domain/"]
COPY ["src/ChatApp.Infrastructure/ChatApp.Infrastructure.csproj", "src/ChatApp.Infrastructure/"]
RUN dotnet restore "src/ChatApp.Bot/ChatApp.Bot.csproj"
COPY . .
WORKDIR "/src/src/ChatApp.Bot"
RUN dotnet build "ChatApp.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatApp.Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatApp.Bot.dll"]
