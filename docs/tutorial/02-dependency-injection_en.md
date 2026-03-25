# 02. Deep Dive into Dependency Injection

## 📚 Learning Objectives

-   Practice various Dependency Injection methods.
-   Compare Constructor Injection vs. Property Injection.
-   Configure complex object graphs.
-   Inject collections (List, Dictionary).
-   Real-world example: Implementing a logging service.

## 🎯 Why is Dependency Injection Necessary?

### Anti-pattern: Excessive Use of the `new` Operator

```csharp
// ❌ Bad example: Tight coupling
public class OrderService
{
    private EmailService emailService = new EmailService();
    private SmsService smsService = new SmsService();
    private OrderRepository repository = new OrderRepository();

    public void PlaceOrder(Order order)
    {
        repository.Save(order);
        emailService.Send(order.CustomerEmail, "Order Complete");
        smsService.Send(order.CustomerPhone, "Order Complete");
    }
}
```

**Problems**:
1.  ❌ **Untestable**: Cannot use Mock objects.
2.  ❌ **Inflexible**: Difficult to change implementations.
3.  ❌ **Hard to Reuse**: Services create dependencies directly.
4.  ❌ **Scattered Configuration**: Each class needs its own configuration.

### Good Pattern: Dependency Injection

```csharp
// ✅ Good example: Loose coupling
public class OrderService
{
    private readonly IEmailService emailService;
    private readonly ISmsService smsService;
    private readonly IOrderRepository repository;

    // Constructor Injection
    public OrderService(
        IEmailService emailService,
        ISmsService smsService,
        IOrderRepository repository)
    {
        this.emailService = emailService;
        this.smsService = smsService;
        this.repository = repository;
    }

    public void PlaceOrder(Order order)
    {
        repository.Save(order);
        emailService.Send(order.CustomerEmail, "Order Complete");
        smsService.Send(order.CustomerPhone, "Order Complete");
    }
}
```

**Advantages**:
1.  ✅ **Testable**: Can inject Mocks.
2.  ✅ **Flexible**: Easy to swap interfaces.
3.  ✅ **Reusable**: Services can be shared.
4.  ✅ **Centralized Configuration**: Managed uniformly in XML.

## 🛠️ Detailed Dependency Injection Methods

### 1. Constructor Injection ⭐ Recommended

Constructor Injection is used for **mandatory dependencies** and is the most recommended method.

#### Code Example

```csharp
namespace SpringNet.Service
{
    public interface ILogService
    {
        void Log(string message);
    }

    public class FileLogService : ILogService
    {
        private string logPath;

        // Dependency injected via constructor
        public FileLogService(string logPath)
        {
            this.logPath = logPath;
        }

        public void Log(string message)
        {
            File.AppendAllText(logPath,
                $("[{DateTime.Now}] {message}\n"));
        }
    }
}
```

#### XML Configuration

```xml
<!-- Constructor Injection -->
<object id="logService"
        type="SpringNet.Service.FileLogService, SpringNet.Service">
    <!-- index: parameter order (0-based) -->
    <constructor-arg index="0" value="C:/logs/app.log" />
</object>
```

#### Injecting Multiple Parameters

```csharp
public class EmailService : IEmailService
{
    private string smtpServer;
    private int port;
    private string username;
    private string password;

    public EmailService(string smtpServer, int port,
                        string username, string password)
    {
        this.smtpServer = smtpServer;
        this.port = port;
        this.username = username;
        this.password = password;
    }
}
```

```xml
<object id="emailService"
        type="SpringNet.Service.EmailService, SpringNet.Service">
    <constructor-arg index="0" value="smtp.gmail.com" />
    <constructor-arg index="1" value="587" />
    <constructor-arg index="2" value="user@example.com" />
    <constructor-arg index="3" value="password123" />
</object>
```

#### Referencing Other Beans

```csharp
public class NotificationService
{
    private IEmailService emailService;
    private ILogService logService;

    public NotificationService(IEmailService emailService,
                               ILogService logService)
    {
        this.emailService = emailService;
        this.logService = logService;
    }
}
```

```xml
<object id="notificationService"
        type="SpringNet.Service.NotificationService, SpringNet.Service">
    <!-- ref: Reference another Bean -->
    <constructor-arg ref="emailService" />
    <constructor-arg ref="logService" />
</object>
```

### 2. Property Injection

Property Injection is used for **optional dependencies**.

#### Code Example

