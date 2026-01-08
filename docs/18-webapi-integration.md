# 18. ASP.NET Web API 통합

## 📚 학습 목표

- ASP.NET Web API 프로젝트 생성
- Spring.NET과 Web API 통합
- RESTful API 설계 및 구현
- JSON 직렬화 설정
- CORS 설정
- API 문서화 (Swagger)

## 🎯 Web API vs MVC

```
MVC Controller     → HTML View 반환
API Controller     → JSON/XML 데이터 반환
```

**사용 시기**:
- ✅ **Web API**: 모바일 앱, SPA, 외부 시스템 연동
- ✅ **MVC**: 전통적인 웹 애플리케이션

## 🛠️ Web API 프로젝트 추가

### 1. 프로젝트 생성

기존 솔루션에 새 프로젝트 추가:
- Visual Studio → 새 프로젝트 추가
- **ASP.NET Web Application (.NET Framework)**
- 이름: `SpringNet.WebAPI`
- 템플릿: **Web API**

### 2. NuGet 패키지 설치

```
PM> Install-Package Spring.Web.Mvc5 -Version 3.0.0
PM> Install-Package Microsoft.AspNet.WebApi -Version 5.2.9
PM> Install-Package Newtonsoft.Json -Version 13.0.1
PM> Install-Package Swashbuckle -Version 5.6.0 (선택)
```

### 3. 프로젝트 참조 추가

SpringNet.WebAPI 프로젝트에서:
- SpringNet.Domain
- SpringNet.Data
- SpringNet.Service

## 📝 Web API Controller 구현

### 1. BoardApiController

`SpringNet.WebAPI/Controllers/BoardApiController.cs`:

```csharp
using SpringNet.Service;
using SpringNet.Service.DTOs;
using System.Collections.Generic;
using System.Web.Http;

namespace SpringNet.WebAPI.Controllers
{
    [RoutePrefix("api/boards")]
    public class BoardApiController : ApiController
    {
        public IBoardService BoardService { get; set; }

        // GET api/boards
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetBoards([FromUri] int page = 1, [FromUri] int pageSize = 10)
        {
            try
            {
                var result = BoardService.GetBoards(page, pageSize);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/boards/5
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetBoard(int id)
        {
            try
            {
                var board = BoardService.GetBoard(id);

                if (board == null)
                {
                    return NotFound();
                }

                return Ok(board);
            }
            catch (System.Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST api/boards
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateBoard([FromBody] CreateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var id = BoardService.CreateBoard(
                    request.Title,
                    request.Content,
                    request.Author
                );

                return Created($"api/boards/{id}", new { id });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/boards/5
        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult UpdateBoard(int id, [FromBody] UpdateBoardRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                BoardService.UpdateBoard(id, request.Title, request.Content, request.CurrentUser);
                return Ok(new { message = "게시글이 수정되었습니다." });
            }
            catch (System.UnauthorizedAccessException ex)
            {
                return Unauthorized();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE api/boards/5
        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult DeleteBoard(int id, [FromUri] string currentUser)
        {
            try
            {
                BoardService.DeleteBoard(id, currentUser);
                return Ok(new { message = "게시글이 삭제되었습니다." });
            }
            catch (System.UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET api/boards/search?keyword=spring
        [HttpGet]
        [Route("search")]
        public IHttpActionResult SearchBoards([FromUri] string keyword, [FromUri] int page = 1, [FromUri] int pageSize = 10)
        {
            try
            {
                var result = BoardService.SearchBoards(keyword, page, pageSize);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST api/boards/5/replies
        [HttpPost]
        [Route("{id:int}/replies")]
        public IHttpActionResult AddReply(int id, [FromBody] CreateReplyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var replyId = BoardService.AddReply(id, request.Content, request.Author);
                return Created($"api/boards/{id}/replies/{replyId}", new { replyId });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
```

### 2. Request/Response 모델

