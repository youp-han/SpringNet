# 16. Stored Procedure 사용법

## 📚 학습 목표

- Stored Procedure 생성 및 실행
- IN/OUT 파라미터 처리
- 결과 집합 반환
- NHibernate에서 프로시저 호출
- Spring.NET 통합

## 🎯 Stored Procedure란?

**Stored Procedure**는 데이터베이스에 저장된 SQL 프로그램으로, 복잡한 비즈니스 로직을 DB에서 실행할 수 있습니다.

**장점**:
- ✅ 성능 향상 (사전 컴파일)
- ✅ 네트워크 트래픽 감소
- ✅ 보안 강화
- ✅ 재사용성

**단점**:
- ❌ DB 종속성 증가
- ❌ 디버깅 어려움
- ❌ 버전 관리 어려움

## 🛠️ SQL Server Stored Procedure 생성

### 1. 단순 조회 프로시저

```sql
-- 게시글 조회
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

### 2. 파라미터가 있는 프로시저

```sql
-- 검색 프로시저
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

### 3. OUT 파라미터 프로시저

```sql
-- 게시글 생성 후 ID 반환
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

### 4. 복잡한 비즈니스 로직 프로시저

```sql
-- 주문 생성 (트랜잭션 포함)
CREATE PROCEDURE sp_CreateOrder
    @UserId INT,
    @ShippingAddress NVARCHAR(500),
    @ReceiverName NVARCHAR(100),
    @ReceiverPhone NVARCHAR(20),
    @OrderId INT OUTPUT,
    @TotalPrice DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        -- 1. 장바구니 조회
        DECLARE @CartId INT;
        SELECT @CartId = Id FROM Carts WHERE UserId = @UserId;

        IF @CartId IS NULL
        BEGIN
            RAISERROR('장바구니가 비어있습니다.', 16, 1);
            RETURN;
        END

        -- 2. 총액 계산
        SELECT @TotalPrice = SUM(Price * Quantity)
        FROM CartItems
        WHERE CartId = @CartId;

        -- 3. 주문 생성
        INSERT INTO Orders (UserId, OrderDate, Status, TotalPrice,
                           ShippingAddress, ReceiverName, ReceiverPhone)
        VALUES (@UserId, GETDATE(), 'Pending', @TotalPrice,
               @ShippingAddress, @ReceiverName, @ReceiverPhone);

        SET @OrderId = SCOPE_IDENTITY();

        -- 4. 주문 항목 생성 및 재고 차감
        INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price)
        SELECT @OrderId, ProductId, Quantity, Price
        FROM CartItems
        WHERE CartId = @CartId;

        UPDATE p
        SET p.Stock = p.Stock - ci.Quantity
        FROM Products p
        INNER JOIN CartItems ci ON p.Id = ci.ProductId
        WHERE ci.CartId = @CartId;

        -- 5. 장바구니 비우기
        DELETE FROM CartItems WHERE CartId = @CartId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
```

### 5. 여러 결과 집합 반환

```sql
-- 게시글과 댓글 함께 반환
CREATE PROCEDURE sp_GetBoardWithReplies
    @BoardId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 첫 번째 결과 집합: 게시글
    SELECT Id, Title, Content, Author, ViewCount, CreatedDate, ModifiedDate
    FROM Boards
    WHERE Id = @BoardId;

    -- 두 번째 결과 집합: 댓글
    SELECT Id, Content, Author, CreatedDate
    FROM Replies
    WHERE BoardId = @BoardId
    ORDER BY CreatedDate;
END
GO
```

## 🔍 NHibernate에서 프로시저 호출

### 1. 단순 조회 프로시저

```csharp
public Board GetBoardById(int boardId)
{
    var query = session.GetNamedQuery("sp_GetBoardById")
        .SetParameter("BoardId", boardId);

    return query.UniqueResult<Board>();
}

// 또는 Native SQL 사용
public Board GetBoardById(int boardId)
{
    var sql = "EXEC sp_GetBoardById :boardId";

    var board = session.CreateSQLQuery(sql)
        .AddEntity(typeof(Board))
        .SetParameter("boardId", boardId)
        .UniqueResult<Board>();

    return board;
}
```

### 2. 매핑 파일에 프로시저 정의

`Board.hbm.xml`:

```xml
<hibernate-mapping>
    <class name="Board" table="Boards">
        <!-- ... -->

        <!-- Stored Procedure 정의 -->
        <sql-query name="sp_GetBoardById">
            <return alias="board" class="Board"/>
            EXEC sp_GetBoardById :boardId
        </sql-query>

        <sql-query name="sp_SearchBoards">
            <return alias="board" class="Board"/>
            EXEC sp_SearchBoards :keyword, :pageNumber, :pageSize
        </sql-query>
    </class>
