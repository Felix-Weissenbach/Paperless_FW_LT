# 📄 Paperless -- Document Management System

Paperless is a modular document-management system that allows users to
upload, store, OCR-process, and search documents. The system is fully
containerized using Docker and consists of multiple cooperating
services.

## 🚀 Features

### 🔧 Backend (REST API)

-   Built with ASP.NET Core
-   Provides endpoints for document management
-   Handles metadata, validation, and database interactions
-   Publishes OCR jobs via RabbitMQ

### 🖼️ Frontend

-   Web UI for uploading, browsing, and managing documents
-   Communicates with the REST API

### 🪵 Logging

-   Entire application uses log4net
-   Logs are stored on the host machine for persistence

### 📄 OCR Worker

-   Listens for incoming OCR jobs from RabbitMQ
-   Downloads the uploaded file from MinIO
-   Performs OCR
-   Stores OCR results back in MinIO
-   Notifies the REST service

### 🐇 RabbitMQ

-   Message broker for asynchronous OCR job processing

### 💾 PostgreSQL Database

-   Stores document metadata

### 🗄️ MinIO

-   S3-compatible object storage for uploaded files and OCR outputs

### 🛠️ Adminer

-   Simple web UI for inspecting the PostgreSQL database

## 🧩 System Architecture Overview

    Frontend  <-->  REST API  <--> PostgreSQL
                             \
                              \--> RabbitMQ --> OCR Worker --> MinIO

Each component runs in its own container for full isolation and
scalability.

## 🐳 Running the Project

### 1️⃣ Requirements

-   Docker
-   Docker Compose

### 2️⃣ Start the entire system

Run:

``` bash
docker-compose up --build
```

### 3️⃣ Accessing the Services

  Service                  URL
  ------------------------ ------------------------
  Frontend                 http://localhost
  REST API                 http://localhost:8081
  RabbitMQ Management UI   http://localhost:15672
  Adminer                  http://localhost:8082
  MinIO Console            http://localhost:9001

## 🧹 Stopping the System

``` bash
docker-compose down
```

Remove volumes:

``` bash
docker-compose down -v
```

## 📂 Project Structure

    /Paperless
     ├─ Backend (REST API)
     ├─ OCR Worker
     ├─ Frontend
     ├─ docker-compose.yml
     ├─ README.md

## 📝 Logs

Logs are stored in:

    ./logs/
