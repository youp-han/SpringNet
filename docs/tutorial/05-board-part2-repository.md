# 05. 게시판 Part 2: Repository 패턴

## 📚 학습 목표

- Repository 패턴의 개념 및 장점
- Generic Repository 구현
- Board Repository 구현
- LINQ to NHibernate 쿼리
- 페이징 및 검색 기능

## 🎯 Repository 패턴이란?

**Repository**는 데이터 액세스 로직을 캡슐화하는 패턴입니다.

```
Controller → Service → Repository → Database
```

**장점**:
- ✅ 데이터 액세스 로직 중앙화
- ✅ 비즈니스 로직과 분리
- ✅ 테스트 용이 (Mock 가능)
- ✅ 쿼리 재사용

## 🛠️ Generic Repository 구현

본격적으로 Repository를 구현하기 전에, NHibernate의 중요한 개념인 **"현재 세션 컨텍스트(Current Session Context)"**에 대해 알아봐야 합니다.

### `GetCurrentSession()`이란?

NHibernate는 `sessionFactory.OpenSession()`을 통해 매번 새로운 세션을 열 수도 있지만, `sessionFactory.GetCurrentSession()`을 사용하여 **컨텍스트에 바인딩된 현재 세션**을 가져올 수도 있습니다. 웹 요청과 같은 특정 범위 내에서 단 하나의 세션만 사용하도록 보장해주는 매우 편리하고 효율적인 방법입니다.

이 튜토리얼의 Repository 구현은 `GetCurrentSession()`을 사용할 것입니다. 하지만 이 기능을 사용하려면 `hibernate.cfg.xml`에 어떤 컨텍스트를 사용할지 명시해야 합니다.

#### `hibernate.cfg.xml` 업데이트

`SpringNet.Data/hibernate.cfg.xml` 파일을 열고 `<session-factory>` 내부에 다음 속성을 추가 또는 수정합니다.

```xml
        <!-- ... 다른 property들 ... -->

        <!-- Spring.NET 선언적 트랜잭션 관리를 위한 세션 컨텍스트 설정 -->
        <property name="current_session_context_class">
            Spring.Data.NHibernate.SpringSessionContext, Spring.Data.NHibernate
        </property>

        <!-- Mappings -->
        <mapping assembly="SpringNet.Domain" />
```
`Spring.Data.NHibernate.SpringSessionContext` 값은 Spring.NET이 NHibernate의 `GetCurrentSession()`을 관리하도록 지시하며, 이는 선언적 트랜잭션을 사용할 때 권장되는 설정입니다.

### Step 1: IRepository 인터페이스

`SpringNet.Data/Repositories/IRepository.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SpringNet.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        // 기본 CRUD
        T GetById(int id);
        IList<T> GetAll();
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        void Delete(int id);

        // 조건 조회
        IList<T> Find(Expression<Func<T, bool>> predicate);
        T FindOne(Expression<Func<T, bool>> predicate);

        // 페이징
        IList<T> GetPaged(int pageNumber, int pageSize);
        IList<T> GetPaged(int pageNumber, int pageSize,
                          Expression<Func<T, bool>> predicate);

        // 카운트
        int Count();
        int Count(Expression<Func<T, bool>> predicate);
    }
}
```

### Step 2: Repository 구현

`SpringNet.Data/Repositories/Repository.cs`:

```csharp
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SpringNet.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly ISessionFactory sessionFactory;

        public Repository(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        protected ISession CurrentSession
        {
            get { return sessionFactory.GetCurrentSession(); }
        }

        public virtual T GetById(int id)
        {
            return CurrentSession.Get<T>(id);
        }

        public virtual IList<T> GetAll()
        {
            return CurrentSession.Query<T>().ToList();
        }

        public virtual void Add(T entity)
        {
            CurrentSession.Save(entity);
        }

        public virtual void Update(T entity)
        {
            CurrentSession.Update(entity);
        }

        public virtual void Delete(T entity)
        {
            CurrentSession.Delete(entity);
        }

        public virtual void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                Delete(entity);
            }
        }

        public virtual IList<T> Find(Expression<Func<T, bool>> predicate)
        {
            return CurrentSession.Query<T>().Where(predicate).ToList();
        }

        public virtual T FindOne(Expression<Func<T, bool>> predicate)
        {
            return CurrentSession.Query<T>().FirstOrDefault(predicate);
        }

        public virtual IList<T> GetPaged(int pageNumber, int pageSize)
        {
            return CurrentSession.Query<T>()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public virtual IList<T> GetPaged(int pageNumber, int pageSize,
                                        Expression<Func<T, bool>> predicate)
        {
            return CurrentSession.Query<T>()
                .Where(predicate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public virtual int Count()
        {
            return CurrentSession.Query<T>().Count();
        }

        public virtual int Count(Expression<Func<T, bool>> predicate)
        {
            return CurrentSession.Query<T>().Count(predicate);
        }
    }
}
```

