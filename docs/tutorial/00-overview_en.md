# Spring.NET + NHibernate Learning Roadmap

## 📚 Learning Overview

This tutorial provides a step-by-step guide to developing practical web applications using Spring.NET and NHibernate.

### Learning Objectives

-   Understand and practice **Spring.NET core concepts** (IoC, DI, AOP)
-   Integrate with databases using **NHibernate ORM**
-   Design and implement **layered architecture**
-   Enhance practical skills through three **real-world projects**

## 🎯 Project Structure

```
SpringNet/
├── SpringNet.Domain/        # Domain Model (Entities)
├── SpringNet.Data/          # Data Access (Repository, NHibernate)
├── SpringNet.Service/       # Business Logic (Service Layer)
├── SpringNet.Infrastructure/ # Common Infrastructure (Helpers, Utilities)
└── SpringNet.Web/           # Web Presentation (MVC)
```

## 📖 Learning Phases

### Phase 1: Basic Concepts (Steps 1-3)

| Step | Document | Content | Difficulty |
|------|----------|---------|------------|
| 1 | [01-springnet-basics_en.md](./01-springnet-basics_en.md) | Spring.NET Basic Concepts (IoC, DI) | ⭐ |
| 2 | [02-dependency-injection_en.md](./02-dependency-injection_en.md) | Deep Dive into Dependency Injection | ⭐ |
| 3 | [03-nhibernate-setup_en.md](./03-nhibernate-setup_en.md) | NHibernate Setup and Basics | ⭐⭐ |

### Phase 2: Bulletin Board System (Steps 4-7)

| Step | Document | Content | Difficulty |
|------|----------|---------|------------|
| 4 | [04-board-part1-domain_en.md](./04-board-part1-domain_en.md) | Domain Model Design | ⭐⭐ |
| 5 | [05-board-part2-repository_en.md](./05-board-part2-repository_en.md) | Repository Pattern Implementation | ⭐⭐ |
| 6 | [06-board-part3-service_en.md](./06-board-part3-service_en.md) | Service Layer Implementation | ⭐⭐⭐ |
| 7 | [07-board-part4-mvc_en.md](./07-board-part4-mvc_en.md) | MVC Controller and View | ⭐⭐⭐ |

### Phase 3: User Management (Steps 8-9)

| Step | Document | Content | Difficulty |
|------|----------|---------|------------|
| 8 | [08-user-part1-authentication_en.md](./08-user-part1-authentication_en.md) | User Registration, Login Implementation | ⭐⭐⭐ |
| 9 | [09-user-part2-authorization_en.md](./09-user-part2-authorization_en.md) | Permission Management and Security | ⭐⭐⭐⭐ |

### Phase 4: Shopping Mall System (Steps 10-12)

| Step | Document | Content | Difficulty |
|------|----------|---------|------------|
| 10 | [10-shopping-part1-products_en.md](./10-shopping-part1-products_en.md) | Product Management | ⭐⭐⭐ |
| 11 | [11-shopping-part2-cart_en.md](./11-shopping-part2-cart_en.md) | Shopping Cart Functionality | ⭐⭐⭐⭐ |
| 12 | [12-shopping-part3-order_en.md](./12-shopping-part3-order_en.md) | Order Processing and Payment | ⭐⭐⭐⭐ |

### Phase 5: Advanced Topics (Steps 13-14)

| Step | Document | Content | Difficulty |
|------|----------|---------|------------|
| 13 | [13-transaction-management_en.md](./13-transaction-management_en.md) | Transaction Management | ⭐⭐⭐⭐ |
| 14 | [14-best-practices_en.md](./14-best-practices_en.md) | Best Practices | ⭐⭐⭐⭐⭐ |

### Phase 6: Advanced Practical Skills (Steps 15-19)

