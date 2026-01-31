# 15. NHibernate 고급 쿼리

## 📚 학습 목표

- HQL (Hibernate Query Language) 마스터
- LINQ to NHibernate 고급 사용법
- Criteria API 및 QueryOver API를 활용한 동적 쿼리 생성
- Native SQL 실행 및 DTO 매핑
- Future Queries를 이용한 성능 최적화

## 🎯 NHibernate 쿼리 방법 5가지

이 튜토리얼의 모든 예제는 서비스 계층에서 `ISessionFactory`를 주입받아 `sessionFactory.GetCurrentSession()`을 통해 `ISession`을 얻는 것을 기준으로 작성되었습니다.

```
1. HQL          - 객체 지향 쿼리 언어. 문자열 기반이지만 강력함.
2. LINQ         - C# LINQ 표현식. 타입 안정성, 가독성, IntelliSense 지원 (권장).
3. Criteria API - 동적 쿼리 생성을 위한 객체 기반 API. (레거시, QueryOver 권장)
4. QueryOver    - Criteria API의 타입 안정(Type-Safe) 버전.
5. Native SQL   - 특정 데이터베이스에 종속적인 원본 SQL 직접 실행.
```

## 📝 1. HQL (Hibernate Query Language)

### 기본 조회

```csharp
// 전체 조회
var boards = sessionFactory.GetCurrentSession().CreateQuery("from Board").List<Board>();

// 조건 조회
var boards = sessionFactory.GetCurrentSession().CreateQuery("from Board b where b.ViewCount > 100")
    .List<Board>();

// 파라미터 바인딩
var boards = sessionFactory.GetCurrentSession().CreateQuery(
    "from Board b where b.Author = :author and b.ViewCount > :minViews")
    .SetParameter("author", "홍길동")
    .SetParameter("minViews", 50)
    .List<Board>();

// Named 파라미터 (날짜)
var boards = sessionFactory.GetCurrentSession().CreateQuery(
    "from Board b where b.CreatedDate between :startDate and :endDate")
    .SetDateTime("startDate", startDate)
    .SetDateTime("endDate", endDate)
    .List<Board>();
```

### JOIN 쿼리

```csharp
// Inner Join (댓글을 작성한 적 있는 게시판 조회)
var query = @"
    select b from Board b
    inner join b.Replies r
    where r.Author = :author";

var boards = sessionFactory.GetCurrentSession().CreateQuery(query)
    .SetParameter("author", "홍길동")
    .List<Board>();

// Left Join Fetch (Eager Loading, N+1 문제 방지)
var query = @"
    from Product p
    left join fetch p.Category
    where p.Price > :minPrice";

var products = sessionFactory.GetCurrentSession().CreateQuery(query)
    .SetParameter("minPrice", 1000)
    .List<Product>();

// Multiple Joins (주문, 주문 항목, 상품을 한 번에 조회)
var query = @"
    from Order o
    left join fetch o.Items oi
    left join fetch oi.Product
    where o.Id = :orderId";
```

### 집계 함수

```csharp
// COUNT
var count = sessionFactory.GetCurrentSession().CreateQuery("select count(*) from Board")
    .UniqueResult<long>();

// SUM
var totalViews = sessionFactory.GetCurrentSession().CreateQuery(
    "select sum(b.ViewCount) from Board b")
    .UniqueResult<long>();

// AVG
var avgViews = sessionFactory.GetCurrentSession().CreateQuery(
    "select avg(b.ViewCount) from Board b where b.Author = :author")
    .SetParameter("author", "홍길동")
    .UniqueResult<double>();

// GROUP BY
var query = @"
    select p.Category.Name, count(p), avg(p.Price)
    from Product p
    group by p.Category.Name
    having count(p) > 3";

var results = sessionFactory.GetCurrentSession().CreateQuery(query).List<object[]>();
foreach (var result in results)
{
    var categoryName = result[0];
    var productCount = result[1];
    var avgPrice = result[2];
}
```

### 정렬 및 페이징

```csharp
// 정렬
var boards = sessionFactory.GetCurrentSession().CreateQuery("from Board b order by b.CreatedDate desc")
    .List<Board>();

// 여러 컬럼 정렬
var boards = sessionFactory.GetCurrentSession().CreateQuery(
    "from Board b order by b.ViewCount desc, b.CreatedDate desc")
    .List<Board>();

// 페이징
var boards = sessionFactory.GetCurrentSession().CreateQuery("from Board b order by b.CreatedDate desc")
    .SetFirstResult(0)      // OFFSET
    .SetMaxResults(10)      // LIMIT
    .List<Board>();
```

### 서브쿼리

