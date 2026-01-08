# 14. 베스트 프랙티스

## 📚 학습 목표

- Spring.NET + NHibernate 실전 팁
- 성능 최적화
- 보안 강화
- 코드 품질 향상

## 🎯 아키텍처 패턴

### Layered Architecture (계층 구조)

```
┌─────────────────────────┐
│   Presentation Layer    │  ← Controllers, Views
├─────────────────────────┤
│   Service Layer         │  ← Business Logic
├─────────────────────────┤
│   Data Access Layer     │  ← Repositories
├─────────────────────────┤
│   Domain Layer          │  ← Entities
└─────────────────────────┘
```

**규칙**:
- ✅ 각 레이어는 바로 아래 레이어만 참조
- ✅ Controller는 Service만 호출 (Repository 직접 호출 금지)
- ✅ Service는 비즈니스 로직 담당
- ✅ Repository는 데이터 액세스만

## 🚀 성능 최적화

### 1. Lazy Loading vs Eager Loading

```csharp
// ❌ N+1 문제
var boards = session.Query<Board>().ToList();
foreach (var board in boards)
{
    // 각 board마다 SELECT 실행!
    Console.WriteLine(board.Replies.Count);
}

// ✅ Eager Loading으로 해결
var boards = session.Query<Board>()
    .Fetch(b => b.Replies)
    .ToList();
```

### 2. Second Level Cache 사용

```xml
<!-- hibernate.cfg.xml -->
<property name="cache.use_second_level_cache">true</property>
<property name="cache.provider_class">
    NHibernate.Cache.HashtableCacheProvider
</property>
```

```xml
<!-- Product.hbm.xml -->
<class name="Product" table="Products">
    <cache usage="read-write" />
    <!-- ... -->
</class>
```

### 3. Batch Fetching

```xml
<property name="adonet.batch_size">20</property>
```

### 4. Projection 사용

```csharp
// ❌ 전체 엔티티 조회 (무거움)
var boards = session.Query<Board>().ToList();

// ✅ 필요한 필드만 조회
var boardSummaries = session.Query<Board>()
    .Select(b => new { b.Id, b.Title, b.Author })
    .ToList();
```

## 🔒 보안 강화

### 1. SQL Injection 방지

```csharp
// ❌ 위험: SQL Injection 가능
var query = $"from Board b where b.Title = '{userInput}'";
var boards = session.CreateQuery(query).List<Board>();

// ✅ 안전: 파라미터 바인딩
var boards = session.Query<Board>()
    .Where(b => b.Title == userInput)
    .ToList();
```

### 2. XSS 방지

```html
<!-- ❌ 위험 -->
@Html.Raw(Model.Content)

<!-- ✅ 안전 -->
@Model.Content

<!-- 또는 화이트리스트 기반 허용 -->
@Html.Sanitize(Model.Content)
```

### 3. CSRF 방지

```csharp
// 모든 POST 요청에 적용
[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult Create(...)
{
}
```

```html
<!-- View에서 토큰 포함 -->
@using (Html.BeginForm())
{
    @Html.AntiForgeryToken()
    <!-- ... -->
}
```

### 4. 비밀번호 저장

```csharp
// ❌ 절대 금지: 평문 저장
user.Password = password;

// ✅ 해시 + Salt 사용
using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000))
{
    user.PasswordHash = Convert.ToBase64String(pbkdf2.GetBytes(32));
}

// 더 나은 방법: BCrypt 라이브러리
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
```

## 💡 코드 품질

### 1. SOLID 원칙

**Single Responsibility Principle**:
```csharp
// ❌ 나쁜 예: 여러 책임
public class UserService
{
    public void Register() { }
    public void SendEmail() { }
    public void ValidateInput() { }
}

// ✅ 좋은 예: 단일 책임
public class UserService
{
    public void Register() { }
}

public class EmailService
{
    public void SendEmail() { }
}

public class ValidationService
{
    public void ValidateInput() { }
}
```

