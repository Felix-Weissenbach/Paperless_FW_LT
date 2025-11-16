FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

RUN mkdir -p /tmp && chmod 777 /tmp

COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Install Tesseract + Ghostscript
RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    tesseract-ocr-deu \
    ghostscript \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

ENTRYPOINT ["dotnet", "OCRWorker.dll"]
