# 17. 세션 관리 (NHibernate Session & Web Session)

## 📚 학습 목표

- NHibernate Session 생명주기 이해
- Session per Request 패턴
- Open Session in View 패턴
- LazyInitializationException 해결
- ASP.NET Web Session 관리
- Session State 최적화

## 🎯 NHibernate Session vs Web Session

```
┌─────────────────────────────────┐
│   NHibernate Session            │  ← DB 연결 관리
│   (ISession)                    │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│   ASP.NET Web Session           │  ← 사용자 상태 관리
│   (HttpSessionState)            │
└─────────────────────────────────┘
```

## 📦 Part 1: NHibernate Session 관리

### 1. Session 생명주기

```csharp
// ❌ 잘못된 예: Session 재사용
public class BadRepository
{
    private ISession session; // 클래스 필드로 Session 보관 (위험!)

    public BadRepository(ISessionFactory sessionFactory)
    {
        this.session = sessionFactory.OpenSession(); // 한 번만 생성
    }

    public Board GetBoard(int id)
    {
        return session.Get<Board>(id); // 계속 같은 Session 사용 (문제!)
    }
}

// ✅ 올바른 예: Session per Request
public class GoodRepository
{
    private readonly ISessionFactory sessionFactory;

    public GoodRepository(ISessionFactory sessionFactory)
    {
        this.sessionFactory = sessionFactory;
    }

    public Board GetBoard(int id)
    {
        using (var session = sessionFactory.OpenSession()) // 요청마다 새로운 Session
        {
            return session.Get<Board>(id);
        }
    } // using 블록 종료 시 Session 자동 닫힘
}
```

### 2. Session per Request 패턴

**개념**: HTTP 요청당 하나의 Session 사용

```csharp
public class SessionPerRequestModule : IHttpModule
{
    private const string SessionKey = "NHibernate.CurrentSession";

    public void Init(HttpApplication context)
    {
        context.BeginRequest += BeginRequest;
        context.EndRequest += EndRequest;
    }

    private void BeginRequest(object sender, EventArgs e)
    {
        var app = (HttpApplication)sender;
        var sessionFactory = GetSessionFactory();

        // 요청 시작 시 Session 생성
        var session = sessionFactory.OpenSession();
        app.Context.Items[SessionKey] = session;
    }

    private void EndRequest(object sender, EventArgs e)
    {
        var app = (HttpApplication)sender;
        var session = app.Context.Items[SessionKey] as ISession;

        // 요청 종료 시 Session 닫기
        if (session != null)
        {
            if (session.Transaction != null && session.Transaction.IsActive)
            {
                session.Transaction.Rollback();
            }

            session.Dispose();
            app.Context.Items.Remove(SessionKey);
        }
    }

    private ISessionFactory GetSessionFactory()
    {
        return NHibernateHelper.SessionFactory;
    }

    public void Dispose() { }
}
```

**Web.config 등록**:

```xml
<system.webServer>
    <modules>
        <add name="NHibernateSessionModule"
             type="SpringNet.Web.Infrastructure.SessionPerRequestModule, SpringNet.Web" />
    </modules>
</system.webServer>
```

### 3. Open Session in View 패턴

**목적**: LazyInitializationException 방지

```csharp
public class OpenSessionInViewModule : IHttpModule
{
    public void Init(HttpApplication context)
    {
        context.BeginRequest += BeginRequest;
        context.EndRequest += EndRequest;
    }

    private void BeginRequest(object sender, EventArgs e)
    {
        var session = NHibernateHelper.SessionFactory.OpenSession();
        session.BeginTransaction();

        CurrentSessionContext.Bind(session);
    }

    private void EndRequest(object sender, EventArgs e)
    {
        var session = CurrentSessionContext.Unbind(
            NHibernateHelper.SessionFactory);

        if (session != null)
        {
            try
            {
                if (session.Transaction != null && session.Transaction.IsActive)
                {
                    // View 렌더링 완료 후 커밋
                    session.Transaction.Commit();
                }
            }
            catch
            {
                if (session.Transaction != null && session.Transaction.IsActive)
                {
                    session.Transaction.Rollback();
                }
                throw;
            }
            finally
            {
                session.Dispose();
            }
        }
    }

    public void Dispose() { }
}
```

### 4. CurrentSessionContext 설정

**hibernate.cfg.xml**:

```xml
<hibernate-configuration>
    <session-factory>
        <!-- Session Context 설정 -->
        <property name="current_session_context_class">
            web
        </property>
        <!-- 또는 Spring.NET 통합 시 -->
        <!--
        <property name="current_session_context_class">
            Spring.Data.NHibernate.SpringSessionContext, Spring.Data.NHibernate
        </property>
        -->

        <!-- 기타 설정 -->
    </session-factory>
</hibernate-configuration>
```

