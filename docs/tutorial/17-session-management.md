# 17. 세션 관리 (NHibernate Session & Web Session)

## 📚 학습 목표

- NHibernate `ISession`과 ASP.NET `Session`의 차이점 이해
- Spring.NET의 자동화된 NHibernate 세션 관리 ("Open Session in View") 이해
- `LazyInitializationException`의 원인과 해결 방안 마스터
- 의존성 주입(DI)을 활용한 ASP.NET Web Session 관리

## 🎯 NHibernate Session vs. Web Session

이 튜토리얼에서는 두 가지 다른 "세션"을 다룹니다. 두 개념을 혼동하지 않는 것이 매우 중요합니다.

-   **NHibernate `ISession`**:
    -   **목적**: 데이터베이스와의 단일 작업 단위(Unit of Work)를 나타냅니다.
    -   **생명주기**: 일반적으로 단일 웹 요청(single web request) 동안 유지됩니다.
    -   **특징**: 1차 캐시(Identity Map)를 제공하고, 엔티티의 변경 사항을 추적하며, 트랜잭션을 관리합니다.

-   **ASP.NET `HttpSessionState` (Web Session)**:
    -   **목적**: 특정 사용자의 상태(state)를 여러 웹 요청에 걸쳐 유지합니다.
    -   **생명주기**: 사용자가 브라우저를 닫거나, 서버에서 설정된 타임아웃이 만료될 때까지 유지됩니다.
    -   **특징**: 로그인 정보, 장바구니 ID 등 사용자별 데이터를 서버 측에 저장합니다.

## 📦 Part 1: NHibernate 세션 관리와 "Open Session in View"

### 1. Spring.NET의 자동화된 세션 관리

이전 튜토리얼에서 우리는 `hibernate.cfg.xml`에 다음과 같이 `current_session_context_class`를 설정했습니다.

```xml
<property name="current_session_context_class">
    Spring.Data.NHibernate.SpringSessionContext, Spring.Data.NHibernate
</property>
```

또한, `Global.asax.cs`에서 `Spring.Web.Mvc.SpringMvcApplication`을 사용하도록 설정했습니다. 이 두 가지 설정을 통해 Spring.NET은 **"Open Session in View" 패턴**을 자동으로 구현합니다.

**Open Session in View 패턴이란?**
웹 요청이 시작될 때 NHibernate 세션을 열고, 요청이 완전히 끝나고 뷰(View) 렌더링이 완료된 후에 세션을 닫는 패턴입니다.

**동작 방식**:
1.  **요청 시작**: Spring.NET이 `ISession`을 생성하고 현재 요청의 컨텍스트에 바인딩합니다.
2.  **서비스 계층**: `[Transaction]` 속성이 붙은 서비스 메서드가 실행되면, Spring은 현재 세션을 사용하여 트랜잭션을 시작합니다.
3.  **서비스 메서드 종료**: 메서드가 성공적으로 끝나면 트랜잭션을 커밋합니다. (세션은 아직 닫지 않습니다.) 예외가 발생하면 롤백합니다.
4.  **컨트롤러 및 뷰**: 컨트롤러는 서비스로부터 받은 영속 엔티티(Lazy-loaded 컬렉션 포함)를 뷰로 전달합니다. 뷰가 렌더링되는 동안 엔티티의 지연 로딩된(Lazy-loaded) 프로퍼티(예: `board.Replies`)에 접근하면, 세션이 아직 열려 있으므로 추가 쿼리가 정상적으로 실행됩니다. `LazyInitializationException`이 발생하지 않습니다.
5.  **요청 종료**: 뷰 렌더링까지 모든 작업이 완료되면, Spring.NET이 세션을 닫고 리소스를 해제합니다.

> **결론**: Spring.NET의 웹 통합 기능을 사용하면 `IHttpModule` 등을 수동으로 구현할 필요 없이, 간단한 설정만으로 `Session-per-Request`와 `Open Session in View` 패턴이 자동으로 적용됩니다.

### 2. `LazyInitializationException` 완벽 해결 가이드

`LazyInitializationException`은 NHibernate 세션이 이미 닫힌 후에 지연 로딩(Lazy Loading)으로 설정된 엔티티의 프로퍼티나 컬렉션에 접근하려고 할 때 발생하는 고전적인 예외입니다.

**문제 상황 예시**:
```csharp
// (Service Layer - 나쁜 예)
public Board GetBoard(int id)
{
    using (var session = sessionFactory.OpenSession()) // 메서드 종료 시 세션이 닫힘!
    {
        return session.Get<Board>(id); 
    }
}

// (Controller)
public ActionResult Detail(int id)
{
    var board = boardService.GetBoard(id); // board는 여기서 'Detached' 상태가 됨
    return View(board);
}

// (View)
@foreach (var reply in Model.Replies) // 세션이 닫혔으므로 LazyInitializationException 발생!
{
    // ...
}
```