## ♻️ 기존 Repository 리팩토링

이전 `03-nhibernate-setup.md` 튜토리얼에서 만들었던 `IProductRepository`와 `ProductRepository`는 기본적인 CRUD 기능만 가지고 있었습니다. 이제 우리는 모든 엔티티에 공통적으로 사용할 수 있는 제네릭 `IRepository<T>`와 `Repository<T>`를 만들었으므로, 기존 `ProductRepository`도 이 제네릭 구현을 사용하도록 리팩토링해야 합니다. 이렇게 하면 코드 중복을 제거하고 유지보수성을 높일 수 있습니다.

### Step 3: IProductRepository 리팩토링

`IProductRepository`가 제네릭 `IRepository<Product>`를 상속받도록 수정합니다. 이제 `IProductRepository`에는 `Product` 엔티티에만 특화된 메서드 시그니처만 남기면 됩니다. (현재는 추가할 메서드가 없습니다.)

`SpringNet.Data/Repositories/IProductRepository.cs`:

```csharp
using SpringNet.Domain.Entities;
using System.Collections.Generic;

namespace SpringNet.Data.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        // Product와 관련된 특별한 기능이 필요하다면 여기에 추가합니다.
        // 예: IList<Product> GetFeaturedProducts();
    }
}
```

### Step 4: ProductRepository 리팩토링

`ProductRepository`가 제네릭 `Repository<Product>`를 상속받도록 수정합니다. 생성자에서 `base(sessionFactory)`를 호출하여 부모 클래스의 생성자를 실행해주는 것 외에는 모든 코드가 사라집니다. `GetById`, `Add`, `Delete` 등의 모든 기본 CRUD 메서드는 이제 부모인 `Repository<T>` 클래스에서 제공합니다.

`SpringNet.Data/Repositories/ProductRepository.cs`:

```csharp
using NHibernate;
using SpringNet.Domain.Entities;

namespace SpringNet.Data.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(ISessionFactory sessionFactory)
            : base(sessionFactory)
        {
            // 모든 기본 CRUD 기능은 제네릭 Repository<T>에 의해 처리됩니다.
            // Product와 관련된 특별한 구현이 필요하다면 여기에 추가합니다.
        }
    }
}
```

이제 `ProductRepository`는 매우 간결해졌으며, 모든 공통 데이터 액세스 로직은 `Repository<T>`에 위임되었습니다.

## 📝 Board Repository 구현

### Step 5: IBoardRepository 인터페이스

`SpringNet.Data/Repositories/IBoardRepository.cs`:

```csharp
using SpringNet.Domain.Entities;
using System.Collections.Generic;

namespace SpringNet.Data.Repositories
{
    public interface IBoardRepository : IRepository<Board>
    {
        // 게시판 전용 메서드
        IList<Board> GetByAuthor(string author);
        IList<Board> SearchByTitle(string keyword);
        IList<Board> SearchByContent(string keyword);
        IList<Board> GetRecent(int count);
        IList<Board> GetPopular(int count);
        Board GetWithReplies(int id);
        int GetTotalPages(int pageSize);
    }
}
```


### Step 6: BoardRepository 구현

`SpringNet.Data/Repositories/BoardRepository.cs`:

