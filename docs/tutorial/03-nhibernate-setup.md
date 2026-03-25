# 03. NHibernate 설정 및 기본

## 📚 학습 목표

- NHibernate ORM의 핵심 개념 이해
- NHibernate 설정 및 연동
- 엔티티 매핑 (XML Mapping)
- SessionFactory 설정
- 기본 CRUD 작업 실습

## 🎯 NHibernate란?

**NHibernate**는 .NET을 위한 **Object-Relational Mapping (ORM)** 프레임워크입니다. Java의 Hibernate를 .NET으로 포팅한 것으로, 객체와 데이터베이스 테이블 간의 매핑을 자동화합니다.

### ORM의 장점

```
C# 객체 (Entity)  ←→  NHibernate  ←→  데이터베이스 테이블

Product.cs              Product 테이블
├── Id                  ├── Id (PK)
├── Name                ├── Name
├── Price               ├── Price
└── Category            └── CategoryId (FK)
```

**장점**:
- ✅ SQL 자동 생성 (CRUD)
- ✅ 객체 지향적 쿼리 (HQL, LINQ)
- ✅ 데이터베이스 독립성
- ✅ Lazy Loading, Caching 지원
- ✅ 트랜잭션 관리

## 💡 NHibernate 핵심 개념

### 1. SessionFactory

애플리케이션당 1개의 SessionFactory를 생성합니다 (무겁고 비용이 큼).

```csharp
// SessionFactory는 싱글톤으로 관리
ISessionFactory sessionFactory = BuildSessionFactory();
```

### 2. Session

데이터베이스 연결을 나타내며, CRUD 작업을 수행합니다.

```csharp
// 요청마다 새로운 Session 생성
using (ISession session = sessionFactory.OpenSession())
{
    // CRUD 작업
    var product = session.Get<Product>(1);
}
```

### 3. Transaction

데이터 변경 작업은 트랜잭션 내에서 수행합니다.

```csharp
using (ISession session = sessionFactory.OpenSession())
using (ITransaction tx = session.BeginTransaction())
{
    session.Save(product);
    tx.Commit(); // 커밋
}
```

### 4. 엔티티 (Entity)

데이터베이스 테이블에 매핑되는 C# 클래스입니다.

```csharp
public class Product
{
    public virtual int Id { get; set; }
    public virtual string Name { get; set; }
    public virtual decimal Price { get; set; }
}
```

## 🛠️ NHibernate 설정 실습

### Step 1: NuGet 패키지 설치

SpringNet.Data 프로젝트에 다음 패키지를 설치하세요:

```
PM> Install-Package NHibernate -Version 5.4.0
PM> Install-Package FluentNHibernate -Version 3.1.0 (선택사항)
PM> Install-Package System.Data.SQLite -Version 1.0.117 (SQLite 사용 시)
```

### Step 2: 데이터베이스 준비

#### SQL Server 사용 시

```sql
CREATE DATABASE SpringNetDB;
GO

USE SpringNetDB;
GO

CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(500),
    CreatedDate DATETIME DEFAULT GETDATE()
);
```

#### SQLite 사용 시 (학습용 권장)

`SpringNet.Data/Database/springnet.db` 파일 생성 (자동으로 생성됨)

### Step 3: 엔티티 클래스 생성

`SpringNet.Domain/Entities/Product.cs`:

```csharp
using System;

namespace SpringNet.Domain.Entities
{
    public class Product
    {
        // virtual: NHibernate Lazy Loading을 위해 필요
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual decimal Price { get; set; }
        public virtual string Description { get; set; }
        public virtual DateTime CreatedDate { get; set; }

        public Product()
        {
            CreatedDate = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Id}] {Name} - {Price:C}";
        }
    }
}
```

**중요**:
- 모든 프로퍼티는 `virtual`로 선언 (Lazy Loading 지원)
- 기본 생성자 필요

### Step 4: NHibernate 매핑 파일 생성

`SpringNet.Data/Mappings/Product.hbm.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="Product" table="Products">

        <!-- Primary Key -->
        <id name="Id" column="Id">
            <generator class="identity" />
        </id>

        <!-- Properties -->
        <property name="Name" column="Name" type="string"
                  length="100" not-null="true" />

        <property name="Price" column="Price" type="decimal"
                  not-null="true" />

        <property name="Description" column="Description" type="string"
                  length="500" />

        <property name="CreatedDate" column="CreatedDate" type="datetime"
                  not-null="true" />

    </class>

</hibernate-mapping>
```

