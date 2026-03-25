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

### 0. 의존성(Dependency)이란?

코드를 작성하다 보면 한 객체가 다른 객체를 **사용**해야 하는 상황이 생깁니다. 이때 사용하는 쪽을 **의존하는 객체(dependent)**, 사용되는 쪽을 **의존 대상(dependency)** 이라고 합니다.

```
OrderService  ──사용──▶  EmailService
  (의존하는 쪽)            (의존 대상)
```

의존성 자체는 자연스러운 것입니다. 문제는 **어떻게 의존하느냐**입니다.

#### 강한 결합 (Tight Coupling)

```csharp
public class OrderService
{
    // OrderService가 EmailService를 직접 생성
    // → 구체적인 구현 클래스에 묶임
    private EmailService emailService = new EmailService();
}
```

이렇게 하면:
- `EmailService`를 `SlackService`로 바꾸려면 `OrderService` 코드를 직접 수정해야 함
- 테스트 시 진짜 이메일이 발송되는 것을 막을 방법이 없음
- `EmailService`의 생성자가 바뀌면 `OrderService`도 함께 바뀌어야 함

#### 느슨한 결합 (Loose Coupling)

```csharp
public class OrderService
{
    // 구체 클래스가 아닌 인터페이스에 의존
    private IEmailService emailService;

    // 어떤 구현을 쓸지는 외부에서 결정
    public OrderService(IEmailService emailService)
    {
        this.emailService = emailService;
    }
}
```

이렇게 하면 `OrderService`는 `IEmailService`라는 **계약(contract)** 만 알고, 실제 구현이 무엇인지는 신경 쓰지 않습니다. 구현 교체, 테스트, 확장이 모두 자유로워집니다.

> **📌 핵심**: 의존성 문제의 본질은 "객체를 누가 만드느냐"가 아니라 "어디에 의존하느냐"입니다. 구체 클래스가 아닌 **추상(인터페이스)에 의존**하는 것이 출발점입니다.

---

### 1. IoC (Inversion of Control) - 제어의 역전

#### 개념적 배경

전통적인 프로그래밍에서 **제어의 흐름**은 개발자가 직접 가집니다.

```
개발자 코드
  → 객체 생성 (new EmailService())
  → 의존성 연결 (emailService = new EmailService())
  → 메서드 호출 (emailService.Send())
```

개발자가 모든 것을 직접 orchestrate합니다. 이것은 작은 프로그램에서는 괜찮지만, 시스템이 커질수록 문제가 됩니다. 수십 개의 서비스가 서로 얽히면 **객체 생성 코드만으로도 수백 줄**이 되고, 변경 하나가 연쇄적인 수정을 요구합니다.

**IoC는 이 제어권을 프레임워크에 넘기는 것**입니다.

```
프레임워크(Spring.NET)
  → XML 설정 읽기
  → 객체 생성 (new EmailService(), new OrderService(...))
  → 의존성 연결 (orderService에 emailService 주입)
  → 관리 (생명주기, 스코프)
```

개발자는 **"무엇을 어떻게 연결할지"를 설정 파일에 선언**하고, 실제 생성과 연결은 Spring이 처리합니다.

#### 할리우드 원칙

IoC는 종종 **"할리우드 원칙"** 으로 설명됩니다.

> *"Don't call us, we'll call you."*
> 우리한테 전화하지 마세요, 우리가 전화할게요.

배우(개발자가 만든 클래스)가 영화사(프레임워크)에 연락하는 것이 아니라, 영화사가 필요할 때 배우를 부릅니다. 클래스는 자신이 언제, 어떻게 사용될지 알 필요가 없습니다. 그냥 역할(인터페이스)만 잘 구현하면 됩니다.

#### SOLID와의 연결: DIP (Dependency Inversion Principle)

IoC/DI의 이론적 기반은 SOLID 원칙 중 **D — 의존성 역전 원칙(DIP)** 입니다.

> 1. 고수준 모듈은 저수준 모듈에 의존해서는 안 된다. 둘 다 추상화에 의존해야 한다.
> 2. 추상화는 세부 사항에 의존해서는 안 된다. 세부 사항이 추상화에 의존해야 한다.

```
❌ 전통적 의존 방향:
OrderService(고수준) ──▶ EmailService(저수준, 구체 클래스)

✅ DIP 적용 후:
OrderService(고수준) ──▶ IEmailService(추상화) ◀── EmailService(저수준)
```

