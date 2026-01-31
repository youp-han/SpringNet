# 02. 의존성 주입 (Dependency Injection) 심화

## 📚 학습 목표

- 의존성 주입의 다양한 방법 실습
- 생성자 주입 vs 프로퍼티 주입 비교
- 복잡한 객체 그래프 설정
- 컬렉션 주입 (List, Dictionary)
- 실전 예제: 로깅 서비스 구현

## 🎯 의존성 주입이 필요한 이유

### 안티패턴: new 연산자 남용

```csharp
// ❌ 나쁜 예: 강한 결합
public class OrderService
{
    private EmailService emailService = new EmailService();
    private SmsService smsService = new SmsService();
    private OrderRepository repository = new OrderRepository();

    public void PlaceOrder(Order order)
    {
        repository.Save(order);
        emailService.Send(order.CustomerEmail, "주문 완료");
        smsService.Send(order.CustomerPhone, "주문 완료");
    }
}
```

**문제점**:
1. ❌ **테스트 불가능**: Mock 객체 사용 불가
2. ❌ **유연성 없음**: 구현 변경이 어려움
3. ❌ **재사용 어려움**: 서비스가 직접 생성
4. ❌ **설정 분산**: 각 클래스에서 설정 필요

### 좋은 패턴: 의존성 주입

```csharp
// ✅ 좋은 예: 느슨한 결합
public class OrderService
{
    private readonly IEmailService emailService;
    private readonly ISmsService smsService;
    private readonly IOrderRepository repository;

    // 생성자 주입
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
        emailService.Send(order.CustomerEmail, "주문 완료");
        smsService.Send(order.CustomerPhone, "주문 완료");
    }
}
```

**장점**:
1. ✅ **테스트 가능**: Mock 주입 가능
2. ✅ **유연함**: 인터페이스 교체 쉬움
3. ✅ **재사용성**: 서비스 공유
4. ✅ **중앙 설정**: XML에서 일괄 관리

## 🛠️ 의존성 주입 방법 상세

### 1. 생성자 주입 (Constructor Injection) ⭐ 권장

생성자 주입은 **필수 의존성**에 사용하며, 가장 권장되는 방법입니다.

#### 코드 예제

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

        // 생성자로 의존성 주입
        public FileLogService(string logPath)
        {
            this.logPath = logPath;
        }

        public void Log(string message)
        {
            File.AppendAllText(logPath,
                $"[{DateTime.Now}] {message}\n");
        }
    }
}
```

#### XML 설정

```xml
<!-- 생성자 주입 -->
<object id="logService"
        type="SpringNet.Service.FileLogService, SpringNet.Service">
    <!-- index: 파라미터 순서 (0부터 시작) -->
    <constructor-arg index="0" value="C:/logs/app.log" />
</object>
```

#### 여러 파라미터 주입

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

#### 다른 Bean 참조

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
    <!-- ref: 다른 Bean 참조 -->
    <constructor-arg ref="emailService" />
    <constructor-arg ref="logService" />
</object>
```

### 2. 프로퍼티 주입 (Property Injection)

프로퍼티 주입은 **선택적 의존성**에 사용합니다.

#### 코드 예제

```csharp
public class ProductService
{
    // 필수 의존성 - 생성자 주입
    private readonly IProductRepository repository;

    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }

    // 선택적 의존성 - 프로퍼티 주입
    public ILogService Logger { get; set; }
    public ICacheService Cache { get; set; }

    public Product GetProduct(int id)
    {
        Logger?.Log($"Getting product {id}");

        // 캐시 확인
        var cached = Cache?.Get($"product_{id}");
        if (cached != null) return cached as Product;

        // DB 조회
        var product = repository.GetById(id);
        Cache?.Set($"product_{id}", product);

        return product;
    }
}
```

#### XML 설정

```xml
<object id="productService"
        type="SpringNet.Service.ProductService, SpringNet.Service">
    <!-- 생성자 주입 (필수) -->
    <constructor-arg ref="productRepository" />

    <!-- 프로퍼티 주입 (선택) -->
    <property name="Logger" ref="logService" />
    <property name="Cache" ref="cacheService" />
</object>
```

### 3. 메서드 주입 (Method Injection)

거의 사용되지 않지만, 특수한 경우에 사용합니다.

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

## 📦 컬렉션 주입

