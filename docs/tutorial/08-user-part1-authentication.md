# 08. 사용자 관리 Part 1: 인증 (Authentication)

## 📚 학습 목표

- 사용자 인증 시스템 구현
- 회원가입 및 로그인
- 비밀번호 암호화 (Hash)
- 세션 관리

## 🛠️ User 엔티티 생성

> **⚠️ 기존 파일 삭제 필요**: `SpringNet.Domain/` 루트에 이미 `User.cs` 파일이 있습니다 (네임스페이스: `SpringNet.Domain`, 최소 구현). 이 파일을 **삭제**하고, 아래 설명에 따라 `Entities` 폴더에 새로 만드세요. `SpringNet.Domain.csproj`에서도 기존 `<Compile Include="User.cs" />` 항목을 제거합니다.

`SpringNet.Domain/Entities/User.cs` (신규 생성):

```csharp
using System;

namespace SpringNet.Domain.Entities
{
    public class User
    {
        public virtual int Id { get; set; }
        public virtual string Username { get; set; }
        public virtual string Email { get; set; }
        public virtual string PasswordHash { get; set; }
        public virtual string FullName { get; set; }
        public virtual string Role { get; set; } // Admin, User
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime? LastLoginDate { get; set; }
        public virtual bool IsActive { get; set; }

        public User()
        {
            CreatedDate = DateTime.Now;
            IsActive = true;
            Role = "User";
        }
    }
}
```

### User 매핑

`SpringNet.Data/Mappings/User.hbm.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">
    <class name="User" table="Users">
        <id name="Id"><generator class="identity" /></id>
        <property name="Username" length="50" not-null="true" unique="true" />
        <property name="Email" length="100" not-null="true" unique="true" />
        <property name="PasswordHash" length="255" not-null="true" />
        <property name="FullName" length="100" />
        <property name="Role" length="20" not-null="true" />
        <property name="CreatedDate" type="datetime" not-null="true" />
        <property name="LastLoginDate" type="datetime" />
        <property name="IsActive" type="boolean" not-null="true" />
    </class>
</hibernate-mapping>
```

## 📦 UserRepository 구현

User 엔티티 저장소(Repository)를 Data 레이어에 추가합니다. 이 Repository는 이번 튜토리얼의 AuthService뿐만 아니라 이후 튜토리얼(장바구니, 주문)에서도 사용됩니다.

### IUserRepository 인터페이스

`SpringNet.Data/Repositories/IUserRepository.cs`:

```csharp
using SpringNet.Domain.Entities;

namespace SpringNet.Data.Repositories
{
    public interface IUserRepository
    {
        void Add(User user);
        User GetById(int id);
        User GetByUsername(string username);
        User GetByEmail(string email);
        void Update(User user);
        bool ExistsByUsername(string username);
        bool ExistsByEmail(string email);
    }
}
```

### UserRepository 구현

`SpringNet.Data/Repositories/UserRepository.cs`:

```csharp
using NHibernate;
using SpringNet.Domain.Entities;
using System.Linq;

namespace SpringNet.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ISessionFactory sessionFactory;

        public UserRepository(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public void Add(User user)
        {
            sessionFactory.GetCurrentSession().Save(user);
        }

        public User GetById(int id)
        {
            return sessionFactory.GetCurrentSession().Get<User>(id);
        }

        public User GetByUsername(string username)
        {
            return sessionFactory.GetCurrentSession().Query<User>()
                .FirstOrDefault(u => u.Username == username && u.IsActive);
        }

        public User GetByEmail(string email)
        {
            return sessionFactory.GetCurrentSession().Query<User>()
                .FirstOrDefault(u => u.Email == email);
        }

        public void Update(User user)
        {
            sessionFactory.GetCurrentSession().Update(user);
        }

        public bool ExistsByUsername(string username)
        {
            return sessionFactory.GetCurrentSession().Query<User>()
                .Any(u => u.Username == username);
        }

        public bool ExistsByEmail(string email)
        {
            return sessionFactory.GetCurrentSession().Query<User>()
                .Any(u => u.Email == email);
        }
    }
}
```

