# 15. NHibernate 고급 쿼리

## 📚 학습 목표

- HQL (Hibernate Query Language) 마스터
- LINQ to NHibernate 고급 사용법
- Criteria API 활용
- Native SQL 실행
- QueryOver API
- 성능 최적화 쿼리

## 🎯 NHibernate 쿼리 방법 5가지

```
1. HQL          - 객체 지향 쿼리 언어
2. LINQ         - C# LINQ 표현식 (권장)
3. Criteria API - 동적 쿼리 생성
4. QueryOver    - Type-safe Criteria API
5. Native SQL   - 원본 SQL 직접 실행
```

## 📝 1. HQL (Hibernate Query Language)

### 기본 조회

```csharp
// 전체 조회
var boards = session.CreateQuery("from Board").List<Board>();

// 조건 조회
var boards = session.CreateQuery("from Board b where b.ViewCount > 100")
    .List<Board>();

// 파라미터 바인딩
var boards = session.CreateQuery(
    "from Board b where b.Author = :author and b.ViewCount > :minViews")
    .SetParameter("author", "홍길동")
    .SetParameter("minViews", 50)
    .List<Board>();

// Named 파라미터
var boards = session.CreateQuery(
    "from Board b where b.CreatedDate between :startDate and :endDate")
    .SetDateTime("startDate", startDate)
    .SetDateTime("endDate", endDate)
    .List<Board>();
```

### JOIN 쿼리

```csharp
// Inner Join
var query = @"
    select b from Board b
    inner join b.Replies r
    where r.Author = :author";

var boards = session.CreateQuery(query)
    .SetParameter("author", "홍길동")
    .List<Board>();

// Left Join Fetch (Eager Loading)
var query = @"
    from Board b
    left join fetch b.Replies
    where b.ViewCount > :minViews";

var boards = session.CreateQuery(query)
    .SetParameter("minViews", 100)
    .List<Board>();

// Multiple Joins
var query = @"
    from Board b
    left join fetch b.Replies r
    left join fetch b.Category c
    where c.Name = :categoryName";
```

### 집계 함수

```csharp
// COUNT
var count = session.CreateQuery("select count(*) from Board")
    .UniqueResult<long>();

// SUM
var totalViews = session.CreateQuery(
    "select sum(b.ViewCount) from Board b")
    .UniqueResult<long>();

// AVG
var avgViews = session.CreateQuery(
    "select avg(b.ViewCount) from Board b where b.Author = :author")
    .SetParameter("author", "홍길동")
    .UniqueResult<double>();

// GROUP BY
var query = @"
    select b.Author, count(b), sum(b.ViewCount)
    from Board b
    group by b.Author
    having count(b) > 5";

var results = session.CreateQuery(query).List<object[]>();
foreach (var result in results)
{
    var author = result[0];
    var postCount = result[1];
    var totalViews = result[2];
}
```

### 정렬 및 페이징

```csharp
// 정렬
var boards = session.CreateQuery("from Board b order by b.CreatedDate desc")
    .List<Board>();

// 여러 컬럼 정렬
var boards = session.CreateQuery(
    "from Board b order by b.ViewCount desc, b.CreatedDate desc")
    .List<Board>();

// 페이징
var boards = session.CreateQuery("from Board b order by b.CreatedDate desc")
    .SetFirstResult(0)      // OFFSET
    .SetMaxResults(10)      // LIMIT
    .List<Board>();
```

### 서브쿼리

```csharp
// 서브쿼리 - 댓글이 많은 게시글
var query = @"
    from Board b
    where (select count(r) from Reply r where r.Board = b) > :minReplies";

var boards = session.CreateQuery(query)
    .SetParameter("minReplies", 5)
    .List<Board>();

// IN 서브쿼리
var query = @"
    from Board b
    where b.Author in (
        select u.Username from User u where u.Role = 'Admin'
    )";
```

### UPDATE/DELETE

```csharp
// 벌크 UPDATE
var updated = session.CreateQuery(
    "update Board b set b.ViewCount = b.ViewCount + 1 where b.Id = :id")
    .SetParameter("id", boardId)
    .ExecuteUpdate();

// 벌크 DELETE
var deleted = session.CreateQuery(
    "delete from Reply r where r.CreatedDate < :date")
    .SetDateTime("date", cutoffDate)
    .ExecuteUpdate();
```

## 🔍 2. LINQ to NHibernate (권장)