### List 주입

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
    <constructor-arg>
        <list element-type="SpringNet.Service.INotificationChannel, SpringNet.Service">
            <ref object="emailService" />
            <ref object="smsService" />
            <ref object="pushService" />
        </list>
    </constructor-arg>
</object>
```

### Dictionary 주입

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
    <constructor-arg>
        <dictionary key-type="string" value-type="string">
            <entry key="AppName" value="SpringNet App" />
            <entry key="Version" value="1.0.0" />
            <entry key="MaxUsers" value="1000" />
        </dictionary>
    </constructor-arg>
</object>
```

## 🧪 실전 예제: 로깅 시스템 구축

### Step 1: 인터페이스 및 구현 클래스 작성

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

            // 디렉토리 생성
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
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                          $"[{appName}] [{level}] {message}\n";

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

### Step 2: 복합 Logger 생성

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

#### 📢 프로젝트 파일 업데이트

튜토리얼 01에서 생성한 파일을 포함하도록 `SpringNet.Service.csproj`를 이미 수정했습니다. 이제 새로운 로거 파일들을 추가해 봅시다.

`SpringNet.Service.csproj` 파일을 열고 `<ItemGroup>` 섹션을 다음과 같이 업데이트합니다.

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
**참고**: `Logging` 폴더를 만들고 그 안에 파일을 넣었으므로, 경로에 `Logging\`이 포함됩니다.

### Step 3: XML 설정

`Config/applicationContext.xml` 파일을 열어 이전 단계의 Bean들은 그대로 두고, 새로운 로거 관련 Bean들을 추가하고 `homeController` 정의를 수정합니다. `HomeController`가 생성자 주입을 사용하도록 변경되었으므로, XML 설정도 이에 맞게 `<constructor-arg>`를 사용해야 합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://www.springframework.net
         http://www.springframework.net/xsd/spring/spring-objects.xsd">

    <!-- 튜토리얼 01에서 추가한 Bean들 -->
    <object id="testService" type="System.String">
        <constructor-arg value="Spring.NET is working!" />
    </object>
    <object id="greetingService"
            type="SpringNet.Service.GreetingService, SpringNet.Service">
        <constructor-arg name="prefix" value="안녕하세요" />
    </object>
    
    <!-- === 여기서부터 새로 추가 === -->

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

    <!-- Composite Logger (여러 Logger 결합) -->
    <object id="logger"
            type="SpringNet.Service.Logging.CompositeLogger, SpringNet.Service">
        <constructor-arg name="loggers">
            <list element-type="SpringNet.Service.Logging.ILogger, SpringNet.Service">
                <ref object="fileLogger" />
                <ref object="consoleLogger" />
            </list>
        </constructor-arg>
    </object>

    <!-- HomeController에 모든 의존성을 생성자로 주입 -->
    <object id="homeController"
            type="SpringNet.Web.Controllers.HomeController, SpringNet.Web">
        <constructor-arg name="testService" ref="testService" />
        <constructor-arg name="greetingService" ref="greetingService" />
        <constructor-arg name="logger" ref="logger" />
    </object>

</objects>
```

**핵심 변경사항**:
1.  `fileLogger`, `consoleLogger`, 그리고 이 둘을 합친 `logger` Bean을 정의했습니다.
2.  `homeController` Bean이 `<property>` 대신 `<constructor-arg>`를 사용하여 모든 의존성(`testService`, `greetingService`, `logger`)을 생성자를 통해 주입받도록 수정했습니다.

### Step 4: Controller에서 사용

`Controllers/HomeController.cs`를 열어 생성자 주입을 통해 `ILogger`를 받도록 수정합니다.

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

        // 생성자를 통해 모든 의존성 주입
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
            ViewBag.Greeting = greetingService.GetGreeting("개발자");
            
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

## 🔄 생성자 주입 vs 프로퍼티 주입

### 비교표

| 기준 | 생성자 주입 | 프로퍼티 주입 |
|------|------------|---------------|
| **용도** | 필수 의존성 | 선택적 의존성 |
| **불변성** | readonly 가능 (권장) | 변경 가능 |
| **Null 체크** | 불필요 | 필요 (?. 연산자) |
| **테스트** | Mock 주입 명확 | Mock 주입 필요 시 설정 |
| **가독성** | 의존성 명확 | 숨겨질 수 있음 |
| **권장도** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |

### 선택 가이드

