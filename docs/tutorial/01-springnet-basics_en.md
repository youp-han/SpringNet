# 01. Spring.NET Basic Concepts

## 📚 Learning Objectives

-   Understand the core concepts of the Spring.NET framework
-   Grasp the principles of IoC (Inversion of Control)
-   Learn the basics of DI (Dependency Injection)
-   Familiarize with Spring.NET configuration methods (XML-based)

## 🎯 What is Spring.NET?

Spring.NET is an **enterprise application framework** for the .NET platform. It is a port of Java's Spring Framework to .NET, helping developers easily apply object-oriented programming best practices.

### Key Features

-   **Lightweight Container**: Manages object creation and lifecycle.
-   **Dependency Injection**: Reduces coupling between objects.
-   **AOP (Aspect-Oriented Programming)**: Separates cross-cutting concerns.
-   **Transaction Management**: Handles declarative transactions.

## 💡 Core Concepts

### 1. IoC (Inversion of Control)

**Traditional Approach** (Developer directly creates objects):

```csharp
// Bad example: Direct dependency creation (tight coupling)
public class OrderService
{
    private EmailService emailService;

    public OrderService()
    {
        // OrderService directly creates EmailService
        emailService = new EmailService();
    }

    public void PlaceOrder(Order order)
    {
        // Process order...
        emailService.SendConfirmation(order);
    }
}
```

**Problems**:
-   ❌ OrderService depends on the concrete implementation of EmailService.
-   ❌ Difficult to test (cannot use Mock objects).
-   ❌ Changes to EmailService require modifications in OrderService.

**IoC Approach** (Spring creates and injects objects):

```csharp
// Good example: Dependency Injection (loose coupling)
public class OrderService
{
    private IEmailService emailService;

    // Spring injects dependencies via the constructor
    public OrderService(IEmailService emailService)
    {
        this.emailService = emailService;
    }

    public void PlaceOrder(Order order)
    {
        // Process order...
        emailService.SendConfirmation(order);
    }
}
```

**Advantages**:
-   ✅ Depends on interfaces (loose coupling).
-   ✅ Easy to test (can inject Mocks).
-   ✅ Increased flexibility (easy to swap implementations).

### 2. DI (Dependency Injection)

Dependency Injection is a pattern where **dependencies required by an object are injected from an external source**.

#### 3 Methods of DI

**① Constructor Injection** - **Most Recommended**

```csharp
public class ProductService
{
    private readonly IProductRepository repository;

    // Injected via constructor
    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }
}
```

**② Property Injection**

```csharp
public class ProductService
{
    // Injected via property
    public IProductRepository Repository { get; set; }

    public void DoSomething()
    {
        var products = Repository.GetAll();
    }
}
```

**③ Method Injection**

```csharp
public class ProductService
{
    private IProductRepository repository;

    // Injected via method
    public void SetRepository(IProductRepository repository)
    {
        this.repository = repository;
    }
}
```

### 3. Spring Container (IoC Container)

The Spring Container is the core component responsible for **creating, configuring, and managing objects**.

```
┌─────────────────────────────┐
│   Spring IoC Container      │
│                             │
│  ┌──────────────────────┐   │
│  │  Configuration       │   │
│  │  (applicationContext.│   │
│  │   xml)               │   │
│  └──────────────────────┘   │
│           ↓                 │
│  ┌──────────────────────┐   │
│  │  Bean Factory        │   │
│  │  - Bean Creation     │   │
│  │  - Dependency        │   │
│  │    Injection         │   │
│  └──────────────────────┘   │
│           ↓                 │
│  ┌──────────────────────┐   │
│  │  Managed Beans       │   │
│  │  (Services,          │   │
│  │   Repositories)      │   │
│  └──────────────────────┘   │
└─────────────────────────────┘
```

## 🛠️ Spring.NET Configuration Practice

### 1. Project Structure Check

Current project:
```
SpringNet.Web/
├── Config/
│   └── applicationContext.xml  # Spring configuration file
├── Controllers/
│   └── HomeController.cs
└── Web.config                   # Spring activation configuration
```

### 2. Activate Spring in Web.config

Check the `SpringNet.Web/Web.config` file:

```xml
<configSections>
    <sectionGroup name="spring">
        <!-- Register Spring Context Handler -->
        <section name="context"
                 type="Spring.Context.Support.WebContextHandler, Spring.Web" />
    </sectionGroup>
</configSections>

<spring>
    <context>
        <!-- Specify Spring configuration file location -->
        <resource uri="~/Config/applicationContext.xml" />
    </context>
</spring>
```