### 기본 쿼리

```csharp
using NHibernate.Linq;

// 전체 조회
var boards = session.Query<Board>().ToList();

// 조건 조회
var boards = session.Query<Board>()
    .Where(b => b.ViewCount > 100)
    .ToList();

// 복잡한 조건
var boards = session.Query<Board>()
    .Where(b => b.Author == "홍길동" && b.ViewCount > 50)
    .OrderByDescending(b => b.CreatedDate)
    .ToList();

// 문자열 검색
var boards = session.Query<Board>()
    .Where(b => b.Title.Contains(keyword) || b.Content.Contains(keyword))
    .ToList();
```

### JOIN

```csharp
// Inner Join
var results = from b in session.Query<Board>()
              join r in session.Query<Reply>() on b.Id equals r.Board.Id
              where r.Author == "홍길동"
              select b;

// Left Join
var results = from b in session.Query<Board>()
              join r in session.Query<Reply>() on b.Id equals r.Board.Id into replies
              from r in replies.DefaultIfEmpty()
              select new { Board = b, Reply = r };

// Eager Loading
var boards = session.Query<Board>()
    .Fetch(b => b.Replies)          // 1단계 관계
    .ThenFetch(r => r.Author)       // 2단계 관계
    .ToList();

// Multiple Fetch
var boards = session.Query<Board>()
    .Fetch(b => b.Replies)
    .Fetch(b => b.Category)
    .ToList();
```

### 집계 함수

```csharp
// COUNT
var count = session.Query<Board>().Count();
var countWithCondition = session.Query<Board>()
    .Count(b => b.ViewCount > 100);

// SUM
var totalViews = session.Query<Board>().Sum(b => b.ViewCount);

// AVG
var avgViews = session.Query<Board>()
    .Where(b => b.Author == "홍길동")
    .Average(b => b.ViewCount);

// MIN/MAX
var maxViews = session.Query<Board>().Max(b => b.ViewCount);
var minViews = session.Query<Board>().Min(b => b.ViewCount);

// GROUP BY
var authorStats = session.Query<Board>()
    .GroupBy(b => b.Author)
    .Select(g => new
    {
        Author = g.Key,
        PostCount = g.Count(),
        TotalViews = g.Sum(b => b.ViewCount),
        AvgViews = g.Average(b => b.ViewCount)
    })
    .ToList();
```

### Projection

```csharp
// Anonymous Type
var summaries = session.Query<Board>()
    .Select(b => new
    {
        b.Id,
        b.Title,
        b.Author,
        ReplyCount = b.Replies.Count
    })
    .ToList();

// DTO
var dtos = session.Query<Board>()
    .Select(b => new BoardDto
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author,
        ViewCount = b.ViewCount
    })
    .ToList();
```

### 서브쿼리

```csharp
// 댓글이 많은 게시글
var boards = session.Query<Board>()
    .Where(b => session.Query<Reply>()
        .Count(r => r.Board.Id == b.Id) > 5)
    .ToList();

// Any
var boards = session.Query<Board>()
    .Where(b => b.Replies.Any(r => r.Author == "홍길동"))
    .ToList();

// All
var boards = session.Query<Board>()
    .Where(b => b.Replies.All(r => r.CreatedDate > DateTime.Now.AddDays(-7)))
    .ToList();
```

### 페이징

```csharp
// 기본 페이징
var pageNumber = 1;
var pageSize = 10;

var boards = session.Query<Board>()
    .OrderByDescending(b => b.CreatedDate)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToList();

// 페이징 헬퍼 메서드
public PagedResult<Board> GetPagedBoards(int pageNumber, int pageSize)
{
    var query = session.Query<Board>()
        .OrderByDescending(b => b.CreatedDate);

    var totalCount = query.Count();
    var items = query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return new PagedResult<Board>
    {
        Items = items,
        TotalCount = totalCount,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
    };
}
```

## 🎯 3. Criteria API

### 기본 사용법

```csharp
// 전체 조회
var boards = session.CreateCriteria<Board>().List<Board>();

// 조건 추가
var boards = session.CreateCriteria<Board>()
    .Add(Restrictions.Gt("ViewCount", 100))
    .List<Board>();

// 여러 조건 (AND)
var boards = session.CreateCriteria<Board>()
    .Add(Restrictions.Eq("Author", "홍길동"))
    .Add(Restrictions.Gt("ViewCount", 50))
    .List<Board>();

// OR 조건
var boards = session.CreateCriteria<Board>()
    .Add(Restrictions.Or(
        Restrictions.Like("Title", keyword, MatchMode.Anywhere),
        Restrictions.Like("Content", keyword, MatchMode.Anywhere)
    ))
    .List<Board>();
```

