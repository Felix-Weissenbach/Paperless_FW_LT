@echo off

REM --------------------------------------------------
REM Paperless Integration Test Script
REM --------------------------------------------------
title Paperless Integration Test
echo CURL Testing for Paperless
echo Syntax: $1 [pause]
echo - pause: optional, if set, the script will pause after each block
echo.

set BASE_URL=http://localhost:8081
set TEST_DIR=%CD%\test-data
set TEST_FILE=%TEST_DIR%\test.pdf

set "pauseFlag=0"
for %%a in (%*) do (
    if /I "%%a"=="pause" (
        set "pauseFlag=1"
    )
)



if %pauseFlag%==1 pause
REM -------------------------------------------------
REM 1) Prepare test data
REM -------------------------------------------------
echo 1) Preparing test PDF
if not exist %TEST_DIR% mkdir %TEST_DIR%

echo This is a test document for Paperless integration testing. > %TEST_FILE%

echo Created test file: %TEST_FILE%
echo.

if %pauseFlag%==1 pause
REM -------------------------------------------------
REM 2) Upload document
REM -------------------------------------------------
echo 2) Upload document
curl -i -X POST %BASE_URL%/document ^
  -H "Accept: application/json" ^
  -F "file=@%TEST_FILE%;type=application/pdf"

echo.
echo Should return HTTP 201 or 200
echo.

if %pauseFlag%==1 pause
REM -------------------------------------------------
REM 3) List documents
REM -------------------------------------------------
echo 3) List documents
curl -i -X GET %BASE_URL%/document ^
  -H "Accept: application/json"

echo.
echo Should return uploaded document
echo.

if %pauseFlag%==1 pause
REM -------------------------------------------------
REM 4) (Optional) Search documents
REM -------------------------------------------------
echo 4) Search documents (Elasticsearch)
curl -i -X GET "%BASE_URL%/document/search?query=test" ^
  -H "Accept: application/json"

echo.
echo Should return document containing OCR text
echo.

if %pauseFlag%==1 pause
REM -------------------------------------------------
REM Done
REM -------------------------------------------------
echo Integration test finished.
pause