**매핑 파일 설명**:
- `<class>`: 엔티티와 테이블 매핑
- `<id>`: 기본 키 설정
- `<generator>`: 자동 증가 전략
  - `identity`: SQL Server IDENTITY
  - `sequence`: Oracle SEQUENCE
  - `assigned`: 수동 할당
- `<property>`: 컬럼 매핑

**중요**: `Product.hbm.xml` 파일 속성 설정:
1. 솔루션 탐색기에서 파일 우클릭
2. 속성 → 빌드 작업 → **포함 리소스** 선택

### Step 5: SQLite NuGet 패키지 설치

이 튜토리얼은 학습용 DB로 **SQLite**를 사용합니다. NHibernate에서 SQLite를 사용하려면 드라이버 패키지를 별도로 설치해야 합니다.

Visual Studio의 **패키지 관리자 콘솔** (도구 → NuGet 패키지 관리자 → 패키지 관리자 콘솔)에서 아래 명령을 실행합니다. **기본 프로젝트**를 `SpringNet.Data`로 설정한 후 실행하세요.

```powershell
Install-Package System.Data.SQLite.Core -Version 1.0.118.0
```

설치 후 `SpringNet.Data/packages.config`에 아래 항목이 추가되었는지 확인합니다:

```xml
<package id="System.Data.SQLite.Core" version="1.0.118.0" targetFramework="net48" />
```

> **💡 SQL Server를 사용하는 경우**: SQLite 패키지 설치 없이 다음 Step의 `hibernate.cfg.xml`에서 SQLite 대신 SQL Server 설정을 사용하면 됩니다.

### Step 6: hibernate.cfg.xml 생성

`SpringNet.Data/hibernate.cfg.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-configuration xmlns="urn:nhibernate-configuration-2.2">
    <session-factory>

        <!-- Database Connection -->
        <!-- SQL Server -->
        <!--
        <property name="connection.provider">
            NHibernate.Connection.DriverConnectionProvider
        </property>
        <property name="connection.driver_class">
            NHibernate.Driver.SqlClientDriver
        </property>
        <property name="connection.connection_string">
            Server=localhost;Database=SpringNetDB;Integrated Security=true;
        </property>
        <property name="dialect">
            NHibernate.Dialect.MsSql2012Dialect
        </property>
        -->

        <!-- SQLite (학습용 권장) -->
        <property name="connection.provider">
            NHibernate.Connection.DriverConnectionProvider
        </property>
        <property name="connection.driver_class">
            NHibernate.Driver.SQLite20Driver
        </property>
        <property name="connection.connection_string">
            Data Source=|DataDirectory|\springnet.db;Version=3;
        </property>
        <property name="dialect">
            NHibernate.Dialect.SQLiteDialect
        </property>

        <!-- Settings -->
        <property name="show_sql">true</property>
        <property name="format_sql">true</property>
        <property name="hbm2ddl.auto">update</property>

        <!-- Mappings -->
        <!-- ⚠️ 중요: hbm.xml 파일은 SpringNet.Data 프로젝트에 EmbeddedResource로 등록됩니다.
             SpringNet.Domain이 아니라 SpringNet.Data를 지정해야 합니다. -->
        <mapping assembly="SpringNet.Data" />

    </session-factory>
</hibernate-configuration>
```

**`|DataDirectory|`란?**

`|DataDirectory|`는 ASP.NET 애플리케이션에서 `App_Data` 폴더의 실제 경로로 자동 변환되는 특수한 문자열입니다. 따라서 SQLite를 사용하면 `springnet.db` 파일은 `SpringNet.Web/App_Data/` 폴더에 생성됩니다. 웹 프로젝트가 실행될 때 `SpringNet.Data` 라이브러리가 이 설정을 사용하게 됩니다.

