# 14. 베스트 프랙티스

## 📚 학습 목표

- Spring.NET + NHibernate 실전 팁 및 권장 사항
- 성능 최적화 기법 (Lazy Loading, 캐싱, 배치 등)
- 애플리케이션 보안 강화 (SQL Injection, XSS, CSRF, 비밀번호 해싱)
- 코드 품질 향상 (SOLID 원칙, 예외 처리, 로깅)
- 효과적인 테스트 전략 (단위 테스트)

## 🎯 아키텍처 패턴: 계층형 아키텍처

우리가 이 튜토리얼에서 지속적으로 사용해온 `Layered Architecture`는 애플리케이션의 관심사를 분리하고 유지보수성을 높이는 가장 일반적인 패턴입니다.

```
┌─────────────────────────┐
│   Presentation Layer    │  ← Controllers, Views (사용자 인터페이스 및 요청 처리)
├─────────────────────────┤
│   Service Layer         │  ← Business Logic, Transaction Management (비즈니스 규칙 및 트랜잭션 처리)
├─────────────────────────┤
│   Data Access Layer     │  ← Repositories (데이터베이스 접근 및 매핑)
├─────────────────────────┤
│   Domain Layer          │  ← Entities, Value Objects (도메인 모델 정의)
└─────────────────────────┘
```

**핵심 규칙**:
-   **단방향 의존성**: 각 레이어는 바로 아래 레이어만 참조해야 합니다 (상위 레이어를 참조해서는 안 됩니다).
-   **책임 분리**: 각 레이어는 명확하고 단일한 책임(Single Responsibility)을 가집니다.
-   **느슨한 결합**: 인터페이스 기반 프로그래밍과 의존성 주입(DI)을 통해 레이어 간의 결합도를 낮춥니다.
-   **Service Layer의 역할**: Controller는 Service만 호출해야 하며 Repository를 직접 호출하지 않습니다. Service는 비즈니스 로직을 담당하고, 여러 Repository를 조합하거나 트랜잭션을 관리합니다.
-   **Repository의 역할**: 데이터베이스 CRUD 작업 및 엔티티 매핑에만 집중합니다.

## 🚀 성능 최적화

### 1. Lazy Loading vs Eager Loading 및 N+1 문제 방지

관계형 데이터는 대부분 `Lazy Loading` (지연 로딩)이 기본값으로 설정됩니다. 이는 연관된 엔티티를 실제로 사용할 때 로드하여 메모리 사용량을 최적화합니다. 하지만 부적절하게 사용하면 이른바 **N+1 문제**를 야기하여 성능 저하의 주범이 될 수 있습니다.

```csharp
// ❌ N+1 문제 예시: 게시글 목록 조회 후 각 게시글의 댓글 수 조회
// 1번의 SELECT 쿼리로 모든 게시글을 가져오고,
// 각 게시글의 Replies를 접근할 때마다 N번의 SELECT 쿼리가 추가로 발생 (N+1 문제)
var boards = boardRepository.GetAll(); // Board 10개 조회 -> SELECT 1번
foreach (var board in boards)
{
    Console.WriteLine($"게시글 '{board.Title}'의 댓글 수: {board.Replies.Count}"); // 각 Replies.Count 접근 시 SELECT N번 (N=10)
}

// ✅ Eager Loading으로 N+1 문제 해결: Fetch/FetchMany 사용
// 1번의 SELECT 쿼리로 게시글과 댓글을 한 번에 가져옴
var boardsWithReplies = sessionFactory.GetCurrentSession().Query<Board>()
    .Fetch(b => b.Replies) // 게시글 조회 시 댓글 목록을 즉시 로딩
    .ToList();

foreach (var board in boardsWithReplies)
{
    Console.WriteLine($"게시글 '{board.Title}'의 댓글 수: {board.Replies.Count}"); // 추가 쿼리 없이 접근 가능
}
```
`Fetch` 또는 `FetchMany`는 `IQueryOver` 또는 `LINQ to NHibernate`에서 Eager Loading을 수행하여 N+1 문제를 방지하는 효과적인 방법입니다.

### 2. Second Level Cache (2차 캐시) 사용

NHibernate의 2차 캐시는 여러 세션 팩토리에서 공유되는 캐시로, 애플리케이션 전체의 성능을 향상시킬 수 있습니다.

