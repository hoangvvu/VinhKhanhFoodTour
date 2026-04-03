#!/bin/bash
# Quick Start Script for VinhKhanhFoodTour Admin Web

echo "================================"
echo "VinhKhanhFoodTour Admin Web Setup"
echo "================================"
echo ""

# Navigate to project directory
cd VinhKhanhFoodTour.AdminWeb

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore

# Create database
echo "🗄️  Creating database and running migrations..."
dotnet ef database update

# Run the application
echo ""
echo "🚀 Starting application..."
echo "📍 Swagger UI will be available at: https://localhost:5001/swagger"
echo "📍 API will be available at: https://localhost:5001"
echo ""

dotnet run

echo ""
echo "================================"
echo "✅ Setup Complete!"
echo "================================"
