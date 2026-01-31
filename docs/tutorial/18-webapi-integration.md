# 18. ASP.NET Web API 통합

## 📚 학습 목표

- 기존 MVC 프로젝트에 ASP.NET Web API 기능 추가
- Spring.NET을 사용하여 Web API 컨트롤러에 의존성 주입(DI)
- RESTful API 설계 및 구현 (GET, POST, PUT, DELETE)
- Web API의 JSON 직렬화 설정 최적화
- CORS (Cross-Origin Resource Sharing) 설정
- Swagger를 이용한 API 문서 자동화

## 🎯 Web API vs. MVC Controller

이전까지 우리가 만든 `Controller`는 주로 `ActionResult` (대부분 `ViewResult`)를 반환하여 사용자에게 HTML 페이지를 보여주는 역할을 했습니다. 반면, **API Controller**는 클라이언트(예: 모바일 앱, JavaScript 기반의 SPA, 다른 서버 등)가 프로그래밍 방식으로 사용할 수 있도록 **데이터(주로 JSON 또는 XML 형식)**를 반환합니다.

-   ✅ **Web API**: 모바일 앱, SPA(Single Page Application), 외부 시스템 연동을 위한 데이터 제공
-   ✅ **MVC**: 전통적인 서버 렌더링 웹 애플리케이션의 UI 제공

이 튜토리얼에서는 별도의 프로젝트를 만드는 대신, 우리의 기존 `SpringNet.Web` 프로젝트에 Web API 기능을 통합하는 실용적인 방법을 알아봅니다.

## 🛠️ 기존 `SpringNet.Web` 프로젝트에 Web API 추가

### Step 1: 필요한 NuGet 패키지 설치

`SpringNet.Web` 프로젝트의 패키지 관리자 콘솔에서 다음 패키지들을 설치합니다. (일부는 이미 설치되어 있을 수 있습니다.)

```powershell
PM> Install-Package Microsoft.AspNet.WebApi -Version 5.2.9
PM> Install-Package Newtonsoft.Json -Version 13.0.1
PM> Install-Package Swashbuckle -Version 5.6.0
PM> Install-Package Microsoft.AspNet.WebApi.Cors -Version 5.2.9
```

-   `Microsoft.AspNet.WebApi`: Web API 핵심 프레임워크입니다.
-   `Newtonsoft.Json`: .NET 객체와 JSON 간의 직렬화/역직렬화를 위한 강력한 라이브러리입니다.
-   `Swashbuckle`: Web API 문서를 자동으로 생성하고 테스트 UI를 제공합니다.
-   `Microsoft.AspNet.WebApi.Cors`: 다른 도메인에서의 API 요청(CORS)을 허용하기 위해 필요합니다.

### Step 2: Web API 설정 파일 추가

`SpringNet.Web/App_Start/` 폴더에 `WebApiConfig.cs` 파일을 새로 추가합니다. 이 파일은 API 라우팅, 포매터 설정 등 Web API와 관련된 전역 설정을 담당합니다.

`SpringNet.Web/App_Start/WebApiConfig.cs`:
```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Web.Http;
using System.Web.Http.Cors;

namespace SpringNet.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // CORS (Cross-Origin Resource Sharing) 활성화
            var cors = new EnableCorsAttribute(
                origins: "*", // 모든 도메인에서의 요청을 허용 (프로덕션에서는 특정 도메인만 지정: "http://example.com,https://another.com")
                headers: "*", // 모든 헤더 허용
                methods: "*"  // 모든 HTTP 메서드 허용
            );
            config.EnableCors(cors);

            // Attribute Routing 활성화 (예: [RoutePrefix("api/boards")])
            config.MapHttpAttributeRoutes();

            // 기본 라우팅 (Convention-based routing)
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // JSON 포매터 설정
            var jsonFormatter = config.Formatters.JsonFormatter;
            // 속성 이름을 camelCase로 변경 (예: Title -> title)
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            // Null 값은 무시
            jsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            // JSON 응답을 보기 좋게 들여쓰기
            jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            // 순환 참조(Reference Loop) 무시 (예: Board -> Replies -> Board...)
            jsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            // XML 포매터 제거 (JSON 응답만 사용)
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
```

### Step 3: `Global.asax.cs` 업데이트

애플리케이션 시작 시 `WebApiConfig.Register`를 호출하도록 `Global.asax.cs` 파일을 수정합니다. Spring.NET과의 통합을 위해 `SpringMvcApplication`을 이미 상속받고 있으므로, 추가적인 DI 설정은 필요하지 않습니다.

