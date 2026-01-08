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

## 📝 Board Repository 구현

### Step 3: IBoardRepository 인터페이스

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

### Step 4: BoardRepository 구현

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

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net">

    <!-- SessionFactory -->
    <object id="sessionFactory"
            type="SpringNet.Data.NHibernateHelper, SpringNet.Data"
            factory-method="SessionFactory">
    </object>

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
