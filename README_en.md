# Spring.NET + NHibernate Complete Learning Guide

<div align="center">

![Spring.NET](https://img.shields.io/badge/Spring.NET-3.0-green)
![NHibernate](https://img.shields.io/badge/NHibernate-5.4-blue)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

**Practical Tutorials for Enterprise .NET Application Development**

[Get Started](#-get-started) • [Learning Roadmap](#-learning-roadmap) • [Project Structure](#-project-structure) • [Contribute](#-contribute)

</div>

---

## 📖 Introduction

This project provides a **Korean tutorial** for step-by-step learning of enterprise web application development using **Spring.NET** and **NHibernate**.

### 💡 Why this Guide?

- ✅ **Solves Lack of Korean Resources** - Addresses the scarcity of Korean learning materials for Spring.NET and NHibernate.
- ✅ **Practical Project-Oriented** - Includes three practical projects: a bulletin board, user management, and a shopping mall.
- ✅ **Systematic Step-by-Step Learning** - 20 structured steps from basic to advanced.
- ✅ **Complete Example Code** - Provides working code examples for every step.
- ✅ **Real-World Patterns** - Includes practical design patterns like Repository, Spring.NET context-based Unit of Work, Specification, Soft Delete, and Audit Trail.

## 🎯 Learning Objectives

Upon completing this tutorial, you will be able to:

- 🔹 **Spring.NET IoC/DI** - Master Inversion of Control and Dependency Injection.
- 🔹 **NHibernate ORM** - Implement database integration through Object-Relational Mapping.
- 🔹 **Layered Architecture** - Design and implement a decoupled architecture with Domain, Data, Service, and Web layers.
- 🔹 **RESTful Web API** - Develop Web APIs integrated within an existing ASP.NET MVC project.
- 🔹 **Advanced Queries** - Utilize HQL, LINQ, Criteria API, and Stored Procedures effectively.
- 🔹 **Design Patterns** - Apply Repository, Spring.NET context-based Unit of Work, Specification, Soft Delete, and Audit Trail patterns.
- 🔹 **Practical Projects** - Implement a bulletin board, user management system, and a shopping mall.
- 🔹 **Transaction Management** - Implement declarative transactions, solve concurrency issues.
- 🔹 **Performance & Security** - Optimize with caching (second-level cache), Lazy/Eager Loading, and secure against SQL Injection, XSS, CSRF, password hashing.

## 🚀 Get Started

### Prerequisites

- **Visual Studio 2022** (Community or higher)
- **.NET Framework 4.8**
- **SQL Server 2019+** or **SQLite** (for learning purposes)

### Installation and Execution

1. **Clone the Project**
   ```bash
   git clone https://github.com/yourusername/SpringNet.git
   cd SpringNet
   ```

2. **Restore NuGet Packages**
   - NuGet packages will be restored automatically when you open the solution in Visual Studio.
   - Alternatively, run `nuget restore SpringNet.sln` from the project folder.

3. **Database Configuration**
   - For SQL Server: Modify the connection string in `SpringNet.Data/hibernate.cfg.xml`.
   - For SQLite: Use the default settings; the file will be automatically created in `SpringNet.Web/App_Data/springnet.db`.

4. **Start Learning**
   - Begin with `docs/tutorial/00-overview.md`.
   - Proceed sequentially from 01, 02, 03... following the instructions in each step.

## 📚 Learning Roadmap

### Total 20 Tutorials (Approx. 40-50 hours of learning)

<details>
<summary><b>Phase 1: Basic Concepts (Steps 1-3)</b> ⭐</summary>

- [01. Spring.NET Basic Concepts](docs/tutorial/01-springnet-basics_en.md) - Understanding IoC, DI
- [02. Deep Dive into Dependency Injection](docs/tutorial/02-dependency-injection_en.md) - Constructor/Property Injection
- [03. NHibernate Setup](docs/tutorial/03-nhibernate-setup_en.md) - Basic ORM Configuration

</details>

<details>
<summary><b>Phase 2: Bulletin Board System (Steps 4-7)</b> ⭐⭐</summary>

- [04. Domain Model Design](docs/tutorial/04-board-part1-domain_en.md) - Entity Design and Mapping
- [05. Repository Pattern](docs/tutorial/05-board-part2-repository_en.md) - Data Access Layer
- [06. Service Layer](docs/tutorial/06-board-part3-service_en.md) - Business Logic Separation
- [07. MVC Controller & View](docs/tutorial/07-board-part4-mvc_en.md) - Web Presentation

</details>

<details>
<summary><b>Phase 3: User Management (Steps 8-9)</b> ⭐⭐⭐</summary>

- [08. Authentication](docs/tutorial/08-user-part1-authentication_en.md) - User Registration, Login Implementation
- [09. Authorization](docs/tutorial/09-user-part2-authorization_en.md) - Permission Management

</details>

<details>
<summary><b>Phase 4: Shopping Mall System (Steps 10-12)</b> ⭐⭐⭐⭐</summary>

- [10. Product Management](docs/tutorial/10-shopping-part1-products_en.md) - Category, Product CRUD
- [11. Shopping Cart Functionality](docs/tutorial/11-shopping-part2-cart_en.md) - Shopping Cart Features
- [12. Order Processing](docs/tutorial/12-shopping-part3-order_en.md) - Order and Payment

</details>

<details>
<summary><b>Phase 5: Advanced Topics (Steps 13-14)</b> ⭐⭐⭐⭐</summary>

- [13. Transaction Management](docs/tutorial/13-transaction-management_en.md) - ACID, Isolation Levels
- [14. Best Practices](docs/tutorial/14-best-practices_en.md) - Security, Performance Optimization

</details>

<details open>
<summary><b>Phase 6: Advanced Practical Skills (Steps 15-19)</b> ⭐⭐⭐⭐⭐</summary>

- [15. NHibernate Advanced Queries](docs/tutorial/15-advanced-nhibernate-queries_en.md) - HQL, LINQ, Criteria
- [16. Stored Procedure Usage](docs/tutorial/16-stored-procedures_en.md) - How to Use Stored Procedures
- [17. Session Management](docs/tutorial/17-session-management_en.md) - NHibernate & Web Session
- [18. Web API Integration](docs/tutorial/18-webapi-integration_en.md) - RESTful API Development
- [19. Advanced CRUD Patterns](docs/tutorial/19-advanced-crud-patterns_en.md) - UoW, Specification

</details>

## 🏗️ Project Structure (Updated)

```
SpringNet/
│
├── docs/tutorial/                          # 📚 Learning Documents (20 Tutorials)
│   ├── 00-overview.md            # Overall Roadmap
│   ├── 01-springnet-basics.md    # Spring.NET Basics
│   ├── 02-dependency-injection.md
│   └── ... (Total 20 files)
│
├── SpringNet.Domain/              # 🎯 Domain Layer
│   └── Entities/                 # Entity Classes (e.g., Board.cs, Reply.cs, User.cs, Product.cs)
│   └── Specifications/           # Specification Pattern Implementation
│
├── SpringNet.Data/                # 💾 Data Access Layer
│   ├── Repositories/             # Repository Implementations (Generic Repository, Specific Repositories)
│   ├── Mappings/                 # NHibernate Mappings (*.hbm.xml, Filters.hbm.xml)
│   ├── Listeners/                # NHibernate Event Listeners (AuditEventListener)
│   └── NHibernateHelper.cs       # SessionFactory Management (or LocalSessionFactoryObject)
│
├── SpringNet.Service/             # 🔧 Service Layer
│   ├── Abstractions/             # Abstractions (e.g., IWebUserSession)
│   ├── DTOs/                     # Data Transfer Objects
│   └── Logging/                  # Logging Interfaces and Implementations
│
├── SpringNet.Infrastructure/      # 🛠️ Common Infrastructure (Currently empty, use as needed)
│
├── SpringNet.Web/                 # 🌐 Web Presentation (MVC & Web API Integrated)
│   ├── Controllers/              # MVC and API Controllers
│   ├── Filters/                  # Custom Filters (e.g., AuthorizeAttribute)
│   ├── Infrastructure/           # Web Infrastructure-related Code (e.g., WebUserSession)
│   ├── Models/                   # View Models and API Request/Response Models
│   ├── Views/
│   ├── Config/                   # Spring Configuration Files (applicationContext.xml split)
│   │   ├── applicationContext.xml
│   │   ├── dataAccess.xml
│   │   ├── services.xml
│   │   └── controllers.xml
│   └── Web.config
│
└── SpringNet.Tests/               # 🧪 Unit and Integration Test Project (Optional)
    └── ServiceTests/             # Service Layer Tests
```

## 🛠️ Technology Stack (Updated)

### Core Frameworks

| Technology | Version | Purpose |
|------------|---------|---------|
| **Spring.NET** | 3.0.0 | IoC/DI Container, AOP, Transaction Management |
| **NHibernate** | 5.4.0 | ORM (Object-Relational Mapping) |
| **ASP.NET MVC** | 5.2.9 | Web Framework |
| **ASP.NET Web API** | 5.2.9 | RESTful API (Integrated into existing MVC project) |
| **.NET Framework** | 4.8 | Runtime |

### Key NuGet Packages

-   `Spring.Core`, `Spring.Web`, `Spring.Web.Mvc5` (Spring.NET Core)
-   `Spring.Transaction.Interceptor` (Declarative Transactions)
-   `NHibernate`, `NHibernate.Caches.SysCache2` (NHibernate Core)
-   `Microsoft.AspNet.WebApi`, `Microsoft.AspNet.WebApi.Cors` (Web API Integration)
-   `Newtonsoft.Json` (JSON Serialization)
-   `Moq` (Unit Test Mocking)
-   `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk` (Unit Test Framework)
-   `Swashbuckle` (Web API Documentation)
-   `BCrypt.Net-Next` (Password Hashing - Optional Recommendation)
-   `System.Data.SQLite` (SQLite DB Driver - Optional)

### Database

- **SQL Server 2019+** (Production)
- **SQLite** (for learning purposes)

### Development Tools

- **Visual Studio 2022**
- **NuGet Package Manager**
- **SQL Server Management Studio** (Optional)
- **Postman** or **Swagger UI** (for Web API testing)

## 👥 Target Audience

### Recommended for

- ✅ Developers with basic knowledge of .NET Framework
- ✅ Those who want to learn Spring.NET or NHibernate
- ✅ Those interested in enterprise architecture
- ✅ Those maintaining legacy .NET projects
- ✅ Developers with Java Spring Framework experience looking to transition to .NET

### Prerequisites

- 📌 **Required**: Basic C# syntax, ASP.NET MVC fundamentals
- 📌 **Recommended**: Basic SQL, Object-Oriented Programming, Design Patterns

## 📖 Learning Methodology

### Recommended Learning Order

1. **Learn Sequentially** - Proceed from `00-overview.md` to `19-advanced-crud-patterns.md` in order. Each tutorial builds upon the previous one.
2. **Write Code Directly** - Type out the example code yourself (no copy-pasting!) to better understand the implementation principles.
3. **Run and Test** - Execute the project and verify its operation at each step to deepen your understanding.
4. **Solve Practice Problems** - Tackle the practice problems in each step to solidify your knowledge.
5. **Review** - If you don't understand something, go back to previous steps or re-read the key summaries.

### Learning Tips

- 💡 **Pace Yourself** - It's more effective to proceed steadily, perhaps 1-2 steps per day, rather than trying to learn too much at once.
- 💡 **Understand the Code** - Go beyond just typing; focus on *why* the code is written that way.
- 💡 **Troubleshoot Errors** - Practice reading error messages (stack traces) and attempting to solve issues yourself.
- 💡 **Hands-on Learning** - Invest more time in practical exercises than theoretical learning.

## 🎯 Practical Projects

### 1. Bulletin Board System 📝

**Features**: Board CRUD, Comments, View Count, Paging, Search, User-specific Permission Management

**Learning Content**:
- Entity Relationship Mapping (One-to-Many)
- Repository Pattern and Generic Repository
- Service Layer and Business Logic Separation
- MVC Controller Implementation and View Integration

### 2. User Management System 👤

**Features**: User Registration, Login, Role-Based Permission Management, Session Management, Spring.NET XML Configuration Separation

**Learning Content**:
- Password Hashing and Security
- Authentication and Authorization Implementation
- Custom Authorize Attributes
- Spring.NET Container Configuration Separation (dataAccess.xml, services.xml, controllers.xml)

### 3. Shopping Mall System 🛒

**Features**: Product Management, Categories, Shopping Cart, Order Processing, Inventory Management

**Learning Content**:
- Complex Multi-Entity Relationships
- Transaction Boundary Setting and Composite Transaction Processing (Order Creation)
- Inventory Management and Concurrency Considerations
- DTO Pattern for Data Transfer Between Layers

## 📊 Learning Progress Check

Check off each step upon completion:

- [ ] Phase 1: Basic Concepts (01-03) ⭐
- [ ] Phase 2: Bulletin Board System (04-07) ⭐⭐
- [ ] Phase 3: User Management (08-09) ⭐⭐⭐
- [ ] Phase 4: Shopping Mall System (10-12) ⭐⭐⭐⭐
- [ ] Phase 5: Advanced Topics (13-14) ⭐⭐⭐⭐
- [ ] Phase 6: Advanced Practical Skills (15-19) ⭐⭐⭐⭐⭐

## 🤝 Contribute

This project is open source. Contributions are welcome!

### How to Contribute

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Contribution Guidelines

- 📝 Typo corrections, explanation enhancements
- 💡 Add new examples
- 🐛 Bug fixes
- 📚 Write additional tutorials (discuss via issues first)

## 📝 License

This project is licensed under the **MIT License**. You are free to use, modify, and distribute it.

## 🙏 Acknowledgements

- [Spring.NET](http://springframework.net/) - Official documentation and community
- [NHibernate](https://nhibernate.info/) - ORM Framework
- All contributors and learners

## 📞 Questions & Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/SpringNet/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/SpringNet/discussions)

## 🌟 Star History

If this project was helpful, please ⭐ Star it!

---

<div align="center">

**Happy Learning! 📚**

Made with ❤️ for .NET Developers

[⬆ Back to Top](#spring.net--nhibernate-complete-learning-guide)

</div>