# API Testing Examples for Admin Web

## Quick API Testing Guide

### 1. Login and Get Token

**Request:**
```http
POST /api/auth/login HTTP/1.1
Host: localhost:5001
Content-Type: application/json

{
    "email": "admin@example.com",
    "password": "password123"
}
```

**Response:**
```json
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "message": "Đăng nhập thành công"
}
```

**cURL:**
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"password123"}'
```

---

### 2. Create Admin Account (Requires Admin Token)

**Request:**
```http
POST /api/auth/register-admin HTTP/1.1
Host: localhost:5001
Content-Type: application/json
Authorization: Bearer YOUR_ADMIN_TOKEN

{
    "name": "John Doe",
    "email": "john@example.com",
    "password": "SecurePassword123!"
}
```

**cURL:**
```bash
curl -X POST https://localhost:5001/api/auth/register-admin \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "name": "John Doe",
    "email": "john@example.com",
    "password": "SecurePassword123!"
  }'
```

---

### 3. Create Shop (Admin Only)

**Request:**
```http
POST /api/shops HTTP/1.1
Host: localhost:5001
Content-Type: application/json
Authorization: Bearer YOUR_ADMIN_TOKEN

{
    "name": "Phở Hà Nội",
    "address": "123 Nguyễn Huệ, Quận 1, TP.HCM",
    "phone": "0912345678",
    "description": "Famous Pho restaurant in Ho Chi Minh City",
    "latitude": 10.7769,
    "longitude": 106.6869,
    "radius": 500
}
```

**cURL:**
```bash
curl -X POST https://localhost:5001/api/shops \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "name": "Phở Hà Nội",
    "address": "123 Nguyễn Huệ, Quận 1, TP.HCM",
    "phone": "0912345678",
    "description": "Famous Pho restaurant in Ho Chi Minh City",
    "latitude": 10.7769,
    "longitude": 106.6869,
    "radius": 500
  }'
```

---

### 4. Get All Shops (Admin Only)

**Request:**
```http
GET /api/shops HTTP/1.1
Host: localhost:5001
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**cURL:**
```bash
curl -X GET https://localhost:5001/api/shops \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

---

### 5. Get Shop Details

**Request:**
```http
GET /api/shops/1 HTTP/1.1
Host: localhost:5001
Authorization: Bearer YOUR_TOKEN
```

**cURL:**
```bash
curl -X GET https://localhost:5001/api/shops/1 \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

### 6. Update Shop

**Request:**
```http
PUT /api/shops/1 HTTP/1.1
Host: localhost:5001
Content-Type: application/json
Authorization: Bearer YOUR_TOKEN

{
    "name": "Phở Hà Nội - Updated",
    "address": "456 Nguyễn Huệ, Quận 1, TP.HCM",
    "phone": "0987654321",
    "description": "Updated description",
    "latitude": 10.7770,
    "longitude": 106.6870,
    "radius": 600
}
```

**cURL:**
```bash
curl -X PUT https://localhost:5001/api/shops/1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "name": "Phở Hà Nội - Updated",
    "address": "456 Nguyễn Huệ, Quận 1, TP.HCM",
    "phone": "0987654321"
  }'
```

---

### 7. Verify Shop (Admin Only)

**Request:**
```http
PUT /api/shops/1/verify HTTP/1.1
Host: localhost:5001
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**cURL:**
```bash
curl -X PUT https://localhost:5001/api/shops/1/verify \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

---

### 8. Create Shop Manager (Admin Only)

**Request:**
```http
POST /api/auth/register-shop-manager HTTP/1.1
Host: localhost:5001
Content-Type: application/json
Authorization: Bearer YOUR_ADMIN_TOKEN

{
    "name": "Manager Name",
    "email": "manager@example.com",
    "password": "ManagerPassword123!",
    "shopId": 1
}
```

**cURL:**
```bash
curl -X POST https://localhost:5001/api/auth/register-shop-manager \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "name": "Manager Name",
    "email": "manager@example.com",
    "password": "ManagerPassword123!",
    "shopId": 1
  }'
```

---

### 9. Get Dashboard Statistics (Admin Only)

**Request:**
```http
GET /api/statistics/dashboard HTTP/1.1
Host: localhost:5001
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**cURL:**
```bash
curl -X GET https://localhost:5001/api/statistics/dashboard \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

---

### 10. Record Shop Statistics (Admin Only)

