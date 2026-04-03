@echo off
REM Quick Start Script for VinhKhanhFoodTour Admin Web

echo ================================
echo VinhKhanhFoodTour Admin Web Setup
echo ================================
echo.

REM Navigate to project directory
cd VinhKhanhFoodTour.AdminWeb

REM Restore packages
echo [94m[1m^> Restoring NuGet packages...[0m
dotnet restore

REM Create database
echo [94m[1m^> Creating database and running migrations...[0m
dotnet ef database update

REM Run the application
echo.
echo [92m[1m^> Starting application...[0m
echo [94m^> Swagger UI available at: https://localhost:5001/swagger[0m
echo [94m^> API available at: https://localhost:5001[0m
echo.

dotnet run

echo.
echo ================================
echo [92m^> Setup Complete![0m
echo ================================
pause