```csharp
// 서브쿼리 - 댓글이 5개 이상인 게시글
var query = @"
    from Board b
    where (select count(r) from Reply r where r.Board = b) > :minReplies";

var boards = sessionFactory.GetCurrentSession().CreateQuery(query)
    .SetParameter("minReplies", 5)
    .List<Board>();

// IN 서브쿼리 - 관리자 역할의 사용자가 작성한 게시글
var query = @"
    from Board b
    where b.Author in (
        select u.Username from User u where u.Role = 'Admin'
    )";
```

### UPDATE/DELETE (벌크 연산)

벌크 연산은 NHibernate 세션 캐시를 거치지 않고 데이터베이스에 직접 쿼리를 실행하므로, 캐시와 영속성 컨텍스트의 엔티티 상태가 일치하지 않을 수 있습니다. 사용에 주의가 필요합니다.

```csharp
// 벌크 UPDATE
var updated = sessionFactory.GetCurrentSession().CreateQuery(
    "update Board b set b.ViewCount = 0 where b.CreatedDate < :date")
    .SetDateTime("date", aWeekAgo)
    .ExecuteUpdate();

// 벌크 DELETE
var deleted = sessionFactory.GetCurrentSession().CreateQuery(
    "delete from Reply r where r.Author = :authorName")
    .SetParameter("authorName", "탈퇴한사용자")
    .ExecuteUpdate();
```

## 🔍 2. LINQ to NHibernate (권장)

C# 개발자에게 가장 친숙하고, 타입 안정성과 가독성이 높아 유지보수에 유리합니다.

### 기본 쿼리

```csharp
using NHibernate.Linq;

// 전체 조회
var boards = sessionFactory.GetCurrentSession().Query<Board>().ToList();

// 조건 조회
var boards = sessionFactory.GetCurrentSession().Query<Board>()
    .Where(b => b.ViewCount > 100)
    .ToList();

// 복잡한 조건 및 정렬
var products = sessionFactory.GetCurrentSession().Query<Product>()
    .Where(p => p.Category.Name == "Electronics" && p.Price < 1000)
    .OrderByDescending(p => p.Stock)
    .ToList();
```

### JOIN 및 Eager Loading

```csharp
// Inner Join (LINQ Join 문법)
var results = from b in sessionFactory.GetCurrentSession().Query<Board>()
              join r in sessionFactory.GetCurrentSession().Query<Reply>() on b.Id equals r.Board.Id
              where r.Author == "홍길동"
              select b;

// Eager Loading (N+1 문제 방지)
var orders = sessionFactory.GetCurrentSession().Query<Order>()
    .Fetch(o => o.User) // Order와 User Eager Loading
    .ToList();

// Multiple/Nested Eager Loading (ThenFetch)
// 주문, 주문 항목, 각 항목의 상품 정보를 한 번에 Eager Loading
var orderDetails = sessionFactory.GetCurrentSession().Query<Order>()
    .Where(o => o.Id == orderId)
    .FetchMany(o => o.Items)    // 1단계 관계 (Order -> OrderItem)
    .ThenFetch(oi => oi.Product) // 2단계 관계 (OrderItem -> Product)
    .SingleOrDefault();
```

### 집계 함수

```csharp
// COUNT
var count = sessionFactory.GetCurrentSession().Query<Product>().Count();
var countWithCondition = sessionFactory.GetCurrentSession().Query<Product>()
    .Count(p => p.Price > 1000);

// SUM
var totalStockValue = sessionFactory.GetCurrentSession().Query<Product>().Sum(p => p.Stock * p.Price);

// AVG
var avgPrice = sessionFactory.GetCurrentSession().Query<Product>()
    .Where(p => p.Category.Name == "Books")
    .Average(p => p.Price);

// GROUP BY
var categoryStats = sessionFactory.GetCurrentSession().Query<Product>()
    .GroupBy(p => p.Category.Name)
    .Select(g => new
    {
        CategoryName = g.Key,
        ProductCount = g.Count(),
        TotalStock = g.Sum(p => p.Stock),
        AveragePrice = g.Average(p => p.Price)
    })
    .ToList();
```

### Projection (부분 조회)

```csharp
// 익명 타입으로 프로젝션
var productSummaries = sessionFactory.GetCurrentSession().Query<Product>()
    .Select(p => new
    {
        p.Id,
        p.Name,
        p.Price,
        CategoryName = p.Category.Name // Join 없이도 연관 엔티티의 속성 접근 가능
    })
    .ToList();

// DTO로 프로젝션 (강력 추천)
var productDtos = sessionFactory.GetCurrentSession().Query<Product>()
    .Select(p => new ProductDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        ImageUrl = p.ImageUrl,
        CategoryName = p.Category.Name
    })
    .ToList();
```