### 5. Repository에서 Current Session 사용

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ISessionFactory sessionFactory;

    public Repository(ISessionFactory sessionFactory)
    {
        this.sessionFactory = sessionFactory;
    }

    // Current Session 가져오기
    protected ISession CurrentSession
    {
        get
        {
            return sessionFactory.GetCurrentSession();
        }
    }

    public T GetById(int id)
    {
        // Session 직접 열지 않고 Current Session 사용
        return CurrentSession.Get<T>(id);
    }

    public IList<T> GetAll()
    {
        return CurrentSession.Query<T>().ToList();
    }

    public void Add(T entity)
    {
        CurrentSession.Save(entity);
    }

    public void Update(T entity)
    {
        CurrentSession.Update(entity);
    }

    public void Delete(T entity)
    {
        CurrentSession.Delete(entity);
    }
}
```

### 6. LazyInitializationException 해결 방법

#### 문제 상황

```csharp
// Controller
public ActionResult Detail(int id)
{
    var board = boardService.GetBoard(id);
    return View(board); // View에서 board.Replies 접근 시 에러!
}

// Service
public Board GetBoard(int id)
{
    using (var session = sessionFactory.OpenSession())
    {
        var board = session.Get<Board>(id);
        return board;
    } // Session이 여기서 닫힘!
}