```csharp
using NHibernate;
using NHibernate.Linq;
using SpringNet.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Data.Repositories
{
    public class BoardRepository : Repository<Board>, IBoardRepository
    {
        public BoardRepository(ISessionFactory sessionFactory)
            : base(sessionFactory)
        {
        }

        public IList<Board> GetByAuthor(string author)
        {
            return CurrentSession.Query<Board>()
                .Where(b => b.Author == author)
                .OrderByDescending(b => b.CreatedDate)
                .ToList();
        }

        public IList<Board> SearchByTitle(string keyword)
        {
            return CurrentSession.Query<Board>()
                .Where(b => b.Title.Contains(keyword))
                .OrderByDescending(b => b.CreatedDate)
                .ToList();
        }

        public IList<Board> SearchByContent(string keyword)
        {
            return CurrentSession.Query<Board>()
                .Where(b => b.Content.Contains(keyword) ||
                           b.Title.Contains(keyword))
                .OrderByDescending(b => b.CreatedDate)
                .ToList();
        }

        public IList<Board> GetRecent(int count)
        {
            return CurrentSession.Query<Board>()
                .OrderByDescending(b => b.CreatedDate)
                .Take(count)
                .ToList();
        }

        public IList<Board> GetPopular(int count)
        {
            return CurrentSession.Query<Board>()
                .OrderByDescending(b => b.ViewCount)
                .Take(count)
                .ToList();
        }

        public Board GetWithReplies(int id)
        {
            // Eager Loading: 댓글도 함께 로딩
            return CurrentSession.Query<Board>()
                .Fetch(b => b.Replies)
                .FirstOrDefault(b => b.Id == id);
        }

        public int GetTotalPages(int pageSize)
        {
            var totalCount = Count();
            return (int)Math.Ceiling((double)totalCount / pageSize);
        }
    }
}
```

## 🔍 HQL 및 LINQ 쿼리

### HQL (Hibernate Query Language)

```csharp
// HQL 기본 쿼리
var boards = CurrentSession.CreateQuery("from Board b where b.ViewCount > 100")
    .List<Board>();

// 파라미터 바인딩
var boards = CurrentSession.CreateQuery(
    "from Board b where b.Author = :author")
    .SetParameter("author", "홍길동")
    .List<Board>();

// Join
var boards = CurrentSession.CreateQuery(@"
    from Board b
    left join fetch b.Replies
    where b.ViewCount > :minCount")
    .SetParameter("minCount", 50)
    .List<Board>();
```

### LINQ to NHibernate (권장)

```csharp
// 기본 조회
var boards = CurrentSession.Query<Board>()
    .Where(b => b.ViewCount > 100)
    .ToList();

// 복잡한 조건
var boards = CurrentSession.Query<Board>()
    .Where(b => b.Author == "홍길동" && b.ViewCount > 50)
    .OrderByDescending(b => b.CreatedDate)
    .ToList();

// Projection (일부 필드만)
var titles = CurrentSession.Query<Board>()
    .Select(b => new { b.Title, b.Author })
    .ToList();

// Join (Eager Loading)
var boards = CurrentSession.Query<Board>()
    .Fetch(b => b.Replies)
    .ToList();
```

## 📦 Reply Repository

### IReplyRepository

`SpringNet.Data/Repositories/IReplyRepository.cs`:

```csharp
using SpringNet.Domain.Entities;
using System.Collections.Generic;

namespace SpringNet.Data.Repositories
{
    public interface IReplyRepository : IRepository<Reply>
    {
        IList<Reply> GetByBoardId(int boardId);
        IList<Reply> GetByAuthor(string author);
        int GetCountByBoardId(int boardId);
    }
}
```

### ReplyRepository

`SpringNet.Data/Repositories/ReplyRepository.cs`:

```csharp
using NHibernate;
using SpringNet.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Data.Repositories
{
    public class ReplyRepository : Repository<Reply>, IReplyRepository
    {
        public ReplyRepository(ISessionFactory sessionFactory)
            : base(sessionFactory)
        {
        }

        public IList<Reply> GetByBoardId(int boardId)
        {
            return CurrentSession.Query<Reply>()
                .Where(r => r.Board.Id == boardId)
                .OrderBy(r => r.CreatedDate)
                .ToList();
        }

        public IList<Reply> GetByAuthor(string author)
        {
            return CurrentSession.Query<Reply>()
                .Where(r => r.Author == author)
                .OrderByDescending(r => r.CreatedDate)
                .ToList();
        }

        public int GetCountByBoardId(int boardId)
        {
            return CurrentSession.Query<Reply>()
                .Count(r => r.Board.Id == boardId);
        }
    }
}
```

### 📢 프로젝트 파일 업데이트

이제 `SpringNet.Data.csproj` 파일을 업데이트하여 이번 튜토리얼에서 추가 및 수정한 파일들을 모두 포함시켜야 합니다. 이전 튜토리얼에서 추가했던 `IProductRepository`와 `ProductRepository`는 이번에 **수정**되었고, 나머지 파일들은 **새로 추가**되었습니다.

`SpringNet.Data.csproj` 파일의 `<Compile>` 아이템 그룹이 다음과 같이 구성되도록 업데이트하세요.