</hibernate-mapping>
```

사용:

```csharp
var board = session.GetNamedQuery("sp_GetBoardById")
    .SetInt32("boardId", 1)
    .UniqueResult<Board>();

var boards = session.GetNamedQuery("sp_SearchBoards")
    .SetString("keyword", "spring")
    .SetInt32("pageNumber", 1)
    .SetInt32("pageSize", 10)
    .List<Board>();
```

### 3. OUT 파라미터 처리

```csharp
public int CreateBoard(string title, string content, string author)
{
    using (var connection = sessionFactory.OpenSession().Connection)
    {
        using (var command = connection.CreateCommand())
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

            // OUT 파라미터 값 가져오기
            return (int)boardIdParam.Value;
        }
    }
}
```

### 4. IDbCommand 직접 사용

```csharp
public class BoardRepository : Repository<Board>, IBoardRepository
{
    public (int orderId, decimal totalPrice) CreateOrder(
        int userId,
        string shippingAddress,
        string receiverName,
        string receiverPhone)
    {
        using (var connection = sessionFactory.OpenStatelessSession().Connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_CreateOrder";

                // IN 파라미터
                command.Parameters.Add(new SqlParameter("@UserId", userId));
                command.Parameters.Add(new SqlParameter("@ShippingAddress", shippingAddress));
                command.Parameters.Add(new SqlParameter("@ReceiverName", receiverName));
                command.Parameters.Add(new SqlParameter("@ReceiverPhone", receiverPhone));

                // OUT 파라미터
                var orderIdParam = new SqlParameter("@OrderId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(orderIdParam);

                var totalPriceParam = new SqlParameter("@TotalPrice", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output,
                    Precision = 18,
                    Scale = 2
                };
                command.Parameters.Add(totalPriceParam);

                command.ExecuteNonQuery();

                return (
                    orderId: (int)orderIdParam.Value,
                    totalPrice: (decimal)totalPriceParam.Value
                );
            }
        }
    }
}
```

### 5. 여러 결과 집합 처리

```csharp
public BoardWithRepliesDto GetBoardWithReplies(int boardId)
{
    using (var connection = sessionFactory.OpenSession().Connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "sp_GetBoardWithReplies";
            command.Parameters.Add(new SqlParameter("@BoardId", boardId));

            using (var reader = command.ExecuteReader())
            {
                var result = new BoardWithRepliesDto();

                // 첫 번째 결과 집합: 게시글
                if (reader.Read())
                {
                    result.Board = new BoardDto
                    {
                        Id = reader.GetInt32(0),
                        Title = reader.GetString(1),
                        Content = reader.GetString(2),
                        Author = reader.GetString(3),
                        ViewCount = reader.GetInt32(4),
                        CreatedDate = reader.GetDateTime(5)
                    };
                }

                // 두 번째 결과 집합: 댓글
                if (reader.NextResult())
                {
                    result.Replies = new List<ReplyDto>();

                    while (reader.Read())
                    {
                        result.Replies.Add(new ReplyDto
                        {
                            Id = reader.GetInt32(0),
                            Content = reader.GetString(1),
                            Author = reader.GetString(2),
                            CreatedDate = reader.GetDateTime(3)
                        });
                    }
                }

                return result;
            }
        }
    }
}
```

## 🏗️ Repository 패턴 적용

### IProcedureRepository 인터페이스

```csharp
public interface IProcedureRepository
{
    Board GetBoardById(int boardId);
    IList<Board> SearchBoards(string keyword, int pageNumber, int pageSize);
    int CreateBoard(string title, string content, string author);
    (int orderId, decimal totalPrice) CreateOrder(int userId, OrderRequestDto request);
    BoardStatistics GetBoardStatistics(int boardId);
}
```

### ProcedureRepository 구현

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
        using (var session = sessionFactory.OpenSession())
        {
            return session.GetNamedQuery("sp_GetBoardById")
                .SetInt32("boardId", boardId)
                .UniqueResult<Board>();
        }
    }

    public IList<Board> SearchBoards(string keyword, int pageNumber, int pageSize)
    {
        using (var session = sessionFactory.OpenSession())
        {
            return session.GetNamedQuery("sp_SearchBoards")
                .SetString("keyword", keyword)
                .SetInt32("pageNumber", pageNumber)
                .SetInt32("pageSize", pageSize)
                .List<Board>();
        }
    }

    public int CreateBoard(string title, string content, string author)
    {
        using (var connection = sessionFactory.OpenSession().Connection)
        using (var command = connection.CreateCommand())
        {
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "sp_CreateBoard";

            command.Parameters.Add(new SqlParameter("@Title", title));
            command.Parameters.Add(new SqlParameter("@Content", content));
            command.Parameters.Add(new SqlParameter("@Author", author));

            var boardIdParam = new SqlParameter("@BoardId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(boardIdParam);

            command.ExecuteNonQuery();

            return (int)boardIdParam.Value;
        }
    }

    public BoardStatistics GetBoardStatistics(int boardId)
    {
        using (var connection = sessionFactory.OpenSession().Connection)
        using (var command = connection.CreateCommand())
        {
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "sp_GetBoardStatistics";
            command.Parameters.Add(new SqlParameter("@BoardId", boardId));

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new BoardStatistics
                    {
                        BoardId = boardId,
                        ViewCount = reader.GetInt32(reader.GetOrdinal("ViewCount")),
                        ReplyCount = reader.GetInt32(reader.GetOrdinal("ReplyCount")),
                        LastReplyDate = reader.IsDBNull(reader.GetOrdinal("LastReplyDate"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("LastReplyDate"))
                    };
                }

                return null;
            }
        }
    }
}
```