`OrderService`는 추상화(`IEmailService`)에만 의존하고, 구체 구현(`EmailService`)도 그 추상화를 향해 의존합니다. **의존의 방향이 역전**됩니다. Spring.NET은 이 원칙을 시스템 전체에 자동으로 적용할 수 있게 해주는 도구입니다.

---

### 2. IoC vs DI — 무엇이 다른가?

이 두 용어는 자주 혼용되지만 의미가 다릅니다.

| 구분 | IoC | DI |
|------|-----|----|
| 종류 | **원칙 / 패턴** | **IoC를 구현하는 기법** |
| 의미 | 제어권을 프레임워크에 넘김 | 의존성을 외부에서 주입함 |
| 관계 | 상위 개념 | IoC의 가장 대표적인 구현 방법 |
| 비유 | "내가 직접 요리하지 않겠다" | "요리사가 음식을 내 테이블에 가져다 준다" |

DI 외에도 IoC를 구현하는 방법은 있습니다 (Service Locator, Factory 등). 하지만 현대적인 프레임워크에서 IoC는 대부분 DI를 통해 구현되며, Spring.NET도 마찬가지입니다.

```
IoC (원칙)
 └─ DI (구현 방법)  ← Spring.NET이 사용하는 방법
 └─ Service Locator (다른 구현 방법)
 └─ Factory Pattern (또 다른 방법)
```

---

### 3. DI (Dependency Injection) - 의존성 주입

의존성 주입은 **객체가 필요로 하는 의존성을 외부에서 주입**하는 패턴입니다.

#### DI의 3가지 방법

**① 생성자 주입 (Constructor Injection)** — **가장 권장**

```csharp
public class ProductService
{
    private readonly IProductRepository repository;

    // 객체 생성 시점에 의존성이 확정됨 → 불변성 보장
    public ProductService(IProductRepository repository)
    {
        this.repository = repository;
    }
}
```

- 의존성이 `null`일 수 없음 (생성 자체가 실패)
- `readonly` 필드로 불변성 보장
- 의존성이 명확하게 드러남 → **이 튜토리얼에서 권장하는 방식**

**② 프로퍼티 주입 (Property Injection)**

```csharp
public class ProductService
{
    // 선택적 의존성에 적합 (없어도 동작 가능한 경우)
    public IProductRepository Repository { get; set; }
}
```

- 선택적 의존성(optional dependency)에 적합
- 의존성이 `null`일 수 있어 NullReferenceException 위험
- 순환 참조 해결에 가끔 사용

**③ 메서드 주입 (Method Injection)**

```csharp
public class ProductService
{
    private IProductRepository repository;

    // 특정 메서드 호출 시점에 주입
    public void SetRepository(IProductRepository repository)
    {
        this.repository = repository;
    }
}
```

- 거의 사용하지 않음
- 런타임에 의존성을 교체해야 하는 특수한 경우

---

### 4. Spring Container — 동작 원리

Spring Container(IoC Container)는 애플리케이션이 시작될 때 다음 순서로 동작합니다.

```
애플리케이션 시작
        │
        ▼
① XML 설정 파싱 (applicationContext.xml)
   - 어떤 Bean이 있는지 읽음
   - 각 Bean의 타입, 의존성, 스코프 파악
        │
        ▼
② 의존성 그래프 구성
   - A가 B를 필요로 하고, B가 C를 필요로 한다면
   - C → B → A 순서로 생성 계획 수립
        │
        ▼
③ Bean 인스턴스 생성
   - 순서대로 객체 생성 (new)
   - 생성자/프로퍼티에 의존성 주입
        │
        ▼
④ Bean 레지스트리에 등록
   - id로 조회 가능한 상태가 됨
   - Singleton Bean은 이 시점에 공유 인스턴스로 보관
        │
        ▼
⑤ 요청 처리 시작 (웹 애플리케이션)
   - SpringControllerFactory가 컨트롤러 요청 시
     컨테이너에서 이미 만들어진 Bean 반환
```

실제 `applicationContext.xml`에서 `homeController`가 `testService`를 참조하면:

```xml
<object id="testService" type="System.String">
    <constructor-arg value="Spring.NET is working!" />
</object>

<object id="homeController" type="...HomeController, SpringNet.Web">
    <constructor-arg ref="testService" />  <!-- testService Bean 참조 -->
</object>
```

Spring은 `testService`를 먼저 만들고, 그것을 인자로 넘겨 `homeController`를 생성합니다. 개발자가 `new`를 한 줄도 쓰지 않아도 됩니다.

---

### 5. 실제 요청의 CRUD 흐름