**설정 설명**:
- `show_sql`: SQL 쿼리 출력 (디버깅용)
- `format_sql`: SQL 포맷팅
- `hbm2ddl.auto`: 스키마 자동 생성
  - `create`: 시작 시 테이블 삭제 후 재생성
  - `update`: 변경사항만 반영
  - `validate`: 매핑 검증만
  - `create-drop`: 종료 시 테이블 삭제

### Step 7: SessionFactory 생성 클래스

`SpringNet.Data/NHibernateHelper.cs`:

```csharp
using NHibernate;
using NHibernate.Cfg;
using System;

namespace SpringNet.Data
{
    public class NHibernateHelper
    {
        private static ISessionFactory sessionFactory;
        private static readonly object lockObject = new object();

        public static ISessionFactory SessionFactory
        {
            get
            {
                if (sessionFactory == null)
                {
                    lock (lockObject)
                    {
                        if (sessionFactory == null)
                        {
                            sessionFactory = BuildSessionFactory();
                        }
                    }
                }
                return sessionFactory;
            }
        }

        private static ISessionFactory BuildSessionFactory()
        {
            try
            {
                var configuration = new Configuration();
                configuration.Configure(); // hibernate.cfg.xml 읽기

                return configuration.BuildSessionFactory();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SessionFactory 생성 실패: {ex}");
                throw;
            }
        }

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }
    }
}
```

## 🧪 기본 CRUD 작업

### Repository 패턴 기본

`SpringNet.Data/Repositories/IProductRepository.cs`:

```csharp
using SpringNet.Domain.Entities;
using System.Collections.Generic;

namespace SpringNet.Data.Repositories
{
    public interface IProductRepository
    {
        void Add(Product product);
        Product GetById(int id);
        IList<Product> GetAll();
        void Update(Product product);
        void Delete(int id);
    }
}
```

`SpringNet.Data/Repositories/ProductRepository.cs`:

```csharp
using NHibernate;
using SpringNet.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Data.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private ISessionFactory sessionFactory;

        // Spring이 주입
        public ProductRepository(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public void Add(Product product)
        {
            using (ISession session = sessionFactory.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                session.Save(product);
                tx.Commit();
            }
        }

        public Product GetById(int id)
        {
            using (ISession session = sessionFactory.OpenSession())
            {
                return session.Get<Product>(id);
            }
        }

        public IList<Product> GetAll()
        {
            using (ISession session = sessionFactory.OpenSession())
            {
                return session.Query<Product>().ToList();
            }
        }

        public void Update(Product product)
        {
            using (ISession session = sessionFactory.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                session.Update(product);
                tx.Commit();
            }
        }

        public void Delete(int id)
        {
            using (ISession session = sessionFactory.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                var product = session.Get<Product>(id);
                if (product != null)
                {
                    session.Delete(product);
                    tx.Commit();
                }
            }
        }
    }
}
```

### 📢 프로젝트 설정 중간 점검

지금까지 `Domain`, `Data` 프로젝트에 많은 파일을 추가했습니다. 애플리케이션이 올바르게 동작하려면, 각 프로젝트 파일(`.csproj`)을 수정하여 새 파일들을 포함하고, 일부 파일의 속성을 설정해야 합니다.

#### 1. 폴더 및 파일 정리

- **폴더 생성**:
  - `SpringNet.Domain` 프로젝트에 `Entities` 폴더를 만듭니다.
  - `SpringNet.Data` 프로젝트에 `Mappings`와 `Repositories` 폴더를 만듭니다.
- **파일 이동**:
  - `Product.cs` -> `SpringNet.Domain/Entities/`
  - `Product.hbm.xml` -> `SpringNet.Data/Mappings/`
  - `IProductRepository.cs`, `ProductRepository.cs` -> `SpringNet.Data/Repositories/`
- **파일 삭제**: `SpringNet.Domain/Class1.cs`와 `SpringNet.Data/Class1.cs`는 이제 필요 없으니 삭제합니다.

#### 2. `SpringNet.Domain.csproj` 업데이트

`ItemGroup`에 `Product.cs`를 추가합니다.

```xml
<ItemGroup>
  <Compile Include="Entities\Product.cs" />
  <Compile Include="Properties\AssemblyInfo.cs" />
</ItemGroup>
```

#### 3. `SpringNet.Data.csproj` 업데이트

