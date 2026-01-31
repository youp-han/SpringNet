# 16. Stored Procedure 사용법

## 📚 학습 목표

- Stored Procedure(저장 프로시저)의 장단점 이해
- SQL Server에서 Stored Procedure 생성 및 실행
- IN/OUT 파라미터 처리 및 결과 집합 반환
- NHibernate에서 다양한 방법으로 프로시저 호출
- Repository 패턴과 Spring.NET을 통한 통합

## 🎯 Stored Procedure란?

**Stored Procedure**는 데이터베이스에 저장되어 재사용 가능한 SQL 프로그램으로, 복잡한 비즈니스 로직을 데이터베이스 서버에서 직접 실행할 수 있게 해줍니다.

**장점**:
-   ✅ **성능 향상**: 최초 실행 시 컴파일되어 실행 계획이 캐싱되므로 반복 호출 시 성능이 좋습니다.
-   ✅ **네트워크 트래픽 감소**: 여러 SQL 문을 한 번의 호출로 실행하여 클라이언트-서버 간의 네트워크 왕복을 줄입니다.
-   ✅ **보안 강화**: 사용자에게 테이블에 대한 직접 접근 권한 대신 프로시저 실행 권한만 부여하여 보안을 강화할 수 있습니다.
-   ✅ **모듈화 및 재사용성**: 비즈니스 로직을 DB에 캡슐화하여 여러 애플리케이션에서 재사용할 수 있습니다.

**단점**:
-   ❌ **DB 종속성 증가**: 특정 데이터베이스의 SQL 방언(Dialect)에 종속되어 DB 변경 시 수정이 어렵습니다.
-   ❌ **디버깅 및 유지보수 어려움**: 애플리케이션 코드와 분리되어 있어 디버깅 및 버전 관리가 어렵습니다.
-   ❌ **테스트 복잡성**: 단위 테스트가 어렵고 통합 테스트에 의존하게 될 수 있습니다.

> **참고**: 이 튜토리얼의 모든 Stored Procedure 예제는 **Microsoft SQL Server (T-SQL)** 구문을 기준으로 작성되었습니다.

## 🛠️ SQL Server Stored Procedure 생성 예제

### 1. 단순 조회 프로시저

```sql
-- 특정 ID의 게시글 조회
CREATE PROCEDURE sp_GetBoardById
    @BoardId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, Content, Author, ViewCount, CreatedDate, ModifiedDate
    FROM Boards
    WHERE Id = @BoardId;
END
GO
```

### 2. 페이징을 포함한 검색 프로시저

```sql
-- 키워드로 게시글을 검색하고 페이징 처리
CREATE PROCEDURE sp_SearchBoards
    @Keyword NVARCHAR(100),
    @PageNumber INT = 1,
    @PageSize INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    SELECT Id, Title, Author, ViewCount, CreatedDate
    FROM Boards
    WHERE Title LIKE '%' + @Keyword + '%'
       OR Content LIKE '%' + @Keyword + '%'
    ORDER BY CreatedDate DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
```

### 3. OUT 파라미터를 사용한 생성 프로시저

```sql
-- 게시글 생성 후 새로 생성된 ID를 OUTPUT 파라미터로 반환
CREATE PROCEDURE sp_CreateBoard
    @Title NVARCHAR(200),
    @Content NVARCHAR(MAX),
    @Author NVARCHAR(50),
    @BoardId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Boards (Title, Content, Author, ViewCount, CreatedDate)
    VALUES (@Title, @Content, @Author, 0, GETDATE());

    SET @BoardId = SCOPE_IDENTITY();
END
GO
```

### 4. 여러 결과 집합(Multiple Result Sets) 반환 프로시저

```sql
-- 게시글과 해당 게시글의 댓글 목록을 두 개의 결과 집합으로 반환
CREATE PROCEDURE sp_GetBoardWithReplies
    @BoardId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 첫 번째 결과 집합: 게시글 정보
    SELECT Id, Title, Content, Author, ViewCount, CreatedDate, ModifiedDate
    FROM Boards
    WHERE Id = @BoardId;

    -- 두 번째 결과 집합: 댓글 목록
    SELECT Id, Content, Author, CreatedDate
    FROM Replies
    WHERE BoardId = @BoardId
    ORDER BY CreatedDate;
END
GO
```

