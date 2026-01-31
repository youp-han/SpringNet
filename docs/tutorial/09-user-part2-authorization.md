# 09. 사용자 관리 Part 2: 인가 (Authorization)

## 📚 학습 목표

- 권한 관리 시스템 구현
- Custom Authorization Attribute
- Role 기반 접근 제어
- 게시판과 사용자 연동

## 🔐 Authorization Attribute 구현

`SpringNet.Web/Filters/AuthorizeAttribute.cs`:

```csharp
using System.Web.Mvc;
using System.Web.Routing;

namespace SpringNet.Web.Filters
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            var userId = httpContext.Session["UserId"];
            return userId != null;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    { "controller", "Account" },
                    { "action", "Login" },
                    { "returnUrl", filterContext.HttpContext.Request.RawUrl }
                });
        }
    }

    public class AdminAuthorizeAttribute : CustomAuthorizeAttribute
    {
        protected override bool AuthorizeCore(System.Web.HttpContextBase httpContext)
        {
            if (!base.AuthorizeCore(httpContext))
                return false;

            var role = httpContext.Session["Role"]?.ToString();
            return role == "Admin";
        }
    }
}
```

## 🎮 컨트롤러 적용

> **⚠️ 중요 참고:**
> 아래 `BoardController` 예제는 `CustomAuthorize` 및 `AdminAuthorize` 속성을 어떻게 적용하는지 보여주는 **코드 스니펫**입니다. 이전 튜토리얼에서 작성한 `BoardController.cs` 파일에 이 속성들을 적절히 통합해야 합니다. 또한 `AdminPanel` 액션에 해당하는 `Views/Board/AdminPanel.cshtml` 뷰 파일을 간단하게 생성해야 합니다 (예: `<h2>관리자 패널</h2>`).

```csharp
using SpringNet.Web.Filters;
using SpringNet.Service;
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class BoardController : Controller
    {
        private readonly IBoardService boardService;

        // 생성자 주입
        public BoardController(IBoardService boardService)
        {
            this.boardService = boardService;
        }

        // 목록: 로그인 불필요
        public ActionResult Index(int page = 1)
        {
            // ...
        }

        // 작성: 로그인 필요
        [CustomAuthorize]
        public ActionResult Create()
        {
            return View();
        }

        // 관리자 전용
        [AdminAuthorize]
        public ActionResult AdminPanel()
        {
            // 관리자 기능
            return View();
        }
    }
}
```

## 📝 Board 엔티티 및 관련 코드 수정

사용자 권한 관리를 위해 게시글(`Board`) 엔티티를 수정하고, 이 변경사항이 파급되는 모든 계층의 코드도 함께 업데이트해야 합니다.

### 1. 데이터베이스 스키마 업데이트

`Boards` 테이블에 `AuthorId` 컬럼을 추가합니다.

```sql
ALTER TABLE Boards
ADD AuthorId INT NOT NULL DEFAULT 0; -- 기존 데이터가 있다면 0으로 초기화, 나중에 올바른 값으로 업데이트 필요
```

### 2. `SpringNet.Domain/Entities/Board.cs` 수정

기존 `Author` 프로퍼티를 `AuthorId`와 `AuthorName`으로 변경합니다.

```csharp
using System;
using System.Collections.Generic;

namespace SpringNet.Domain.Entities
{
    public class Board
    {
        public virtual int Id { get; set; }
        public virtual string Title { get; set; }
        public virtual string Content { get; set; }
        // 기존 Author 대신 AuthorId와 AuthorName 사용
        public virtual int AuthorId { get; set; }
        public virtual string AuthorName { get; set; } // 작성자 표시용

        public virtual int ViewCount { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime? ModifiedDate { get; set; }

        public virtual IList<Reply> Replies { get; set; }

        public Board()
        {
            CreatedDate = DateTime.Now;
            ViewCount = 0;
            Replies = new List<Reply>();
        }

        public virtual void IncreaseViewCount()
        {
            ViewCount++;
        }

        public virtual void UpdateContent(string title, string content)
        {
            Title = title;
            Content = content;
            ModifiedDate = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Id}] {Title} by {AuthorName} ({ViewCount} views)";
        }
    }
}
```

### 3. `SpringNet.Data/Mappings/Board.hbm.xml` 수정