`SpringNet.Data/hibernate.cfg.xml` 설정 예시:
```xml
<!-- hibernate.cfg.xml -->
<property name="cache.use_second_level_cache">true</property>
<property name="cache.provider_class">
    NHibernate.Cache.HashtableCacheProvider <!-- 간단한 인메모리 캐시 (개발/테스트용) -->
    <!-- 또는 NHibernate.Caches.SysCache2.SysCacheProvider, NHibernate.Caches.SysCache2 (ASP.NET Cache) -->
    <!-- 또는 제3자 캐시 (Redis, Memcached 등) -->
</property>
<property name="cache.use_query_cache">true</property> <!-- 쿼리 결과도 캐시 -->
```
엔티티 매핑 파일에 캐시 사용을 명시합니다. (`SpringNet.Data/Mappings/Product.hbm.xml` 예시)
```xml
<!-- Product.hbm.xml -->
<class name="Product" table="Products">
    <cache usage="read-write" /> <!-- 엔티티의 캐시 전략 (read-only, read-write, nonstrict-read-write, transactional) -->
    <!-- ... -->
</class>
```
쿼리 캐시를 사용하려면 쿼리 시 `SetCacheable(true)`를 호출해야 합니다.
```csharp
// 쿼리 결과 캐시 사용 예시
var popularProducts = sessionFactory.GetCurrentSession().Query<Product>()
    .Where(p => p.IsAvailable)
    .OrderByDescending(p => p.Stock)
    .Take(10)
    .Cacheable() // 쿼리 캐시 적용
    .ToList();
```

### 3. Batch Fetching (배치 페칭)

NHibernate는 여러 개의 지연 로딩될 컬렉션이나 연관 엔티티를 한 번의 쿼리로 가져오도록 최적화할 수 있습니다.

`SpringNet.Data/hibernate.cfg.xml` 설정 예시:
```xml
<property name="adonet.batch_size">20</property> <!-- JDBC/ADO.NET 배치 크기 -->
```
엔티티 매핑 파일에 `batch-size` 속성을 추가합니다.
```xml
<!-- Product.hbm.xml 예시 -->
<class name="Product" table="Products" batch-size="10"> <!-- Product 엔티티를 배치로 로딩할 때 크기 -->
    <!-- ... -->
</class>
```
컬렉션 매핑에도 `batch-size`를 적용할 수 있습니다.
```xml
<!-- Board.hbm.xml 예시 (Replies 컬렉션) -->
<bag name="Replies" inverse="true" cascade="all-delete-orphan" lazy="true" batch-size="5">
    <key column="BoardId" />
    <one-to-many class="Reply" />
</bag>
```

### 4. Projection 사용 (부분 조회)

필요한 데이터만 정확히 조회하는 `Projection`은 불필요한 데이터를 데이터베이스에서 가져오지 않아 네트워크 트래픽과 메모리 사용량을 줄여줍니다.

```csharp
// ❌ 전체 엔티티 조회 (대용량 엔티티의 경우 무거움)
var fullProducts = productRepository.GetAll();

// ✅ 필요한 필드만 조회 (ProductDto에 매핑)
var productSummaries = sessionFactory.GetCurrentSession().Query<Product>()
    .Where(p => p.IsAvailable)
    .Select(p => new ProductDto // 특정 DTO로 바로 프로젝션
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        ImageUrl = p.ImageUrl
        // CategoryName = p.Category.Name // N+1 문제 발생 가능성 있으므로 주의
    })
    .ToList();

// 카테고리 이름까지 한 번에 가져오려면 Fetch와 함께 사용
var productSummariesWithCategory = sessionFactory.GetCurrentSession().Query<Product>()
    .Fetch(p => p.Category)
    .Where(p => p.IsAvailable)
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        ImageUrl = p.ImageUrl,
        CategoryName = p.Category.Name
    })
    .ToList();
```

## 🔒 보안 강화

### 1. SQL Injection 방지

사용자 입력을 SQL 쿼리에 직접 포함하지 않고, **파라미터 바인딩**을 사용하여 쿼리 문자열과 데이터를 분리합니다. NHibernate의 LINQ to NHibernate나 `CreateQuery`의 `SetParameter`는 기본적으로 SQL Injection을 방지합니다.

