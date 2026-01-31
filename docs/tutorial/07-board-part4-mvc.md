# 07. 게시판 Part 4: MVC 컨트롤러 및 뷰

## 📚 학습 목표

- Spring.NET MVC 컨트롤러 구현
- Razor 뷰 작성
- 폼 데이터 바인딩
- 페이징 UI 구현

## 🛠️ BoardController 구현

> **⚠️ 중요 보안 경고:**
> 현재 `Edit` 및 `Delete` 액션에서 `Session["Username"]`을 사용하여 현재 사용자를 확인하는 방식은 **임시 방편이며 보안에 매우 취약**합니다. 이 튜토리얼은 MVC 컨트롤러 구현에 초점을 맞추므로 이 방식을 사용하지만, 실제 애플리케이션에서는 안전한 인증/인가 메커니즘을 사용해야 합니다. 사용자 인증/인가에 대한 자세한 내용은 이후 튜토리얼에서 다룰 예정입니다.

`SpringNet.Web/Controllers/BoardController.cs`:

```csharp
using SpringNet.Service;
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class BoardController : Controller
    {
        private readonly IBoardService boardService;

        // 생성자 주입을 통해 BoardService 의존성 주입
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
            return View(board);
        }

        // 작성 폼 (GET /Board/Create)
        public ActionResult Create()
        {
            return View();
        }

        // 작성 처리 (POST /Board/Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string title, string content, string author)
        {
            try
            {
                var id = boardService.CreateBoard(title, content, author);
                return RedirectToAction("Detail", new { id });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // 수정 폼 (GET /Board/Edit/5)
        public ActionResult Edit(int id)
        {
            var board = boardService.GetBoard(id, increaseViewCount: false);
            return View(board);
        }

        // 수정 처리 (POST /Board/Edit/5)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, string title, string content)
        {
            try
            {
                var currentUser = Session["Username"]?.ToString() ?? "Guest";
                boardService.UpdateBoard(id, title, content, currentUser);
                return RedirectToAction("Detail", new { id });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // 삭제 (POST /Board/Delete/5)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                var currentUser = Session["Username"]?.ToString() ?? "Guest";
                boardService.DeleteBoard(id, currentUser);
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

        // 댓글 작성 (POST /Board/AddReply)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReply(int boardId, string content, string author)
        {
            try
            {
                boardService.AddReply(boardId, content, author);
                return RedirectToAction("Detail", new { id = boardId });
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id = boardId });
            }
        }
    }
}
```

### 📢 프로젝트 파일 및 폴더 설정
이 튜토리얼에서 생성하는 파일들을 프로젝트에 추가하고 폴더를 정리해야 합니다.

1.  **`BoardController.cs` 추가**
    -   `SpringNet.Web/Controllers/` 폴더에 `BoardController.cs` 파일을 생성합니다.
    -   `SpringNet.Web.csproj` 파일의 `<Compile>` 아이템 그룹에 다음을 추가합니다.
    ```xml
    <ItemGroup>
      <Compile Include="Controllers\BoardController.cs" />
      <!-- ... 다른 컨트롤러들 ... -->
    </ItemGroup>
    ```

2.  **`Views/Board` 폴더 생성 및 뷰 파일 추가**
    -   `SpringNet.Web/Views/` 폴더 아래에 `Board` 폴더를 새로 만듭니다.
    -   이 폴더 안에 `Index.cshtml`, `Detail.cshtml`, `Create.cshtml`, `Edit.cshtml` 파일들을 생성하고 각 코드 블록의 내용을 복사하여 붙여넣습니다.
    -   `.csproj` 파일은 일반적으로 `Views` 폴더의 `.cshtml` 파일을 자동으로 포함하므로 별도 수정은 필요 없습니다.

## 📝 Razor 뷰 작성

### Index.cshtml (목록)

`Views/Board/Index.cshtml`:

```html
@model SpringNet.Service.DTOs.PagedResultDto<SpringNet.Service.DTOs.BoardDto>

@{
    ViewBag.Title = "게시판";
}

<h2>게시판</h2>

<div class="mb-3">
    <a href="@Url.Action("Create")" class="btn btn-primary">글쓰기</a>

    <form method="get" action="@Url.Action("Search")" class="d-inline float-right">
        <div class="input-group">
            <input type="text" name="keyword" class="form-control"
                   placeholder="검색..." value="@ViewBag.Keyword" />
            <button type="submit" class="btn btn-secondary">검색</button>
        </div>
    </form>
</div>

<table class="table table-hover">
    <thead>
        <tr>
            <th width="60">번호</th>
            <th>제목</th>
            <th width="100">작성자</th>
            <th width="80">조회수</th>
            <th width="100">작성일</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var board in Model.Items)
        {
            <tr>
                <td>@board.Id</td>
                <td>
                    <a href="@Url.Action("Detail", new { id = board.Id })">
                        @board.Title
                    </a>
                    @if (board.ReplyCount > 0)
                    {
                        <span class="badge badge-info">@board.ReplyCount</span>
                    }
                </td>
                <td>@board.Author</td>
                <td>@board.ViewCount</td>
                <td>@board.CreatedDate.ToString("yyyy-MM-dd")</td>
            </tr>
        }
    </tbody>
</table>

<!-- 페이징 -->
<nav>
    <ul class="pagination">
        @if (Model.HasPreviousPage)
        {
            <li class="page-item">
                <a class="page-link" href="@Url.Action(ViewContext.RouteData.Values["action"].ToString(), new { page = Model.PageNumber - 1, keyword = ViewBag.Keyword })">
                    이전
                </a>
            </li>
        }

        @for (int i = 1; i <= Model.TotalPages; i++)
        {
            <li class="page-item @(i == Model.PageNumber ? "active" : "")">
                <a class="page-link" href="@Url.Action(ViewContext.RouteData.Values["action"].ToString(), new { page = i, keyword = ViewBag.Keyword })">@i</a>
            </li>
        }

        @if (Model.HasNextPage)
        {
            <li class="page-item">
                <a class="page-link" href="@Url.Action(ViewContext.RouteData.Values["action"].ToString(), new { page = Model.PageNumber + 1, keyword = ViewBag.Keyword })">
                    다음
                </a>
            </li>
        }
    </ul>
</nav>
```

