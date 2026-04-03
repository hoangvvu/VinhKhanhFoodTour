# API Documentation

## Overview

The Admin Web API provides endpoints for managing the VinhKhanhFoodTour admin panel. All endpoints require JWT authentication except for the login endpoint.

## Authentication

### Login
```http
POST /api/auth/login
Content-Type: application/json

{
    "email": "admin@example.com",
    "password": "password123"
}
```

**Response:**
```json
{
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "message": "Đăng nhập thành công"
}
```

### Using the Token

Add the token to the Authorization header:
```http
Authorization: Bearer <token>
```

## Endpoints

### Authentication Endpoints

#### Register Admin (Admin Only)
```http
POST /api/auth/register-admin
Authorization: Bearer <token>
Content-Type: application/json

{
    "name": "New Admin",
    "email": "newadmin@example.com",
    "password": "SecurePassword123!"
}
```

#### Register Shop Manager (Admin Only)
```http
POST /api/auth/register-shop-manager
Authorization: Bearer <token>
Content-Type: application/json

{
    "name": "Manager Name",
    "email": "manager@example.com",
    "password": "SecurePassword123!",
    "shopId": 1
}
```

#### Get Profile
```http
GET /api/auth/profile
Authorization: Bearer <token>
```

**Response:**
```json
{
    "userId": 1,
    "name": "Admin Name",
    "email": "admin@example.com",
    "role": "Admin",
    "isActive": true,
    "lastLogin": "2024-01-03T10:00:00Z",
    "managedShop": null
}
```

### Shops Endpoints

#### Get All Shops (Admin Only)
```http
GET /api/shops
Authorization: Bearer <token>
```

**Response:**
```json
[
    {
        "shopId": 1,
        "name": "Shop Name",
        "address": "123 Street",
        "phone": "0912345678",
        "isVerified": true,
        "isActive": true,
        "averageRating": 4.5,
        "totalOrders": 150,
        "managerCount": 2,
        "createdAt": "2024-01-01T00:00:00Z"
    }
]
```

#### Get Shop Details
```http
GET /api/shops/{shopId}
Authorization: Bearer <token>
```

#### Create Shop (Admin Only)
```http
POST /api/shops
Authorization: Bearer <token>
Content-Type: application/json

{
    "name": "New Shop",
    "address": "123 Main St",
    "phone": "0912345678",
    "description": "Shop description",
    "latitude": 10.7769,
    "longitude": 106.6869,
    "radius": 500
}
```

#### Update Shop (Admin/ShopManager)
```http
PUT /api/shops/{shopId}
Authorization: Bearer <token>
Content-Type: application/json

{
    "name": "Updated Name",
    "address": "456 New St",
    "phone": "0987654321",
    "description": "Updated description",
    "latitude": 10.7770,
    "longitude": 106.6870,
    "radius": 600
}
```

#### Verify Shop (Admin Only)
```http
PUT /api/shops/{shopId}/verify
Authorization: Bearer <token>
```

#### Deactivate Shop (Admin Only)
```http
DELETE /api/shops/{shopId}
Authorization: Bearer <token>
```

### Users Endpoints

#### Get All Users (Admin Only)
```http
GET /api/users
Authorization: Bearer <token>
```

#### Get User Details (Admin Only)
```http
GET /api/users/{userId}
Authorization: Bearer <token>
```

#### Deactivate User (Admin Only)
```http
PUT /api/users/{userId}/deactivate
Authorization: Bearer <token>
```

#### Activate User (Admin Only)
```http
PUT /api/users/{userId}/activate
Authorization: Bearer <token>
```

#### Delete User (Admin Only)
```http
DELETE /api/users/{userId}
Authorization: Bearer <token>
```

### Statistics Endpoints

#### Get Dashboard Statistics (Admin Only)
```http
GET /api/statistics/dashboard
Authorization: Bearer <token>
```

**Response:**
```json
{
    "summary": {
        "totalShops": 10,
        "totalManagers": 15,
        "verifiedShops": 8,
        "activeShops": 10
    },
    "topShops": [
        {
            "shopId": 1,
            "name": "Top Shop",
            "averageRating": 4.8,
            "totalOrders": 500
        }
    ],
    "recentShops": [
        {
            "shopId": 2,
            "name": "Recent Shop",
            "createdAt": "2024-01-03T00:00:00Z",
            "isVerified": false
        }
    ]
}
```

#### Get Shop Statistics
```http
GET /api/statistics/shop/{shopId}
Authorization: Bearer <token>
```

#### Record Shop Statistics (Admin Only)
```http
POST /api/statistics/shop/{shopId}/record
Authorization: Bearer <token>
Content-Type: application/json

{
    "totalVisits": 1000,
    "totalOrders": 50,
    "totalRevenue": 5000000,
    "averageRating": 4.5,
    "reviewCount": 25
}
```

#### Get Audit Logs (Admin Only)
```http
GET /api/statistics/audit-logs?pageNumber=1&pageSize=50
Authorization: Bearer <token>
```

## Error Responses

### Unauthorized (401)
```json
{
    "message": "Unauthorized"
}
```

### Forbidden (403)
```json
{
    "message": "Access denied"
}
```

### Not Found (404)
```json
{
    "message": "Resource not found"
}
```

### Bad Request (400)
```json
{
    "message": "Invalid input: field is required"
}
```

### Server Error (500)
```json
{
    "message": "An error occurred while processing your request"
}
```

## Rate Limiting

Currently, there is no rate limiting implemented. This should be added in production.

## CORS

The API accepts requests from any origin. For production, configure CORS to accept only trusted origins.

## Versioning

Current API version: v1 (not versioned in URLs)

## Testing

### Using cURL
```bash
# Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"password123"}'

# Get Shops (with token)
curl -X GET https://localhost:5001/api/shops \
  -H "Authorization: Bearer <token>"
```

### Using Swagger

Swagger UI is available at `/swagger` in development mode.

## Changelog

### Version 1.0.0
- Initial API release
- Authentication endpoints
- Shop management
- User management
- Statistics endpoints