> **💡 설계 포인트**: `UserRepository`는 `GetCurrentSession()`을 직접 사용합니다. 세션은 Spring.NET의 트랜잭션 관리와 NHibernate의 `CurrentSessionContext`에 의해 외부에서 열리고 닫힙니다. Repository 내부에서 세션을 직접 열지 않도록 주의하세요.

### 🔐 인증 서비스 구현

#### DTOs/UserDto.cs

`SpringNet.Service/DTOs/UserDto.cs`:

```csharp
namespace SpringNet.Service.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }
}
```

### IAuthService

`SpringNet.Service/IAuthService.cs`:

```csharp
using SpringNet.Service.DTOs;

namespace SpringNet.Service
{
    public interface IAuthService
    {
        UserDto Register(string username, string email, string password, string fullName);
        UserDto Login(string username, string password);
        void Logout(int userId);
        bool IsUsernameAvailable(string username);
        bool IsEmailAvailable(string email);
        void ChangePassword(int userId, string oldPassword, string newPassword);
    }
}
```

### AuthService 구현

`SpringNet.Service/AuthService.cs`:

```csharp
using NHibernate;
using SpringNet.Domain.Entities;
using SpringNet.Service.DTOs;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SpringNet.Service
{
    public class AuthService : IAuthService
    {
        private readonly ISessionFactory sessionFactory;

        public AuthService(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public UserDto Register(string username, string email, string password, string fullName)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    // 중복 검사
                    if (!IsUsernameAvailable(username))
                        throw new ArgumentException("이미 사용 중인 사용자명입니다.");

                    if (!IsEmailAvailable(email))
                        throw new ArgumentException("이미 사용 중인 이메일입니다.");

                    var user = new User
                    {
                        Username = username,
                        Email = email,
                        PasswordHash = HashPassword(password),
                        FullName = fullName
                    };

                    sessionFactory.GetCurrentSession().Save(user);
                    tx.Commit();

                    return MapToUserDto(user);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public UserDto Login(string username, string password)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var user = sessionFactory.GetCurrentSession().Query<User>()
                        .FirstOrDefault(u => u.Username == username && u.IsActive);

                    if (user == null)
                        throw new UnauthorizedAccessException("사용자를 찾을 수 없습니다.");

                    if (!VerifyPassword(password, user.PasswordHash))
                        throw new UnauthorizedAccessException("비밀번호가 일치하지 않습니다.");

                    // 마지막 로그인 시간 업데이트
                    user.LastLoginDate = DateTime.Now;
                    sessionFactory.GetCurrentSession().Update(user);
                    tx.Commit();

                    return MapToUserDto(user);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void Logout(int userId)
        {
            // Logout logic would typically be handled at the web layer (e.g., clearing session)
            // No direct DB interaction here, but method signature is part of IAuthService.
            // If LastLoginDate was to be cleared or a logout timestamp recorded, it would be done here.
            // For now, no action.
        }

        public bool IsUsernameAvailable(string username)
        {
            using (sessionFactory.GetCurrentSession().BeginTransaction())
            {
                return !sessionFactory.GetCurrentSession().Query<User>().Any(u => u.Username == username);
            }
        }

        public bool IsEmailAvailable(string email)
        {
            using (sessionFactory.GetCurrentSession().BeginTransaction())
            {
                return !sessionFactory.GetCurrentSession().Query<User>().Any(u => u.Email == email);
            }
        }

        public void ChangePassword(int userId, string oldPassword, string newPassword)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var user = sessionFactory.GetCurrentSession().Get<User>(userId);
                    if (user == null) throw new ArgumentException("사용자를 찾을 수 없습니다.");
                    
                    if (!VerifyPassword(oldPassword, user.PasswordHash))
                        throw new UnauthorizedAccessException("기존 비밀번호가 일치하지 않습니다.");

                    user.PasswordHash = HashPassword(newPassword);
                    sessionFactory.GetCurrentSession().Update(user);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        // SHA256 해싱
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            var hash = HashPassword(password);
            return hash == passwordHash;
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };
        }
    }
}
```