```csharp
// ❌ 위험: SQL Injection 가능성 (userInput에 "'; DROP TABLE Users; --" 등이 들어오면 위험)
// var query = $"from Board b where b.Title = '{userInput}'";
// var boards = session.CreateQuery(query).List<Board>();

// ✅ 안전: 파라미터 바인딩을 사용하는 LINQ to NHibernate (권장)
var boards = sessionFactory.GetCurrentSession().Query<Board>()
    .Where(b => b.Title == userInput) // userInput이 자동으로 파라미터로 바인딩됨
    .ToList();

// ✅ 안전: HQL에서 SetParameter 사용
var boardsHQL = sessionFactory.GetCurrentSession().CreateQuery("from Board b where b.Title = :title")
    .SetParameter("title", userInput) // userInput이 파라미터로 바인딩됨
    .List<Board>();
```

### 2. XSS (Cross-Site Scripting) 방지

사용자로부터 입력받은 데이터를 웹 페이지에 출력할 때, `HTML Encoding`을 사용하여 스크립트 코드가 실행되지 않도록 합니다. ASP.NET MVC의 Razor 뷰 엔진은 기본적으로 HTML Encoding을 수행합니다.

```html
<!-- ❌ 위험: 사용자 입력에 스크립트 태그가 있다면 실행될 수 있음 -->
@Html.Raw(Model.Content) 

<!-- ✅ 안전: Razor는 기본적으로 HTML Encoding 수행 -->
<p>@Model.Content</p> 

<!-- 특정 HTML 태그만 허용하는 경우, 화이트리스트 기반의 Sanitizer 라이브러리 사용 -->
<!-- 예: AntiXssLibrary, HtmlSanitizer 등 -->
```

### 3. CSRF (Cross-Site Request Forgery) 방지

사용자의 의도와는 상관없이 공격자의 의도에 따라 웹 사이트 기능을 요청하게 만드는 공격입니다. ASP.NET MVC에서는 `AntiForgeryToken`을 사용하여 방지합니다.

**Controller**: 모든 POST 요청 메서드에 `[ValidateAntiForgeryToken]` 속성을 추가합니다.
```csharp
[HttpPost]
[ValidateAntiForgeryToken] // CSRF 토큰 유효성 검사
public ActionResult CreateBoard(BoardDto model)
{
    // ...
}
```
**View**: 폼 전송 시 `Html.AntiForgeryToken()` 헬퍼를 사용하여 토큰을 포함합니다.
```html
@using (Html.BeginForm("CreateBoard", "Board", FormMethod.Post))
{
    @Html.AntiForgeryToken() <!-- hidden input 형태로 토큰 생성 -->
    <!-- ... form fields ... -->
    <button type="submit">등록</button>
}
```

### 4. 비밀번호 저장

**절대로 비밀번호를 평문으로 저장해서는 안 됩니다.** 해시(Hash) 함수와 Salt(소금)를 사용하여 저장합니다. Salt는 각 사용자마다 고유하게 생성되는 임의의 값으로, 무지개 테이블 공격(Rainbow Table Attack)을 방지합니다.

이전 `08-user-part1-authentication.md`에서 `AuthService`는 SHA256을 사용했습니다. SHA256은 해시 함수이지만, 단순 SHA256은 레인보우 테이블 공격에 취약할 수 있습니다. **현대적인 애플리케이션에서는 `PBKDF2`, `bcrypt`, `scrypt`와 같이 계산 비용이 높고 Salt를 사용하는 해시 알고리즘을 사용하는 것이 강력히 권장됩니다.**

`AuthService`를 리팩토링하여 `Rfc2898DeriveBytes` (.NET 프레임워크 제공) 또는 `BCrypt.Net`과 같은 라이브러리를 사용하여 비밀번호를 해싱하는 것을 고려하십시오.