**해결 방안**:

#### 해결 방법 1: Open Session in View 패턴 활용 (권장)

가장 이상적인 해결책입니다. Spring.NET이 제공하는 자동화된 Open Session in View 패턴을 활용하면 됩니다. 서비스 계층의 메서드는 `using` 블록 없이 `GetCurrentSession()`을 통해 세션에 접근하고, `[Transaction]` 속성을 사용하여 트랜잭션 경계를 선언합니다.

```csharp
// (Service Layer - 올바른 예)
[Transaction(ReadOnly = true)] // 읽기 전용 작업
public Board GetBoardForDetailView(int id)
{
    // GetCurrentSession은 Spring에 의해 관리되는 현재 요청의 세션을 반환
    var board = sessionFactory.GetCurrentSession().Get<Board>(id);
    // 이 메서드가 끝나도 세션은 닫히지 않고 뷰 렌더링까지 살아있음
    return board;
}
```
이제 컨트롤러와 뷰 코드를 수정하지 않아도 `Model.Replies`에 접근할 때 예외가 발생하지 않습니다.

#### 해결 방법 2: Eager Loading (필요 시 선택)

뷰에서 항상 특정 컬렉션이 필요하다는 것을 명확히 알고 있을 때, 쿼리 시점에 `Fetch` 또는 `FetchMany`를 사용하여 관련 데이터를 미리 로드하는 방법입니다.

```csharp
[Transaction(ReadOnly = true)]
public Board GetBoardWithReplies(int id)
{
    return sessionFactory.GetCurrentSession().Query<Board>()
        .FetchMany(b => b.Replies) // Replies 컬렉션을 Eager Loading
        .Where(b => b.Id == id)
        .SingleOrDefault();
}
```
**장점**: 쿼리의 의도가 명확해집니다.
**단점**: 항상 필요하지 않은 데이터까지 로드하여 오버헤드가 발생할 수 있습니다.

#### 해결 방법 3: DTO 변환 (가장 좋은 아키텍처)

서비스 계층에서 엔티티를 DTO(Data Transfer Object)로 변환하여 반환하는 방법입니다. 이 방법은 프레젠테이션 계층(Controller, View)이 도메인 모델(Entity)에 직접 의존하지 않게 하여 계층 간의 결합도를 낮추는 가장 좋은 아키텍처 패턴입니다.

```csharp
[Transaction(ReadOnly = true)]
public BoardDetailDto GetBoardAsDto(int id)
{
    var board = sessionFactory.GetCurrentSession().Query<Board>()
        .FetchMany(b => b.Replies) // DTO 변환에 필요하므로 Eager Loading
        .Where(b => b.Id == id)
        .SingleOrDefault();

    if (board == null) return null;

    // 서비스 계층 내에서 필요한 모든 데이터를 DTO로 변환
    return new BoardDetailDto
    {
        Id = board.Id,
        Title = board.Title,
        Content = board.Content,
        Replies = board.Replies.Select(r => new ReplyDto
        {
            Id = r.Id,
            Content = r.Content,
            Author = r.Author
        }).ToList()
    };
}
```
**장점**:
-   `LazyInitializationException`을 근본적으로 방지합니다.
-   프레젠테이션 계층이 도메인 모델의 변경에 영향을 받지 않습니다.
-   API 응답 등 필요한 데이터만 선택적으로 가공하여 전달할 수 있습니다.

## 🌐 Part 2: ASP.NET Web Session 관리 (DI 활용)

정적(static) 클래스나 `HttpContext.Current`를 직접 사용하는 것은 단위 테스트를 어렵게 하고 코드의 유연성을 떨어뜨립니다. 의존성 주입(DI)을 사용하여 웹 세션을 관리하는 현대적인 방법을 알아봅니다.

### 1. `IWebUserSession` 인터페이스 정의

세션 관리를 위한 추상화 인터페이스를 정의합니다.

`SpringNet.Service/Abstractions/IWebUserSession.cs`:
```csharp
// (새 폴더 Abstractions 생성 후 그 안에 파일 생성)
namespace SpringNet.Service.Abstractions
{
    public interface IWebUserSession
    {
        int? UserId { get; }
        string Username { get; }
        string Role { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }

        void SetAuthInfo(int userId, string username, string role);
        void Clear();
    }
}
```

### 2. `WebUserSession` 구현

`HttpSessionStateBase`를 사용하여 인터페이스를 구현합니다. `HttpContextBase`를 생성자에서 받아 유연성을 높입니다.

