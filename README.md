# PathFinder – Smart Internship Management System (Backend)

## Project Overview
PathFinder is a cloud-based Smart Internship Management System developed to connect students, companies, and administrators through a centralized internship management platform.

This repository contains the backend REST API developed using ASP.NET Core Web API. The system implements secure authentication, role-based authorization, automated CI/CD pipelines, and cloud deployment using modern DevOps practices.

---

## System Architecture
```
Frontend (React – Vercel)
        ↓
Backend API (.NET Web API – Docker Container)
        ↓
Azure Web App (Cloud Hosting)
        ↓
Azure SQL Database

CI/CD Workflow:
Developer → GitHub → GitHub Actions → Docker → Docker Hub → Azure Web App → Live Application
```
---

## Technology Stack

| Category | Technology |
|-----------|------------|
| Backend Framework | ASP.NET Core Web API (.NET 8) |
| Programming Language | C# |
| Authentication | JWT Authentication |
| Password Security | BCrypt |
| Database | Azure SQL Database |
| Containerization | Docker |
| CI/CD | GitHub Actions |
| Container Registry | Docker Hub |
| Cloud Platform | Microsoft Azure |
| API Documentation | Swagger |

---

## Repository Structure
```
PathFinder_Backend/
│
├── src/                        # Application source code
│   └── PathFinder.API/
│       ├── Controllers/
│       ├── Models/
│       ├── DTOs/
│       ├── Services/
│       ├── Repositories/
│       ├── Data/
│       ├── Middleware/
│       ├── Program.cs
│       └── appsettings.json
│
│                 
│── Dockerfile
│── docker-compose.yml
│
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── cd.yml
│
│
           
├── .gitignore
├── README.md
```


---

## Branch Strategy

| Branch | Purpose |
|--------|---------|
| main | Production deployment |
| develop | Integration testing |
| QA | Testing |
| devops | CI/CD configuration |

This branching strategy ensures controlled development and stable releases.

---

## Local Development Setup

### Prerequisites
- .NET SDK 8+
- Docker Desktop
- Git
- Azure SQL Database access

---

### Clone Repository

```bash
git clone https://github.com/Jenitha23/PathFinder_Backend.git
cd PathFinder_Backend
Run Application (Without Docker)
dotnet restore
dotnet build
dotnet run
```
Application URL:   http://localhost:5249

Swagger Documentation: http://localhost:5249/swagger

Environment Configuration
Sensitive credentials are managed using environment variables.

Configure locally or in Azure Web App:
- ConnectionStrings__DefaultConnection=<Azure SQL Connection String>
- Jwt__Key=<Secret Key>
- Jwt__Issuer=PathFinder
- Jwt__Audience=PathFinderUsers

Docker Containerization
Build Docker Image
```
docker build -t pathfinder-backend .
```
Run Docker Container
```
docker run -p 8080:8080 pathfinder-backend
```
Docker ensures consistent environments across development, testing, and production.

---

### CI/CD Pipeline (GitHub Actions)
# Continuous Integration (CI)
      Triggered on code push:
      - Source checkout
      - Build validation
      - Unit testing
      - Docker image build

# Continuous Deployment (CD)
      Triggered when code is merged into main branch:
      - Docker image built automatically
      - Image pushed to Docker Hub
      - Azure Web App pulls latest container
      - Backend redeployed automatically

# Pipeline location:
      - .github/workflows/

---
      
## Cloud Deployment
Backend Hosting
- Azure Web App (Container-based deployment)
- Docker Hub stores versioned images

Database
- Azure SQL Database with secure connectivity

Security Implementation
- JWT-based authentication
- Role-based authorization (ADMIN / STUDENT / COMPANY)
- BCrypt password hashing

GitHub Secrets for credentials
- Secure environment variable management
- No sensitive data stored in source code

---

## API Documentation
- Swagger UI: "https://pathfinder-fqgwf0e6bvc2cmbq.southeastasia-01.azurewebsites.net/swagger"

---
Contributors:  PathFinder Development Team
----