Spring.NET + NHibernate + ASP.NET MVC로 구성된 이 프로젝트에서 HTTP 요청이 어떻게 처리되는지 전체 흐름입니다.

```
브라우저
  │  HTTP Request (GET /Board/Detail/5)
  ▼
ASP.NET MVC 라우터
  │  URL → Controller/Action 매핑
  ▼
SpringControllerFactory
  │  Spring Container에서 BoardController Bean 꺼냄
  │  (이미 IBoardService가 주입된 상태)
  ▼
BoardController.Detail(id: 5)
  │  비즈니스 로직 호출
  ▼
BoardService.GetBoard(5)          ← Spring이 주입한 IBoardService
  │  유효성 검사, 비즈니스 규칙 처리
  ▼
BoardRepository.GetWithReplies(5) ← Spring이 주입한 IBoardRepository
  │  데이터 조회 요청
  ▼
NHibernate (ORM)
  │  객체 ↔ SQL 변환
  ▼
Database (SQL Server / SQLite)
  │  SELECT * FROM Boards WHERE Id = 5
  ▼
(역방향으로 결과 반환)
  ▼
Board 엔티티 객체
  ▼
BoardDto (Service에서 변환)
  ▼
View (Razor .cshtml)
  ▼
브라우저  HTTP Response (HTML)
```

각 레이어에서 Spring.NET DI가 하는 역할:

| 레이어 | 클래스 | Spring이 주입하는 것 |
|--------|--------|---------------------|
| Controller | `BoardController` | `IBoardService` |
| Service | `BoardService` | `IBoardRepository`, `IReplyRepository`, `ISessionFactory` |
| Repository | `BoardRepository` | `ISessionFactory` |
| NHibernate | `SessionFactory` | `hibernate.cfg.xml` 설정 |

> **핵심 포인트**: 각 레이어는 바로 아래 레이어의 **인터페이스**만 알고 있습니다. `BoardController`는 `BoardService`가 실제로 어떻게 구현됐는지 모릅니다. Spring이 런타임에 적절한 구현체를 연결해줍니다. 이것이 IoC/DI가 만들어내는 **레이어 간 독립성**입니다.

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
        <!-- Spring 설정 파일 위치 지정 (file:// 접두어 필수) -->
        <resource uri="file://~/Config/applicationContext.xml" />
    </context>
</spring>
```

**설명**:
- `<configSections>`: Spring.NET 설정 섹션 정의
- `<spring><context>`: Spring 컨텍스트 설정
- `<resource uri="...">`: 설정 파일 경로. **`file://~/` 접두어**를 붙여야 ASP.NET 웹 애플리케이션 루트 기준으로 파일을 찾습니다.

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

현재 프로젝트의 `Global.asax.cs`는 `SpringControllerFactory`를 수동 등록하는 방식을 사용합니다.

```csharp
// 현재 프로젝트의 Global.asax.cs (수동 방식 - 동작함)
ControllerBuilder.Current.SetControllerFactory(new SpringControllerFactory());
```

이 방식도 완벽히 동작합니다. 이 파일은 **그대로 두어도** 됩니다.

> **참고: SpringMvcApplication 방식 (선택)**
>
> `System.Web.HttpApplication` 대신 `Spring.Web.Mvc.SpringMvcApplication`을 상속하면 `SpringControllerFactory` 수동 등록 없이 동일한 결과를 얻을 수 있습니다. 두 방식 모두 Spring.NET DI가 컨트롤러에 적용됩니다.
>
> ```csharp
> // 선택적 대안: SpringMvcApplication 상속
> public class MvcApplication : SpringMvcApplication
> {
>     protected void Application_Start()
>     {
>         AreaRegistration.RegisterAllAreas();
>         RouteConfig.RegisterRoutes(RouteTable.Routes);
>         // SetControllerFactory 코드 불필요
>     }
> }
> ```

**핵심**: 현재 코드를 그대로 유지하고 다음 단계로 진행하세요. Spring.NET이 이미 컨트롤러에 DI를 적용하고 있습니다.

### 5. Controller에서 DI 받기

컨트롤러에서 의존성을 주입받는 방법은 생성자 주입을 사용하는 것이 가장 좋습니다. 이는 의존성이 명확하게 드러나고, 객체가 생성될 때 모든 의존성이 준비되었음을 보장합니다.

> **현재 `HomeController.cs`는 프로퍼티 주입** (`public string TestService { get; set; }`)을 사용합니다. 이 튜토리얼에서는 이것을 생성자 주입으로 교체합니다. 기존 파일 내용을 **아래 코드로 전체 교체**하세요.

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