새로운 `AuthorId`와 `AuthorName` 프로퍼티를 매핑하고 기존 `Author` 매핑은 제거합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="Board" table="Boards">

        <!-- Primary Key -->
        <id name="Id" column="Id">
            <generator class="identity" />
        </id>

        <!-- Properties -->
        <property name="Title" column="Title" type="string"
                  length="200" not-null="true" />

        <property name="Content" column="Content" type="string"
                  not-null="true" />

        <!-- 기존 Author 매핑 제거, AuthorId와 AuthorName 추가 -->
        <property name="AuthorId" column="AuthorId" type="int"
                  not-null="true" />
        <property name="AuthorName" column="AuthorName" type="string"
                  length="50" not-null="true" />

        <property name="ViewCount" column="ViewCount" type="int"
                  not-null="true" />

        <property name="CreatedDate" column="CreatedDate" type="datetime"
                  not-null="true" />

        <property name="ModifiedDate" column="ModifiedDate" type="datetime" />

        <!-- One-to-Many Relationship -->
        <bag name="Replies" inverse="true" cascade="all-delete-orphan" lazy="true">
            <key column="BoardId" />
            <one-to-many class="Reply" />
        </bag>

    </class>

</hibernate-mapping>
```

### 4. `SpringNet.Service/DTOs/BoardDto.cs` 수정

`BoardDto`와 `BoardDetailDto`에 `AuthorId` 프로퍼티를 추가합니다.

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
        // Author 대신 AuthorId와 AuthorName 추가
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } 
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
        public string Author { get; set; } // 댓글 작성자는 string Author 그대로 사용
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

### 5. `SpringNet.Service/IBoardService.cs` 수정

`CreateBoard`, `UpdateBoard`, `DeleteBoard` 메서드의 시그니처를 `AuthorId`를 포함하도록 변경합니다. `Author` 대신 `AuthorName`을 받습니다.

```csharp
using SpringNet.Service.DTOs;

namespace SpringNet.Service
{
    public interface IBoardService
    {
        // CRUD: author 대신 authorId와 authorName 사용
        int CreateBoard(string title, string content, int authorId, string authorName);
        BoardDetailDto GetBoard(int id, bool increaseViewCount = true);
        PagedResultDto<BoardDto> GetBoards(int pageNumber, int pageSize);
        // Update, Delete 메서드도 author 대신 authorId 사용
        void UpdateBoard(int id, string title, string content, int currentUserId);
        void DeleteBoard(int id, int currentUserId);

        // 검색
        PagedResultDto<BoardDto> SearchBoards(string keyword, int pageNumber, int pageSize);

        // 댓글
        int AddReply(int boardId, string content, int authorId, string authorName);
        void DeleteReply(int replyId, int currentUserId);

        // 통계
        IList<BoardDto> GetRecentBoards(int count);
        IList<BoardDto> GetPopularBoards(int count);
    }
}
```

### 6. `SpringNet.Service/BoardService.cs` 수정

`BoardService`의 메서드들을 변경된 `Board` 엔티티와 `IBoardService` 인터페이스에 맞춰 업데이트합니다. `AuthorId`를 사용하여 비즈니스 로직과 권한 검사를 수행합니다.

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

        public int CreateBoard(string title, string content, int authorId, string authorName)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("제목은 필수입니다.");
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("내용은 필수입니다.");

            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var board = new Board { Title = title, Content = content, AuthorId = authorId, AuthorName = authorName };
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
                    var board = boardRepository.GetWithReplies(id);
                    if (board == null) throw new ArgumentException($"게시글 {id}를 찾을 수 없습니다.");

                    if (increaseViewCount)
                    {
                        board.IncreaseViewCount();
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

        public void UpdateBoard(int id, string title, string content, int currentUserId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var board = boardRepository.GetById(id);
                    if (board == null) throw new ArgumentException($"게시글 {id}를 찾을 수 없습니다.");

                    // 권한 확인: 작성자 ID로 확인
                    if (board.AuthorId != currentUserId)
                        throw new UnauthorizedAccessException("수정 권한이 없습니다.");

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

        public void DeleteBoard(int id, int currentUserId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var board = boardRepository.GetById(id);
                    if (board == null) return;
                    
                    // 권한 확인: 작성자 ID로 확인
                    if (board.AuthorId != currentUserId)
                        throw new UnauthorizedAccessException("삭제 권한이 없습니다.");

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

        public int AddReply(int boardId, string content, int authorId, string authorName)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var board = boardRepository.GetById(boardId);
                    if (board == null) throw new ArgumentException($"게시글 {boardId}를 찾을 수 없습니다.");

                    var reply = new Reply { Board = board, Content = content, Author = authorName };
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

        public void DeleteReply(int replyId, int currentUserId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var reply = replyRepository.GetById(replyId);
                    if (reply == null) return;
                    
                    // 권한 확인: 작성자 ID로 확인. 댓글 엔티티에 AuthorId가 없으므로 AuthorName과 비교.
                    // 더 정확한 권한 확인을 위해 Reply 엔티티에도 AuthorId를 추가하는 것이 좋습니다.
                    if (reply.Author != currentUserId.ToString()) // 임시 방편
                        throw new UnauthorizedAccessException("삭제 권한이 없습니다.");

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
                AuthorId = board.AuthorId, // AuthorId 매핑
                AuthorName = board.AuthorName, // AuthorName 매핑
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
                AuthorId = board.AuthorId, // AuthorId 매핑
                AuthorName = board.AuthorName, // AuthorName 매핑
                ViewCount = board.ViewCount,
                CreatedDate = board.CreatedDate,
                ModifiedDate = board.ModifiedDate,
                ReplyCount = board.Replies?.Count ?? 0,
                Replies = board.Replies?
                    .Select(r => new ReplyDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        Author = r.Author, // 댓글은 AuthorName 사용
                        CreatedDate = r.CreatedDate
                    })
                    .OrderBy(r => r.CreatedDate)
                    .ToList()
            };
        }
    }
}
```