**Request:**
```http
POST /api/statistics/shop/1/record HTTP/1.1
Host: localhost:5001
Content-Type: application/json
Authorization: Bearer YOUR_ADMIN_TOKEN

{
    "totalVisits": 1500,
    "totalOrders": 75,
    "totalRevenue": 7500000,
    "averageRating": 4.5,
    "reviewCount": 35
}
```

**cURL:**
```bash
curl -X POST https://localhost:5001/api/statistics/shop/1/record \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "totalVisits": 1500,
    "totalOrders": 75,
    "totalRevenue": 7500000,
    "averageRating": 4.5,
    "reviewCount": 35
  }'
```

---

### 11. Get All Users (Admin Only)

**Request:**
```http
GET /api/users HTTP/1.1
Host: localhost:5001
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**cURL:**
```bash
curl -X GET https://localhost:5001/api/users \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

---

### 12. Deactivate User (Admin Only)

**Request:**
```http
PUT /api/users/2/deactivate HTTP/1.1
Host: localhost:5001
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**cURL:**
```bash
curl -X PUT https://localhost:5001/api/users/2/deactivate \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

---

### 13. Get User Profile

**Request:**
```http
GET /api/auth/profile HTTP/1.1
Host: localhost:5001
Authorization: Bearer YOUR_TOKEN
```

**cURL:**
```bash
curl -X GET https://localhost:5001/api/auth/profile \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Postman Collection

You can import the Swagger definition into Postman:

1. Open Postman
2. Click "Import"
3. Paste URL: `https://localhost:5001/swagger/v1/swagger.json`
4. Click "Import"
5. All endpoints will be available with documentation

---

## JavaScript Fetch Examples

### Login
```javascript
fetch('https://localhost:5001/api/auth/login', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        email: 'admin@example.com',
        password: 'password123'
    })
})
.then(res => res.json())
.then(data => {
    localStorage.setItem('token', data.token);
    console.log('Login successful');
})
.catch(err => console.error(err));
```

### Get Shops
```javascript
const token = localStorage.getItem('token');
fetch('https://localhost:5001/api/shops', {
    headers: {
        'Authorization': `Bearer ${token}`
    }
})
.then(res => res.json())
.then(data => console.log(data))
.catch(err => console.error(err));
```

### Create Shop
```javascript
const token = localStorage.getItem('token');
fetch('https://localhost:5001/api/shops', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
        name: 'New Shop',
        address: '123 Street',
        phone: '0912345678',
        description: 'Shop description',
        latitude: 10.7769,
        longitude: 106.6869,
        radius: 500
    })
})
.then(res => res.json())
.then(data => console.log(data))
.catch(err => console.error(err));
```

---

## Python Examples

### Login
```python
import requests
import json

url = 'https://localhost:5001/api/auth/login'
data = {
    'email': 'admin@example.com',
    'password': 'password123'
}

response = requests.post(url, json=data, verify=False)
token = response.json()['token']
print(f"Token: {token}")
```

### Get Shops
```python
import requests

url = 'https://localhost:5001/api/shops'
headers = {
    'Authorization': f'Bearer {token}'
}

response = requests.get(url, headers=headers, verify=False)
shops = response.json()
print(json.dumps(shops, indent=2))
```

### Create Shop
```python
import requests

url = 'https://localhost:5001/api/shops'
headers = {
    'Authorization': f'Bearer {token}',
    'Content-Type': 'application/json'
}
data = {
    'name': 'New Shop',
    'address': '123 Street',
    'phone': '0912345678',
    'description': 'Shop description',
    'latitude': 10.7769,
    'longitude': 106.6869,
    'radius': 500
}

response = requests.post(url, json=data, headers=headers, verify=False)
print(response.json())
```

---

## Common Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 500 | Server Error |

---

## Error Response Examples

### Invalid Credentials
```json
{
    "message": "Email hoặc mật khẩu không chính xác"
}
```

### Unauthorized (Missing Token)
```json
{
    "message": "Unauthorized"
}
```

### Forbidden (Insufficient Role)
```json
{
    "message": "Access denied"
}
```

### Resource Not Found
```json
{
    "message": "Shop not found"
}
```

---

## Tips for Testing

1. **Use Swagger UI** - Most convenient for quick testing
2. **Save Token** - Copy the token from login and reuse it
3. **Check Content-Type** - Always use `application/json`
4. **Include Bearer Token** - Format: `Authorization: Bearer YOUR_TOKEN`
5. **Handle HTTPS** - Use `verify=False` in Python for self-signed certificates
6. **Check Response** - Always validate the response status and message

---

Happy testing! 🎉