```csharp
// ✅ 권장: Rfc2898DeriveBytes 사용 예시 (.NET 기본 제공)
public static string HashPassword(string password)
{
    // Salt 생성
    byte[] salt;
    new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

    var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000); // 10000번 반복 (Iteration)
    byte[] hash = pbkdf2.GetBytes(20); // 20바이트 해시 생성

    byte[] hashBytes = new byte[36]; // Salt (16) + Hash (20)
    Array.Copy(salt, 0, hashBytes, 0, 16);
    Array.Copy(hash, 0, hashBytes, 16, 20);

    return Convert.ToBase64String(hashBytes);
}

public static bool VerifyPassword(string password, string hashedPassword)
{
    byte[] hashBytes = Convert.FromBase64String(hashedPassword);
    byte[] salt = new byte[16];
    Array.Copy(hashBytes, 0, salt, 0, 16);

    var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
    byte[] hash = pbkdf2.GetBytes(20);

    for (int i = 0; i < 20; i++)
    {
        if (hashBytes[i + 16] != hash[i]) return false;
    }
    return true;
}

// ✅ 더 강력하게 권장: BCrypt.Net 라이브러리 사용 예시 (NuGet 설치 필요)
// Install-Package BCrypt.Net-Next
/*
public static string HashPasswordBCrypt(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password);
}

public static bool VerifyPasswordBCrypt(string password, string hashedPassword)
{
    return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
}
*/
```

## 💡 코드 품질 향상

### 1. SOLID 원칙

객체 지향 설계의 다섯 가지 기본 원칙으로, 유지보수, 확장성, 유연성을 높이는 데 기여합니다.

-   **S - Single Responsibility Principle (단일 책임 원칙)**: 클래스는 하나의, 오직 하나의 변경 이유만을 가져야 합니다.
    ```csharp
    // ❌ 나쁜 예: UserService가 사용자 관리, 이메일 전송, 유효성 검증 등 여러 책임을 가짐
    // public class UserService { public void RegisterUser(); public void SendWelcomeEmail(); public void ValidateUserData(); }

    // ✅ 좋은 예: 각 클래스가 단일 책임을 가짐
    public class UserService { public void RegisterUser(); }
    public class EmailService { public void SendEmail(); }
    public class UserValidator { public bool IsValid(); }
    ```
-   **O - Open/Closed Principle (개방-폐쇄 원칙)**: 소프트웨어 요소는 확장에 대해 열려 있어야 하고, 수정에 대해 닫혀 있어야 합니다. (기존 코드를 수정하지 않고 기능 확장)
-   **L - Liskov Substitution Principle (리스코프 치환 원칙)**: 자식 클래스는 부모 클래스의 기능을 완벽히 수행할 수 있어야 합니다 (자식 타입이 부모 타입으로 사용될 수 있어야 합니다).
-   **I - Interface Segregation Principle (인터페이스 분리 원칙)**: 클라이언트는 자신이 사용하지 않는 인터페이스에 의존해서는 안 됩니다. (큰 인터페이스보다는 작은 인터페이스 여러 개가 좋습니다).
-   **D - Dependency Inversion Principle (의존성 역전 원칙)**:
    1.  상위 모듈은 하위 모듈에 의존해서는 안 됩니다. 둘 다 추상화에 의존해야 합니다.
    2.  추상화는 세부 사항에 의존해서는 안 됩니다. 세부 사항이 추상화에 의존해야 합니다.
    ```csharp
    // ✅ 추상화(인터페이스)에 의존
    public class OrderService
    {
        private readonly IOrderRepository repository; // 하위 모듈(Repository)의 추상화(인터페이스)에 의존

        public OrderService(IOrderRepository repository)
        {
            this.repository = repository;
        }
        // ...
    }
    ```

### 2. 예외 처리

예외는 프로그램의 비정상적인 흐름을 처리하는 중요한 메커니즘입니다. 올바른 예외 처리는 애플리케이션의 안정성과 디버깅 용이성을 높입니다.

```csharp
// ❌ 예외 무시: 문제 발생 시 원인을 파악하기 어렵고, 데이터 손상으로 이어질 수 있음
// try { DoSomething(); } catch { }

// ❌ 너무 광범위한 예외 처리: 특정 예외를 놓치거나, 원치 않는 예외까지 처리할 수 있음
// try { DoSomething(); } catch (Exception ex) { logger.Error("Unhandled error", ex); }

// ✅ 구체적인 예외 처리 및 적절한 로깅/재throw
try
{
    // ... 비즈니스 로직 ...
}
catch (ArgumentException ex) // 특정 비즈니스 규칙 위반 예외
{
    logger.Warning("유효성 검증 실패: " + ex.Message); // 사용자에게 보여줄 메시지 로깅
    throw; // 예외를 다시 던져 상위 계층에서 처리하도록 함
}
catch (NHibernate.Exceptions.GenericADOException ex) // 데이터베이스 관련 예외
{
    logger.Error("데이터베이스 작업 실패", ex); // 기술적인 예외는 상세 로깅
    throw new ApplicationException("데이터 처리 중 오류가 발생했습니다. 관리자에게 문의하세요.", ex); // 사용자에게는 일반적인 메시지 전달
}
```