### 7. `SpringNet.Web/Controllers/BoardController.cs` 수정

`BoardController`의 액션 메서드들이 `Session["UserId"]`와 `Session["Username"]`을 사용하여 `AuthorId`와 `AuthorName`을 가져와 `IBoardService` 메서드에 전달하도록 수정합니다. `Create`와 `Edit` 액션에는 `CustomAuthorize` 속성을 적용합니다.

```csharp
using SpringNet.Service;
using SpringNet.Web.Filters; // CustomAuthorizeAttribute 사용
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class BoardController : Controller
    {
        private readonly IBoardService boardService;

        public BoardController(IBoardService boardService)
        {
            this.boardService = boardService;
        }

        // 목록 (GET /Board/Index?page=1)
        public ActionResult Index(int page = 1)
        {
            const int pageSize = 10;
            var result = boardService.GetBoards(page, pageSize);

            return View(result);
        }

        // 상세 (GET /Board/Detail/5)
        public ActionResult Detail(int id)
        {
            var board = boardService.GetBoard(id);
            // ViewBag.IsAuthor = (board.AuthorId == (int?)Session["UserId"]); // 뷰에서 권한 확인 시
            return View(board);
        }

        // 작성 폼 (GET /Board/Create): 로그인 필요
        [CustomAuthorize]
        public ActionResult Create()
        {
            return View();
        }

        // 작성 처리 (POST /Board/Create): 로그인 필요
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize]
        public ActionResult Create(string title, string content)
        {
            // 세션에서 UserId와 Username 가져오기
            var currentUserId = (int?)Session["UserId"];
            var currentUserName = Session["Username"]?.ToString();

            if (currentUserId == null || string.IsNullOrEmpty(currentUserName))
            {
                ModelState.AddModelError("", "로그인이 필요합니다.");
                return View();
            }

            try
            {
                var id = boardService.CreateBoard(title, content, currentUserId.Value, currentUserName);
                return RedirectToAction("Detail", new { id });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // 수정 폼 (GET /Board/Edit/5): 로그인 필요
        [CustomAuthorize]
        public ActionResult Edit(int id)
        {
            var board = boardService.GetBoard(id, increaseViewCount: false);
            if (board == null) return HttpNotFound();

            // 현재 사용자가 게시글 작성자인지 확인
            var currentUserId = (int?)Session["UserId"];
            if (board.AuthorId != currentUserId)
            {
                TempData["Error"] = "수정 권한이 없습니다.";
                return RedirectToAction("Detail", new { id });
            }
            return View(board);
        }

        // 수정 처리 (POST /Board/Edit/5): 로그인 필요
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize]
        public ActionResult Edit(int id, string title, string content)
        {
            var currentUserId = (int?)Session["UserId"];
            if (currentUserId == null)
            {
                ModelState.AddModelError("", "로그인이 필요합니다.");
                return View();
            }

            try
            {
                boardService.UpdateBoard(id, title, content, currentUserId.Value);
                return RedirectToAction("Detail", new { id });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // 삭제 (POST /Board/Delete/5): 로그인 필요
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize]
        public ActionResult Delete(int id)
        {
            var currentUserId = (int?)Session["UserId"];
            if (currentUserId == null)
            {
                TempData["Error"] = "로그인이 필요합니다.";
                return RedirectToAction("Detail", new { id });
            }

            try
            {
                boardService.DeleteBoard(id, currentUserId.Value);
                return RedirectToAction("Index");
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }

        // 검색 (GET /Board/Search?keyword=spring&page=1)
        public ActionResult Search(string keyword, int page = 1)
        {
            const int pageSize = 10;
            var result = boardService.SearchBoards(keyword, page, pageSize);

            ViewBag.Keyword = keyword;
            return View("Index", result);
        }

        // 댓글 작성 (POST /Board/AddReply): 로그인 필요
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize]
        public ActionResult AddReply(int boardId, string content)
        {
            var currentUserId = (int?)Session["UserId"];
            var currentUserName = Session["Username"]?.ToString();

            if (currentUserId == null || string.IsNullOrEmpty(currentUserName))
            {
                TempData["Error"] = "로그인이 필요합니다.";
                return RedirectToAction("Detail", new { id = boardId });
            }

            try
            {
                boardService.AddReply(boardId, content, currentUserId.Value, currentUserName);
                return RedirectToAction("Detail", new { id = boardId });
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id = boardId });
            }
        }
        
        // 관리자 전용 기능 (예시)
        [AdminAuthorize]
        public ActionResult AdminPanel()
        {
            ViewBag.Message = "관리자만 접근 가능합니다.";
            return View();
        }
    }
}
```