## 🔍 NHibernate에서 프로시저 호출

NHibernate는 프로시저를 호출하는 여러 방법을 제공합니다. 각 방법의 장단점을 이해하고 상황에 맞게 사용하는 것이 중요합니다.

### 방법 1: `GetNamedQuery` 사용 (매핑 파일에 정의)

가장 권장되는 방법 중 하나로, 프로시저 호출을 매핑 파일(`.hbm.xml`)에 **Named Query**로 정의하고 코드에서는 해당 이름을 호출합니다. 이를 통해 SQL 코드를 C# 코드로부터 분리할 수 있습니다.

`Board.hbm.xml`에 프로시저 호출을 위한 `sql-query` 정의 추가:
```xml
<hibernate-mapping>
    <class name="Board" table="Boards">
        <!-- ... 기존 property 및 collection 매핑 ... -->

        <!-- Stored Procedure를 위한 Named SQL Query 정의 -->
        <sql-query name="sp_GetBoardById">
            <!-- 프로시저 결과가 Board 엔티티에 매핑됨을 명시 -->
            <return class="Board"/>
            EXEC sp_GetBoardById :boardId
        </sql-query>

        <sql-query name="sp_SearchBoards">
            <return class="Board"/>
            EXEC sp_SearchBoards :keyword, :pageNumber, :pageSize
        </sql-query>
    </class>
</hibernate-mapping>
```
> **참고**: `sql-query`의 `name` 속성은 가독성과 유지보수를 위해 `엔티티명.프로시저명` (예: `Board.sp_GetBoardById`)과 같이 충돌을 방지하는 네이밍 컨벤션을 사용하는 것이 좋습니다.

**서비스/리포지토리 계층에서 사용**:
```csharp
public Board GetBoardById_NamedQuery(int boardId)
{
    // 세션 팩토리로부터 현재 세션을 가져옴
    var session = sessionFactory.GetCurrentSession();

    return session.GetNamedQuery("sp_GetBoardById")
        .SetInt32("boardId", boardId)
        .UniqueResult<Board>();
}

public IList<Board> SearchBoards_NamedQuery(string keyword, int page, int size)
{
    var session = sessionFactory.GetCurrentSession();
    
    return session.GetNamedQuery("sp_SearchBoards")
        .SetString("keyword", keyword)
        .SetInt32("pageNumber", page)
        .SetInt32("pageSize", size)
        .List<Board>();
}
```

### 방법 2: `CreateSQLQuery` 사용 (Native SQL)

코드에서 직접 SQL(프로시저 호출 구문 포함)을 실행하는 방식입니다. 간단한 테스트나 동적으로 쿼리를 변경해야 할 때 유용합니다.

```csharp
public Board GetBoardById_NativeSql(int boardId)
{
    var session = sessionFactory.GetCurrentSession();
    var sql = "EXEC sp_GetBoardById :boardId";

    return session.CreateSQLQuery(sql)
        .AddEntity(typeof(Board)) // 결과가 Board 엔티티에 매핑됨을 명시
        .SetParameter("boardId", boardId)
        .UniqueResult<Board>();
}
```

### 방법 3: `IDbCommand` 직접 사용 (OUT 파라미터 및 다중 결과 집합 처리)

OUT 파라미터가 있거나, 여러 결과 집합을 반환하는 프로시저를 호출할 때는 NHibernate의 API만으로는 한계가 있습니다. 이 경우, NHibernate 세션에서 기본 ADO.NET `IDbConnection`을 얻어와 `IDbCommand`를 직접 사용하여 처리하는 것이 가장 효과적입니다.

**누락된 DTO 정의 추가**:
`BoardWithRepliesDto`와 `BoardStatistics` DTO를 `SpringNet.Service/DTOs` 폴더의 DTO 파일에 추가합니다.
```csharp
public class BoardWithRepliesDto
{
    public BoardDto Board { get; set; }
    public IList<ReplyDto> Replies { get; set; }
}
public class BoardStatistics
{
    public int BoardId { get; set; }
    public int ViewCount { get; set; }
    public int ReplyCount { get; set; }
    public DateTime? LastReplyDate { get; set; }
}
```