```csharp
using System.ComponentModel.DataAnnotations;

namespace SpringNet.WebAPI.Models
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

## 🔧 Spring.NET 통합

### 1. Web.config 설정

`SpringNet.WebAPI/Web.config`:

```xml
<?xml version="1.0"?>
<configuration>
    <configSections>
        <sectionGroup name="spring">
            <section name="context"
                     type="Spring.Context.Support.WebContextHandler, Spring.Web" />
        </sectionGroup>
    </configSections>

    <spring>
        <context>
            <resource uri="~/Config/applicationContext.xml" />
        </context>
    </spring>

    <system.web>
        <compilation debug="true" targetFramework="4.8" />
        <httpRuntime targetFramework="4.8" />
    </system.web>

    <system.webServer>
        <handlers>
            <remove name="ExtensionlessUrlHandler-Integrated-4.0" />
            <add name="ExtensionlessUrlHandler-Integrated-4.0"
                 path="*." verb="*"
                 type="System.Web.Handlers.TransferRequestHandler"
                 preCondition="integratedMode,runtimeVersionv4.0" />
        </handlers>
    </system.webServer>
</configuration>
```

### 2. applicationContext.xml

`SpringNet.WebAPI/Config/applicationContext.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net">

    <!-- SessionFactory -->
    <object id="sessionFactory"
            type="SpringNet.Data.NHibernateHelper, SpringNet.Data"
            factory-method="SessionFactory">
    </object>

    <!-- Repositories -->
    <object id="boardRepository"
            type="SpringNet.Data.Repositories.BoardRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <object id="replyRepository"
            type="SpringNet.Data.Repositories.ReplyRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <!-- Services -->
    <object id="boardService"
            type="SpringNet.Service.BoardService, SpringNet.Service">
        <constructor-arg ref="boardRepository" />
        <constructor-arg ref="replyRepository" />
        <constructor-arg ref="sessionFactory" />
    </object>

    <!-- API Controllers -->
    <object id="boardApiController"
            type="SpringNet.WebAPI.Controllers.BoardApiController, SpringNet.WebAPI">
        <property name="BoardService" ref="boardService" />
    </object>

</objects>
```

### 3. WebApiConfig.cs

`SpringNet.WebAPI/App_Start/WebApiConfig.cs`:

```csharp
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SpringNet.WebAPI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Attribute Routing 활성화
            config.MapHttpAttributeRoutes();

            // 기본 라우팅
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // JSON Formatter 설정
            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            jsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            jsonFormatter.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            // XML Formatter 제거 (JSON만 사용)
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
```

### 4. Global.asax.cs

```csharp
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Spring.Web.Mvc;

namespace SpringNet.WebAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Spring.NET Dependency Resolver for Web API
            GlobalConfiguration.Configuration.DependencyResolver =
                new Spring.Web.Http.SpringWebApiDependencyResolver();
        }
    }
}
```

### 5. SpringWebApiDependencyResolver

`SpringNet.Infrastructure/Spring.Web.Http/SpringWebApiDependencyResolver.cs`:

```csharp
using Spring.Context.Support;
using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;

namespace Spring.Web.Http
{
    public class SpringWebApiDependencyResolver : IDependencyResolver
    {
        public object GetService(Type serviceType)
        {
            try
            {
                return ContextRegistry.GetContext().GetObject(serviceType);
            }
            catch
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                var names = ContextRegistry.GetContext().GetObjectNamesForType(serviceType);
                var services = new List<object>();

                foreach (var name in names)
                {
                    services.Add(ContextRegistry.GetContext().GetObject(name));
                }

                return services;
            }
            catch
            {
                return new List<object>();
            }
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }

        public void Dispose()
        {
            // No-op
        }
    }
}
```

## 🌐 CORS 설정 (Cross-Origin)

### 1. NuGet 패키지 설치

```
PM> Install-Package Microsoft.AspNet.WebApi.Cors
```

### 2. WebApiConfig에서 CORS 활성화

```csharp
using System.Web.Http;
using System.Web.Http.Cors;

public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        // CORS 활성화
        var cors = new EnableCorsAttribute(
            origins: "*",           // 허용할 도메인 (* = 모든 도메인)
            headers: "*",           // 허용할 헤더
            methods: "*"            // 허용할 HTTP 메서드
        );
        config.EnableCors(cors);

        // 또는 특정 도메인만 허용
        /*
        var cors = new EnableCorsAttribute(
            origins: "http://localhost:3000,https://myapp.com",
            headers: "*",
            methods: "GET,POST,PUT,DELETE"
        );
        */

        config.MapHttpAttributeRoutes();
        // ...
    }
}
```

### 3. Controller별 CORS 설정

```csharp
// 특정 Controller에만 CORS 적용
[EnableCors(origins: "http://localhost:3000", headers: "*", methods: "*")]
public class BoardApiController : ApiController
{
    // ...
}

