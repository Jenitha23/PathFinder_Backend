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

* Student, Company, and Admin Authentication
* Secure Login & Registration APIs
* JWT-Based Authorization
* Password Encryption
* Role-Based Access Control
* RESTful API Architecture*