### 3. Logging (로깅)

애플리케이션의 동작을 추적하고 문제 발생 시 원인을 파악하는 데 필수적입니다. 이 튜토리얼 시리즈에서는 `SpringNet.Service/Logging` 폴더에 `ILogger` 인터페이스를 정의하고 `ConsoleLogger`, `FileLogger`, `CompositeLogger`를 구현하여 로깅 시스템을 구축했습니다. 서비스 계층에서 이 `ILogger`를 주입받아 사용합니다.

```csharp
// (Service Layer 예시)
using SpringNet.Service.Logging; // ILogger 인터페이스 사용
using Spring.Transaction.Interceptor; // [Transaction] 속성을 위해 필요

namespace SpringNet.Service
{
    public class BoardService : IBoardService
    {
        private readonly IBoardRepository boardRepository;
        private readonly IReplyRepository replyRepository;
        private readonly ISessionFactory sessionFactory;
        private readonly ILogger logger; // ILogger 주입

        public BoardService(
            IBoardRepository boardRepository,
            IReplyRepository replyRepository,
            ISessionFactory sessionFactory,
            ILogger logger) // 생성자 주입
        {
            this.boardRepository = boardRepository;
            this.replyRepository = replyRepository;
            this.sessionFactory = sessionFactory;
            this.logger = logger; // 로거 초기화
        }

        [Transaction(ReadOnly = true)]
        public BoardDetailDto GetBoard(int id, bool increaseViewCount = true)
        {
            logger.LogInfo($"게시글 조회 시작: ID = {id}");

            try
            {
                // 실제 boardRepository의 GetBoard 메서드는 increaseViewCount 매개변수가 없음.
                // 튜토리얼 예시이므로 단순화하여 GetById로 가정
                var board = boardRepository.GetById(id);
                
                if (board == null)
                {
                    logger.LogWarning($"게시글을 찾을 수 없음: ID = {id}");
                    return null;
                }
                
                if (increaseViewCount)
                {
                    board.IncreaseViewCount(); // Board 엔티티의 비즈니스 로직
                    boardRepository.Update(board); // 변경사항 저장 (Transaction 범위 내에서 자동)
                }

                logger.LogDebug($"게시글 조회 완료: {board.Title}");
                return MapToBoardDetailDto(board); // DTO 매핑 메서드는 생략
            }
            catch (Exception ex)
            {
                logger.LogError($"게시글 조회 중 오류 발생: ID = {id}", ex);
                throw; // 예외를 다시 던져 트랜잭션 롤백 등을 유도
            }
        }
        // ...
        // BoardDetailDto와 MapToBoardDetailDto는 BoardService에 정의되어 있다고 가정합니다.
        // ...
    }
}
```

## 📊 테스트 전략

테스트는 소프트웨어의 품질을 보장하고 회귀를 방지하는 데 필수적입니다.

### 단위 테스트 (Unit Test)

가장 작은 단위(클래스, 메서드)를 독립적으로 테스트합니다. 의존성(Repository, 다른 서비스 등)은 Mock(모의 객체)을 사용하여 격리시킵니다.