**Explanation**:
-   `<configSections>`: Defines Spring.NET configuration sections.
-   `<spring><context>`: Spring context configuration.
-   `<resource uri="...">`: Path to the configuration file.

### 3. applicationContext.xml - Bean Definition

`SpringNet.Web/Config/applicationContext.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://www.springframework.net
         http://www.springframework.net/xsd/spring/spring-objects.xsd">

    <!-- Bean definition example 1: Simple string -->
    <object id="testService" type="System.String">
        <constructor-arg value="Spring.NET is working!" />
    </object>

    <!-- Bean definition example 2: HomeController -->
    <object id="homeController"
            type="SpringNet.Web.Controllers.HomeController, SpringNet.Web">
        <!-- Property injection -->
        <property name="TestService" ref="testService" />
    </object>

</objects>
```

**Bean Definition Structure**:

```xml
<object id="beanID" type="FullClassName, AssemblyName">
    <!-- Constructor injection -->
    <constructor-arg value="value" />
    <constructor-arg ref="anotherBeanID" />

    <!-- Property injection -->
    <property name="PropertyName" value="value" />
    <property name="PropertyName" ref="anotherBeanID" />
</object>
```

### 4. Global.asax.cs - Spring MVC Integration

Open `SpringNet.Web/Global.asax.cs` and modify the `MvcApplication` class to inherit from `Spring.Web.Mvc.SpringMvcApplication`. This streamlines the integration between Spring.NET and ASP.NET MVC, eliminating the need to manually set the `ControllerFactory`.

```csharp
using System.Web.Mvc;
using System.Web.Routing;
using Spring.Web.Mvc; // Required for SpringMvcApplication

namespace SpringNet.Web
{
    // Inherit from SpringMvcApplication instead of System.Web.HttpApplication
    public class MvcApplication : SpringMvcApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // The ControllerBuilder.Current.SetControllerFactory(...) line is no longer needed
            // as SpringMvcApplication automatically configures the ControllerFactory.
        }
    }
}
```

**Key**: `SpringMvcApplication` is a convenient base class that configures Spring.NET to automatically handle controller creation and dependency injection.

### 5. Receiving DI in Controller

The best practice for injecting dependencies into a controller is to use constructor injection. This makes dependencies explicit and ensures that the object is fully initialized upon creation.

`SpringNet.Web/Controllers/HomeController.cs`:
```csharp
using SpringNet.Service; // For IGreetingService
using System.Web.Mvc;

public class HomeController : Controller
{
    // readonly fields to store injected dependencies
    private readonly string testService;
    private readonly IGreetingService greetingService;

    // Constructor Injection: Spring injects beans via this constructor
    public HomeController(string testService, IGreetingService greetingService)
    {
        this.testService = testService;
        this.greetingService = greetingService;
    }

    public ActionResult Index()
    {
        // Use injected services
        ViewBag.Message = testService;
        ViewBag.Greeting = greetingService.GetGreeting("Developer");
        return View();
    }
}
```

## 🧪 Practice: Creating a Simple Service

### Step 1: Define Interface

Create `SpringNet.Service/IGreetingService.cs`:

```csharp
namespace SpringNet.Service
{
    public interface IGreetingService
    {
        string GetGreeting(string name);
    }
}
```

### Step 2: Implement Class

Create `SpringNet.Service/GreetingService.cs`:

```csharp
namespace SpringNet.Service
{
    public class GreetingService : IGreetingService
    {
        private readonly string prefix;

        // Constructor Injection
        public GreetingService(string prefix)
        {
            this.prefix = prefix;
        }

        public string GetGreeting(string name)
        {
            return $"{prefix}, {name}!";
        }
    }
}
```

#### 📢 Update Project Files (Important)

After adding new class files, you need to update the `.csproj` file so Visual Studio recognizes and compiles them.

1.  Delete `Class1.cs` from the `SpringNet.Service` folder.
2.  Open `SpringNet.Service.csproj` in a text editor and modify it as follows:

    Before:
    ```xml
    <ItemGroup>
      <Compile Include="Class1.cs" />
      <Compile Include="Properties\AssemblyInfo.cs" />
    </ItemGroup>
    ```

    After:
    ```xml
    <ItemGroup>
      <Compile Include="GreetingService.cs" />
      <Compile Include="IGreetingService.cs" />
      <Compile Include="Properties\AssemblyInfo.cs" />
    </ItemGroup>
    ```