| Step | Document | Content | Difficulty |
|------|----------|---------|------------|
| 15 | [15-advanced-nhibernate-queries_en.md](./15-advanced-nhibernate-queries_en.md) | NHibernate Advanced Queries (HQL, LINQ, Criteria) | ⭐⭐⭐⭐ |
| 16 | [16-stored-procedures_en.md](./16-stored-procedures_en.md) | Stored Procedure Usage | ⭐⭐⭐ |
| 17 | [17-session-management_en.md](./17-session-management_en.md) | Session Management (NHibernate & Web Session) | ⭐⭐⭐⭐ |
| 18 | [18-webapi-integration_en.md](./18-webapi-integration_en.md) | ASP.NET Web API Integration | ⭐⭐⭐⭐ |
| 19 | [19-advanced-crud-patterns_en.md](./19-advanced-crud-patterns_en.md) | Advanced CRUD Patterns (UoW, Specification) | ⭐⭐⭐⭐⭐ |

## 🛠️ Required Tools

-   **Visual Studio 2022**
-   **.NET Framework 4.8**
-   **SQL Server 2019+** or **SQLite** (for learning purposes)
-   **NuGet Packages**:
    -   Spring.Core (>= 3.0.0)
    -   Spring.Web.Mvc5 (>= 3.0.0)
    -   NHibernate (>= 5.3.0)
    -   FluentNHibernate (>= 3.1.0) - Optional

## 📋 Pre-Learning Checklist

-   [ ] Visual Studio 2022 installed
-   [ ] SQL Server installed and running (or SQLite)
-   [ ] SpringNet solution created (5 projects)
-   [ ] NuGet packages installed
-   [ ] Git repository initialized (optional)

## 🚀 Learning Methodology

### Recommended Learning Order

1.  **Learn Sequentially**: Proceed from 01 to 19 in order.
2.  **Write Code Directly**: Type out the code yourself instead of copy-pasting to understand it better.
3.  **Run and Test**: Always run and verify the operation at each step.
4.  **Troubleshoot Errors**: Read stack traces carefully and try to resolve errors.
5.  **Review**: If something is unclear, go back to previous steps and review.

### Document Structure

Each tutorial document is structured as follows:

```
1. Learning Objectives  # What you will learn in this step
2. Concept Explanation  # Necessary theoretical explanations
3. Code Implementation  # Step-by-step code writing
4. Execution and Testing # Verify operation
5. Key Summary          # Summary of what was learned
6. Practice Problems    # Self-study exercises
7. Next Steps           # What to learn next
```

## 💡 Learning Tips

### Spring.NET Core Concepts
-   **IoC (Inversion of Control)**: Delegation of object creation/management to the framework.
-   **DI (Dependency Injection)**: Injection of necessary objects from external sources.
-   **Bean**: An object managed by Spring (defined in XML or by Attribute).

### NHibernate Core Concepts
-   **ORM**: Object-Relational Mapping - Mapping C# objects to DB tables.
-   **Session**: An object managing database connections.
-   **HQL**: Object-oriented query language.
-   **Lazy Loading**: Loading data only when needed.

### Common Mistakes
1.  ❌ Spring configuration file path errors
2.  ❌ NHibernate mapping errors
3.  ❌ Circular reference issues
4.  ❌ Transaction not applied
5.  ❌ Session management errors

## 📚 References

- [Spring.NET Official Documentation](http://springframework.net/doc-latest/reference/html/)
- [NHibernate Official Documentation](https://nhibernate.info/doc/)
- [Spring.NET GitHub](https://github.com/spring-projects/spring-net)

## 🎓 Expected Outcomes

Upon completing this tutorial:

✅ **Enterprise Architecture** design skills
✅ **Spring.NET IoC/DI** complete understanding
✅ **NHibernate ORM** practical application (HQL, LINQ, Criteria, Stored Procedure)
✅ **Layered Pattern** implementation skills
✅ **3 Practical Projects** for portfolio
✅ **Web API Development** skills
✅ **Advanced Design Patterns** application (UoW, Specification, Repository)
✅ **Session Management** mastery

## 🤝 Need Help?

If you encounter difficulties at any step:
1.  Read the error message carefully.
2.  Check your configuration files (XML, Web.config) again.
3.  Go back to previous steps and ensure nothing was missed.

---

**Ready? Then start with [01-springnet-basics_en.md](./01-springnet-basics_en.md)!** 🚀