```csharp
// (SpringNet.Tests 프로젝트 예시)
using NUnit.Framework;
using Moq; // Moq 라이브러리 사용
using SpringNet.Data.Repositories;
using SpringNet.Domain.Entities;
using SpringNet.Service;
using SpringNet.Service.DTOs;
using NHibernate; // ISessionFactory Mocking을 위해 필요
using System.Linq;
using System.Collections.Generic;
using SpringNet.Service.Logging; // ILogger Mocking을 위해 필요

namespace SpringNet.Tests.ServiceTests
{
    [TestFixture]
    public class BoardServiceTests
    {
        private Mock<IBoardRepository> mockBoardRepository;
        private Mock<IReplyRepository> mockReplyRepository;
        private Mock<ISessionFactory> mockSessionFactory;
        private Mock<ILogger> mockLogger; // ILogger Mock 추가
        private BoardService boardService;

        [SetUp]
        public void Setup()
        {
            mockBoardRepository = new Mock<IBoardRepository>();
            mockReplyRepository = new Mock<IReplyRepository>();
            mockSessionFactory = new Mock<ISessionFactory>();
            mockLogger = new Mock<ILogger>(); // Mock 초기화

            // ISessionFactory Mock: GetCurrentSession().BeginTransaction() 체인을 모의 객체로 만듦
            var mockSession = new Mock<ISession>();
            var mockTransaction = new Mock<ITransaction>();
            mockSessionFactory.Setup(sf => sf.GetCurrentSession()).Returns(mockSession.Object);
            mockSession.Setup(s => s.BeginTransaction()).Returns(mockTransaction.Object);
            mockTransaction.Setup(t => t.Commit()); // Commit 호출 시 아무것도 하지 않음 (성공 가정)


            boardService = new BoardService(
                mockBoardRepository.Object,
                mockReplyRepository.Object,
                mockSessionFactory.Object,
                mockLogger.Object); // Mock 객체 주입
        }

        [Test]
        public void CreateBoard_ValidInput_ReturnsNewBoardId()
        {
            // Arrange
            var title = "새 게시글 제목";
            var content = "새 게시글 내용";
            var author = "테스터";
            var newBoard = new Board { Id = 1, Title = title, Content = content, Author = author };

            // boardRepository.Add(board)가 호출될 때 board 객체의 Id를 설정
            mockBoardRepository.Setup(r => r.Add(It.IsAny<Board>()))
                .Callback<Board>(board => board.Id = 1);

            // Act
            var resultId = boardService.CreateBoard(title, content, author);

            // Assert
            Assert.AreEqual(1, resultId);
            // mockBoardRepository.Verify(r => r.Add(It.IsAny<Board>()), Times.Once()); // Add 메서드가 한 번 호출되었는지 검증
            mockLogger.Verify(l => l.LogInfo(It.IsAny<string>()), Times.AtLeastOnce()); // 로거가 호출되었는지 검증
        }

        [Test]
        public void GetBoard_ExistingBoard_ReturnsBoardDetailDto()
        {
            // Arrange
            var boardId = 1;
            var domainBoard = new Board
            {
                Id = boardId,
                Title = "테스트 게시글",
                Content = "내용",
                Author = "작성자",
                Replies = new List<Reply>() // GetBoardWithReplies가 아닌 GetById를 사용하므로 Reply 목록은 비워둠
            };
            mockBoardRepository.Setup(r => r.GetById(boardId)).Returns(domainBoard); // GetById를 사용하여 board 반환

            // Act
            var result = boardService.GetBoard(boardId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(boardId, result.Id);
            mockBoardRepository.Verify(r => r.Update(domainBoard), Times.Once()); // 조회수 증가로 인해 Update가 호출되었는지 검증
            mockLogger.Verify(l => l.LogInfo(It.IsAny<string>()), Times.AtLeastOnce());
        }

        [Test]
        public void GetBoard_NonExistingBoard_ReturnsNull()
        {
            // Arrange
            var boardId = 99;
            mockBoardRepository.Setup(r => r.GetById(boardId)).Returns((Board)null); // GetById를 사용하여 null 반환

            // Act
            var result = boardService.GetBoard(boardId);

            // Assert
            Assert.IsNull(result);
            mockLogger.Verify(l => l.LogWarning(It.IsAny<string>()), Times.AtLeastOnce());
        }
    }
}
```
**참고**: 위 단위 테스트 예시를 실행하려면 `SpringNet.Tests` 프로젝트에 `Moq` NuGet 패키지를 설치해야 합니다.

```
PM> Install-Package Moq
```

### 통합 테스트 (Integration Test)

여러 구성 요소(예: 서비스 + 리포지토리 + 실제 데이터베이스)가 함께 작동하는 것을 테스트합니다. 실제 환경과 유사하게 구성하여 테스트합니다.

### End-to-End 테스트 (E2E Test)