## 🎮 AccountController

`SpringNet.Web/Controllers/AccountController.cs`:

```csharp
using SpringNet.Service;
using SpringNet.Service.DTOs;
using System;
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService authService;

        // 생성자 주입 사용
        public AccountController(IAuthService authService)
        {
            this.authService = authService;
        }

        // 회원가입 폼
        public ActionResult Register()
        {
            return View();
        }

        // 회원가입 처리
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(string username, string email,
                                     string password, string confirmPassword,
                                     string fullName)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "비밀번호가 일치하지 않습니다.");
                return View();
            }

            try
            {
                var user = authService.Register(username, email, password, fullName);
                TempData["Success"] = "회원가입이 완료되었습니다.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // 로그인 폼
        public ActionResult Login()
        {
            return View();
        }

        // 로그인 처리
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password, string returnUrl)
        {
            try
            {
                var user = authService.Login(username, password);

                // 세션에 사용자 정보 저장
                Session["UserId"] = user.Id;
                Session["Username"] = user.Username;
                Session["Role"] = user.Role;

                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        // 로그아웃
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
```

### 📢 계정 관련 뷰 (Razor Views)

#### `Views/Account/Register.cshtml` (회원가입 폼)

```html
@{
    ViewBag.Title = "회원가입";
}

<h2>회원가입</h2>

@if (!ViewData.ModelState.IsValid)
{
    <div class="alert alert-danger">
        @Html.ValidationSummary()
    </div>
}

<form method="post" action="@Url.Action("Register")">
    @Html.AntiForgeryToken()

    <div class="form-group">
        <label>사용자명</label>
        <input type="text" name="username" class="form-control" required />
    </div>
    <div class="form-group">
        <label>이메일</label>
        <input type="email" name="email" class="form-control" required />
    </div>
    <div class="form-group">
        <label>비밀번호</label>
        <input type="password" name="password" class="form-control" required />
    </div>
    <div class="form-group">
        <label>비밀번호 확인</label>
        <input type="password" name="confirmPassword" class="form-control" required />
    </div>
    <div class="form-group">
        <label>이름</label>
        <input type="text" name="fullName" class="form-control" />
    </div>

    <button type="submit" class="btn btn-primary">가입하기</button>
    <a href="@Url.Action("Login")" class="btn btn-secondary">로그인</a>
</form>
```

#### `Views/Account/Login.cshtml` (로그인 폼)

```html
@{
    ViewBag.Title = "로그인";
}

<h2>로그인</h2>

@if (!ViewData.ModelState.IsValid)
{
    <div class="alert alert-danger">
        @Html.ValidationSummary()
    </div>
}

<form method="post" action="@Url.Action("Login", new { returnUrl = Request.QueryString["ReturnUrl"] })">
    @Html.AntiForgeryToken()

    <div class="form-group">
        <label>사용자명</label>
        <input type="text" name="username" class="form-control" required />
    </div>
    <div class="form-group">
        <label>비밀번호</label>
        <input type="password" name="password" class="form-control" required />
    </div>

    <button type="submit" class="btn btn-primary">로그인</button>
    <a href="@Url.Action("Register")" class="btn btn-secondary">회원가입</a>
</form>
```

### 세션 관리

```csharp
// 세션 저장
Session["UserId"] = user.Id;

// 세션 읽기
var userId = Session["UserId"];

// 세션 삭제
Session.Clear();
```

## ⚙️ Spring.NET XML 설정 분리 (Refactoring)

