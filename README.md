# Spring.NET + NHibernate 완벽 학습 가이드

<div align="center">

![Spring.NET](https://img.shields.io/badge/Spring.NET-3.0.3-green)
![NHibernate](https://img.shields.io/badge/NHibernate-5.6-blue)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple)
![License](https://img.shields.io/badge/license-MIT-blue)

**엔터프라이즈 .NET 애플리케이션 개발을 위한 실전 튜토리얼**

[시작하기](#-시작하기) • [학습 로드맵](#-학습-로드맵) • [프로젝트 구조](#-프로젝트-구조) • [기여하기](#-기여하기)

</div>

---

## 📖 소개

이 프로젝트는 **Spring.NET**과 **NHibernate**를 활용한 엔터프라이즈 웹 애플리케이션 개발을 단계별로 학습할 수 있는 **한글 튜토리얼**입니다.

### 💡 왜 이 가이드인가?

- ✅ **한글 자료 부족 해결** - Spring.NET과 NHibernate의 한글 학습 자료가 부족한 현실을 해결
- ✅ **실전 프로젝트 중심** - 게시판, 사용자 관리, 쇼핑몰 등 3개의 실전 프로젝트 포함
- ✅ **단계별 학습** - 기초부터 고급까지 20개의 체계적인 단계
- ✅ **완전한 예제 코드** - 모든 단계마다 동작하는 코드 예제 제공
- ✅ **실무 패턴** - Repository, Spring.NET 컨텍스트 기반의 Unit of Work, Specification, Soft Delete, Audit Trail 등 실무 디자인 패턴 포함

## 🎯 학습 목표

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

- 🔹 **Spring.NET IoC/DI** - 의존성 주입과 제어의 역전 완벽 이해
- 🔹 **NHibernate ORM** - 객체-관계 매핑을 통한 데이터베이스 연동
- 🔹 **레이어드 아키텍처** - Domain, Data, Service, Web 계층 분리 설계
- 🔹 **RESTful Web API** - 기존 ASP.NET MVC 프로젝트 내에 Web API 통합 개발
- 🔹 **고급 쿼리** - HQL, LINQ, Criteria API, Stored Procedure
- 🔹 **디자인 패턴** - Repository, Spring.NET 컨텍스트 기반의 Unit of Work, Specification, Soft Delete, Audit Trail 패턴
- 🔹 **실전 프로젝트** - 게시판, 사용자 관리 시스템, 쇼핑몰 구현
- 🔹 **트랜잭션 관리** - 선언적 트랜잭션, 동시성 문제 해결
- 🔹 **성능 및 보안** - 캐싱, Lazy/Eager Loading, SQL Injection, XSS, CSRF, 비밀번호 해싱

## 🚀 시작하기

### 필수 요구사항

- **Visual Studio 2022** (Community 이상)
- **.NET Framework 4.8**
- **SQL Server 2019+** 또는 **SQLite** (학습용)

### 설치 및 실행

1. **프로젝트 클론**
   ```bash
   git clone https://github.com/yourusername/SpringNet.git
   cd SpringNet
   ```

2. **NuGet 패키지 복원**
   ```bash
   nuget restore SpringNet.sln
   ```

3. **데이터베이스 설정**
   - **SQLite 사용 시 (권장)**: NuGet 패키지 관리자 콘솔에서 아래 명령 실행 후 기본 설정 그대로 사용
     ```powershell
     Install-Package System.Data.SQLite.Core -Version 1.0.118.0
     ```
     DB 파일은 `SpringNet.Web/App_Data/springnet.db`에 자동 생성됩니다.
   - **SQL Server 사용 시**: `SpringNet.Data/hibernate.cfg.xml` 연결 문자열 수정

4. **학습 시작**
   - `docs/tutorial/00-overview.md` 파일부터 시작
   - 순서대로 01, 02, 03... 진행

## 📚 학습 로드맵

### 총 20개 튜토리얼 (약 40-50시간 학습 분량)

<details>
<summary><b>Phase 1: 기초 개념 (1-3단계)</b> ⭐</summary>

- [01. Spring.NET 기본 개념](docs/tutorial/01-springnet-basics.md) - IoC/DI 원리, CRUD 흐름 이해
- [02. 의존성 주입 심화](docs/tutorial/02-dependency-injection.md) - 생성자/프로퍼티 주입
- [03. NHibernate 설정](docs/tutorial/03-nhibernate-setup.md) - ORM 기본 설정, SQLite/SQL Server 연결

</details>

<details>
<summary><b>Phase 2: 게시판 시스템 (4-7단계)</b> ⭐⭐</summary>

- [04. 도메인 모델 설계](docs/tutorial/04-board-part1-domain.md) - Entity 설계 및 매핑
- [05. Repository 패턴](docs/tutorial/05-board-part2-repository.md) - 데이터 액세스 계층
- [06. Service Layer](docs/tutorial/06-board-part3-service.md) - 비즈니스 로직 분리
- [07. MVC 컨트롤러 & 뷰](docs/tutorial/07-board-part4-mvc.md) - 웹 프레젠테이션

</details>

<details>
<summary><b>Phase 3: 사용자 관리 (8-9단계)</b> ⭐⭐⭐</summary>

- [08. 인증 (Authentication)](docs/tutorial/08-user-part1-authentication.md) - 회원가입, 로그인, UserRepository
- [09. 인가 (Authorization)](docs/tutorial/09-user-part2-authorization.md) - 권한 관리 ⚠️ **Board 엔티티 Breaking Change 포함**

</details>

<details>
<summary><b>Phase 4: 쇼핑몰 시스템 (10-12단계)</b> ⭐⭐⭐⭐</summary>

- [10. 상품 관리](docs/tutorial/10-shopping-part1-products.md) - 카테고리, 상품 CRUD
- [11. 장바구니](docs/tutorial/11-shopping-part2-cart.md) - 장바구니 기능
- [12. 주문 처리](docs/tutorial/12-shopping-part3-order.md) - 주문 및 결제

</details>

<details>
<summary><b>Phase 5: 고급 주제 (13-14단계)</b> ⭐⭐⭐⭐</summary>

- [13. 트랜잭션 관리](docs/tutorial/13-transaction-management.md) - ACID, 격리 수준
- [14. 베스트 프랙티스](docs/tutorial/14-best-practices.md) - 보안, 성능 최적화

</details>

<details open>
<summary><b>Phase 6: 실무 심화 (15-19단계)</b> ⭐⭐⭐⭐⭐</summary>

- [15. NHibernate 고급 쿼리](docs/tutorial/15-advanced-nhibernate-queries.md) - HQL, LINQ, Criteria
- [16. Stored Procedure](docs/tutorial/16-stored-procedures.md) - 프로시저 사용법
- [17. 세션 관리](docs/tutorial/17-session-management.md) - NHibernate & Web Session
- [18. Web API 통합](docs/tutorial/18-webapi-integration.md) - RESTful API 개발
- [19. 고급 CRUD 패턴](docs/tutorial/19-advanced-crud-patterns.md) - UoW, Specification

</details>

<details>
<summary><b>📎 부록</b></summary>

- [SQL 스크립트 모음](docs/tutorial/appendix-sql-scripts.md) - 전체 DDL (SQL Server/SQLite), Tutorial 09 마이그레이션 SQL, 테스트 데이터

</details>

## 🏗️ 프로젝트 구조 (업데이트)

```
SpringNet/
│
├── docs/tutorial/                          # 📚 학습 문서 (20개 튜토리얼 + 부록)
│   ├── 00-overview.md                # 전체 로드맵
│   ├── 01-springnet-basics.md        # Spring.NET 기초
│   ├── 02-dependency-injection.md
│   ├── ... (01~19 총 20개 튜토리얼)
│   └── appendix-sql-scripts.md       # 부록: 전체 DDL 및 마이그레이션 SQL
│
├── SpringNet.Domain/              # 🎯 도메인 계층
│   └── Entities/                 # 엔티티 클래스 (예: Board.cs, Reply.cs, User.cs, Product.cs)
│   └── Specifications/           # Specification 패턴 구현
│
├── SpringNet.Data/                # 💾 데이터 액세스 계층
│   ├── Repositories/             # Repository 구현 (Generic Repository, Specific Repositories)
│   ├── Mappings/                 # NHibernate 매핑 (*.hbm.xml, Filters.hbm.xml)
│   ├── Listeners/                # NHibernate 이벤트 리스너 (AuditEventListener)
│   └── NHibernateHelper.cs       # SessionFactory 관리 (또는 LocalSessionFactoryObject)
│
├── SpringNet.Service/             # 🔧 서비스 계층
│   ├── Abstractions/             # 추상화 (예: IWebUserSession)
│   ├── DTOs/                     # Data Transfer Objects
│   └── Logging/                  # 로깅 인터페이스 및 구현
│
├── SpringNet.Infrastructure/      # 🛠️ 공통 인프라 (현재 비어있음, 필요시 활용)
│
├── SpringNet.Web/                 # 🌐 웹 프레젠테이션 (MVC & Web API 통합)
│   ├── Controllers/              # MVC 및 API 컨트롤러
│   ├── Filters/                  # 커스텀 필터 (예: AuthorizeAttribute)
│   ├── Infrastructure/           # 웹 인프라 관련 코드 (예: WebUserSession)
│   ├── Models/                   # View Models 및 API Request/Response 모델
│   ├── Views/
│   ├── Config/                   # Spring 설정 파일 (applicationContext.xml 분리)
│   │   ├── applicationContext.xml
│   │   ├── dataAccess.xml
│   │   ├── services.xml
│   │   └── controllers.xml
│   └── Web.config
│
└── SpringNet.Tests/               # 🧪 단위 및 통합 테스트 프로젝트 (선택)
    └── ServiceTests/             # 서비스 계층 테스트
```

## 🛠️ 기술 스택

### 핵심 프레임워크

| 기술 | 버전 | 용도 |
|------|------|------|
| **Spring.NET** | 3.0.0 | IoC/DI 컨테이너, AOP, 트랜잭션 관리 |
| **NHibernate** | 5.6.0 | ORM (객체-관계 매핑) |
| **ASP.NET MVC** | 5.2.9 | 웹 프레임워크 |
| **ASP.NET Web API** | 5.2.9 | RESTful API (기존 MVC 프로젝트에 통합) |
| **.NET Framework** | 4.8 | 런타임 |

### 주요 NuGet 패키지

-   `Spring.Core`, `Spring.Web`, `Spring.Web.Mvc5` (Spring.NET 핵심)
-   `Spring.Transaction.Interceptor` (선언적 트랜잭션)
-   `NHibernate`, `NHibernate.Caches.SysCache2` (NHibernate 핵심)
-   `Microsoft.AspNet.WebApi`, `Microsoft.AspNet.WebApi.Cors` (Web API 통합)
-   `Newtonsoft.Json` (JSON 직렬화)
-   `Moq` (단위 테스트 Mocking)
-   `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk` (단위 테스트 프레임워크)
-   `Swashbuckle` (Web API 문서화)
-   `BCrypt.Net-Next` (비밀번호 해싱 - 선택적 권장)
-   `System.Data.SQLite` (SQLite DB 드라이버 - 선택적)

### 데이터베이스

- **SQL Server 2019+** (프로덕션)
- **SQLite** (학습용)

### 개발 도구

- **Visual Studio 2022**
- **NuGet Package Manager**
- **SQL Server Management Studio** (선택)

## 👥 학습 대상

### 이런 분들에게 추천합니다

- ✅ .NET Framework 기본 지식이 있는 개발자
- ✅ Spring.NET 또는 NHibernate를 배우고 싶은 분
- ✅ 엔터프라이즈 아키텍처에 관심 있는 분
- ✅ 레거시 .NET 프로젝트를 유지보수하는 분
- ✅ Java Spring Framework 경험자 (.NET으로 전환)

### 선수 지식

- 📌 **필수**: C# 기본 문법, ASP.NET MVC 기초
- 📌 **권장**: SQL 기본, 객체지향 프로그래밍, 디자인 패턴

## 📖 학습 방법

### 권장 학습 순서

1. **문서 읽기** - 각 단계의 개념 이해
2. **코드 작성** - 예제를 직접 타이핑 (복사 금지!)
3. **실행 및 테스트** - 동작 확인
4. **연습 문제** - 각 단계의 연습 문제 해결
5. **복습** - 이해 안 되면 이전 단계 재학습

### 학습 팁

- 💡 **천천히 진행** - 하루 1-2개 단계씩 진행
- 💡 **코드 이해** - 왜 이렇게 작성했는지 생각하기
- 💡 **에러 해결** - 에러 메시지를 읽고 스스로 해결 시도
- 💡 **실습 중심** - 이론보다 실습에 더 많은 시간 투자

## 🎯 실전 프로젝트

### 1. 게시판 시스템 📝

**기능**: 게시글 CRUD, 댓글, 조회수, 페이징, 검색

**학습 내용**:
- Entity 관계 매핑 (One-to-Many)
- Repository 패턴
- Service Layer 분리
- MVC Controller 구현

### 2. 사용자 관리 시스템 👤

**기능**: 회원가입, 로그인, 권한 관리, 세션 관리

**학습 내용**:
- 비밀번호 암호화
- 인증/인가
- Custom Attribute
- 세션 관리

### 3. 쇼핑몰 시스템 🛒

**기능**: 상품 관리, 장바구니, 주문 처리, 재고 관리

**학습 내용**:
- 복잡한 트랜잭션
- 다중 엔티티 관계
- 비즈니스 로직 구현
- 주문 상태 관리

## 📊 학습 진도 체크

각 단계를 완료한 후 체크하세요:

- [ ] Phase 1: 기초 개념 (01-03) ⭐
- [ ] Phase 2: 게시판 시스템 (04-07) ⭐⭐
- [ ] Phase 3: 사용자 관리 (08-09) ⭐⭐⭐
- [ ] Phase 4: 쇼핑몰 시스템 (10-12) ⭐⭐⭐⭐
- [ ] Phase 5: 고급 주제 (13-14) ⭐⭐⭐⭐
- [ ] Phase 6: 실무 심화 (15-19) ⭐⭐⭐⭐⭐

## 🤝 기여하기

이 프로젝트는 오픈소스입니다. 기여를 환영합니다!

### 기여 방법

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### 기여 가이드라인

- 📝 오타 수정, 설명 보완
- 💡 새로운 예제 추가
- 🐛 버그 수정
- 📚 추가 튜토리얼 작성

## 📝 라이선스

이 프로젝트는 **MIT License**를 따릅니다. 자유롭게 사용, 수정, 배포할 수 있습니다.

## 🙏 감사의 말

- [Spring.NET](http://springframework.net/) - 공식 문서 및 커뮤니티
- [NHibernate](https://nhibernate.info/) - ORM 프레임워크
- 모든 기여자 및 학습자 여러분

## 📞 문의 및 지원

- **Issues**: [GitHub Issues](https://github.com/yourusername/SpringNet/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/SpringNet/discussions)

## 🌟 Star History

이 프로젝트가 도움이 되었다면 ⭐ Star를 눌러주세요!

---

<div align="center">

**Happy Learning! 📚**

Made with ❤️ for .NET Developers

[⬆ 맨 위로](#springnet--nhibernate-완벽-학습-가이드)

</div>