애플리케이션 전체의 흐름을 사용자 관점에서 테스트합니다. (예: UI를 통한 로그인, 상품 추가, 주문 과정 전체)

## 🎯 체크리스트

### 코드 품질 및 유지보수 체크리스트

-   [x] **계층형 아키텍처** 규칙을 준수하여 관심사를 명확히 분리했는가?
-   [x] **SOLID 원칙**을 고려하여 코드를 작성했는가? (특히 단일 책임, 의존성 역전)
-   [x] **인터페이스 기반 프로그래밍**으로 결합도를 낮추고 유연성을 확보했는가?
-   [x] **생성자 주입**을 우선적으로 사용하여 의존성을 명확히 관리했는가?
-   [x] **명확하고 구체적인 예외 처리**를 적용하고, 적절한 로깅을 수행했는가?
-   [x] **로깅 시스템**을 활용하여 애플리케이션의 동작을 추적할 수 있는가?

### 성능 체크리스트

-   [x] **N+1 문제**를 방지하기 위해 `Fetch`/`FetchMany`를 적절히 사용했는가?
-   [x] **Lazy Loading**의 장점을 활용하되, 필요한 경우 Eager Loading으로 최적화했는가?
-   [x] **Second Level Cache**를 활성화하고 중요한 엔티티/쿼리에 적용했는가?
-   [x] **Batch Fetching** 설정을 통해 컬렉션 로딩 성능을 최적화했는가?
-   [x] **Projection**을 사용하여 필요한 데이터만 조회했는가?
-   [x] **Connection Pool**이 적절히 설정되어 있는가? (DB 설정 관련)

### 보안 체크리스트

-   [x] 모든 사용자 입력에 대해 **SQL Injection** 방지 (파라미터 바인딩 사용)
-   [x] 웹 페이지 출력 시 **XSS** 방지 (HTML Encoding)
-   [x] 중요한 액션에 **CSRF 토큰**을 사용하여 공격 방지
-   [x] **비밀번호**는 평문이 아닌 안전한 **해시(Salt 포함)** 방식으로 저장했는가?
-   [x] 민감한 정보는 암호화하여 저장하고, 적절한 접근 제어를 적용했는가?

## 💡 핵심 정리

이 튜토리얼은 Spring.NET과 NHibernate 기반 애플리케이션 개발의 핵심적인 모범 사례들을 다루었습니다.

-   **아키텍처**: 계층형 아키텍처를 통해 역할 분리 및 유지보수성 확보.
-   **성능**: Lazy/Eager Loading, 캐싱, 배치, 프로젝션을 통한 최적화.
-   **보안**: 일반적인 웹 취약점(SQLi, XSS, CSRF) 및 비밀번호 관리에 대한 방어 기법.
-   **코드 품질**: SOLID 원칙, 견고한 예외 처리, 효과적인 로깅 구현.
-   **테스트**: 단위 테스트를 통한 코드 검증 및 안정성 확보.

이러한 베스트 프랙티스들을 적용하면 더욱 안정적이고, 성능이 우수하며, 유지보수가 용이한 애플리케이션을 개발할 수 있을 것입니다.

## 🎓 축하합니다!

Spring.NET + NHibernate 튜토리얼의 핵심 내용을 모두 학습했습니다!

이제 다음을 할 수 있습니다:
-   ✅ Spring.NET의 IoC/DI(제어의 역전/의존성 주입) 컨테이너 활용
-   ✅ NHibernate ORM을 이용한 객체-관계 매핑 및 데이터베이스 연동
-   ✅ 계층형 아키텍처 기반의 애플리케이션 설계 및 구현
-   ✅ 선언적 트랜잭션을 포함한 효과적인 트랜잭션 관리
-   ✅ 일반적인 웹 보안 취약점으로부터 애플리케이션 보호
-   ✅ 애플리케이션의 성능 최적화 및 코드 품질 관리
-   ✅ 단위 테스트를 통한 코드 안정성 검증

이제 학습한 내용을 바탕으로 자신만의 Spring.NET + NHibernate 프로젝트를 시작해보세요! 🚀
```

## 🚀 다음 단계

다음: **[15-advanced-nhibernate-queries.md](./15-advanced-nhibernate-queries.md)** - 고급 NHibernate 쿼리
