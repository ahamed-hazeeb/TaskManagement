# Task Management API

A production-ready REST API for team collaboration and task management built with ASP.NET Core 8.

## 🚀 Features

- **JWT Authentication** - Secure token-based authentication
- **Role-Based Authorization** - Owner, Manager, and Member roles
- **Team Management** - Create teams and manage memberships
- **Project Organization** - Group tasks into projects within teams
- **Task Tracking** - Create, assign, and track tasks with status workflows
- **Advanced Filtering** - Filter by status, priority, assignee, date range
- **Pagination** - Efficient data loading with page metadata
- **Search** - Full-text search across tasks
- **Global Error Handling** - Consistent error responses
- **Response Caching** - Improved performance
- **Health Checks** - Monitor API health
- **API Versioning** - Future-proof API design

## 🏗️ Architecture
```
TaskManagement/
├── TaskManagement.Api          # HTTP endpoints, controllers, middleware
├── TaskManagement.Core         # Domain models, DTOs, interfaces
├── TaskManagement.Application  # Business logic, services, validators
└── TaskManagement.Infrastructure # Data access, repositories, EF Core
```

**Clean Architecture Benefits:**
- Separation of concerns
- Testability
- Maintainability
- Technology independence

## 🛠️ Tech Stack

- **Framework**: ASP.NET Core 8
- **Database**: SQL Server
- **ORM**: Entity Framework Core 8
- **Authentication**: JWT Bearer Tokens
- **Validation**: FluentValidation
- **Password Hashing**: BCrypt
- **Documentation**: Swagger/OpenAPI
- **Logging**: Built-in ASP.NET Core logging

## 📋 Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB, Express, or Full)
- Visual Studio 2022 / VS Code / Rider

## 🚀 Getting Started

### 1. Clone the repository
```bash
git clone https://github.com/ahamed-hazeeb/TaskManagement.git
cd TaskManagement
```

### 2. Update connection string

Edit `TaskManagement.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Apply database migrations
```bash
cd TaskManagement.Api
dotnet ef database update
```

### 4. Run the application
```bash
dotnet run
```

### 5. Open Swagger UI

Navigate to: `https://localhost:7032/swagger`

## 📚 API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/register` | Register new user |
| POST | `/api/v1/auth/login` | Login and get JWT token |
| GET | `/api/v1/auth/me` | Get current user info |

### Teams

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/v1/teams` | Create team | ✅ |
| GET | `/api/v1/teams/{id}` | Get team details | ✅ |
| GET | `/api/v1/teams/my-teams` | Get user's teams | ✅ |
| PUT | `/api/v1/teams/{id}` | Update team | ✅ Owner/Manager |
| DELETE | `/api/v1/teams/{id}` | Delete team | ✅ Owner only |
| POST | `/api/v1/teams/{id}/members` | Add member | ✅ Owner/Manager |
| DELETE | `/api/v1/teams/{id}/members/{userId}` | Remove member | ✅ Owner/Manager |
| POST | `/api/v1/teams/{id}/leave` | Leave team | ✅ |

### Projects

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/v1/teams/{teamId}/projects` | Create project | ✅ |
| GET | `/api/v1/teams/{teamId}/projects` | List team projects | ✅ |
| GET | `/api/v1/teams/{teamId}/projects/{id}` | Get project details | ✅ |
| PUT | `/api/v1/teams/{teamId}/projects/{id}` | Update project | ✅ Owner/Manager |
| DELETE | `/api/v1/teams/{teamId}/projects/{id}` | Delete project | ✅ Owner/Manager |