**리포지토리 계층에서 `IDbCommand` 사용 예시**:
```csharp
// (리포지토리 클래스 내부)
public int CreateBoard_AdoNet(string title, string content, string author)
{
    // GetCurrentSession()에서 Connection을 얻어와 현재 트랜잭션에 참여시킴
    using (var command = sessionFactory.GetCurrentSession().Connection.CreateCommand())
    {
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_CreateBoard";

        // IN 파라미터
        command.Parameters.Add(new SqlParameter("@Title", title));
        command.Parameters.Add(new SqlParameter("@Content", content));
        command.Parameters.Add(new SqlParameter("@Author", author));

        // OUT 파라미터
        var boardIdParam = new SqlParameter("@BoardId", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(boardIdParam);

        command.ExecuteNonQuery();

        return (int)boardIdParam.Value;
    }
}

public BoardWithRepliesDto GetBoardWithReplies_AdoNet(int boardId)
{
    using (var command = sessionFactory.GetCurrentSession().Connection.CreateCommand())
    {
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "sp_GetBoardWithReplies";
        command.Parameters.Add(new SqlParameter("@BoardId", boardId));

        using (var reader = command.ExecuteReader())
        {
            var result = new BoardWithRepliesDto();

            // 첫 번째 결과 집합 (게시글)
            if (reader.Read())
            {
                result.Board = new BoardDto { /* reader에서 데이터 매핑 */ };
            }

            // 두 번째 결과 집합 (댓글)
            if (reader.NextResult())
            {
                result.Replies = new List<ReplyDto>();
                while (reader.Read())
                {
                    result.Replies.Add(new ReplyDto { /* reader에서 데이터 매핑 */ });
                }
            }
            return result;
        }
    }
}
```

## 🏗️ Repository 패턴 및 Spring.NET 통합

프로시저 호출 로직을 별도의 Repository로 캡슐화하여 Service 계층의 비즈니스 로직과 분리하는 것이 좋습니다.

### `IProcedureRepository` 인터페이스 및 구현

`SpringNet.Data/Repositories/IProcedureRepository.cs`:
```csharp
public interface IProcedureRepository
{
    Board GetBoardById(int boardId);
    IList<Board> SearchBoards(string keyword, int pageNumber, int pageSize);
    int CreateBoard(string title, string content, string author);
    (int orderId, decimal totalPrice) CreateOrder(int userId, string shippingAddress, string receiverName, string receiverPhone);
}
```

`SpringNet.Data/Repositories/ProcedureRepository.cs`:
```csharp
public class ProcedureRepository : IProcedureRepository
{
    private readonly ISessionFactory sessionFactory;

    public ProcedureRepository(ISessionFactory sessionFactory)
    {
        this.sessionFactory = sessionFactory;
    }

    public Board GetBoardById(int boardId)
    {
        return sessionFactory.GetCurrentSession().GetNamedQuery("sp_GetBoardById")
            .SetInt32("boardId", boardId)
            .UniqueResult<Board>();
    }

    public IList<Board> SearchBoards(string keyword, int pageNumber, int pageSize)
    {
        return sessionFactory.GetCurrentSession().GetNamedQuery("sp_SearchBoards")
            .SetString("keyword", keyword)
            .SetInt32("pageNumber", pageNumber)
            .SetInt32("pageSize", pageSize)
            .List<Board>();
    }
    
    // CreateBoard, CreateOrder 등 IDbCommand를 사용하는 메서드 구현...
}
```

### Spring.NET 설정 및 사용

`SpringNet.Web/Config/dataAccess.xml`에 `ProcedureRepository`를 Bean으로 등록합니다.
```xml
<!-- dataAccess.xml -->
    <!-- ... 기존 Repository Bean 설정 ... -->

    <!-- Procedure Repository -->
    <object id="procedureRepository"
            type="SpringNet.Data.Repositories.ProcedureRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```