```csharp
public class ProductService
{
    // Mandatory dependency - Constructor Injection
    private readonly IProductRepository repository;

    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }

    // Optional dependency - Property Injection
    public ILogService Logger { get; set; }
    public ICacheService Cache { get; set; }

    public Product GetProduct(int id)
    {
        Logger?.Log($"Getting product {id}");

        // Check cache
        var cached = Cache?.Get($"product_{id}");
        if (cached != null) return cached as Product;

        // Query DB
        var product = repository.GetById(id);
        Cache?.Set($"product_{id}", product);

        return product;
    }
}
```

#### XML Configuration

```xml
<object id="productService"
        type="SpringNet.Service.ProductService, SpringNet.Service">
    <!-- Constructor Injection (mandatory) -->
    <constructor-arg ref="productRepository" />

    <!-- Property Injection (optional) -->
    <property name="Logger" ref="logService" />
    <property name="Cache" ref="cacheService" />
</object>
```

### 3. Method Injection

Rarely used, but for special cases.

```csharp
public class ReportService
{
    private IDataSource dataSource;

    public void SetDataSource(IDataSource dataSource)
    {
        this.dataSource = dataSource;
    }
}
```

```xml
<object id="reportService"
        type="SpringNet.Service.ReportService, SpringNet.Service">
    <property name="SetDataSource" ref="dataSource" />
</object>
```

## 📦 Collection Injection

### List Injection

```csharp
public class NotificationManager
{
    private IList<INotificationChannel> channels;

    public NotificationManager(IList<INotificationChannel> channels)
    {
        this.channels = channels;
    }

    public void NotifyAll(string message)
    {
        foreach (var channel in channels)
        {
            channel.Send(message);
        }
    }
}
```

```xml
<object id="notificationManager"
        type="SpringNet.Service.NotificationManager, SpringNet.Service">
    <constructor-arg name="channels">
        <list element-type="SpringNet.Service.INotificationChannel, SpringNet.Service">
            <ref object="emailService" />
            <ref object="smsService" />
            <ref object="pushService" />
        </list>
    </constructor-arg>
</object>
```

### Dictionary Injection

```csharp
public class ConfigurationService
{
    private IDictionary<string, string> settings;

    public ConfigurationService(IDictionary<string, string> settings)
    {
        this.settings = settings;
    }

    public string GetSetting(string key)
    {
        return settings.ContainsKey(key) ? settings[key] : null;
    }
}
```

```xml
<object id="configService"
        type="SpringNet.Service.ConfigurationService, SpringNet.Service">
    <constructor-arg name="settings">
        <dictionary key-type="string" value-type="string">
            <entry key="AppName" value="SpringNet App" />
            <entry key="Version" value="1.0.0" />
            <entry key="MaxUsers" value="1000" />
        </dictionary>
    </constructor-arg>
</object>
```

## 🧪 Real-world Example: Building a Logging System

### Step 1: Create Interface and Implementation Classes

`SpringNet.Service/Logging/ILogger.cs`:

```csharp
namespace SpringNet.Service.Logging
{
    public interface ILogger
    {
        void Debug(string message);
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}
```

`SpringNet.Service/Logging/FileLogger.cs`:

```csharp
using System;
using System.IO;

namespace SpringNet.Service.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string logFilePath;
        private readonly string appName;

        public FileLogger(string logFilePath, string appName)
        {
            this.logFilePath = logFilePath;
            this.appName = appName;

            // Create directory
            var directory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void Debug(string message) => Log("DEBUG", message);
        public void Info(string message) => Log("INFO", message);
        public void Warning(string message) => Log("WARNING", message);
        public void Error(string message) => Log("ERROR", message);

        private void Log(string level, string message)
        {
            var logEntry = $("[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                          "[{appName}] [{level}] {message}\n");

            File.AppendAllText(logFilePath, logEntry);
        }
    }
}
```

`SpringNet.Service/Logging/ConsoleLogger.cs`:

```csharp
using System;

namespace SpringNet.Service.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void Debug(string message)
            => Console.WriteLine($"[DEBUG] {message}");

        public void Info(string message)
            => Console.WriteLine($"[INFO] {message}");

        public void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARNING] {message}");
            Console.ResetColor();
        }

        public void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }
    }
}
```

### Step 2: Create Composite Logger

`SpringNet.Service/Logging/CompositeLogger.cs`:

```csharp
using System.Collections.Generic;

namespace SpringNet.Service.Logging
{
    public class CompositeLogger : ILogger
    {
        private readonly IList<ILogger> loggers;

        public CompositeLogger(IList<ILogger> loggers)
        {
            this.loggers = loggers;
        }

        public void Debug(string message)
        {
            foreach (var logger in loggers)
                logger.Debug(message);
        }

        public void Info(string message)
        {
            foreach (var logger in loggers)
                logger.Info(message);
        }

        public void Warning(string message)
        {
            foreach (var logger in loggers)
                logger.Warning(message);
        }

        public void Error(string message)
        {
            foreach (var logger in loggers)
                logger.Error(message);
        }
    }
}
```