### Tasks

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/v1/projects/{projectId}/tasks` | Create task | ✅ |
| GET | `/api/v1/projects/{projectId}/tasks/paged` | List tasks (with filters) | ✅ |
| GET | `/api/v1/projects/{projectId}/tasks/{id}` | Get task details | ✅ |
| PUT | `/api/v1/projects/{projectId}/tasks/{id}` | Update task | ✅ |
| DELETE | `/api/v1/projects/{projectId}/tasks/{id}` | Delete task | ✅ Owner/Manager |
| POST | `/api/v1/projects/{projectId}/tasks/{id}/assign` | Assign task | ✅ |
| POST | `/api/v1/projects/{projectId}/tasks/{id}/unassign` | Unassign task | ✅ |
| PUT | `/api/v1/projects/{projectId}/tasks/{id}/status` | Update status | ✅ |

### Health

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | API health check |

## 🔐 Authentication Flow

### 1. Register
```bash
POST /api/v1/auth/register
Content-Type: application/json

{
  "email": "john@example.com",
  "fullName": "John Doe",
  "password": "Password123",
  "confirmPassword": "Password123"
}
```

**Response:**
```json
{
  "userId": 1,
  "email": "john@example.com",
  "fullName": "John Doe",
  "role": 2,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-01-26T10:00:00Z"
}
```

### 2. Use Token

Add to request headers:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## 📊 Task Filtering Examples

### Filter by status
```
GET /api/v1/projects/1/tasks/paged?status=2&page=1&pageSize=20
```

Status values:
- `1` = Todo
- `2` = InProgress
- `3` = InReview
- `4` = Done

### Filter by priority
```
GET /api/v1/projects/1/tasks/paged?priority=3&page=1&pageSize=20
```

Priority values:
- `1` = Low
- `2` = Medium
- `3` = High
- `4` = Urgent

### Combined filters with sorting
```
GET /api/v1/projects/1/tasks/paged?status=2&priority=3&sortBy=dueDate&sortDescending=false&page=1&pageSize=10
```

### Search
```
GET /api/v1/projects/1/tasks/paged?searchTerm=login&page=1&pageSize=20
```

## 🗄️ Database Schema
```
Users
├── Id (PK)
├── Email (Unique)
├── FullName
├── PasswordHash
├── Role
└── CreatedAt

Teams
├── Id (PK)
├── Name
├── Description
└── CreatedAt

TeamMembers
├── Id (PK)
├── TeamId (FK)
├── UserId (FK)
├── Role (Owner/Manager/Member)
└── JoinedAt

Projects
├── Id (PK)
├── Name
├── TeamId (FK)
├── Description
├── Deadline
└── CreatedAt

Tasks
├── Id (PK)
├── Title
├── Description
├── ProjectId (FK)
├── AssignedToUserId (FK, nullable)
├── Status
├── Priority
├── DueDate
├── CreatedAt
└── CompletedAt
```

## 🧪 Testing

### Using Swagger UI

1. Navigate to `https://localhost:7032/swagger`
2. Click "Authorize" and enter token
3. Try endpoints interactively

### Using cURL
```bash
# Register
curl -X POST "https://localhost:7032/api/v1/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "fullName": "Test User",
    "password": "Password123",
    "confirmPassword": "Password123"
  }'

# Login
curl -X POST "https://localhost:7032/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123"
  }'

# Get teams (with auth)
curl -X GET "https://localhost:7032/api/v1/teams/my-teams" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## 🚢 Deployment

### Prepare for Production

1. **Update appsettings.Production.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_PRODUCTION_CONNECTION_STRING"
  },
  "JwtSettings": {
    "Secret": "GENERATE_A_SECURE_SECRET_AT_LEAST_32_CHARACTERS_LONG",
    "Issuer": "TaskManagementApi",
    "Audience": "TaskManagementClient",
    "ExpirationInMinutes": 60
  }
}
```

2. **Publish**
```bash
dotnet publish -c Release -o ./publish
```

3. **Deploy to Azure App Service / IIS / Docker**

See deployment guides in `/docs` folder.

## 📝 License

This project is licensed under the MIT License.

## 👤 Author

**Ahamed Hazeeb**
- GitHub: [@ahamed-hazeeb](https://github.com/ahamed-hazeeb)
- LinkedIn: [ahamed-hazeeb](www.linkedin.com/in/ahamed-hazeeb-902782146)

## 🙏 Acknowledgments

- Built with ASP.NET Core
- Inspired by modern task management systems
- Created as a portfolio project for .NET developer positions