# 07. 게시판 Part 4: MVC 컨트롤러 및 뷰

## 📚 학습 목표

- Spring.NET MVC 컨트롤러 구현
- Razor 뷰 작성
- 폼 데이터 바인딩
- 페이징 UI 구현

## 🛠️ BoardController 구현

`SpringNet.Web/Controllers/BoardController.cs`:

```csharp
using SpringNet.Service;
using SpringNet.Service.DTOs;
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class BoardController : Controller
    {
        public IBoardService BoardService { get; set; }

        // 목록 (GET /Board/Index?page=1)
        public ActionResult Index(int page = 1)
        {
            const int pageSize = 10;
            var result = BoardService.GetBoards(page, pageSize);

            return View(result);
        }

        // 상세 (GET /Board/Detail/5)
        public ActionResult Detail(int id)
        {
            var board = BoardService.GetBoard(id);
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
                var id = BoardService.CreateBoard(title, content, author);
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
            var board = BoardService.GetBoard(id, increaseViewCount: false);
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
                BoardService.UpdateBoard(id, title, content, currentUser);
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
                BoardService.DeleteBoard(id, currentUser);
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
            var result = BoardService.SearchBoards(keyword, page, pageSize);

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
                BoardService.AddReply(boardId, content, author);
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
                <a class="page-link" href="@Url.Action("Index", new { page = Model.PageNumber - 1 })">
                    이전
                </a>
            </li>
        }

        @for (int i = 1; i <= Model.TotalPages; i++)
        {
            <li class="page-item @(i == Model.PageNumber ? "active" : "")">
                <a class="page-link" href="@Url.Action("Index", new { page = i })">@i</a>
            </li>
        }

        @if (Model.HasNextPage)
        {
            <li class="page-item">
                <a class="page-link" href="@Url.Action("Index", new { page = Model.PageNumber + 1 })">
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

## 🔧 applicationContext.xml 설정

```xml
<!-- Board Controller -->
<object id="boardController"
        type="SpringNet.Web.Controllers.BoardController, SpringNet.Web">
    <property name="BoardService" ref="boardService" />
</object>
```

## 💡 핵심 정리

### MVC 패턴

- **Model**: DTO (데이터)
- **View**: Razor (화면)
- **Controller**: 요청 처리

### Spring.NET MVC

✅ Property Injection으로 Service 주입
✅ `[HttpPost]`로 HTTP 메서드 제한
✅ `[ValidateAntiForgeryToken]`로 CSRF 방지

## 🚀 다음 단계

게시판 완성! 다음: **[08-user-part1-authentication.md](./08-user-part1-authentication.md)**