#### 📢 Update Project Files

We already modified `SpringNet.Service.csproj` in Tutorial 01 to include the created files. Now, let's add the new logger files.

Open `SpringNet.Service.csproj` and update the `<ItemGroup>` section as follows:

```xml
<ItemGroup>
  <Compile Include="GreetingService.cs" />
  <Compile Include="IGreetingService.cs" />
  <Compile Include="Logging\CompositeLogger.cs" />
  <Compile Include="Logging\ConsoleLogger.cs" />
  <Compile Include="Logging\FileLogger.cs" />
  <Compile Include="Logging\ILogger.cs" />
  <Compile Include="Properties\AssemblyInfo.cs" />
</ItemGroup>
```
**Note**: Since we created a `Logging` folder and placed the files inside it, `Logging\` is included in the path.

### Step 3: XML Configuration

Open `Config/applicationContext.xml`. Keep the Beans from the previous steps and add the new logger-related Beans, then modify the `homeController` definition. Since `HomeController` has been changed to use constructor injection, the XML configuration must also use `<constructor-arg>` accordingly.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://www.springframework.net
         http://www.springframework.net/xsd/spring/spring-objects.xsd">

    <!-- Beans added in Tutorial 01 -->
    <object id="testService" type="System.String">
        <constructor-arg value="Spring.NET is working!" />
    </object>
    <object id="greetingService"
            type="SpringNet.Service.GreetingService, SpringNet.Service">
        <constructor-arg name="prefix" value="Hello" />
    </object>
    
    <!-- === New additions start here === -->

    <!-- File Logger -->
    <object id="fileLogger"
            type="SpringNet.Service.Logging.FileLogger, SpringNet.Service">
        <constructor-arg name="logFilePath" value="C:/logs/springnet.log" />
        <constructor-arg name="appName" value="SpringNetApp" />
    </object>

    <!-- Console Logger -->
    <object id="consoleLogger"
            type="SpringNet.Service.Logging.ConsoleLogger, SpringNet.Service">
    </object>

    <!-- Composite Logger (Combines multiple Loggers) -->
    <object id="logger"
            type="SpringNet.Service.Logging.CompositeLogger, SpringNet.Service">
        <constructor-arg name="loggers">
            <list element-type="SpringNet.Service.Logging.ILogger, SpringNet.Service">
                <ref object="fileLogger" />
                <ref object="consoleLogger" />
            </list>
        </constructor-arg>
    </object>

    <!-- HomeController injects all dependencies via constructor -->
    <object id="homeController"
            type="SpringNet.Web.Controllers.HomeController, SpringNet.Web">
        <constructor-arg name="testService" ref="testService" />
        <constructor-arg name="greetingService" ref="greetingService" />
        <constructor-arg name="logger" ref="logger" />
    </object>

</objects>
```

**Key Changes**:
1.  Defined `fileLogger` and `consoleLogger` Beans, and a `logger` Bean which is a `CompositeLogger` combining them.
2.  Modified the `homeController` Bean to use `<constructor-arg>` instead of `<property>` to inject all dependencies (`testService`, `greetingService`, `logger`) via its constructor.

### Step 4: Use in Controller

Open `Controllers/HomeController.cs` and modify it to receive `ILogger` via constructor injection.

```csharp
using SpringNet.Service;
using SpringNet.Service.Logging;
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly string testService;
        private readonly IGreetingService greetingService;
        private readonly ILogger logger;

        // Inject all dependencies via constructor
        public HomeController(
            string testService,
            IGreetingService greetingService,
            ILogger logger)
        {
            this.testService = testService;
            this.greetingService = greetingService;
            this.logger = logger;
        }

        public ActionResult Index()
        {
            logger.Info("Index page accessed");

            ViewBag.Message = testService;
            ViewBag.Greeting = greetingService.GetGreeting("Developer");
            
            return View();
        }

        public ActionResult About()
        {
            logger.Debug("About page accessed");

            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            logger.Warning("Contact page accessed");

            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}
```

## 🔄 Constructor Injection vs. Property Injection

### Comparison Table