### Restrictions 종류

```csharp
// 같음
Restrictions.Eq("Author", "홍길동")

// 같지 않음
Restrictions.Not(Restrictions.Eq("Author", "홍길동"))

// 크다/작다
Restrictions.Gt("ViewCount", 100)  // >
Restrictions.Ge("ViewCount", 100)  // >=
Restrictions.Lt("ViewCount", 100)  // <
Restrictions.Le("ViewCount", 100)  // <=

// 범위
Restrictions.Between("ViewCount", 50, 200)

// Like
Restrictions.Like("Title", keyword, MatchMode.Anywhere)  // %keyword%
Restrictions.Like("Title", keyword, MatchMode.Start)     // keyword%
Restrictions.Like("Title", keyword, MatchMode.End)       // %keyword

// In
Restrictions.In("Author", new[] { "홍길동", "김철수", "이영희" })

// IsNull/IsNotNull
Restrictions.IsNull("ModifiedDate")
Restrictions.IsNotNull("ModifiedDate")

// 날짜 범위
Restrictions.Between("CreatedDate", startDate, endDate)
```

### 동적 쿼리 생성

```csharp
public IList<Board> SearchBoards(BoardSearchCriteria criteria)
{
    var query = session.CreateCriteria<Board>();

    // 조건을 동적으로 추가
    if (!string.IsNullOrEmpty(criteria.Author))
    {
        query.Add(Restrictions.Eq("Author", criteria.Author));
    }

    if (!string.IsNullOrEmpty(criteria.Keyword))
    {
        query.Add(Restrictions.Or(
            Restrictions.Like("Title", criteria.Keyword, MatchMode.Anywhere),
            Restrictions.Like("Content", criteria.Keyword, MatchMode.Anywhere)
        ));
    }

    if (criteria.MinViewCount.HasValue)
    {
        query.Add(Restrictions.Ge("ViewCount", criteria.MinViewCount.Value));
    }

    if (criteria.StartDate.HasValue && criteria.EndDate.HasValue)
    {
        query.Add(Restrictions.Between("CreatedDate",
            criteria.StartDate.Value, criteria.EndDate.Value));
    }

    // 정렬
    if (criteria.OrderBy == "ViewCount")
    {
        query.AddOrder(Order.Desc("ViewCount"));
    }
    else
    {
        query.AddOrder(Order.Desc("CreatedDate"));
    }

    // 페이징
    if (criteria.PageNumber > 0 && criteria.PageSize > 0)
    {
        query.SetFirstResult((criteria.PageNumber - 1) * criteria.PageSize);
        query.SetMaxResults(criteria.PageSize);
    }

    return query.List<Board>();
}
```

## 🚀 4. QueryOver API (Type-Safe)

```csharp
// 기본 쿼리
var boards = session.QueryOver<Board>()
    .Where(b => b.ViewCount > 100)
    .List();

// 여러 조건
var boards = session.QueryOver<Board>()
    .Where(b => b.Author == "홍길동")
    .And(b => b.ViewCount > 50)
    .OrderBy(b => b.CreatedDate).Desc
    .List();

// Like 검색
var boards = session.QueryOver<Board>()
    .WhereRestrictionOn(b => b.Title)
    .IsInsensitiveLike(keyword, MatchMode.Anywhere)
    .List();

// Join
Reply replyAlias = null;
var boards = session.QueryOver<Board>()
    .JoinAlias(b => b.Replies, () => replyAlias)
    .Where(() => replyAlias.Author == "홍길동")
    .List();

// Projection
var dtos = session.QueryOver<Board>()
    .Select(
        Projections.Property<Board>(b => b.Id),
        Projections.Property<Board>(b => b.Title),
        Projections.Property<Board>(b => b.Author)
    )
    .List<object[]>();
```

## 💾 5. Native SQL

### 기본 사용

