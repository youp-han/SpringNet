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
            // 유효성 검증
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("제목은 필수입니다.");
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("내용은 필수입니다.");

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

                    return board.Id;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public BoardDetailDto GetBoard(int id, bool increaseViewCount = true)
        {
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                try
                {
                    var board = session.Query<Board>()
                        .Fetch(b => b.Replies)
                        .FirstOrDefault(b => b.Id == id);

                    if (board == null)
                        throw new ArgumentException($"게시글 {id}를 찾을 수 없습니다.");

                    if (increaseViewCount)
                    {
                        board.IncreaseViewCount();
                        session.Update(board);
                    }

                    transaction.Commit();

                    return MapToBoardDetailDto(board);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public PagedResultDto<BoardDto> GetBoards(int pageNumber, int pageSize)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var totalCount = session.Query<Board>().Count();
                var boards = session.Query<Board>()
                    .OrderByDescending(b => b.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

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
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                try
                {
                    var board = session.Get<Board>(id);
                    if (board == null)
                        throw new ArgumentException($"게시글 {id}를 찾을 수 없습니다.");

                    // 권한 확인
                    if (board.Author != currentUser)
                        throw new UnauthorizedAccessException("수정 권한이 없습니다.");

                    board.UpdateContent(title, content);
                    session.Update(board);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void DeleteBoard(int id, string currentUser)
        {
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                try
                {
                    var board = session.Get<Board>(id);
                    if (board == null)
                        throw new ArgumentException($"게시글 {id}를 찾을 수 없습니다.");

                    if (board.Author != currentUser)
                        throw new UnauthorizedAccessException("삭제 권한이 없습니다.");

                    session.Delete(board);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public PagedResultDto<BoardDto> SearchBoards(string keyword, int pageNumber, int pageSize)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var query = session.Query<Board>()
                    .Where(b => b.Title.Contains(keyword) || b.Content.Contains(keyword));

                var totalCount = query.Count();
                var boards = query
                    .OrderByDescending(b => b.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

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
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                try
                {
                    var board = session.Get<Board>(boardId);
                    if (board == null)
                        throw new ArgumentException($"게시글 {boardId}를 찾을 수 없습니다.");

                    var reply = new Reply
                    {
                        Board = board,
                        Content = content,
                        Author = author
                    };

                    session.Save(reply);
                    transaction.Commit();

                    return reply.Id;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void DeleteReply(int replyId, string currentUser)
        {
            using (var session = sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                try
                {
                    var reply = session.Get<Reply>(replyId);
                    if (reply == null)
                        throw new ArgumentException($"댓글 {replyId}를 찾을 수 없습니다.");

                    if (reply.Author != currentUser)
                        throw new UnauthorizedAccessException("삭제 권한이 없습니다.");

                    session.Delete(reply);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public IList<BoardDto> GetRecentBoards(int count)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var boards = session.Query<Board>()
                    .OrderByDescending(b => b.CreatedDate)
                    .Take(count)
                    .ToList();

                return boards.Select(MapToBoardDto).ToList();
            }
        }

        public IList<BoardDto> GetPopularBoards(int count)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var boards = session.Query<Board>()
                    .OrderByDescending(b => b.ViewCount)
                    .Take(count)
                    .ToList();

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

## 💡 트랜잭션 관리

### 수동 트랜잭션

```csharp
using (var session = sessionFactory.OpenSession())
using (var transaction = session.BeginTransaction())
{
    try
    {
        // 작업 수행
        session.Save(entity);
        transaction.Commit(); // 성공 시 커밋
    }
    catch
    {
        transaction.Rollback(); // 실패 시 롤백
        throw;
    }
}
```

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

```xml
<!-- Board Service -->
<object id="boardService"
        type="SpringNet.Service.BoardService, SpringNet.Service">
    <constructor-arg ref="boardRepository" />
    <constructor-arg ref="replyRepository" />
    <constructor-arg ref="sessionFactory" />
</object>
```

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