**Dependency Inversion Principle**:
```csharp
// ✅ 인터페이스에 의존
public class OrderService
{
    private readonly IOrderRepository repository;

    public OrderService(IOrderRepository repository)
    {
        this.repository = repository;
    }
}
```

### 2. 예외 처리

```csharp
// ❌ 예외 무시
try
{
    DoSomething();
}
catch { }

// ❌ 너무 광범위한 예외
try
{
    DoSomething();
}
catch (Exception ex)
{
    // 모든 예외를 잡아버림
}

// ✅ 구체적인 예외 처리
try
{
    DoSomething();
}
catch (ArgumentException ex)
{
    // 특정 예외만 처리
    logger.Warning("Invalid argument", ex);
    throw;
}
catch (DataException ex)
{
    logger.Error("Database error", ex);
    throw new ApplicationException("데이터 처리 중 오류가 발생했습니다.", ex);
}
```

### 3. Logging

```csharp
// log4net 설정
public class BoardService : IBoardService
{
    private static readonly ILog logger = LogManager.GetLogger(typeof(BoardService));

    public BoardDto GetBoard(int id)
    {
        logger.Info($"게시글 조회: {id}");

        try
        {
            var board = repository.GetById(id);
            logger.Debug($"게시글 조회 완료: {board.Title}");
            return MapToDto(board);
        }
        catch (Exception ex)
        {
            logger.Error($"게시글 조회 실패: {id}", ex);
            throw;
        }
    }
}
```

## 📊 테스트

### Unit Test

```csharp
[TestFixture]
public class BoardServiceTests
{
    private Mock<IBoardRepository> mockRepository;
    private BoardService service;

    [SetUp]
    public void Setup()
    {
        mockRepository = new Mock<IBoardRepository>();
        service = new BoardService(mockRepository.Object);
    }

    [Test]
    public void GetBoard_ValidId_ReturnsBoard()
    {
        // Arrange
        var expected = new Board { Id = 1, Title = "Test" };
        mockRepository.Setup(r => r.GetById(1)).Returns(expected);

        // Act
        var result = service.GetBoard(1);

        // Assert
        Assert.AreEqual(expected.Title, result.Title);
    }
}
```

## 🎯 체크리스트

### 코드 리뷰 체크리스트

- [ ] 모든 public 메서드에 적절한 예외 처리
- [ ] SQL Injection 취약점 없음
- [ ] XSS 취약점 없음
- [ ] CSRF 토큰 사용
- [ ] 비밀번호 해시 저장
- [ ] 트랜잭션 적절히 사용
- [ ] N+1 문제 없음
- [ ] 적절한 인덱스 설정
- [ ] 로깅 적절히 사용
- [ ] 단위 테스트 작성

### 성능 체크리스트

- [ ] Second Level Cache 활성화
- [ ] Lazy Loading 적절히 사용
- [ ] Batch Fetching 설정
- [ ] Connection Pool 설정
- [ ] 불필요한 데이터 조회 제거
- [ ] Projection 사용

## 💡 핵심 정리

### Spring.NET 베스트 프랙티스

✅ 인터페이스 기반 프로그래밍
✅ 생성자 주입 우선
✅ 단일 책임 원칙
✅ 의존성 최소화

### NHibernate 베스트 프랙티스

✅ Session 범위 최소화
✅ Transaction 명확히 관리
✅ N+1 문제 방지
✅ Second Level Cache 활용
✅ Lazy Loading 이해하고 사용

### 보안 베스트 프랙티스

✅ 입력 검증
✅ 파라미터 바인딩
✅ XSS 방지
✅ CSRF 방지
✅ 비밀번호 암호화

## 🎓 축하합니다!

Spring.NET + NHibernate 학습을 완료했습니다!

이제 다음을 할 수 있습니다:
- ✅ 엔터프라이즈 애플리케이션 설계
- ✅ Spring.NET IoC/DI 활용
- ✅ NHibernate ORM 사용
- ✅ 레이어드 아키텍처 구현
- ✅ 트랜잭션 관리
- ✅ 보안 강화

계속 학습하고 실전 프로젝트를 만들어보세요! 🚀
