# VinhKhanhFoodTour Admin Web

Admin website for managing shops and shop managers for the VinhKhanhFoodTour application.

## Features

### Admin Features
- **Dashboard**: Overview of all shops, managers, and statistics
- **Shop Management**: Create, view, verify, and manage all shops
- **User Management**: Create admin accounts and shop manager accounts
- **Statistics**: View shop performance metrics and analytics
- **Audit Logs**: Track all system activities and changes

### Shop Manager Features
- **Shop Profile**: Manage their assigned shop information
- **Shop Statistics**: View their shop's performance metrics
- **Order Management**: View and track orders
- **Reviews**: Monitor customer reviews

## Technology Stack

- **Framework**: ASP.NET Core 10.0
- **ORM**: Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens)
- **Database**: SQL Server
- **UI**: Blazor Server Components
- **Password Security**: SHA256 Hashing

## Installation

### Prerequisites
- .NET 10 SDK
- SQL Server (LocalDB or full SQL Server)
- Visual Studio or VS Code

### Setup

1. **Update Connection String**
   - Edit `appsettings.json`
   - Change the `AdminConnection` string if needed:
   ```json
   "ConnectionStrings": {
       "AdminConnection": "Server=(localdb)\\mssqllocaldb;Database=VinhKhanhFoodTourAdmin;Trusted_Connection=true;"
   }
   ```

2. **Update JWT Settings**
   - Edit `appsettings.json` - Change the SecretKey to a strong value in production
   ```json
   "JwtSettings": {
       "SecretKey": "your-very-long-secret-key-at-least-32-characters-for-production!",
       "Issuer": "VinhKhanhFoodTourAdmin",
       "Audience": "VinhKhanhFoodTourAdminUsers",
       "ExpirationHours": "24"
   }
   ```

3. **Create Database**
   ```bash
   dotnet ef database update
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```

5. **Access the Application**
   - Navigate to `https://localhost:5001` (or your configured port)
   - Login page will appear

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with email and password
- `POST /api/auth/register-admin` - Register new admin (Admin only)
- `POST /api/auth/register-shop-manager` - Register shop manager (Admin only)
- `GET /api/auth/profile` - Get current user profile (Authorized)

### Shops
- `GET /api/shops` - Get all shops (Admin only)
- `GET /api/shops/{id}` - Get shop details
- `POST /api/shops` - Create shop (Admin only)
- `PUT /api/shops/{id}` - Update shop (Admin/ShopManager)
- `PUT /api/shops/{id}/verify` - Verify shop (Admin only)
- `DELETE /api/shops/{id}` - Deactivate shop (Admin only)

### Users
- `GET /api/users` - Get all admin users (Admin only)
- `GET /api/users/{id}` - Get user details (Admin only)
- `PUT /api/users/{id}/deactivate` - Deactivate user (Admin only)
- `PUT /api/users/{id}/activate` - Activate user (Admin only)
- `DELETE /api/users/{id}` - Delete user (Admin only)

### Statistics
- `GET /api/statistics/dashboard` - Dashboard statistics (Admin only)
- `GET /api/statistics/shop/{shopId}` - Shop statistics
- `POST /api/statistics/shop/{shopId}/record` - Record shop statistics (Admin only)
- `GET /api/statistics/audit-logs` - Get audit logs (Admin only)

## User Roles

### Admin
- Full access to all features
- Can create and manage all shops
- Can create admin and shop manager accounts
- Can view system statistics and audit logs

### Shop Manager
- Can only access their assigned shop
- Can update shop information
- Can view their shop's statistics
- Can view and manage their shop's orders and reviews

### User
- Basic access level (default for new accounts)

## Database Models

### AdminUser
- UserId (PK)
- Name
- Email (Unique)
- Password (SHA256 hashed)
- Role (Admin, ShopManager, User)
- IsActive
- ManagedShopId (FK, for ShopManager)
- CreatedAt
- LastLogin

### ManagedShop
- ShopId (PK)
- Name
- Address
- Phone
- Description
- Latitude, Longitude
- Radius
- IsVerified
- IsActive
- TotalOrders
- AverageRating
- CreatedAt
- UpdatedAt

### ShopStatistics
- ShopId (FK)
- StatisticsDate
- TotalVisits
- TotalOrders
- TotalRevenue
- AverageRating
- ReviewCount

### AuditLog
- AuditId (PK)
- UserId
- Action
- EntityType
- EntityId
- OldValue
- NewValue
- IpAddress
- CreatedAt

## Security Features

1. **JWT Authentication**: Secure token-based authentication
2. **Password Hashing**: SHA256 hashing for password security
3. **Role-Based Access Control**: Fine-grained authorization
4. **Audit Logging**: Track all administrative actions
5. **CORS Configuration**: Secure cross-origin requests

## Default Credentials

After creating the database, you'll need to create an admin account through the registration endpoint or directly in the database.

### Create Admin via API
```json
POST /api/auth/register-admin
{
    "name": "Admin Name",
    "email": "admin@example.com",
    "password": "SecurePassword123!"
}
```

## File Structure

```
VinhKhanhFoodTour.AdminWeb/
├── Controllers/
│   ├── AuthController.cs
│   ├── ShopsController.cs
│   ├── UsersController.cs
│   └── StatisticsController.cs
├── Data/
│   └── AdminDbContext.cs
├── Models/
│   ├── AdminUser.cs
│   ├── ManagedShop.cs
│   ├── ShopStatistics.cs
│   └── AuditLog.cs
├── Services/
│   ├── AuthenticationService.cs
│   ├── JwtTokenService.cs
│   └── PasswordService.cs
├── Pages/
│   ├── AdminDashboard.razor
│   ├── LoginPage.razor
│   └── ShopDetail.razor
├── Migrations/
│   └── [Migration files]
├── Program.cs
├── appsettings.json
└── VinhKhanhFoodTour.AdminWeb.csproj
```

## Integration with Main API

To fully integrate with the main VinhKhanhFoodTour API:

1. Update the API to include admin endpoints
2. Sync user data between the main app and admin database
3. Share authentication tokens
4. Implement webhook for real-time statistics updates

## Future Enhancements

- [ ] Email notifications for shop managers
- [ ] Advanced analytics and reporting
- [ ] Bulk shop management operations
- [ ] Shop verification workflow
- [ ] Rating system for administrators
- [ ] Multi-language support
- [ ] Dark mode UI
- [ ] Mobile app admin panel
- [ ] Real-time notifications via SignalR
- [ ] Export reports (PDF, Excel)

## License

This project is part of VinhKhanhFoodTour application.

## Support

For issues or questions, please contact the development team.
