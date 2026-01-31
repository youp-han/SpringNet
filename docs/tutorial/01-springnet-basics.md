# 01. Spring.NET 기본 개념

## 📚 학습 목표

- Spring.NET 프레임워크의 핵심 개념 이해
- IoC (Inversion of Control) 원리 이해
- DI (Dependency Injection) 기본 개념
- Spring.NET 설정 방법 (XML 기반)

## 🎯 Spring.NET이란?

Spring.NET은 .NET 플랫폼을 위한 **엔터프라이즈 애플리케이션 프레임워크**입니다. Java의 Spring Framework를 .NET으로 포팅한 것으로, 객체 지향 프로그래밍의 베스트 프랙티스를 쉽게 적용할 수 있도록 도와줍니다.

### 주요 특징

- **경량 컨테이너**: 객체 생성 및 생명주기 관리
- **의존성 주입**: 객체 간 결합도 감소
- **AOP (Aspect-Oriented Programming)**: 횡단 관심사 분리
- **트랜잭션 관리**: 선언적 트랜잭션 처리

## 💡 핵심 개념

### 1. IoC (Inversion of Control) - 제어의 역전

**전통적인 방식** (개발자가 객체를 직접 생성):

```csharp
// 나쁜 예: 직접 의존성 생성 (강한 결합)
public class OrderService
{
    private EmailService emailService;

    public OrderService()
    {
        // OrderService가 EmailService를 직접 생성
        emailService = new EmailService();
    }

    public void PlaceOrder(Order order)
    {
        // 주문 처리...
        emailService.SendConfirmation(order);
    }
}
```

**문제점**:
- ❌ OrderService가 EmailService의 구체적인 구현에 의존
- ❌ 테스트가 어려움 (Mock 객체 사용 불가)
- ❌ EmailService 변경 시 OrderService도 수정 필요

**IoC 방식** (Spring이 객체를 생성하고 주입):

```csharp
// 좋은 예: 의존성 주입 (느슨한 결합)
public class OrderService
{
    private IEmailService emailService;

    // Spring이 생성자를 통해 의존성 주입
    public OrderService(IEmailService emailService)
    {
        this.emailService = emailService;
    }

    public void PlaceOrder(Order order)
    {
        // 주문 처리...
        emailService.SendConfirmation(order);
    }
}
```

**장점**:
- ✅ 인터페이스에 의존 (느슨한 결합)
- ✅ 테스트 용이 (Mock 주입 가능)
- ✅ 유연성 증가 (구현 교체 쉬움)

### 2. DI (Dependency Injection) - 의존성 주입

의존성 주입은 **객체가 필요로 하는 의존성을 외부에서 주입**하는 패턴입니다.

#### DI의 3가지 방법

**① 생성자 주입 (Constructor Injection)** - **가장 권장**

```csharp
public class ProductService
{
    private readonly IProductRepository repository;

    // 생성자를 통한 주입
    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }
}
```

**② 프로퍼티 주입 (Property Injection)**

```csharp
public class ProductService
{
    // 프로퍼티를 통한 주입
    public IProductRepository Repository { get; set; }

    public void DoSomething()
    {
        var products = Repository.GetAll();
    }
}
```

**③ 메서드 주입 (Method Injection)**

```csharp
public class ProductService
{
    private IProductRepository repository;

    // 메서드를 통한 주입
    public void SetRepository(IProductRepository repository)
    {
        this.repository = repository;
    }
}
```

### 3. Spring Container (IoC Container)

Spring Container는 **객체의 생성, 구성, 관리를 담당**하는 핵심 컴포넌트입니다.

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
│  │  - Bean 생성         │   │
│  │  - 의존성 주입       │   │
│  └──────────────────────┘   │
│           ↓                 │
│  ┌──────────────────────┐   │
│  │  Managed Beans       │   │
│  │  (서비스, 리포지토리)│   │
│  └──────────────────────┘   │
└─────────────────────────────┘
```

## 🛠️ Spring.NET 설정 실습

### 1. 프로젝트 구조 확인

현재 프로젝트:
```
SpringNet.Web/
├── Config/
│   └── applicationContext.xml  # Spring 설정 파일
├── Controllers/
│   └── HomeController.cs
└── Web.config                   # Spring 활성화 설정
```

### 2. Web.config에서 Spring 활성화

`SpringNet.Web/Web.config` 파일을 확인하세요:

```xml
<configSections>
    <sectionGroup name="spring">
        <!-- Spring Context Handler 등록 -->
        <section name="context"
                 type="Spring.Context.Support.WebContextHandler, Spring.Web" />
    </sectionGroup>
</configSections>

<spring>
    <context>
        <!-- Spring 설정 파일 위치 지정 -->
        <resource uri="~/Config/applicationContext.xml" />
    </context>