### Detail.cshtml (상세보기)

`Views/Board/Detail.cshtml`:

```html
@model SpringNet.Service.DTOs.BoardDetailDto

@{
    ViewBag.Title = Model.Title;
}

<h2>@Model.Title</h2>

<div class="card">
    <div class="card-body">
        <div class="mb-3">
            <strong>작성자:</strong> @Model.Author |
            <strong>조회수:</strong> @Model.ViewCount |
            <strong>작성일:</strong> @Model.CreatedDate.ToString("yyyy-MM-dd HH:mm")
        </div>

        <hr />

        <div class="content">
            @Html.Raw(Model.Content.Replace("\n", "<br />"))
        </div>
    </div>
</div>

<div class="mt-3">
    <a href="@Url.Action("Index")" class="btn btn-secondary">목록</a>
    <a href="@Url.Action("Edit", new { id = Model.Id })" class="btn btn-warning">수정</a>

    <form method="post" action="@Url.Action("Delete", new { id = Model.Id })"
          style="display:inline;"
          onsubmit="return confirm('정말 삭제하시겠습니까?');">
        @Html.AntiForgeryToken()
        <button type="submit" class="btn btn-danger">삭제</button>
    </form>
</div>

<!-- 댓글 목록 -->
<h4 class="mt-4">댓글 (@Model.ReplyCount)</h4>

@foreach (var reply in Model.Replies)
{
    <div class="card mb-2">
        <div class="card-body">
            <strong>@reply.Author</strong>
            <small class="text-muted">@reply.CreatedDate.ToString("yyyy-MM-dd HH:mm")</small>
            <p>@reply.Content</p>
        </div>
    </div>
}

<!-- 댓글 작성 -->
<form method="post" action="@Url.Action("AddReply")">
    @Html.AntiForgeryToken()
    <input type="hidden" name="boardId" value="@Model.Id" />

    <div class="form-group">
        <input type="text" name="author" class="form-control" placeholder="작성자" required />
    </div>
    <div class="form-group">
        <textarea name="content" class="form-control" rows="3"
                  placeholder="댓글을 입력하세요..." required></textarea>
    </div>
    <button type="submit" class="btn btn-primary">댓글 작성</button>
</form>
```

### Create.cshtml (작성 폼)

`Views/Board/Create.cshtml`:

```html
@{
    ViewBag.Title = "글쓰기";
}

<h2>글쓰기</h2>

@if (!ViewData.ModelState.IsValid)
{
    <div class="alert alert-danger">
        @Html.ValidationSummary()
    </div>
}

<form method="post" action="@Url.Action("Create")">
    @Html.AntiForgeryToken()

    <div class="form-group">
        <label>작성자</label>
        <input type="text" name="author" class="form-control" required />
    </div>

    <div class="form-group">
        <label>제목</label>
        <input type="text" name="title" class="form-control" required />
    </div>

    <div class="form-group">
        <label>내용</label>
        <textarea name="content" class="form-control" rows="10" required></textarea>
    </div>

    <button type="submit" class="btn btn-primary">작성</button>
    <a href="@Url.Action("Index")" class="btn btn-secondary">취소</a>
</form>
```

### Edit.cshtml (수정 폼)

`Views/Board/Edit.cshtml`:

```html
@model SpringNet.Service.DTOs.BoardDetailDto

@{
    ViewBag.Title = "게시글 수정";
}

<h2>게시글 수정</h2>

@if (!ViewData.ModelState.IsValid)
{
    <div class="alert alert-danger">
        @Html.ValidationSummary()
    </div>
}

<form method="post" action="@Url.Action("Edit", new { id = Model.Id })">
    @Html.AntiForgeryToken()

    <div class="form-group">
        <label>작성자</label>
        <input type="text" name="author" class="form-control" value="@Model.Author" readonly />
    </div>

    <div class="form-group">
        <label>제목</label>
        <input type="text" name="title" class="form-control" value="@Model.Title" required />
    </div>

    <div class="form-group">
        <label>내용</label>
        <textarea name="content" class="form-control" rows="10" required>@Model.Content</textarea>
    </div>

    <button type="submit" class="btn btn-primary">수정</button>
    <a href="@Url.Action("Detail", new { id = Model.Id })" class="btn btn-secondary">취소</a>
</form>
```

## 🔧 applicationContext.xml 설정

컨트롤러를 생성자 주입으로 변경했으므로, `applicationContext.xml` 파일의 Bean 설정도 `<constructor-arg>`를 사용하도록 수정합니다.

```xml
    <!-- Board Controller 추가 -->
    <object id="boardController"
            type="SpringNet.Web.Controllers.BoardController, SpringNet.Web">
        <constructor-arg ref="boardService" />
    </object>
```

## 💡 핵심 정리

### MVC 패턴

- **Model**: DTO (데이터)
- **View**: Razor (화면)
- **Controller**: 요청 처리, Service 호출, Model을 View로 전달

### Spring.NET MVC

✅ 생성자 주입으로 Service 의존성 해결
✅ `[HttpPost]`로 HTTP 메서드 제한
✅ `[ValidateAntiForgeryToken]`로 CSRF 방지

## 🚀 다음 단계

게시판 완성! 다음: **[08-user-part1-authentication.md](./08-user-part1-authentication.md)**