`applicationContext.xml` 파일이 점점 길어지고 복잡해지고 있습니다. 이제 Spring.NET 설정 파일들을 역할에 따라 여러 파일로 분리하여 관리하는 방법을 배워보겠습니다. 이는 설정의 가독성, 유지보수성, 그리고 모듈성을 크게 향상시킵니다.

### 1. 새로운 설정 파일 생성

`SpringNet.Web/Config/` 폴더에 다음 세 개의 XML 파일을 새로 생성합니다.

-   `dataAccess.xml`: `SessionFactory`, Repository Bean들을 정의합니다.
-   `services.xml`: Service Bean들과 Logger Bean들을 정의합니다.
-   `controllers.xml`: Controller Bean들을 정의합니다.

#### `dataAccess.xml` 내용

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net">

    <!-- SessionFactory Bean -->
    <object id="sessionFactory"
            type="SpringNet.Data.NHibernateHelper, SpringNet.Data"
            factory-method="SessionFactory"
            singleton="true" />
            
    <!-- Repositories -->
    <object id="userRepository"
            type="SpringNet.Data.Repositories.UserRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <object id="productRepository"
            type="SpringNet.Data.Repositories.ProductRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <object id="boardRepository"
            type="SpringNet.Data.Repositories.BoardRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <object id="replyRepository"
            type="SpringNet.Data.Repositories.ReplyRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```

#### `services.xml` 내용

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net">

    <!-- ... (튜토리얼 01, 02에서 추가한 testService, greetingService, logger 등) ... -->
    <object id="testService" type="System.String">
        <constructor-arg value="Spring.NET is working!" />
    </object>
    <object id="greetingService" type="SpringNet.Service.GreetingService, SpringNet.Service">
        <constructor-arg name="prefix" value="안녕하세요" />
    </object>
    <object id="fileLogger" type="SpringNet.Service.Logging.FileLogger, SpringNet.Service">
        <constructor-arg name="logFilePath" value="C:/logs/springnet.log" />
        <constructor-arg name="appName" value="SpringNetApp" />
    </object>
    <object id="consoleLogger" type="SpringNet.Service.Logging.ConsoleLogger, SpringNet.Service" />
    <object id="logger" type="SpringNet.Service.Logging.CompositeLogger, SpringNet.Service">
        <constructor-arg name="loggers">
            <list element-type="SpringNet.Service.Logging.ILogger, SpringNet.Service">
                <ref object="fileLogger" />
                <ref object="consoleLogger" />
            </list>
        </constructor-arg>
    </object>

    <!-- Services -->
    <object id="productService" type="SpringNet.Service.ProductService, SpringNet.Service">
        <constructor-arg ref="productRepository" />
    </object>
    <object id="boardService" type="SpringNet.Service.BoardService, SpringNet.Service">
        <constructor-arg ref="boardRepository" />
        <constructor-arg ref="replyRepository" />
        <constructor-arg ref="sessionFactory" />
    </object>

    <!-- Auth Service (New!) -->
    <!-- AuthService는 ISessionFactory를 직접 사용하므로 sessionFactory만 주입 -->
    <object id="authService"
            type="SpringNet.Service.AuthService, SpringNet.Service">
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```

#### `controllers.xml` 내용 (생성자 주입으로 통일)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net">

    <!-- HomeController -->
    <object id="homeController"
            type="SpringNet.Web.Controllers.HomeController, SpringNet.Web">
        <constructor-arg name="testService" ref="testService" />
        <constructor-arg name="greetingService" ref="greetingService" />
        <constructor-arg name="logger" ref="logger" />
    </object>

    <!-- BoardController -->
    <object id="boardController"
            type="SpringNet.Web.Controllers.BoardController, SpringNet.Web">
        <constructor-arg name="boardService" ref="boardService" />
    </object>

    <!-- AccountController (New!) -->
    <object id="accountController"
            type="SpringNet.Web.Controllers.AccountController, SpringNet.Web">
        <constructor-arg name="authService" ref="authService" />
    </object>

</objects>
```