**Tip**: If you add files directly to the project in Visual Studio's "Solution Explorer", the `.csproj` file is updated automatically. However, if you create files manually using a text editor or other tools, this manual step is necessary.

### Step 3: Register Bean in applicationContext.xml

Modify the XML configuration to match the constructor injection used in `HomeController`.

```xml
    <!-- Register Greeting Service -->
    <object id="greetingService"
            type="SpringNet.Service.GreetingService, SpringNet.Service">
        <constructor-arg name="prefix" value="Hello" />
    </object>

    <!-- Modify HomeController (using constructor injection) -->
    <object id="homeController"
            type="SpringNet.Web.Controllers.HomeController, SpringNet.Web">
        <constructor-arg name="testService" ref="testService" />
        <constructor-arg name="greetingService" ref="greetingService" />
    </object>
</objects>
```

### Step 4: Use in Controller

`HomeController` has already been modified to use constructor injection as described above.

### Step 5: Display in View

`Views/Home/Index.cshtml`:

```html
@{
    ViewBag.Title = "Home Page";
}

<h2>@ViewBag.Message</h2>
<h3>@ViewBag.Greeting</h3>
```

## 🔍 Bean Scope

Spring.NET allows you to control the lifecycle of Beans:

```xml
<!-- Singleton (Default): One instance per application -->
<object id="singletonService"
        type="SpringNet.Service.MyService, SpringNet.Service"
        singleton="true">
</object>

<!-- Prototype: A new instance created for each request -->
<object id="prototypeService"
        type="SpringNet.Service.MyService, SpringNet.Service"
        singleton="false">
</object>

<!-- Request: One instance per HTTP request (web only) -->
<object id="requestService"
        type="SpringNet.Service.MyService, SpringNet.Service"
        scope="request">
</object>

<!-- Session: One instance per HTTP session (web only) -->
<object id="sessionService"
        type="SpringNet.Service.MyService, SpringNet.Service"
        scope="session">
</object>
```

## 📊 Singleton vs. Prototype Comparison

| Feature | Singleton | Prototype |
|---------|-----------|-----------|
| Instances | 1 | New instance per request |
| Memory Usage | Low | High |
| Performance | Fast | Relatively slower |
| State Management | Requires care | Safe |
| Use Cases | Repository, Service | Command objects |

## 💡 Key Summary

### Advantages of Spring.NET

✅ **Loose Coupling**: Interface-based programming.
✅ **Testability**: Easy to inject Mock objects.
✅ **Maintainability**: Easy to swap implementations by changing configuration.
✅ **Reusability**: Beans can be reused and shared.

### Important Concepts

1.  **IoC**: Spring manages object creation/management.
2.  **DI**: Required dependencies are injected externally.
3.  **Bean**: An object managed by Spring.
4.  **Container**: The container that creates/manages Beans.

### XML Configuration Core

-   `<object>`: Bean definition.
-   `id`: Unique identifier for the Bean.
-   `type`: Full class name (Namespace.ClassName, AssemblyName).
-   `<constructor-arg>`: Constructor injection.
-   `<property>`: Property injection.
-   `ref`: Reference to another Bean.
-   `value`: Literal value.

## 🎯 Practice Problems

### Problem 1: Create a Calculator Service

1.  Create `ICalculatorService` interface.
2.  Implement `CalculatorService` (Add, Subtract methods).
3.  Register in applicationContext.xml.
4.  Use in HomeController.

### Problem 2: Multilingual Support

1.  Create `IMessageService` interface.
2.  Implement `KoreanMessageService`, `EnglishMessageService`.
3.  Test by switching between them in XML comments.

### Problem 3: Scope Experiment

1.  Register two instances of the same service as Singleton and Prototype.
2.  Call them multiple times from the Controller and compare instances.
3.  Use `GetHashCode()` to compare objects.

## ❓ Frequently Asked Questions

**Q1: XML configuration is cumbersome, are there other ways?**
A: Spring.NET 3.0+ also supports Attribute-based configuration, but XML is often clearer and easier to change.

**Q2: Can Bean IDs and class names be different?**
A: Yes, Bean IDs can be arbitrarily assigned. camelCase is commonly used.

**Q3: How are circular references handled?**
A: Spring.NET detects circular references and throws an error. You need to change your design.

## 🚀 Next Steps

You've now understood the basic concepts of Spring.NET!

Next Step: **[02-dependency-injection_en.md](./02-dependency-injection_en.md)** for a deeper dive into Dependency Injection.

---

**Dependency Injection is the core of Spring.NET. Practice it thoroughly!**