`NHibernateHelper`, Repository 파일, 매핑 XML, 설정 파일을 모두 포함하도록 수정합니다.

```xml
  <!-- ... ItemGroup with references ... -->
  <ItemGroup>
    <Compile Include="NHibernateHelper.cs" />
    <Compile Include="Properties\AssemblyInfo.cs" />
    <Compile Include="Repositories\IProductRepository.cs" />
    <Compile Include="Repositories\ProductRepository.cs" />
  </ItemGroup>
  <ItemGroup>
    <None Include="app.config" />
    <None Include="hibernate.cfg.xml">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="packages.config" />
  </ItemGroup>
  <ItemGroup>
    <EmbeddedResource Include="Mappings\Product.hbm.xml" />
  </ItemGroup>
  <!-- ... project references ... -->
```

**중요 파일 속성 요약**:
-   **`Product.hbm.xml`**: `Build Action` -> `Embedded Resource`
-   **`hibernate.cfg.xml`**: `Copy to Output Directory` -> `Copy if newer`

이 설정들은 Visual Studio의 파일 속성 창에서도 직접 변경할 수 있습니다.

---

### Spring.NET 연동

이제 NHibernate의 `SessionFactory`와 우리가 만든 `ProductRepository`를 Spring.NET 컨테이너에 Bean으로 등록할 차례입니다. 이렇게 하면 Spring이 객체의 생명주기를 관리하고, 필요한 곳에 의존성을 주입해 줄 수 있습니다.

`Config/applicationContext.xml` 파일을 열어 다음 Bean들을 추가합니다.

```xml
    <!-- ... 이전 튜토리얼에서 추가한 Bean들 ... -->

    <!-- === NHibernate 설정 추가 === -->

    <!-- SessionFactory Bean -->
    <object id="sessionFactory"
            type="SpringNet.Data.NHibernateHelper, SpringNet.Data"
            factory-method="SessionFactory"
            singleton="true" />

    <!-- Product Repository Bean -->
    <object id="productRepository"
            type="SpringNet.Data.Repositories.ProductRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```

**설명**:
1.  `sessionFactory`: `NHibernateHelper` 클래스의 `SessionFactory` 정적 프로퍼티를 사용하여 `ISessionFactory` 인스턴스를 생성합니다. `factory-method`를 사용하고, `singleton="true"`로 설정하여 애플리케이션 전체에서 단 하나의 인스턴스만 사용하도록 합니다.
2.  `productRepository`: 생성자 주입을 통해 `sessionFactory` Bean을 참조(`ref`)합니다.

## 🔍 HQL (Hibernate Query Language)

HQL은 객체 지향 쿼리 언어입니다.

### 기본 쿼리

```csharp
// 모든 상품 조회
var products = session.CreateQuery("from Product").List<Product>();

// 조건 조회
var products = session.CreateQuery("from Product p where p.Price > :price")
                     .SetParameter("price", 1000)
                     .List<Product>();

// 정렬
var products = session.CreateQuery("from Product p order by p.Price desc")
                     .List<Product>();

// 페이징
var products = session.CreateQuery("from Product")
                     .SetFirstResult(0)
                     .SetMaxResults(10)
                     .List<Product>();
```

### LINQ to NHibernate

```csharp
using System.Linq;

// LINQ 사용
var products = session.Query<Product>()
                     .Where(p => p.Price > 1000)
                     .OrderBy(p => p.Name)
                     .ToList();

// 복잡한 쿼리
var expensiveProducts = session.Query<Product>()
                              .Where(p => p.Price > 5000)
                              .Select(p => new { p.Name, p.Price })
                              .ToList();
```

## 🎯 실전 예제: 상품 관리

### Service Layer

`SpringNet.Service/IProductService.cs`:

```csharp
using SpringNet.Domain.Entities;
using System.Collections.Generic;

namespace SpringNet.Service
{
    public interface IProductService
    {
        void CreateProduct(string name, decimal price, string description);
        Product GetProduct(int id);
        IList<Product> GetAllProducts();
        IList<Product> SearchProducts(string keyword);
        void UpdateProduct(int id, string name, decimal price, string description);
        void DeleteProduct(int id);
    }
}
```

`SpringNet.Service/ProductService.cs`:

