FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore --no-cache

COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /tmp && chmod 777 /tmp

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "GenAIWorker.dll"]