### 서브쿼리 및 고급 연산

```csharp
// 서브쿼리 - 댓글이 5개 이상인 게시글
var boards = sessionFactory.GetCurrentSession().Query<Board>()
    .Where(b => b.Replies.Count > 5)
    .ToList();

// Any (존재 여부 확인) - 홍길동이 작성한 댓글이 하나라도 있는 게시글
var boards = sessionFactory.GetCurrentSession().Query<Board>()
    .Where(b => b.Replies.Any(r => r.Author == "홍길동"))
    .ToList();

// All (모든 항목이 조건 만족) - 모든 댓글이 최근 7일 내에 작성된 게시글
var boards = sessionFactory.GetCurrentSession().Query<Board>()
    .Where(b => b.Replies.All(r => r.CreatedDate > DateTime.Now.AddDays(-7)))
    .ToList();
```

### 페이징

```csharp
// 페이징 헬퍼 메서드 (PagedResultDto는 06번 튜토리얼에서 정의)
public PagedResultDto<ProductDto> GetPagedProducts(int pageNumber, int pageSize)
{
    var query = sessionFactory.GetCurrentSession().Query<Product>()
        .OrderByDescending(p => p.CreatedDate);

    var totalCount = query.Count();
    var items = query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new ProductDto // DTO로 프로젝션
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            CategoryName = p.Category.Name
        })
        .ToList();

    return new PagedResultDto<ProductDto>
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

문자열이 아닌 객체로 쿼리를 구성하여 동적 쿼리 생성에 용이합니다. 하지만 타입 안정성이 없어 오타에 취약하므로, `QueryOver` 사용이 더 권장됩니다.

```csharp
// 검색 조건을 담을 클래스
public class BoardSearchCriteria
{
    public string Author { get; set; }
    public string Keyword { get; set; }
    public int? MinViewCount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string OrderBy { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// 동적 쿼리 생성 메서드
public IList<Board> SearchBoards(BoardSearchCriteria criteria)
{
    var query = sessionFactory.GetCurrentSession().CreateCriteria<Board>();

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
        query.Add(Restrictions.Between("CreatedDate", criteria.StartDate.Value, criteria.EndDate.Value));
    }

    // 정렬
    query.AddOrder(criteria.OrderBy == "ViewCount" ? Order.Desc("ViewCount") : Order.Desc("CreatedDate"));
    
    // 페이징
    query.SetFirstResult((criteria.PageNumber - 1) * criteria.PageSize)
         .SetMaxResults(criteria.PageSize);

    return query.List<Board>();
}
```

## 🚀 4. QueryOver API (Type-Safe Criteria)

Criteria API를 타입 안정적으로 사용할 수 있는 API입니다. LINQ가 더 직관적일 수 있지만, 복잡한 동적 쿼리 시 강력한 기능을 제공합니다.

```csharp
// 기본 쿼리
var boards = sessionFactory.GetCurrentSession().QueryOver<Board>()
    .Where(b => b.ViewCount > 100)
    .List();

// 여러 조건
var boards = sessionFactory.GetCurrentSession().QueryOver<Board>()
    .Where(b => b.Author == "홍길동")
    .And(b => b.ViewCount > 50)
    .OrderBy(b => b.CreatedDate).Desc
    .List();

// Like 검색
var boards = sessionFactory.GetCurrentSession().QueryOver<Board>()
    .WhereRestrictionOn(b => b.Title)
    .IsInsensitiveLike(keyword, MatchMode.Anywhere)
    .List();

// Join
Reply replyAlias = null;
var boards = sessionFactory.GetCurrentSession().QueryOver<Board>()
    .JoinAlias(b => b.Replies, () => replyAlias)
    .Where(() => replyAlias.Author == "홍길동")
    .List();

// Projection
BoardDto dto = null;
var dtos = sessionFactory.GetCurrentSession().QueryOver<Board>()
    .SelectList(list => list
        .Select(b => b.Id).WithAlias(() => dto.Id)
        .Select(b => b.Title).WithAlias(() => dto.Title)
        .Select(b => b.Author).WithAlias(() => dto.Author)
    )
    .TransformUsing(Transformers.AliasToBean<BoardDto>())
    .List<BoardDto>();
```

## 💾 5. Native SQL

데이터베이스에 특화된 기능이나 복잡한 튜닝이 필요할 때 사용합니다. 이식성은 떨어지지만 최고의 성능을 낼 수 있습니다.

```csharp
// 엔티티로 결과 매핑
var sql = "SELECT {b.*} FROM Boards {b} WHERE b.ViewCount > :minViews";
var boards = sessionFactory.GetCurrentSession().CreateSQLQuery(sql)
    .AddEntity("b", typeof(Board))
    .SetParameter("minViews", 100)
    .List<Board>();

// Scalar 값 조회
var sql = "SELECT COUNT(*) FROM Boards WHERE Author = :author";
var count = sessionFactory.GetCurrentSession().CreateSQLQuery(sql)
    .SetParameter("author", "홍길동")
    .UniqueResult<long>();

// DTO로 결과 매핑
var sql = @"
    SELECT Id, Title, Author, ViewCount, CreatedDate
    FROM Boards
    WHERE ViewCount > :minViews
    ORDER BY CreatedDate DESC";

var results = sessionFactory.GetCurrentSession().CreateSQLQuery(sql)
    .SetResultTransformer(Transformers.AliasToBean<BoardDto>())
    .SetParameter("minViews", 100)
    .List<BoardDto>();
// 참고: AliasToBean은 레거시 방식입니다. NHibernate 5+에서는 DTO 생성자를 사용하는 LINQ 프로젝션이 더 권장됩니다.
```

## 🔥 6. Future Queries (쿼리 묶음)

여러 쿼리를 데이터베이스에 한 번의 왕복(roundtrip)으로 보내 실행하여 네트워크 지연을 줄이는 강력한 성능 최적화 기법입니다.

```csharp
// 여러 쿼리를 하나의 DB 호출로 묶기
var pageNumber = 1;
var pageSize = 10;

// 1. 페이징된 상품 목록 쿼리 (아직 실행 안 됨)
var productsFuture = sessionFactory.GetCurrentSession().Query<Product>()
    .Where(p => p.IsAvailable)
    .OrderByDescending(p => p.CreatedDate)
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToFuture();

// 2. 전체 상품 개수 쿼리 (아직 실행 안 됨)
var countFuture = sessionFactory.GetCurrentSession().Query<Product>()
    .Where(p => p.IsAvailable)
    .ToFutureValue(x => x.Count());
    
// 3. 가장 비싼 상품 가격 쿼리 (아직 실행 안 됨)
var maxPriceFuture = sessionFactory.GetCurrentSession().Query<Product>()
    .Where(p => p.IsAvailable)
    .ToFutureValue(x => x.Max(p => p.Price));

// 첫 번째 Future 결과에 접근하는 시점에 모든 쿼리가 한 번에 DB로 전송됨
var products = productsFuture.ToList();
var totalCount = countFuture.Value;
var maxPrice = maxPriceFuture.Value;

Console.WriteLine($"총 {totalCount}개의 상품 중 {products.Count}개를 가져왔습니다.");
Console.WriteLine($"가장 비싼 상품의 가격은 {maxPrice:C}입니다.");
```

## 💡 핵심 정리

### 쿼리 방법 선택 가이드

| 상황 | 추천 방법 | 이유 |
|---|---|---|
| 일반적인 조회, CRUD | **LINQ to NHibernate** | 타입 안정성, 가독성, 유지보수 용이, IntelliSense 지원 |
| 복잡한 동적 쿼리 | **QueryOver** | 타입 안정성을 유지하면서 동적 쿼리 구성 가능 |
| 정적인 복잡한 쿼리 | **HQL** | HQL에 익숙하고 문자열 기반 쿼리가 편할 때 |
| DB 종속 기능, 극한의 성능 | **Native SQL** | 데이터베이스의 특정 함수를 사용하거나 성능 최적화가 필요할 때 |
| 여러 쿼리 동시 실행 | **Future Queries** | 여러 개의 조회 쿼리를 한 번의 DB 왕복으로 처리하여 성능 향상 |

### 베스트 프랙티스

✅ **LINQ 우선 사용**: 대부분의 경우 LINQ는 가장 생산적이고 안전한 선택입니다.
✅ **Fetch Join으로 N+1 방지**: 성능 저하의 주범인 N+1 문제를 항상 경계하고 `Fetch`, `FetchMany`로 해결합니다.
✅ **Projection으로 필요한 데이터만 조회**: `Select`를 사용하여 전체 엔티티가 아닌 필요한 데이터만 DTO로 가져옵니다.
✅ **페이징 습관화**: 대용량 데이터를 한 번에 가져오지 않도록 항상 페이징(`Skip`, `Take`)을 적용합니다.
✅ **Future Queries 활용**: 한 화면을 구성하기 위해 여러 개의 `select` 쿼리가 필요하다면, `Future Queries`로 묶어 DB 왕복 횟수를 줄이는 것을 고려합니다.

## 🚀 다음 단계

다음: **[16-stored-procedures.md](./16-stored-procedures.md)** - Stored Procedure 사용법