`SpringNet.Web/Global.asax.cs`:
```csharp
using Spring.Web.Mvc;
using System.Web.Http; // 추가
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace SpringNet.Web
{
    public class MvcApplication : SpringMvcApplication // SpringMvcApplication 상속 유지
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            // Web API 설정 등록 (이 줄 추가)
            GlobalConfiguration.Configure(WebApiConfig.Register);

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
```
**핵심**: `SpringMvcApplication`은 MVC 컨트롤러뿐만 아니라 Web API 컨트롤러의 의존성 주입도 처리해줍니다. Web API는 자체 DI 컨테이너를 가질 수 있으며, `SpringMvcApplication`이 시작될 때 이 컨테이너를 Spring 컨테이너와 연결해줍니다.

## 📝 Web API 컨트롤러 구현

### Step 4: API 요청/응답 모델 정의

API는 특정 작업에 필요한 데이터만 주고받는 것이 좋습니다. 이를 위해 요청(Request) 모델을 정의합니다.

`SpringNet.Web/Models/ApiModels.cs` 파일을 새로 생성합니다.
```csharp
using System.ComponentModel.DataAnnotations;

namespace SpringNet.Web.Models
{
    public class CreateBoardRequest
    {
        [Required(ErrorMessage = "제목은 필수입니다.")]
        [StringLength(200, ErrorMessage = "제목은 200자 이내여야 합니다.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "내용은 필수입니다.")]
        public string Content { get; set; }

        [Required(ErrorMessage = "작성자는 필수입니다.")]
        [StringLength(50)]
        public string Author { get; set; }
    }

    public class UpdateBoardRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public string CurrentUser { get; set; }
    }

    public class CreateReplyRequest
    {
        [Required]
        [StringLength(500)]
        public string Content { get; set; }

        [Required]
        [StringLength(50)]
        public string Author { get; set; }
    }
}
```

### Step 5: `BoardApiController` 구현

MVC 컨트롤러와 유사하지만 `ApiController`를 상속받고, `ActionResult` 대신 `IHttpActionResult`(또는 DTO 객체)를 반환합니다.

`SpringNet.Web/Controllers/BoardApiController.cs` 파일을 새로 생성합니다.
```csharp
using SpringNet.Service;
using SpringNet.Web.Models; // ApiModels 사용
using System;
using System.Web.Http;

namespace SpringNet.Web.Controllers
{
    [RoutePrefix("api/boards")]
    public class BoardApiController : ApiController
    {
        private readonly IBoardService boardService;

        // 생성자 주입을 통해 BoardService 의존성 주입
        public BoardApiController(IBoardService boardService)
        {
            this.boardService = boardService;
        }

        // GET api/boards
        [HttpGet, Route("")]
        public IHttpActionResult GetBoards([FromUri] int page = 1, [FromUri] int pageSize = 10)
        {
            try
            {
                var result = boardService.GetBoards(page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 실제 프로덕션에서는 ex.Message를 노출하지 않도록 주의
                return InternalServerError(ex);
            }
        }

        // GET api/boards/5
        [HttpGet, Route("{id:int}")]
        public IHttpActionResult GetBoard(int id)
        {
            var board = boardService.GetBoard(id);
            if (board == null)
            {
                return NotFound();
            }
            return Ok(board);
        }

        // POST api/boards
        [HttpPost, Route("")]
        public IHttpActionResult CreateBoard([FromBody] CreateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var id = boardService.CreateBoard(request.Title, request.Content, request.Author);
                var newBoardUri = new Uri(Request.RequestUri, id.ToString());
                return Created(newBoardUri, new { id });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/boards/5
        [HttpPut, Route("{id:int}")]
        public IHttpActionResult UpdateBoard(int id, [FromBody] UpdateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                // CurrentUser는 실제 인증 시스템과 연동하여 가져와야 함
                boardService.UpdateBoard(id, request.Title, request.Content, request.CurrentUser);
                return Ok(new { message = "게시글이 수정되었습니다." });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE api/boards/5
        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult DeleteBoard(int id, [FromUri] string currentUser)
        {
            try
            {
                // CurrentUser는 실제 인증 시스템과 연동하여 가져와야 함
                boardService.DeleteBoard(id, currentUser);
                return Ok(new { message = "게시글이 삭제되었습니다." });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
```

### Step 6: Spring.NET 설정 업데이트

`BoardApiController`를 Spring 컨테이너가 관리하고 의존성을 주입할 수 있도록 `controllers.xml`에 Bean으로 등록합니다.