### 2. `applicationContext.xml` 수정

기존 `applicationContext.xml`의 모든 Bean 정의를 삭제하고, 새로 만든 설정 파일들을 `<import>` 하도록 수정합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net">

    <import resource="~/Config/dataAccess.xml" />
    <import resource="~/Config/services.xml" />
    <import resource="~/Config/controllers.xml" />

</objects>
```

### 3. 프로젝트 파일 업데이트 (`SpringNet.Web.csproj`)

새로 생성한 `dataAccess.xml`, `services.xml`, `controllers.xml` 파일들을 `SpringNet.Web.csproj`에 `Content` 아이템으로 추가합니다.

```xml
<ItemGroup>
  <Content Include="Config\applicationContext.xml" />
  <Content Include="Config\dataAccess.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="Config\services.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <Content Include="Config\controllers.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  <!-- ... 다른 Content 파일들 ... -->
</ItemGroup>
```

## 📢 프로젝트 파일 및 폴더 설정

이 튜토리얼에서 추가한 파일과 폴더들을 프로젝트에 반영해야 합니다.

#### 1. `SpringNet.Domain` 프로젝트
-   `Entities` 폴더에 `User.cs` 파일을 생성합니다.
-   `SpringNet.Domain.csproj`의 `<ItemGroup>` (Compile)에 `Entities\User.cs`를 추가합니다.
    ```xml
    <ItemGroup>
      <Compile Include="Entities\Board.cs" />
      <Compile Include="Entities\Product.cs" />
      <Compile Include="Entities\Reply.cs" />
      <Compile Include="Entities\User.cs" />
      <Compile Include="Properties\AssemblyInfo.cs" />
    </ItemGroup>
    ```

#### 2. `SpringNet.Data` 프로젝트
-   `Mappings` 폴더에 `User.hbm.xml` 파일을 생성합니다.
-   `SpringNet.Data.csproj`의 `<ItemGroup>` (EmbeddedResource)에 `Mappings\User.hbm.xml`를 추가하고, `UserRepository` 관련 파일들도 추가합니다.
    ```xml
    <ItemGroup>
      <EmbeddedResource Include="Mappings\Board.hbm.xml" />
      <EmbeddedResource Include="Mappings\Product.hbm.xml" />
      <EmbeddedResource Include="Mappings\Reply.hbm.xml" />
      <EmbeddedResource Include="Mappings\User.hbm.xml" />
    </ItemGroup>
    <ItemGroup>
        <!-- ... 기존 Repository들 ... -->
        <Compile Include="Repositories\IUserRepository.cs" />
        <Compile Include="Repositories\UserRepository.cs" />
    </ItemGroup>
    ```

#### 3. `SpringNet.Service` 프로젝트
-   `DTOs` 폴더에 `UserDto.cs` 파일을 생성합니다.
-   `SpringNet.Service` 폴더에 `IAuthService.cs`와 `AuthService.cs` 파일을 생성합니다.
-   `SpringNet.Service.csproj`의 `<ItemGroup>` (Compile)에 다음을 추가합니다.
    ```xml
    <ItemGroup>
      <!-- ... 기존 서비스 및 DTO들 ... -->
      <Compile Include="DTOs\UserDto.cs" />
      <Compile Include="IAuthService.cs" />
      <Compile Include="AuthService.cs" />
    </ItemGroup>
    ```

#### 4. `SpringNet.Web` 프로젝트
-   `Controllers` 폴더에 `AccountController.cs` 파일을 생성합니다.
-   `SpringNet.Web.csproj`의 `<ItemGroup>` (Compile)에 `Controllers\AccountController.cs`를 추가합니다.
-   `Views` 폴더에 `Account` 폴더를 새로 만들고, 그 안에 `Register.cshtml`과 `Login.cshtml` 파일을 생성합니다.

## 🚀 다음 단계

다음: **[09-user-part2-authorization.md](./09-user-part2-authorization.md)** - 권한 관리