```xml
<ItemGroup>
  <Compile Include="NHibernateHelper.cs" />
  <Compile Include="Properties\AssemblyInfo.cs" />
  
  <!-- 이번 튜토리얼에서 새로 추가한 파일들 -->
  <Compile Include="Repositories\BoardRepository.cs" />
  <Compile Include="Repositories\IBoardRepository.cs" />
  <Compile Include="Repositories\IReplyRepository.cs" />
  <Compile Include="Repositories\IRepository.cs" />
  <Compile Include="Repositories\ReplyRepository.cs" />
  <Compile Include="Repositories\Repository.cs" />

  <!-- 03번 튜토리얼에서 추가했고 이번에 수정한 파일들 -->
  <Compile Include="Repositories\IProductRepository.cs" />
  <Compile Include="Repositories\ProductRepository.cs" />
</ItemGroup>
```

**참고**: 프로젝트 파일(`.csproj`) 내의 `<Compile>` 아이템 순서는 중요하지 않습니다. Visual Studio는 빌드 시 모든 파일을 올바르게 컴파일합니다. 이 목록은 프로젝트에 어떤 파일이 포함되어 있는지 관리하는 역할을 합니다.

## 🧪 Repository 테스트

### 통합 테스트 예제

```csharp
using NUnit.Framework;
using SpringNet.Data.Repositories;
using SpringNet.Domain.Entities;
using System;

namespace SpringNet.Tests.RepositoryTests
{
    [TestFixture]
    public class BoardRepositoryTests
    {
        private IBoardRepository repository;

        [SetUp]
        public void Setup()
        {
            // SessionFactory 초기화
            var sessionFactory = NHibernateHelper.SessionFactory;
            repository = new BoardRepository(sessionFactory);
        }

        [Test]
        public void Add_Board_IncreasesCount()
        {
            // Arrange
            var initialCount = repository.Count();
            var board = new Board
            {
                Title = "테스트 게시글",
                Content = "테스트 내용",
                Author = "테스터"
            };

            // Act
            repository.Add(board);
            var newCount = repository.Count();

            // Assert
            Assert.AreEqual(initialCount + 1, newCount);
        }

        [Test]
        public void GetByAuthor_ReturnsCorrectBoards()
        {
            // Arrange
            var author = "홍길동";

            // Act
            var boards = repository.GetByAuthor(author);

            // Assert
            Assert.IsNotNull(boards);
            foreach (var board in boards)
            {
                Assert.AreEqual(author, board.Author);
            }
        }
    }
}
```

## 💡 Spring.NET 연동

### applicationContext.xml 설정
이제 새로 만든 `BoardRepository`와 `ReplyRepository`를 Spring 컨테이너가 관리하도록 `applicationContext.xml`에 Bean으로 등록합니다. 파일에 다음 내용을 추가하세요.

```xml
    <!-- Board Repository -->
    <object id="boardRepository"
            type="SpringNet.Data.Repositories.BoardRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <!-- Reply Repository -->
    <object id="replyRepository"
            type="SpringNet.Data.Repositories.ReplyRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```
**참고**: `productRepository`와 동일한 방식으로, 생성자 주입을 통해 `sessionFactory`를 참조하여 각 Repository를 설정합니다.

## 🎯 연습 문제

### 문제 1: 고급 검색

다음 메서드를 `BoardRepository`에 추가:

```csharp
IList<Board> AdvancedSearch(
    string keyword,
    string author,
    DateTime? startDate,
    DateTime? endDate,
    int minViewCount
);
```

### 문제 2: 통계 메서드

다음 메서드 구현:

```csharp
int GetTotalViewCount();
Dictionary<string, int> GetPostCountByAuthor();
IList<Board> GetBoardsWithManyReplies(int minReplyCount);
```

## 💡 핵심 정리

### Repository 패턴 장점

✅ 데이터 액세스 중앙화
✅ 비즈니스 로직과 분리
✅ 쿼리 재사용
✅ 테스트 용이

### LINQ to NHibernate

```csharp
// 기본 쿼리
CurrentSession.Query<T>().Where(...).ToList();

// Eager Loading
CurrentSession.Query<T>().Fetch(x => x.Child).ToList();

// 페이징
CurrentSession.Query<T>().Skip(n).Take(m).ToList();
```

## 🚀 다음 단계

다음: **[06-board-part3-service.md](./06-board-part3-service.md)** - Service Layer 구현