```csharp
public class MyService
{
    // ✅ 생성자 주입: 필수 의존성
    private readonly IRepository repository;
    private readonly IValidator validator;

    public MyService(IRepository repository, IValidator validator)
    {
        this.repository = repository;
        this.validator = validator;
    }

    // ✅ 프로퍼티 주입: 선택적 의존성
    public ILogger Logger { get; set; }
    public ICache Cache { get; set; }

    public void DoSomething()
    {
        // 필수 의존성: 바로 사용
        var data = repository.GetData();
        validator.Validate(data);

        // 선택적 의존성: Null 체크
        Logger?.Info("Operation completed");
        Cache?.Set("key", data);
    }
}
```

## 💡 의존성 주입 베스트 프랙티스

### 1. 인터페이스 사용

```csharp
// ✅ 좋은 예
public class OrderService
{
    private readonly IOrderRepository repository;

    public OrderService(IOrderRepository repository)
    {
        this.repository = repository;
    }
}

// ❌ 나쁜 예
public class OrderService
{
    private readonly OrderRepository repository; // 구체 클래스

    public OrderService(OrderRepository repository)
    {
        this.repository = repository;
    }
}
```

### 2. readonly 사용

```csharp
// ✅ 좋은 예: 불변성 보장
public class ProductService
{
    private readonly IProductRepository repository;

    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }
}

// ❌ 나쁜 예: 변경 가능
public class ProductService
{
    private IProductRepository repository;

    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }
}
```

### 3. Null 검증

```csharp
// ✅ 좋은 예: 생성자에서 검증
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

### 4. 과도한 의존성 주의

```csharp
// ❌ 나쁜 예: 의존성이 너무 많음 (SRP 위반 가능성)
public class HugeService
{
    public HugeService(
        IDep1 dep1, IDep2 dep2, IDep3 dep3,
        IDep4 dep4, IDep5 dep5, IDep6 dep6,
        IDep7 dep7, IDep8 dep8) // 너무 많음!
    {
        // ...
    }
}

// ✅ 좋은 예: 관련 의존성을 Facade로 묶기
public class BetterService
{
    private readonly IServiceFacade facade;

    public BetterService(IServiceFacade facade)
    {
        this.facade = facade;
    }
}
```

## 🎯 연습 문제

### 문제 1: 이메일 발송 시스템

다음 요구사항을 구현하세요:

1. `IEmailSender` 인터페이스 생성
2. `SmtpEmailSender` 구현 (SMTP 서버 정보를 생성자로 주입)
3. `FakeEmailSender` 구현 (테스트용, 콘솔에만 출력)
4. XML에서 주석으로 전환하며 테스트

### 문제 2: 다중 알림 채널

1. `INotificationChannel` 인터페이스 생성
2. `EmailChannel`, `SmsChannel`, `PushChannel` 구현
3. `NotificationService`에서 List로 모든 채널 주입
4. 한 번에 모든 채널로 알림 발송

### 문제 3: 설정 관리

1. Dictionary를 사용하여 앱 설정 주입
2. `ConfigurationService` 생성
3. HomeController에서 설정 값 읽어서 화면에 표시

## 💡 핵심 정리

### 의존성 주입의 3가지 방법

1. **생성자 주입**: 필수 의존성, readonly, 가장 권장
2. **프로퍼티 주입**: 선택적 의존성, Null 체크 필요
3. **메서드 주입**: 특수한 경우에만 사용

### XML 설정 정리

```xml
<!-- 생성자 주입 -->
<constructor-arg value="리터럴값" />
<constructor-arg ref="빈참조" />

<!-- 프로퍼티 주입 -->
<property name="프로퍼티명" value="리터럴값" />
<property name="프로퍼티명" ref="빈참조" />

<!-- 컬렉션 주입 -->
<list element-type="타입">
    <ref object="빈1" />
    <ref object="빈2" />
</list>

<dictionary key-type="string" value-type="string">
    <entry key="키" value="값" />
</dictionary>
```

### 베스트 프랙티스

✅ 인터페이스에 의존
✅ 생성자 주입 우선
✅ readonly 키워드 사용
✅ Null 검증
✅ 의존성 최소화

## 🚀 다음 단계

의존성 주입을 마스터했습니다!

다음 단계: **[03-nhibernate-setup.md](./03-nhibernate-setup.md)**에서 NHibernate 설정을 시작합니다.

---

**의존성 주입은 Spring.NET의 핵심입니다. 충분히 연습하세요!**