```csharp
using SpringNet.Data.Repositories;
using SpringNet.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository repository;

        public ProductService(IProductRepository repository)
        {
            this.repository = repository;
        }

        public void CreateProduct(string name, decimal price, string description)
        {
            var product = new Product
            {
                Name = name,
                Price = price,
                Description = description
            };

            repository.Add(product);
        }

        public Product GetProduct(int id)
        {
            return repository.GetById(id);
        }

        public IList<Product> GetAllProducts()
        {
            return repository.GetAll();
        }

        public IList<Product> SearchProducts(string keyword)
        {
            var allProducts = repository.GetAll();
            return allProducts
                .Where(p => p.Name.Contains(keyword))
                .ToList();
        }

        public void UpdateProduct(int id, string name, decimal price, string description)
        {
            var product = repository.GetById(id);
            if (product != null)
            {
                product.Name = name;
                product.Price = price;
                product.Description = description;
                repository.Update(product);
            }
        }

        public void DeleteProduct(int id)
        {
            repository.Delete(id);
        }
    }
}
```

#### 📢 프로젝트 파일 업데이트
`SpringNet.Service.csproj` 파일에 `IProductService.cs`와 `ProductService.cs`를 추가합니다.

```xml
<ItemGroup>
  <Compile Include="GreetingService.cs" />
  <Compile Include="IGreetingService.cs" />
  <Compile Include="Logging\CompositeLogger.cs" />
  <Compile Include="Logging\ConsoleLogger.cs" />
  <Compile Include="Logging\FileLogger.cs" />
  <Compile Include="Logging\ILogger.cs" />
  <Compile Include="IProductService.cs" />
  <Compile Include="ProductService.cs" />
  <Compile Include="Properties\AssemblyInfo.cs" />
</ItemGroup>
```

### applicationContext.xml 설정

`applicationContext.xml` 파일의 `<objects>` 태그 내부에, 앞서 정의한 `productRepository`에 이어 `productService` Bean을 추가합니다.

```xml
<!-- Product Service -->
<object id="productService"
        type="SpringNet.Service.ProductService, SpringNet.Service">
    <constructor-arg ref="productRepository" />
</object>
```

## 💡 NHibernate 주요 기능

### 1. Lazy Loading

```csharp
public class Category
{
    public virtual int Id { get; set; }
    public virtual string Name { get; set; }

    // Lazy: 실제 사용할 때 로딩
    public virtual IList<Product> Products { get; set; }
}
```

```xml
<class name="Category" table="Categories">
    <id name="Id"><generator class="identity" /></id>
    <property name="Name" />

    <!-- lazy="true": 기본값, 실제 사용 시 로딩 -->
    <bag name="Products" lazy="true">
        <key column="CategoryId" />
        <one-to-many class="Product" />
    </bag>
</class>
```

### 2. Caching

```xml
<property name="cache.use_second_level_cache">true</property>
<property name="cache.provider_class">
    NHibernate.Cache.HashtableCacheProvider
</property>
```

```xml
<class name="Product" table="Products">
    <cache usage="read-write" />
    <!-- ... -->
</class>
```

### 3. Batch Fetching

```xml
<property name="adonet.batch_size">20</property>
```

## 🛠️ 트러블슈팅

### 문제 1: 매핑 파일을 찾을 수 없음

**에러**: `Could not compile the mapping document`

**해결**:
1. `.hbm.xml` 파일 속성 → **포함 리소스** 확인
2. `hibernate.cfg.xml`에서 `<mapping assembly="정확한어셈블리명" />` 확인

### 문제 2: 테이블이 생성되지 않음

**해결**:
1. `hbm2ddl.auto`를 `create` 또는 `update`로 설정
2. 연결 문자열 확인
3. 데이터베이스 권한 확인

### 문제 3: LazyInitializationException

**에러**: `no Session or Session was closed`

**해결**:
1. Session 범위 내에서 모든 데이터 로딩
2. Eager Loading 사용: `lazy="false"`
3. Open Session in View 패턴 사용

## 💡 핵심 정리

### NHibernate 핵심 개념

- **SessionFactory**: 싱글톤, 애플리케이션당 1개
- **Session**: 요청당 1개, using으로 관리
- **Transaction**: 변경 작업 시 필수
- **Entity**: virtual 프로퍼티, 기본 생성자

