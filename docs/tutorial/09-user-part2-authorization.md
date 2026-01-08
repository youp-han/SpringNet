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

```csharp
using SpringNet.Web.Filters;

namespace SpringNet.Web.Controllers
{
    public class BoardController : Controller
    {
        public IBoardService BoardService { get; set; }

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

## 📝 Board 엔티티 수정

```csharp
public class Board
{
    public virtual int Id { get; set; }
    public virtual string Title { get; set; }
    public virtual string Content { get; set; }

    // 작성자 ID 추가
    public virtual int AuthorId { get; set; }
    public virtual string AuthorName { get; set; }

    public virtual int ViewCount { get; set; }
    public virtual DateTime CreatedDate { get; set; }
    public virtual DateTime? ModifiedDate { get; set; }
    public virtual IList<Reply> Replies { get; set; }
}
```

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