`SpringNet.Web/Config/controllers.xml`에 다음 내용을 추가합니다.
```xml
<!-- controllers.xml -->
    <!-- ... 기존 Controller Bean 설정 ... -->

    <!-- API Controllers -->
    <object id="boardApiController"
            type="SpringNet.Web.Controllers.BoardApiController, SpringNet.Web">
        <constructor-arg ref="boardService" />
    </object>
</objects>
```

## 📚 Swagger API 문서화

Swagger는 API를 위한 인터랙티브 문서를 자동으로 생성해주는 강력한 도구입니다.

### 1. `SwaggerConfig.cs` 설정

`Swashbuckle` 패키지를 설치하면 `App_Start` 폴더에 `SwaggerConfig.cs` 파일이 자동으로 생성됩니다. 이 파일을 열어 API 문서에 대한 기본 정보를 설정하고, XML 주석을 사용하도록 설정합니다.

`SpringNet.Web/App_Start/SwaggerConfig.cs` (일부 수정):
```csharp
using Swashbuckle.Application;
using System.Web.Http;
using System;
using System.IO;

[assembly: PreApplicationStartMethod(typeof(SpringNet.Web.SwaggerConfig), "Register")]

namespace SpringNet.Web
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", "SpringNet.Web");
                        
                        // XML 주석 파일 경로 설정
                        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        var commentsFileName = "SpringNet.Web.XML";
                        var commentsFile = Path.Combine(baseDirectory, "bin", commentsFileName);
                        if (File.Exists(commentsFile))
                        {
                            c.IncludeXmlComments(commentsFile);
                        }
                    })
                .EnableSwaggerUi(c =>
                    {
                        // UI 커스터마이징
                    });
        }
    }
}
```

### 2. 프로젝트 속성에서 XML 문서 생성 활성화

1.  `SpringNet.Web` 프로젝트 우클릭 → **속성(Properties)**
2.  **빌드(Build)** 탭으로 이동
3.  **출력(Output)** 섹션에서 **XML 문서 파일(XML documentation file)** 체크
4.  경로가 `bin\SpringNet.Web.XML`로 되어 있는지 확인

### 3. API 컨트롤러에 주석 추가

`BoardApiController.cs`의 각 메서드에 `///`를 사용하여 XML 주석을 추가합니다.

```csharp
/// <summary>
/// 게시판 API
/// </summary>
[RoutePrefix("api/boards")]
public class BoardApiController : ApiController
{
    // ...

    /// <summary>
    /// 게시글 목록을 페이징하여 조회합니다.
    /// </summary>
    /// <param name="page">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>페이징된 게시글 목록</returns>
    [HttpGet, Route("")]
    public IHttpActionResult GetBoards([FromUri] int page = 1, [FromUri] int pageSize = 10)
    {
        // ...
    }

    // ... 다른 메서드에도 주석 추가 ...
}
```

### 4. Swagger UI 접속

애플리케이션을 실행하고 다음 주소로 이동하면 API 문서를 확인할 수 있습니다.
`http://[your-local-host]/swagger`

## 💡 RESTful API 설계 원칙

-   **자원(Resource) 중심의 URL**: URL은 동사가 아닌 명사로 표현합니다. (예: `/getBoards` (❌), `/boards` (✅))
-   **HTTP 메서드로 행위(Verb) 표현**: 자원에 대한 행위는 HTTP 메서드(GET, POST, PUT, DELETE 등)로 표현합니다.
-   **적절한 HTTP 상태 코드 반환**: API는 요청의 결과를 명확한 상태 코드(200, 201, 400, 404, 500 등)로 응답해야 합니다.

## 💡 핵심 정리

-   **통합 방식**: 새 프로젝트 대신 기존 `SpringNet.Web` 프로젝트에 Web API 패키지를 설치하여 기능을 통합했습니다.
-   **의존성 주입**: Spring.NET의 `SpringMvcApplication`이 MVC와 Web API의 DI를 모두 처리하므로, `controllers.xml`에 API 컨트롤러를 Bean으로 등록하고 생성자 주입을 통해 서비스를 주입했습니다.
-   **설정**: `WebApiConfig.cs`에서 라우팅, CORS, JSON 포매터 등을 설정하고, `Global.asax.cs`에서 이를 등록했습니다.
-   **API 문서화**: `Swashbuckle`(Swagger)을 사용하여 코드 주석만으로 인터랙티브한 API 문서를 자동으로 생성했습니다.

## 🚀 다음 단계

다음: **[19-advanced-crud-patterns.md](./19-advanced-crud-patterns.md)** - 고급 CRUD 패턴