</spring>
```

**설명**:
- `<configSections>`: Spring.NET 설정 섹션 정의
- `<spring><context>`: Spring 컨텍스트 설정
- `<resource uri="...">`: 설정 파일 경로

### 3. applicationContext.xml - Bean 정의

`SpringNet.Web/Config/applicationContext.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://www.springframework.net
         http://www.springframework.net/xsd/spring/spring-objects.xsd">

    <!-- Bean 정의 예제 1: 단순 문자열 -->
    <object id="testService" type="System.String">
        <constructor-arg value="Spring.NET is working!" />
    </object>

    <!-- Bean 정의 예제 2: HomeController -->
    <object id="homeController"
            type="SpringNet.Web.Controllers.HomeController, SpringNet.Web">
        <!-- 프로퍼티 주입 -->
        <property name="TestService" ref="testService" />
    </object>

</objects>
```

**Bean 정의 구조**:

```xml
<object id="빈ID" type="전체클래스명, 어셈블리명">
    <!-- 생성자 주입 -->
    <constructor-arg value="값" />
    <constructor-arg ref="다른빈ID" />

    <!-- 프로퍼티 주입 -->
    <property name="프로퍼티명" value="값" />
    <property name="프로퍼티명" ref="다른빈ID" />
</object>
```

### 4. Global.asax.cs - Spring MVC 통합

`SpringNet.Web/Global.asax.cs` 파일을 열어 `MvcApplication` 클래스가 `Spring.Web.Mvc.SpringMvcApplication`을 상속하도록 수정합니다. 이렇게 하면 Spring.NET과 ASP.NET MVC의 통합이 더 원활해지고, `ControllerFactory`를 수동으로 설정할 필요가 없습니다.

```csharp
using System.Web.Mvc;
using System.Web.Routing;
using Spring.Web.Mvc; // SpringMvcApplication을 위해 필요

namespace SpringNet.Web
{
    // System.Web.HttpApplication 대신 SpringMvcApplication을 상속
    public class MvcApplication : SpringMvcApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // SpringMvcApplication이 ControllerFactory를 자동으로 설정해 주므로
            // ControllerBuilder.Current.SetControllerFactory(...) 코드가 더 이상 필요 없음
        }
    }
}
```

**핵심**: `SpringMvcApplication`은 Spring.NET이 컨트롤러의 생성과 의존성 주입을 자동으로 처리하도록 설정해주는 편리한 기반 클래스입니다.

### 5. Controller에서 DI 받기

컨트롤러에서 의존성을 주입받는 방법은 생성자 주입을 사용하는 것이 가장 좋습니다. 이는 의존성이 명확하게 드러나고, 객체가 생성될 때 모든 의존성이 준비되었음을 보장합니다.

`SpringNet.Web/Controllers/HomeController.cs`:
```csharp
using SpringNet.Service; // IGreetingService 사용
using System.Web.Mvc;

public class HomeController : Controller
{
    // 의존성을 저장할 readonly 필드
    private readonly string testService;
    private readonly IGreetingService greetingService;

    // 생성자 주입: Spring이 이 생성자를 통해 Bean들을 주입
    public HomeController(string testService, IGreetingService greetingService)
    {
        this.testService = testService;
        this.greetingService = greetingService;
    }

    public ActionResult Index()
    {
        // 주입된 서비스 사용
        ViewBag.Message = testService;
        ViewBag.Greeting = greetingService.GetGreeting("개발자");
        return View();
    }
}
```

## 🧪 실습: 간단한 서비스 만들기

### Step 1: 인터페이스 정의

`SpringNet.Service/IGreetingService.cs` 생성:

```csharp
namespace SpringNet.Service
{
    public interface IGreetingService
    {
        string GetGreeting(string name);
    }
}
```

### Step 2: 구현 클래스 작성

`SpringNet.Service/GreetingService.cs` 생성:

```csharp
namespace SpringNet.Service
{
    public class GreetingService : IGreetingService
    {
        private readonly string prefix;

        // 생성자 주입
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

#### 📢 프로젝트 파일 업데이트 (중요)

새로운 클래스 파일을 추가한 후에는 Visual Studio가 이를 인식하고 컴파일할 수 있도록 `.csproj` 파일을 업데이트해야 합니다.

1.  `SpringNet.Service` 폴더에서 `Class1.cs` 파일을 삭제합니다.
2.  `SpringNet.Service.csproj` 파일을 텍스트 편집기에서 열고 다음을 수정합니다.

    기존:
    ```xml
    <ItemGroup>
      <Compile Include="Class1.cs" />
      <Compile Include="Properties\AssemblyInfo.cs" />
    </ItemGroup>
    ```