### 매핑 파일

```xml
<hibernate-mapping>
    <class name="엔티티" table="테이블">
        <id name="Id">
            <generator class="identity|sequence|assigned" />
        </id>
        <property name="프로퍼티" column="컬럼" />
    </class>
</hibernate-mapping>
```

### CRUD 패턴

```csharp
using (ISession session = sessionFactory.OpenSession())
using (ITransaction tx = session.BeginTransaction())
{
    session.Save(entity);    // INSERT
    session.Update(entity);  // UPDATE
    session.Delete(entity);  // DELETE
    tx.Commit();
}

// SELECT는 트랜잭션 불필요
var entity = session.Get<Entity>(id);
```

## 🎯 연습 문제

### 문제 1: Category 엔티티 추가

1. `Category` 엔티티 생성 (Id, Name)
2. 매핑 파일 작성
3. Repository 및 Service 구현

### 문제 2: Product-Category 관계

1. Product에 CategoryId 추가
2. Many-to-One 관계 매핑
3. 카테고리별 상품 조회 기능

### 문제 3: 검색 기능 고도화

1. HQL을 사용한 검색
2. LINQ to NHibernate 검색
3. 가격 범위 검색

## ⚙️ 다음 튜토리얼 전 필수 설정: Web.config 업데이트

Tutorial 04부터는 Repository와 Service가 `sessionFactory.GetCurrentSession()`을 사용합니다. 이 방식은 **현재 HTTP 요청에 바인딩된 세션**을 재사용하는 패턴으로, 여러 Repository가 같은 트랜잭션을 공유할 수 있게 해줍니다.

`GetCurrentSession()`이 작동하려면 `Web.config`에서 **OpenSessionInViewModule**을 활성화해야 합니다.

`SpringNet.Web/Web.config`의 `<system.webServer>` 섹션을 찾아 주석을 해제합니다:

```xml
<system.webServer>
    <!-- OpenSessionInViewModule: HTTP 요청 시작 시 NHibernate 세션을 열고,
         요청 종료 시 닫아줍니다. GetCurrentSession() 사용에 필수입니다. -->
    <modules runAllManagedModulesForAllRequests="true">
        <add name="Spring"
             type="Spring.Web.Support.OpenSessionInViewModule, Spring.Web" />
    </modules>
    <handlers>
        <!-- ... 기존 handlers ... -->
    </handlers>
</system.webServer>
```

또한 `hibernate.cfg.xml`에 세션 컨텍스트 설정을 추가합니다:

```xml
<!-- GetCurrentSession()을 위한 웹 세션 컨텍스트 설정 -->
<property name="current_session_context_class">
    Spring.Data.NHibernate.Web.WebSessionContext, Spring.Data.NHibernate5
</property>
```

### OpenSession() vs GetCurrentSession() 비교

| 항목 | `OpenSession()` | `GetCurrentSession()` |
|------|----------------|----------------------|
| 세션 생성 | 매번 새 세션 생성 | 현재 컨텍스트의 세션 재사용 |
| 트랜잭션 공유 | 불가능 (각 Repository마다 별도 세션) | 가능 (여러 Repository가 공유) |
| 사용 패턴 | `using (var session = ...)` | 외부에서 세션 열고 닫음 |
| 적합한 경우 | 단순 조회, 독립 작업 | Service 계층의 복잡한 트랜잭션 |
| 이 튜토리얼 사용 여부 | Tutorial 03 ProductRepository ✅ | Tutorial 04+ 모든 Repository ✅ |

> **📌 이 튜토리얼(03)**의 `ProductRepository`는 학습 편의를 위해 `OpenSession()`을 사용합니다. **Tutorial 04부터는** `GetCurrentSession()` 패턴으로 전환됩니다. Tutorial 04 진행 전에 반드시 위 `Web.config` 설정을 완료하세요.

## 🚀 다음 단계

NHibernate 설정 완료!

다음 단계: **[04-board-part1-domain.md](./04-board-part1-domain.md)**에서 게시판 프로젝트를 시작합니다.

---

**NHibernate는 강력하지만 설정이 중요합니다. 매핑 파일을 정확히 작성하세요!**