### 📢 프로젝트 파일 및 폴더 설정

이 튜토리얼에서 추가한 파일과 폴더들을 프로젝트에 반영해야 합니다.

#### 1. `SpringNet.Web` 프로젝트

-   **`Filters` 폴더 생성**: `SpringNet.Web` 프로젝트에 `Filters` 폴더를 생성하고, `AuthorizeAttribute.cs` 파일을 그 안에 생성합니다.
-   **`SpringNet.Web.csproj` 업데이트**: `SpringNet.Web.csproj` 파일의 `<Compile>` 아이템 그룹에 다음을 추가합니다.
    ```xml
    <ItemGroup>
      <Compile Include="Filters\AuthorizeAttribute.cs" />
      <!-- ... 다른 컨트롤러 및 파일들 ... -->
    </ItemGroup>
    ```
-   **뷰 파일 생성**: `Views/Board/AdminPanel.cshtml` 파일을 생성합니다. (`.cshtml` 파일은 일반적으로 `.csproj`에 자동으로 포함됩니다.)

#### 2. `SpringNet.Domain` 프로젝트
-   `SpringNet.Domain.csproj`의 `<Compile>` 아이템 그룹에 `Entities\User.cs`가 추가되었는지 확인합니다. (이전 튜토리얼에서 추가되었어야 함)

#### 3. `SpringNet.Data` 프로젝트
-   `SpringNet.Data.csproj`의 `<ItemGroup>` (EmbeddedResource)에 `Mappings\User.hbm.xml`가 추가되었는지 확인합니다. (이전 튜토리얼에서 추가되었어야 함)

#### 4. `SpringNet.Service` 프로젝트
-   `SpringNet.Service.csproj`의 `<ItemGroup>` (Compile)에 `DTOs\UserDto.cs`, `IAuthService.cs`, `AuthService.cs`가 추가되었는지 확인합니다. (이전 튜토리얼에서 추가되었어야 함)

## 💡 핵심 정리

### 인증 vs 인가

- **Authentication (인증)**: 당신은 누구인가?
- **Authorization (인가)**: 무엇을 할 수 있는가?

### Role 기반 제어

- `User`: 일반 사용자
- `Admin`: 관리자
- `Moderator`: 중간 권한 (선택)

## 🚀 다음 단계

다음: **[10-shopping-part1-products.md](./10-shopping-part1-products.md)** - 쇼핑몰 상품 관리