    변경:
    ```xml
    <ItemGroup>
      <Compile Include="GreetingService.cs" />
      <Compile Include="IGreetingService.cs" />
      <Compile Include="Properties\AssemblyInfo.cs" />
    </ItemGroup>
    ```

**팁**: Visual Studio의 "솔루션 탐색기"에서 프로젝트에 파일을 직접 추가하면 `.csproj` 파일이 자동으로 업데이트됩니다. 하지만 텍스트 편집기나 다른 도구를 사용해 수동으로 파일을 생성했다면 이 과정이 필요합니다.

### Step 3: applicationContext.xml에 Bean 등록

`HomeController`의 생성자 주입에 맞게 XML 설정을 수정합니다.

```xml
    <!-- 인사말 서비스 등록 -->
    <object id="greetingService"
            type="SpringNet.Service.GreetingService, SpringNet.Service">
        <constructor-arg name="prefix" value="안녕하세요" />
    </object>

    <!-- HomeController 수정 (생성자 주입 사용) -->
    <object id="homeController"
            type="SpringNet.Web.Controllers.HomeController, SpringNet.Web">
        <constructor-arg name="testService" ref="testService" />
        <constructor-arg name="greetingService" ref="greetingService" />
    </object>
</objects>
```

### Step 4: Controller에서 사용

`HomeController`는 이미 위에서 생성자 주입을 사용하도록 수정되었습니다.

### Step 5: View에서 표시

`Views/Home/Index.cshtml`:

```html
@{
    ViewBag.Title = "Home Page";
}

<h2>@ViewBag.Message</h2>
<h3>@ViewBag.Greeting</h3>
```

## 🔍 Bean Scope (범위)

Spring.NET은 Bean의 생명주기를 제어할 수 있습니다:

```xml
<!-- Singleton (기본값): 애플리케이션당 1개 인스턴스 -->
<object id="singletonService"
        type="SpringNet.Service.MyService, SpringNet.Service"
        singleton="true">
</object>

<!-- Prototype: 요청마다 새 인스턴스 생성 -->
<object id="prototypeService"
        type="SpringNet.Service.MyService, SpringNet.Service"
        singleton="false">
</object>

<!-- Request: HTTP 요청당 1개 (웹 전용) -->
<object id="requestService"
        type="SpringNet.Service.MyService, SpringNet.Service"
        scope="request">
</object>

<!-- Session: HTTP 세션당 1개 (웹 전용) -->
<object id="sessionService"
        type="SpringNet.Service.MyService, SpringNet.Service"
        scope="session">
</object>
```

## 📊 Singleton vs Prototype 비교

| 특징 | Singleton | Prototype |
|------|-----------|-----------|
| 인스턴스 수 | 1개 | 요청마다 새로 생성 |
| 메모리 사용 | 적음 | 많음 |
| 성능 | 빠름 | 상대적으로 느림 |
| 상태 유지 | 주의 필요 | 안전 |
| 사용 예 | Repository, Service | Command 객체 |

## 💡 핵심 정리

### Spring.NET의 장점

✅ **느슨한 결합**: 인터페이스 기반 프로그래밍
✅ **테스트 용이**: Mock 객체 주입 가능
✅ **유지보수성**: 설정 변경만으로 구현 교체
✅ **재사용성**: Bean 재사용 및 공유

### 중요 개념

1. **IoC**: 객체 생성/관리를 Spring이 담당
2. **DI**: 필요한 의존성을 외부에서 주입
3. **Bean**: Spring이 관리하는 객체
4. **Container**: Bean을 생성/관리하는 컨테이너

### XML 설정 핵심

- `<object>`: Bean 정의
- `id`: Bean의 고유 식별자
- `type`: 클래스 전체 이름 (네임스페이스.클래스명, 어셈블리명)
- `<constructor-arg>`: 생성자 주입
- `<property>`: 프로퍼티 주입
- `ref`: 다른 Bean 참조
- `value`: 리터럴 값

## 🎯 연습 문제

### 문제 1: 계산기 서비스 만들기

1. `ICalculatorService` 인터페이스 생성
2. `CalculatorService` 구현 (Add, Subtract 메서드)
3. applicationContext.xml에 등록
4. HomeController에서 사용

### 문제 2: 다국어 지원

1. `IMessageService` 인터페이스 생성
2. `KoreanMessageService`, `EnglishMessageService` 구현
3. XML에서 주석으로 하나씩 전환하며 테스트

### 문제 3: Scope 실험

1. Singleton과 Prototype으로 같은 서비스 2개 등록
2. Controller에서 여러 번 호출하여 인스턴스 비교
3. GetHashCode()로 객체 비교

## ❓ 자주 하는 질문

**Q1: XML 설정이 번거로운데 다른 방법은?**
A: Spring.NET 3.0+는 Attribute 기반 설정도 지원하지만, XML이 더 명확하고 변경이 쉽습니다.

**Q2: Bean ID와 클래스명이 달라도 되나요?**
A: 네, Bean ID는 임의로 지정 가능합니다. 일반적으로 camelCase를 사용합니다.

**Q3: 순환 참조는 어떻게 처리하나요?**
A: Spring.NET은 순환 참조를 감지하고 에러를 발생시킵니다. 설계를 변경해야 합니다.

## 🚀 다음 단계

이제 Spring.NET의 기본 개념을 이해했습니다!

다음 단계: **[02-dependency-injection.md](./02-dependency-injection.md)**에서 의존성 주입을 더 깊이 실습합니다.

---

**질문이나 문제가 있으면 설정 파일을 다시 확인하세요!**