// View에서 에러 발생
@foreach (var reply in Model.Replies) // LazyInitializationException!
{
    // ...
}
```

#### 해결 방법 1: Eager Loading

```csharp
public Board GetBoard(int id)
{
    using (var session = sessionFactory.OpenSession())
    {
        // Replies를 미리 로딩
        var board = session.Query<Board>()
            .Fetch(b => b.Replies)
            .FirstOrDefault(b => b.Id == id);

        return board;
    }
}
```

#### 해결 방법 2: DTO 변환

```csharp
public BoardDetailDto GetBoard(int id)
{
    using (var session = sessionFactory.OpenSession())
    {
        var board = session.Query<Board>()
            .Fetch(b => b.Replies)
            .FirstOrDefault(b => b.Id == id);

        // DTO로 변환 (모든 데이터 로딩됨)
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
}
```

#### 해결 방법 3: Open Session in View

```csharp
// Module 사용 (위의 OpenSessionInViewModule 참조)
// Session이 View 렌더링까지 유지됨
public Board GetBoard(int id)
{
    // Current Session 사용
    var board = CurrentSession.Get<Board>(id);
    return board; // Lazy Loading 가능
}
```

### 7. Transaction 관리

```csharp
// ✅ 수동 트랜잭션
public void CreateBoard(string title, string content, string author)
{
    using (var session = sessionFactory.OpenSession())
    using (var transaction = session.BeginTransaction())
    {
        try
        {
            var board = new Board
            {
                Title = title,
                Content = content,
                Author = author
            };

            session.Save(board);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

// ✅ Current Session + Transaction
public void CreateBoard(string title, string content, string author)
{
    var session = CurrentSession;
    var transaction = session.BeginTransaction();

    try
    {
        var board = new Board
        {
            Title = title,
            Content = content,
            Author = author
        };

        session.Save(board);
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
```

## 🌐 Part 2: ASP.NET Web Session 관리

### 1. 기본 Session 사용

```csharp
public class AccountController : Controller
{
    // Session에 값 저장
    public ActionResult Login(string username, string password)
    {
        var user = authService.Login(username, password);

        if (user != null)
        {
            Session["UserId"] = user.Id;
            Session["Username"] = user.Username;
            Session["Role"] = user.Role;
            Session["LoginTime"] = DateTime.Now;

            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // Session에서 값 읽기
    public ActionResult Profile()
    {
        var userId = Session["UserId"] as int?;

        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        var user = userService.GetUser(userId.Value);
        return View(user);
    }

    // Session 삭제
    public ActionResult Logout()
    {
        Session.Clear();       // 모든 세션 값 삭제
        Session.Abandon();     // 세션 완전히 종료
        return RedirectToAction("Index", "Home");
    }
}
```

### 2. Session Helper 클래스

```csharp
public static class SessionHelper
{
    private static HttpSessionStateBase Session
    {
        get { return new HttpSessionStateWrapper(HttpContext.Current.Session); }
    }

    // 사용자 ID
    public static int? UserId
    {
        get { return Session["UserId"] as int?; }
        set { Session["UserId"] = value; }
    }

    // 사용자명
    public static string Username
    {
        get { return Session["Username"] as string; }
        set { Session["Username"] = value; }
    }

    // 역할
    public static string Role
    {
        get { return Session["Role"] as string; }
        set { Session["Role"] = value; }
    }

    // 로그인 여부
    public static bool IsAuthenticated
    {
        get { return UserId.HasValue; }
    }

    // 관리자 여부
    public static bool IsAdmin
    {
        get { return Role == "Admin"; }
    }

    // 세션 초기화
    public static void Clear()
    {
        Session.Clear();
    }
}
```

**사용**:

```csharp
// Controller에서
public ActionResult Index()
{
    if (!SessionHelper.IsAuthenticated)
    {
        return RedirectToAction("Login", "Account");
    }

    ViewBag.Username = SessionHelper.Username;
    return View();
}

// View에서
@if (SessionHelper.IsAuthenticated)
{
    <span>환영합니다, @SessionHelper.Username님!</span>
}
```

### 3. Session 설정 (Web.config)

```xml
<system.web>
    <!-- Session 설정 -->
    <sessionState
        mode="InProc"
        timeout="20"
        cookieless="false"
        cookieName="ASP.NET_SessionId"
        regenerateExpiredSessionId="true">
    </sessionState>

    <!--
    mode 옵션:
    - InProc: 웹 서버 메모리에 저장 (기본값, 빠름)
    - StateServer: 별도 프로세스에 저장
    - SQLServer: SQL Server에 저장
    - Custom: 커스텀 프로바이더
    - Off: 세션 비활성화

    timeout: 세션 타임아웃 (분 단위)
    -->
</system.web>
```

### 4. Session State 모드

#### InProc (기본)

```xml
<!-- 웹 서버 메모리에 저장 -->
<sessionState mode="InProc" timeout="20" />
```

**장점**:
- ✅ 가장 빠름
- ✅ 설정 간단

**단점**:
- ❌ 앱 재시작 시 세션 손실
- ❌ 웹 팜 환경 불가능

#### StateServer

```xml
<!-- 별도 서비스에 저장 -->
<sessionState
    mode="StateServer"
    stateConnectionString="tcpip=127.0.0.1:42424"
    timeout="20" />
```

**장점**:
- ✅ 앱 재시작해도 유지
- ✅ 웹 팜 환경 가능

**단점**:
- ❌ 직렬화 필요
- ❌ 추가 서비스 운영

#### SQL Server

```xml
<!-- SQL Server에 저장 -->
<sessionState
    mode="SQLServer"
    sqlConnectionString="Data Source=.;Integrated Security=SSPI;"
    timeout="20" />
```

**설치**:
```cmd
aspnet_regsql.exe -S localhost -E -ssadd
```

**장점**:
- ✅ 영구 저장
- ✅ 웹 팜 환경 가능
- ✅ 안정적

**단점**:
- ❌ 가장 느림
- ❌ DB 부하

### 5. Session 끊기지 않게 하기

#### 방법 1: Timeout 연장

```xml
<!-- Web.config -->
<sessionState mode="InProc" timeout="60" /> <!-- 60분 -->
```

#### 방법 2: Keep-Alive Ping

```javascript
// View에서 주기적으로 서버 호출
<script>
setInterval(function() {
    $.ajax({
        url: '@Url.Action("KeepAlive", "Home")',
        type: 'GET'
    });
}, 5 * 60 * 1000); // 5분마다
</script>
```

```csharp
// Controller
public ActionResult KeepAlive()
{
    // Session 접근만으로 타임아웃 갱신
    var userId = Session["UserId"];
    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
}
```

#### 방법 3: Sliding Expiration

```xml
<!-- Forms Authentication과 함께 사용 -->
<authentication mode="Forms">
    <forms loginUrl="~/Account/Login"
           timeout="30"
           slidingExpiration="true" /> <!-- 활동 시 자동 연장 -->
</authentication>
```

### 6. Session vs Cookie

| 특징 | Session | Cookie |
|------|---------|--------|
| 저장 위치 | 서버 | 클라이언트 |
| 보안 | 높음 | 낮음 |
| 크기 제한 | 없음 (메모리) | 4KB |
| 수명 | Timeout | 설정한 기간 |
| 사용 예 | 로그인 정보 | Remember Me |

### 7. Session 최적화

#### 최소한의 데이터만 저장

```csharp
// ❌ 나쁜 예: 전체 객체 저장
Session["User"] = userService.GetUser(userId); // 모든 정보 저장

// ✅ 좋은 예: 필요한 정보만 저장
Session["UserId"] = user.Id;
Session["Username"] = user.Username;
Session["Role"] = user.Role;
```

#### Lazy Loading

```csharp
public class SessionUser
{
    private User _user;

    public User User
    {
        get
        {
            if (_user == null)
            {
                var userId = SessionHelper.UserId;
                if (userId.HasValue)
                {
                    _user = userService.GetUser(userId.Value);
                }
            }
            return _user;
        }
    }
}
```

## 💡 핵심 정리

### NHibernate Session

✅ **Session per Request** 패턴 사용
✅ **Current Session** 활용
✅ **Open Session in View**로 Lazy Loading 지원
✅ **DTO 변환**으로 Session 독립성 확보
✅ **Transaction** 명확히 관리

### ASP.NET Web Session

✅ **최소한의 데이터**만 저장
✅ **SessionHelper** 클래스로 중앙 관리
✅ **적절한 Timeout** 설정
✅ **웹 팜 환경**은 StateServer 또는 SQLServer 사용

### LazyInitializationException 해결

1. **Eager Loading** (Fetch)
2. **DTO 변환**
3. **Open Session in View**

## 🚀 다음 단계

다음: **[18-webapi-integration.md](./18-webapi-integration.md)** - ASP.NET Web API 통합