`BoardService`에서 생성자 주입을 통해 `IProcedureRepository`를 사용합니다.
```csharp
// BoardService.cs
public class BoardService : IBoardService
{
    private readonly IBoardRepository boardRepository;
    private readonly IReplyRepository replyRepository;
    private readonly IProcedureRepository procedureRepository; // 프로시저 리포지토리 주입
    private readonly ILogger logger;
    // ISessionFactory는 선언적 트랜잭션을 사용하므로 직접적인 의존성 제거 가능

    public BoardService(
        IBoardRepository boardRepository,
        IReplyRepository replyRepository,
        IProcedureRepository procedureRepository, // 생성자 주입
        ILogger logger)
    {
        this.boardRepository = boardRepository;
        this.replyRepository = replyRepository;
        this.procedureRepository = procedureRepository;
        this.logger = logger;
    }

    // 서비스 메서드에서 프로시저 호출
    public BoardDto GetBoardViaProcedure(int id)
    {
        var board = procedureRepository.GetBoardById(id);
        // ... DTO 매핑 ...
    }
}
```
`services.xml`에서 `boardService` Bean 정의를 업데이트하여 `procedureRepository`를 주입합니다.
```xml
<!-- services.xml -->
<object id="boardService" type="SpringNet.Service.BoardService, SpringNet.Service">
    <constructor-arg ref="boardRepository" />
    <constructor-arg ref="replyRepository" />
    <constructor-arg ref="procedureRepository" /> <!-- 의존성 추가 -->
    <constructor-arg ref="logger" />
</object>
```

## 💡 프로시저 vs ORM: 언제 무엇을 쓸까?

두 기술은 상호 배타적이지 않으며, 상황에 맞게 혼용하는 것이 가장 효과적입니다.

-   **프로시저 사용이 적합한 경우**:
    -   여러 테이블에 걸친 복잡한 비즈니스 로직을 한 번에 처리해야 할 때 (예: `sp_CreateOrder`)
    -   대량의 데이터를 처리하는 배치(Batch) 작업
    -   ORM으로는 표현하기 어렵거나 성능이 나오지 않는 극한의 성능 튜닝이 필요할 때
    -   기존에 Stored Procedure 기반으로 구축된 레거시 시스템과 연동해야 할 때

-   **ORM 사용이 적합한 경우**:
    -   일반적인 CRUD(생성, 읽기, 수정, 삭제) 작업
    -   객체 지향적인 설계와 비즈니스 로직을 애플리케이션 계층에 집중시키고 싶을 때
    -   데이터베이스 독립성을 유지해야 할 때
    -   빠른 개발 속도와 생산성이 중요할 때

## 🎯 베스트 프랙티스

1.  **명명 규칙**: 프로시저와 파라미터에 일관된 명명 규칙(`sp_VerbNoun`, `@ParameterName`)을 적용합니다.
2.  **에러 처리**: 프로시저 내에서 `TRY...CATCH` 블록을 사용하여 에러를 처리하고, `RAISERROR`를 통해 의미 있는 오류를 반환합니다.
3.  **트랜잭션**: 프로시저 내에서 데이터 변경이 발생하면 명시적으로 `BEGIN TRANSACTION`, `COMMIT`, `ROLLBACK`을 사용하여 트랜잭션을 관리합니다.
4.  **No ORM Anti-patterns**: 프로시저를 너무 남용하여 ORM의 장점(객체 그래프 관리, 캐싱 등)을 모두 포기하지 않도록 주의합니다. 간단한 CRUD까지 프로시저로 만드는 것은 비효율적일 수 있습니다.
5.  **캡슐화**: 프로시저 호출 로직을 애플리케이션의 서비스 계층에 직접 노출하지 않고, 데이터 접근 계층(Repository) 내에 캡슐화하여 책임 분리 원칙을 지킵니다.

## 💡 핵심 정리

-   **Stored Procedure**는 성능, 보안, 복잡한 로직 처리에서 장점을 가지지만 DB 종속성과 유지보수의 단점이 있습니다.
-   NHibernate는 **`GetNamedQuery`**, **`CreateSQLQuery`**, **`IDbCommand`** 등 프로시저를 호출하는 다양한 방법을 제공합니다.
-   **OUT 파라미터**나 **다중 결과 집합** 처리는 `IDbCommand`를 직접 사용하는 것이 가장 유연하고 효과적입니다.
-   프로시저 호출 로직은 별도의 **Repository**로 캡슐화하고 **생성자 주입**을 통해 사용하는 것이 좋습니다.
-   **ORM과 프로시저를 혼용**하는 하이브리드 접근 방식이 실용적인 해결책이 될 수 있습니다.

## 🚀 다음 단계

다음: **[17-session-management.md](./17-session-management.md)** - NHibernate 세션 관리 심화