```csharp
// 단순 조회
var sql = "SELECT * FROM Boards WHERE ViewCount > :minViews";
var boards = session.CreateSQLQuery(sql)
    .AddEntity(typeof(Board))
    .SetParameter("minViews", 100)
    .List<Board>();

// Scalar 값 조회
var sql = "SELECT COUNT(*) FROM Boards WHERE Author = :author";
var count = session.CreateSQLQuery(sql)
    .SetParameter("author", "홍길동")
    .UniqueResult<long>();

// DTO 매핑
var sql = @"
    SELECT Id, Title, Author, ViewCount
    FROM Boards
    WHERE ViewCount > :minViews
    ORDER BY CreatedDate DESC";

var results = session.CreateSQLQuery(sql)
    .SetResultTransformer(Transformers.AliasToBean<BoardDto>())
    .SetParameter("minViews", 100)
    .List<BoardDto>();
```

### 복잡한 쿼리

```csharp
// JOIN 쿼리
var sql = @"
    SELECT b.*, COUNT(r.Id) as ReplyCount
    FROM Boards b
    LEFT JOIN Replies r ON b.Id = r.BoardId
    GROUP BY b.Id, b.Title, b.Author, b.Content, b.ViewCount, b.CreatedDate
    HAVING COUNT(r.Id) > :minReplies";

var results = session.CreateSQLQuery(sql)
    .AddEntity("b", typeof(Board))
    .AddScalar("ReplyCount", NHibernateUtil.Int32)
    .SetParameter("minReplies", 5)
    .List<object[]>();

foreach (var result in results)
{
    var board = (Board)result[0];
    var replyCount = (int)result[1];
    Console.WriteLine($"{board.Title}: {replyCount} replies");
}
```

## 🎯 6. Named Queries (재사용)

### Entity에 정의

```csharp
// Board.hbm.xml
<hibernate-mapping>
    <class name="Board" table="Boards">
        <!-- ... -->

        <!-- Named Query 정의 -->
        <query name="Board.GetPopular">
            <![CDATA[
                from Board b
                where b.ViewCount > :minViews
                order by b.ViewCount desc
            ]]>
        </query>

        <query name="Board.SearchByKeyword">
            <![CDATA[
                from Board b
                where b.Title like :keyword or b.Content like :keyword
                order by b.CreatedDate desc
            ]]>
        </query>
    </class>
</hibernate-mapping>
```

### 사용

```csharp
// Named Query 실행
var boards = session.GetNamedQuery("Board.GetPopular")
    .SetParameter("minViews", 100)
    .SetMaxResults(10)
    .List<Board>();

var boards = session.GetNamedQuery("Board.SearchByKeyword")
    .SetParameter("keyword", $"%{keyword}%")
    .List<Board>();
```

## 🔥 7. 성능 최적화 팁

### Batch Fetching

```csharp
// hibernate.cfg.xml
<property name="adonet.batch_size">20</property>

// 매핑 파일
<class name="Board" table="Boards" batch-size="10">
```

### Fetch Join

```csharp
// N+1 문제 방지
var boards = session.Query<Board>()
    .Fetch(b => b.Replies)
    .ToList();
```

### Future Queries (쿼리 묶음)

```csharp
// 여러 쿼리를 하나의 DB 호출로
var boardsFuture = session.Query<Board>()
    .OrderByDescending(b => b.CreatedDate)
    .Take(10)
    .ToFuture();

var countFuture = session.Query<Board>()
    .ToFutureValue(x => x.Count());

// 실제 실행 시점
var boards = boardsFuture.ToList();  // 이 시점에 DB 호출 1번
var totalCount = countFuture.Value;  // 추가 DB 호출 없음
```

## 💡 핵심 정리

### 쿼리 방법 선택 가이드

| 상황 | 추천 방법 |
|------|-----------|
| 일반적인 CRUD | **LINQ** |
| 동적 쿼리 | **Criteria API** |
| 복잡한 비즈니스 로직 | **HQL** |
| Type-Safe 동적 쿼리 | **QueryOver** |
| 성능 최적화 필요 | **Native SQL** |

### 베스트 프랙티스

✅ **LINQ 우선 사용** (가독성, 타입 안정성)
✅ **Fetch Join으로 N+1 방지**
✅ **필요한 컬럼만 Projection**
✅ **페이징 항상 적용**
✅ **파라미터 바인딩** (SQL Injection 방지)

### 성능 최적화

✅ Batch Fetching 설정
✅ Second Level Cache 활용
✅ Future Queries로 쿼리 묶음
✅ Projection으로 데이터 최소화

## 🚀 다음 단계

다음: **[16-stored-procedures.md](./16-stored-procedures.md)** - Stored Procedure 사용법
