# 06. 게시판 Part 3: Service Layer

## 📚 학습 목표

- Service Layer의 역할 및 책임
- 비즈니스 로직 구현
- 트랜잭션 관리
- DTO (Data Transfer Object) 패턴

## 🎯 Service Layer란?

**Service Layer**는 비즈니스 로직을 담당하는 계층입니다.

```
Controller → Service → Repository → Database
```

**책임**:
- ✅ 비즈니스 로직 실행
- ✅ 트랜잭션 관리
- ✅ 여러 Repository 조합
- ✅ 유효성 검증
- ✅ DTO 변환

## 🛠️ Board Service 구현

### Step 1: DTO 클래스 생성

`SpringNet.Service/DTOs/BoardDto.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace SpringNet.Service.DTOs
{
    public class BoardDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int ReplyCount { get; set; }
    }

    public class BoardDetailDto : BoardDto
    {
        public IList<ReplyDto> Replies { get; set; }
    }

    public class ReplyDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PagedResultDto<T>
    {
        public IList<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
```

### Step 2: IBoardService 인터페이스

`SpringNet.Service/IBoardService.cs`:

```csharp
using SpringNet.Service.DTOs;

namespace SpringNet.Service
{
    public interface IBoardService
    {
        // CRUD
        int CreateBoard(string title, string content, string author);
        BoardDetailDto GetBoard(int id, bool increaseViewCount = true);
        PagedResultDto<BoardDto> GetBoards(int pageNumber, int pageSize);
        void UpdateBoard(int id, string title, string content, string currentUser);
        void DeleteBoard(int id, string currentUser);

        // 검색
        PagedResultDto<BoardDto> SearchBoards(string keyword, int pageNumber, int pageSize);

        // 댓글
        int AddReply(int boardId, string content, string author);
        void DeleteReply(int replyId, string currentUser);

        // 통계
        IList<BoardDto> GetRecentBoards(int count);
        IList<BoardDto> GetPopularBoards(int count);
    }
}
```

### Step 3: BoardService 구현

`SpringNet.Service/BoardService.cs`:

```csharp
using NHibernate;
using SpringNet.Data.Repositories;
using SpringNet.Domain.Entities;
using SpringNet.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Service
{
    public class BoardService : IBoardService
    {
        private readonly IBoardRepository boardRepository;
        private readonly IReplyRepository replyRepository;
        private readonly ISessionFactory sessionFactory;

        public BoardService(
            IBoardRepository boardRepository,
            IReplyRepository replyRepository,
            ISessionFactory sessionFactory)
        {
            this.boardRepository = boardRepository;
            this.replyRepository = replyRepository;
            this.sessionFactory = sessionFactory;
        }

        public int CreateBoard(string title, string content, string author)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("제목은 필수입니다.");
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("내용은 필수입니다.");

            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var board = new Board { Title = title, Content = content, Author = author };
                    boardRepository.Add(board);
                    tx.Commit();
                    return board.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public BoardDetailDto GetBoard(int id, bool increaseViewCount = true)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    // 댓글을 함께 가져오기 위해 Repository의 특정 메서드 사용
                    var board = boardRepository.GetWithReplies(id);
                    if (board == null) throw new ArgumentException($"게시글 {id}를 찾을 수 없습니다.");

                    if (increaseViewCount)
                    {
                        board.IncreaseViewCount();
                        // 변경된 내용을 DB에 반영하기 위해 Update 호출
                        boardRepository.Update(board);
                    }

                    tx.Commit();
                    return MapToBoardDetailDto(board);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public PagedResultDto<BoardDto> GetBoards(int pageNumber, int pageSize)
        {
            // 읽기 전용 작업이지만, 일관된 세션 관리를 위해 트랜잭션 사용
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var totalCount = boardRepository.Count();
                var boards = boardRepository.GetPaged(pageNumber, pageSize)
                    .OrderByDescending(b => b.CreatedDate)
                    .ToList();

                tx.Commit();

                return new PagedResultDto<BoardDto>
                {
                    Items = boards.Select(MapToBoardDto).ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
        }

        public void UpdateBoard(int id, string title, string content, string currentUser)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var board = boardRepository.GetById(id);
                    if (board == null) throw new ArgumentException($"게시글 {id}를 찾을 수 없습니다.");
                    if (board.Author != currentUser) throw new UnauthorizedAccessException("수정 권한이 없습니다.");

                    board.UpdateContent(title, content);
                    boardRepository.Update(board);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void DeleteBoard(int id, string currentUser)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var board = boardRepository.GetById(id);
                    if (board == null) return;
                    if (board.Author != currentUser) throw new UnauthorizedAccessException("삭제 권한이 없습니다.");

                    boardRepository.Delete(board.Id);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public PagedResultDto<BoardDto> SearchBoards(string keyword, int pageNumber, int pageSize)
        {
            // 참고: 이 로직은 Repository가 아닌 Service에 있습니다.
            // Repository에 페이징을 포함한 검색 메서드가 없기 때문입니다.
            // 이상적으로는 IBoardRepository에 PagedSearch(..) 같은 메서드를 추가하는 것이 좋습니다.
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var query = sessionFactory.GetCurrentSession().Query<Board>()
                    .Where(b => b.Title.Contains(keyword) || b.Content.Contains(keyword));

                var totalCount = query.Count();
                var boards = query
                    .OrderByDescending(b => b.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                tx.Commit();

                return new PagedResultDto<BoardDto>
                {
                    Items = boards.Select(MapToBoardDto).ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
        }

        public int AddReply(int boardId, string content, string author)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var board = boardRepository.GetById(boardId);
                    if (board == null) throw new ArgumentException($"게시글 {boardId}를 찾을 수 없습니다.");

                    var reply = new Reply { Board = board, Content = content, Author = author };
                    replyRepository.Add(reply);
                    tx.Commit();
                    return reply.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void DeleteReply(int replyId, string currentUser)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var reply = replyRepository.GetById(replyId);
                    if (reply == null) return;
                    if (reply.Author != currentUser) throw new UnauthorizedAccessException("삭제 권한이 없습니다.");

                    replyRepository.Delete(reply);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public IList<BoardDto> GetRecentBoards(int count)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var boards = boardRepository.GetRecent(count);
                tx.Commit();
                return boards.Select(MapToBoardDto).ToList();
            }
        }

        public IList<BoardDto> GetPopularBoards(int count)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var boards = boardRepository.GetPopular(count);
                tx.Commit();
                return boards.Select(MapToBoardDto).ToList();
            }
        }

        // DTO 매핑
        private BoardDto MapToBoardDto(Board board)
        {
            return new BoardDto
            {
                Id = board.Id,
                Title = board.Title,
                Content = board.Content.Length > 100
                    ? board.Content.Substring(0, 100) + "..."
                    : board.Content,
                Author = board.Author,
                ViewCount = board.ViewCount,
                CreatedDate = board.CreatedDate,
                ModifiedDate = board.ModifiedDate,
                ReplyCount = board.Replies?.Count ?? 0
            };
        }

        private BoardDetailDto MapToBoardDetailDto(Board board)
        {
            return new BoardDetailDto
            {
                Id = board.Id,
                Title = board.Title,
                Content = board.Content,
                Author = board.Author,
                ViewCount = board.ViewCount,
                CreatedDate = board.CreatedDate,
                ModifiedDate = board.ModifiedDate,
                ReplyCount = board.Replies?.Count ?? 0,
                Replies = board.Replies?
                    .Select(r => new ReplyDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        Author = r.Author,
                        CreatedDate = r.CreatedDate
                    })
                    .OrderBy(r => r.CreatedDate)
                    .ToList()
            };
        }
    }
}
```