`SpringNet.Web/Infrastructure/WebUserSession.cs`:
```csharp
using SpringNet.Service.Abstractions;
using System.Web;

namespace SpringNet.Web.Infrastructure
{
    public class WebUserSession : IWebUserSession
    {
        private readonly HttpSessionStateBase session;

        public WebUserSession(HttpContextBase httpContext)
        {
            this.session = httpContext.Session;
        }

        public int? UserId => session["UserId"] as int?;
        public string Username => session["Username"] as string;
        public string Role => session["Role"] as string;
        public bool IsAuthenticated => UserId.HasValue;
        public bool IsAdmin => Role == "Admin";

        public void SetAuthInfo(int userId, string username, string role)
        {
            session["UserId"] = userId;
            session["Username"] = username;
            session["Role"] = role;
        }

        public void Clear()
        {
            session.Clear();
            session.Abandon();
        }
    }
}
```

### 3. Spring.NET 설정

`WebUserSession`을 Spring 컨테이너에 **요청 범위(request-scoped)** Bean으로 등록합니다.

`SpringNet.Web/Config/services.xml` 파일에 다음을 추가합니다.
```xml
<!-- services.xml -->
    <!-- ... 기존 서비스 Bean 설정 ... -->

    <!-- HttpContext Provider -->
    <object id="httpContext"
            type="System.Web.HttpContextWrapper, System.Web"
            scope="request">
        <constructor-arg>
            <expression value="T(System.Web.HttpContext).Current" />
        </constructor-arg>
    </object>

    <!-- Web User Session Manager -->
    <object id="webUserSession"
            type="SpringNet.Web.Infrastructure.WebUserSession, SpringNet.Web"
            scope="request">
        <constructor-arg ref="httpContext" />
    </object>
</objects>
```
컨트롤러 설정 파일(`controllers.xml`)도 수정하여 `webUserSession`을 주입합니다.
```xml
<!-- controllers.xml -->
    <!-- ... 기존 컨트롤러 Bean 설정 ... -->
    <object id="accountController" type="SpringNet.Web.Controllers.AccountController, SpringNet.Web">
        <property name="AuthService" ref="authService" />
        <property name="WebUserSession" ref="webUserSession" /> <!-- 의존성 주입 -->
    </object>
    
    <!-- 다른 컨트롤러들도 필요하다면 동일하게 주입 -->
```

### 4. 컨트롤러에서 사용

`AccountController`를 수정하여 `IWebUserSession`을 주입받아 사용합니다.

```csharp
using SpringNet.Service;
using SpringNet.Service.Abstractions; // IWebUserSession
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class AccountController : Controller
    {
        public IAuthService AuthService { get; set; }
        public IWebUserSession WebUserSession { get; set; } // 속성 주입

        // 로그인 처리
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, string returnUrl)
        {
            try
            {
                var user = AuthService.Login(username, password);

                // 이제 IWebUserSession을 통해 세션 관리
                WebUserSession.SetAuthInfo(user.Id, user.Username, user.Role);

                // ...
            }
            // ...
        }

        // 로그아웃
        public ActionResult Logout()
        {
            WebUserSession.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
```

### 5. 뷰(View) 또는 다른 곳에서 사용

`_Layout.cshtml` 같은 공용 뷰에서 로그인 상태를 확인하려면, `BaseController`를 만들고 `IWebUserSession`을 주입하여 `ViewBag`에 담아주는 패턴을 사용할 수 있습니다.

**`BaseController.cs`**:
```csharp
public abstract class BaseController : Controller
{
    public IWebUserSession WebUserSession { get; set; }

    protected override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (WebUserSession != null)
        {
            ViewBag.WebUserSession = WebUserSession;
        }
        base.OnActionExecuting(filterContext);
    }
}
```
이제 모든 컨트롤러가 `Controller` 대신 `BaseController`를 상속받게 하면, 모든 뷰에서 `@ViewBag.WebUserSession`을 통해 세션 정보에 접근할 수 있습니다.

## 💡 핵심 정리

-   **NHibernate 세션**: Spring.NET의 웹 통합 기능(`SpringSessionContext`, `SpringMvcApplication`)을 통해 **Open Session in View 패턴**이 자동으로 구현되므로 수동 관리가 불필요합니다.
-   **`LazyInitializationException`**: 주로 1) **Open Session in View 패턴 활용**, 2) **Eager Loading (`Fetch`)**, 3) **DTO 변환**의 세 가지 방법으로 해결하며, DTO 변환이 아키텍처 관점에서 가장 권장됩니다.
-   **ASP.NET 세션**: 정적 헬퍼 클래스 대신 **인터페이스와 의존성 주입**을 사용하여 세션 관리를 추상화하면, 코드가 더 유연해지고 단위 테스트가 용이해집니다.
-   **세션 데이터**: 웹 세션에는 전체 객체가 아닌 **최소한의 식별자**(ID, 이름 등)만 저장하여 성능과 메모리 사용량을 최적화해야 합니다.

## 🚀 다음 단계

다음: **[18-webapi-integration.md](./18-webapi-integration.md)** - ASP.NET Web API 통합