// CORS 비활성화
[DisableCors]
public class AdminApiController : ApiController
{
    // ...
}
```

## 📚 Swagger API 문서화

### 1. Swashbuckle 설치

```
PM> Install-Package Swashbuckle
```

### 2. SwaggerConfig.cs

설치 시 자동 생성됨: `App_Start/SwaggerConfig.cs`

```csharp
using Swashbuckle.Application;
using System.Web.Http;

[assembly: PreApplicationStartMethod(typeof(SpringNet.WebAPI.SwaggerConfig), "Register")]

namespace SpringNet.WebAPI
{
    public class SwaggerConfig
    {
        public static void Register()
        {
            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "SpringNet API");
                    c.DescribeAllEnumsAsStrings();
                })
                .EnableSwaggerUi(c =>
                {
                    c.DocumentTitle("SpringNet API Documentation");
                });
        }
    }
}
```

### 3. API 문서 주석

```csharp
/// <summary>
/// 게시판 API
/// </summary>
[RoutePrefix("api/boards")]
public class BoardApiController : ApiController
{
    /// <summary>
    /// 게시글 목록 조회
    /// </summary>
    /// <param name="page">페이지 번호</param>
    /// <param name="pageSize">페이지 크기</param>
    /// <returns>게시글 목록</returns>
    [HttpGet]
    [Route("")]
    public IHttpActionResult GetBoards([FromUri] int page = 1, [FromUri] int pageSize = 10)
    {
        // ...
    }

    /// <summary>
    /// 게시글 상세 조회
    /// </summary>
    /// <param name="id">게시글 ID</param>
    /// <returns>게시글 상세 정보</returns>
    [HttpGet]
    [Route("{id:int}")]
    public IHttpActionResult GetBoard(int id)
    {
        // ...
    }
}
```

**프로젝트 속성 설정**:
1. 프로젝트 우클릭 → 속성
2. 빌드 → XML 문서 파일 체크
3. 경로: `bin\SpringNet.WebAPI.xml`

### 4. Swagger UI 접속

```
http://localhost:포트/swagger
```

## 🧪 API 테스트

### Postman 테스트

```
GET    http://localhost:5000/api/boards?page=1&pageSize=10
GET    http://localhost:5000/api/boards/1
POST   http://localhost:5000/api/boards
PUT    http://localhost:5000/api/boards/1
DELETE http://localhost:5000/api/boards/1?currentUser=홍길동
```

**POST 요청 Body** (JSON):
```json
{
    "title": "테스트 게시글",
    "content": "테스트 내용입니다.",
    "author": "홍길동"
}
```

## 💡 RESTful API 설계 원칙

### 1. HTTP 메서드

| 메서드 | 용도 | 예시 |
|--------|------|------|
| GET | 조회 | GET /api/boards |
| POST | 생성 | POST /api/boards |
| PUT | 전체 수정 | PUT /api/boards/1 |
| PATCH | 부분 수정 | PATCH /api/boards/1 |
| DELETE | 삭제 | DELETE /api/boards/1 |

### 2. URL 설계

```
✅ 좋은 예:
GET    /api/boards
GET    /api/boards/1
POST   /api/boards
GET    /api/boards/1/replies

❌ 나쁜 예:
GET    /api/getBoards
POST   /api/createBoard
GET    /api/board_detail?id=1
```

### 3. HTTP 상태 코드

| 코드 | 의미 | 사용 시기 |
|------|------|-----------|
| 200 | OK | 성공 |
| 201 | Created | 생성 성공 |
| 204 | No Content | 삭제 성공 |
| 400 | Bad Request | 잘못된 요청 |
| 401 | Unauthorized | 인증 실패 |
| 403 | Forbidden | 권한 없음 |
| 404 | Not Found | 리소스 없음 |
| 500 | Internal Server Error | 서버 오류 |

## 💡 핵심 정리

### Web API 장점

✅ **플랫폼 독립성** (모바일, 웹, 데스크톱)
✅ **JSON/XML** 데이터 교환
✅ **RESTful** 설계
✅ **확장성**

### Spring.NET 통합

✅ `SpringWebApiDependencyResolver` 사용
✅ applicationContext.xml에 API Controller 등록
✅ Property Injection으로 Service 주입

## 🚀 다음 단계

다음: **[19-advanced-crud-patterns.md](./19-advanced-crud-patterns.md)** - 고급 CRUD 패턴