### 📢 프로젝트 파일 업데이트
새로운 DTO, 인터페이스, 서비스 클래스를 `SpringNet.Service.csproj`에 추가해야 합니다.

1.  `SpringNet.Service` 프로젝트에 `DTOs` 폴더를 생성하고 `BoardDto.cs` 파일을 그 안으로 이동합니다.
2.  `SpringNet.Service.csproj` 파일의 `<Compile>` 아이템 그룹을 다음과 같이 업데이트합니다.

```xml
<ItemGroup>
  <Compile Include="BoardService.cs" />
  <Compile Include="DTOs\BoardDto.cs" />
  <Compile Include="GreetingService.cs" />
  <Compile Include="IBoardService.cs" />
  <Compile Include="IGreetingService.cs" />
  <Compile Include="IProductService.cs" />
  <Compile Include="Logging\CompositeLogger.cs" />
  <Compile Include="Logging\ConsoleLogger.cs" />
  <Compile Include="Logging\FileLogger.cs" />
  <Compile Include="Logging\ILogger.cs" />
  <Compile Include="ProductService.cs" />
  <Compile Include="Properties\AssemblyInfo.cs" />
</ItemGroup>
```

## 💡 트랜잭션 관리

### 수동 트랜잭션

`BoardService`에서 사용한 것처럼, `GetCurrentSession()`으로 현재 세션을 얻어와 트랜잭션을 수동으로 관리하는 패턴입니다.

```csharp
// GetCurrentSession을 사용한 트랜잭션 관리
using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
{
    try
    {
        // Repository를 통한 작업 수행
        var board = new Board { ... };
        boardRepository.Add(board);
        
        tx.Commit(); // 성공 시 커밋
    }
    catch
    {
        tx.Rollback(); // 실패 시 롤백
        throw;
    }
}
```
이 패턴은 서비스 계층의 각 비즈니스 메서드 단위로 트랜잭션을 제어할 수 있게 해줍니다.

### Spring 선언적 트랜잭션 (고급)

```xml
<!-- applicationContext.xml -->
<tx:annotation-driven transaction-manager="transactionManager" />

<object id="transactionManager"
        type="Spring.Data.NHibernate.HibernateTransactionManager, Spring.Data.NHibernate">
    <property name="SessionFactory" ref="sessionFactory" />
</object>
```

```csharp
[Transaction]
public void CreateBoard(string title, string content, string author)
{
    // 트랜잭션 자동 관리
}
```

## 🔧 applicationContext.xml 설정

`IBoardService`의 구현체인 `BoardService`를 Spring 컨테이너에 Bean으로 등록합니다. `applicationContext.xml` 파일에 다음 내용을 추가하세요.

```xml
<!-- Board Service -->
<object id="boardService"
        type="SpringNet.Service.BoardService, SpringNet.Service">
    <constructor-arg ref="boardRepository" />
    <constructor-arg ref="replyRepository" />
    <constructor-arg ref="sessionFactory" />
</object>
```
`BoardService`는 생성자에서 `IBoardRepository`, `IReplyRepository`, `ISessionFactory`를 주입받으므로, XML 설정에서도 이 세 가지 Bean을 `constructor-arg`로 참조(`ref`)해 줍니다.

## 💡 핵심 정리

### Service Layer 책임

✅ 비즈니스 로직
✅ 트랜잭션 관리
✅ 유효성 검증
✅ DTO 변환
✅ 여러 Repository 조합

### DTO 패턴

- **Entity**: 데이터베이스와 매핑
- **DTO**: 계층 간 데이터 전송

```csharp
// Entity → DTO 변환
BoardDto dto = MapToBoardDto(entity);

// Controller에서는 DTO만 사용
return View(dto);
```

## 🚀 다음 단계

다음: **[07-board-part4-mvc.md](./07-board-part4-mvc.md)** - MVC 컨트롤러 및 뷰 구현