## 🔧 Spring.NET 통합

### applicationContext.xml

```xml
<!-- Procedure Repository -->
<object id="procedureRepository"
        type="SpringNet.Data.Repositories.ProcedureRepository, SpringNet.Data">
    <constructor-arg ref="sessionFactory" />
</object>

<!-- Service에서 사용 -->
<object id="boardService"
        type="SpringNet.Service.BoardService, SpringNet.Service">
    <constructor-arg ref="boardRepository" />
    <property name="ProcedureRepository" ref="procedureRepository" />
</object>
```

### Service에서 사용

```csharp
public class BoardService : IBoardService
{
    private readonly IBoardRepository boardRepository;
    public IProcedureRepository ProcedureRepository { get; set; }

    public BoardService(IBoardRepository boardRepository)
    {
        this.boardRepository = boardRepository;
    }

    public BoardDto GetBoardFast(int id)
    {
        // 프로시저 사용 (성능 최적화)
        var board = ProcedureRepository.GetBoardById(id);
        return MapToDto(board);
    }

    public PagedResultDto<BoardDto> SearchBoardsFast(string keyword, int page, int pageSize)
    {
        // 프로시저로 검색
        var boards = ProcedureRepository.SearchBoards(keyword, page, pageSize);

        return new PagedResultDto<BoardDto>
        {
            Items = boards.Select(MapToDto).ToList(),
            PageNumber = page,
            PageSize = pageSize
        };
    }
}
```

## 💡 프로시저 vs ORM

### 프로시저 사용이 적합한 경우

✅ **복잡한 비즈니스 로직** (여러 테이블 조작)
✅ **대량 데이터 처리** (배치 작업)
✅ **성능이 중요한 경우**
✅ **기존 레거시 시스템**

### ORM 사용이 적합한 경우

✅ **단순 CRUD**
✅ **객체 지향적 설계**
✅ **DB 독립성 필요**
✅ **빠른 개발**

## 🎯 베스트 프랙티스

### 1. 프로시저 명명 규칙

```sql
-- ✅ 좋은 예
sp_GetBoardById
sp_SearchBoards
sp_CreateOrder
sp_UpdateBoardViewCount

-- ❌ 나쁜 예
GetBoard          -- sp_ 접두사 없음
board_search      -- 일관성 없음
Update_Board      -- 언더스코어 혼용
```

### 2. 파라미터 명명 규칙

```sql
-- ✅ 좋은 예
@BoardId
@Keyword
@PageNumber

-- ❌ 나쁜 예
@id               -- 너무 짧음
@board_id         -- 언더스코어
@BOARDID          -- 대문자
```

### 3. 에러 처리

```sql
CREATE PROCEDURE sp_CreateBoard
    @Title NVARCHAR(200),
    @Content NVARCHAR(MAX),
    @Author NVARCHAR(50),
    @BoardId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- 유효성 검사
        IF @Title IS NULL OR LEN(@Title) = 0
        BEGIN
            RAISERROR('제목은 필수입니다.', 16, 1);
            RETURN -1;
        END

        -- 비즈니스 로직
        INSERT INTO Boards (Title, Content, Author, ViewCount, CreatedDate)
        VALUES (@Title, @Content, @Author, 0, GETDATE());

        SET @BoardId = SCOPE_IDENTITY();
        RETURN 0; -- 성공
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
        RETURN -1; -- 실패
    END CATCH
END
```

## 💡 핵심 정리

### Stored Procedure 장단점

**장점**:
- ✅ 성능 향상
- ✅ 네트워크 트래픽 감소
- ✅ 보안 강화
- ✅ 복잡한 로직 처리

**단점**:
- ❌ DB 종속성
- ❌ 디버깅 어려움
- ❌ 버전 관리 어려움
- ❌ 테스트 복잡

### 사용 가이드

- **복잡한 로직**: Stored Procedure
- **단순 CRUD**: ORM (NHibernate)
- **성능 중요**: 프로파일링 후 결정
- **하이브리드**: 상황에 맞게 혼용

## 🚀 다음 단계

다음: **[17-session-management.md](./17-session-management.md)** - NHibernate 세션 관리
