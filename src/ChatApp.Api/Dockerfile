FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ChatApp.Api/ChatApp.Api.csproj", "src/ChatApp.Api/"]
COPY ["src/ChatApp.Application/ChatApp.Application.csproj", "src/ChatApp.Application/"]
COPY ["src/ChatApp.Domain/ChatApp.Domain.csproj", "src/ChatApp.Domain/"]
COPY ["src/ChatApp.Infrastructure/ChatApp.Infrastructure.csproj", "src/ChatApp.Infrastructure/"]
RUN dotnet restore "src/ChatApp.Api/ChatApp.Api.csproj"
COPY . .
WORKDIR "/src/src/ChatApp.Api"
RUN dotnet build "ChatApp.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ChatApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatApp.Api.dll"]
