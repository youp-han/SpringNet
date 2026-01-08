# 08. 사용자 관리 Part 1: 인증 (Authentication)

## 📚 학습 목표

- 사용자 인증 시스템 구현
- 회원가입 및 로그인
- 비밀번호 암호화 (Hash)
- 세션 관리

## 🛠️ User 엔티티 생성

`SpringNet.Domain/Entities/User.cs`:

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

## 🔐 인증 서비스 구현

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
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
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

                session.Save(user);
                tx.Commit();

                return MapToUserDto(user);
            }
        }

        public UserDto Login(string username, string password)
        {
            using (var session = sessionFactory.OpenSession())
            {
                var user = session.Query<User>()
                    .FirstOrDefault(u => u.Username == username && u.IsActive);

                if (user == null)
                    throw new UnauthorizedAccessException("사용자를 찾을 수 없습니다.");

                if (!VerifyPassword(password, user.PasswordHash))
                    throw new UnauthorizedAccessException("비밀번호가 일치하지 않습니다.");

                // 마지막 로그인 시간 업데이트
                user.LastLoginDate = DateTime.Now;
                session.Update(user);
                session.Flush();

                return MapToUserDto(user);
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

        public bool IsUsernameAvailable(string username)
        {
            using (var session = sessionFactory.OpenSession())
            {
                return !session.Query<User>().Any(u => u.Username == username);
            }
        }

        public bool IsEmailAvailable(string email)
        {
            using (var session = sessionFactory.OpenSession())
            {
                return !session.Query<User>().Any(u => u.Email == email);
            }
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
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class AccountController : Controller
    {
        public IAuthService AuthService { get; set; }

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
                var user = AuthService.Register(username, email, password, fullName);
                TempData["Success"] = "회원가입이 완료되었습니다.";
                return RedirectToAction("Login");
            }
            catch (System.Exception ex)
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
                var user = AuthService.Login(username, password);

                // 세션에 사용자 정보 저장
                Session["UserId"] = user.Id;
                Session["Username"] = user.Username;
                Session["Role"] = user.Role;

                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (System.Exception ex)
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

## 💡 핵심 정리

### 비밀번호 보안

✅ **절대 평문 저장 금지**
✅ SHA256 이상의 해시 사용
✅ Salt 추가 권장 (실전)
✅ BCrypt 사용 권장 (실전)

### 세션 관리

```csharp
// 세션 저장
Session["UserId"] = user.Id;

// 세션 읽기
var userId = Session["UserId"];

// 세션 삭제
Session.Clear();
```

## 🚀 다음 단계

다음: **[09-user-part2-authorization.md](./09-user-part2-authorization.md)** - 권한 관리