| Criterion | Constructor Injection | Property Injection |
|-----------|-----------------------|--------------------|
| **Usage** | Mandatory dependencies | Optional dependencies |
| **Immutability** | Can be `readonly` (recommended) | Mutable |
| **Null Check** | Not needed | Needed (e.g., `?.` operator) |
| **Testability** | Clear Mock injection | Configured for Mock injection when needed |
| **Readability** | Dependencies explicit | Can be hidden |
| **Recommendation** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |

### Selection Guide

```csharp
public class MyService
{
    // ✅ Constructor Injection: Mandatory dependencies
    private readonly IRepository repository;
    private readonly IValidator validator;

    public MyService(IRepository repository, IValidator validator)
    {
        this.repository = repository;
        this.validator = validator;
    }

    // ✅ Property Injection: Optional dependencies
    public ILogger Logger { get; set; }
    public ICache Cache { get; set; }

    public void DoSomething()
    {
        // Mandatory dependencies: Use directly
        var data = repository.GetData();
        validator.Validate(data);

        // Optional dependencies: Null check
        Logger?.Info("Operation completed");
        Cache?.Set("key", data);
    }
}
```

## 💡 Dependency Injection Best Practices

### 1. Use Interfaces

```csharp
// ✅ Good example
public class OrderService
{
    private readonly IOrderRepository repository;

    public OrderService(IOrderRepository repository)
    {
        this.repository = repository;
    }
}

// ❌ Bad example
public class OrderService
{
    private readonly OrderRepository repository; // Concrete class

    public OrderService(OrderRepository repository)
    {
        this.repository = repository;
    }
}
```

### 2. Use `readonly`

```csharp
// ✅ Good example: Ensures immutability
public class ProductService
{
    private readonly IProductRepository repository;

    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }
}

// ❌ Bad example: Mutable
public class ProductService
{
    private IProductRepository repository;

    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }
}
```

### 3. Null Validation

```csharp
// ✅ Good example: Validate in constructor
public class OrderService
{
    private readonly IOrderRepository repository;

    public OrderService(IOrderRepository repository)
    {
        this.repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }
}
```

### 4. Beware of Excessive Dependencies

```csharp
// ❌ Bad example: Too many dependencies (potential violation of SRP)
public class HugeService
{
    public HugeService(
        IDep1 dep1, IDep2 dep2, IDep3 dep3,
        IDep4 dep4, IDep5 dep5, IDep6 dep6,
        IDep7 dep7, IDep8 dep8) // Too many!
    {
        // ...
    }
}

// ✅ Good example: Group related dependencies with a Facade
public class BetterService
{
    private readonly IServiceFacade facade;

    public BetterService(IServiceFacade facade)
    {
        this.facade = facade;
    }
}
```

## 🎯 Practice Problems

### Problem 1: Create an Email Sending System

Implement the following requirements:

1.  Create `IEmailSender` interface.
2.  Implement `SmtpEmailSender` (inject SMTP server info via constructor).
3.  Implement `FakeEmailSender` (for testing, outputs to console only).
4.  Test by switching between them in XML comments.

### Problem 2: Multiple Notification Channels

1.  Create `INotificationChannel` interface.
2.  Implement `EmailChannel`, `SmsChannel`, `PushChannel`.
3.  Inject all channels as a List into `NotificationService`.
4.  Send notifications to all channels at once.

### Problem 3: Configuration Management

1.  Inject app settings using a Dictionary.
2.  Create `ConfigurationService`.
3.  Read configuration values in HomeController and display them on screen.

## 💡 Key Summary

### 3 Methods of Dependency Injection

1.  **Constructor Injection**: Mandatory dependencies, `readonly`, most recommended.
2.  **Property Injection**: Optional dependencies, requires null checks.
3.  **Method Injection**: Used only in special cases.

### XML Configuration Summary

```xml
<!-- Constructor Injection -->
<constructor-arg value="literalValue" />
<constructor-arg ref="beanReference" />

<!-- Property Injection -->
<property name="PropertyName" value="literalValue" />
<property name="PropertyName" ref="beanReference" />

<!-- Collection Injection -->
<list element-type="Type">
    <ref object="bean1" />
    <ref object="bean2" />
</list>

<dictionary key-type="string" value-type="string">
    <entry key="key" value="value" />
</dictionary>
```

### Best Practices

✅ Depend on interfaces.
✅ Prioritize constructor injection.
✅ Use the `readonly` keyword.
✅ Validate for null.
✅ Minimize dependencies.

## 🚀 Next Steps

You've mastered Dependency Injection!

Next Step: **[03-nhibernate-setup_en.md](./03-nhibernate-setup_en.md)** to begin NHibernate setup.

---

**Dependency Injection is the core of Spring.NET. Practice it thoroughly!**
