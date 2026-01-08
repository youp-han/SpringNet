# 19. 고급 CRUD 패턴

## 📚 학습 목표

- Generic Repository 심화
- Unit of Work 패턴
- Specification 패턴
- Repository + Service 통합 패턴
- Soft Delete 구현
- Audit Trail (변경 이력)
- Bulk Operations (대량 작업)

## 🎯 CRUD의 문제점과 해결

### 기본 CRUD의 한계

```csharp
// ❌ 문제: 반복적인 코드
public class BoardRepository
{
    public void Add(Board board) { session.Save(board); }
    public void Update(Board board) { session.Update(board); }
    public void Delete(Board board) { session.Delete(board); }
}

public class ProductRepository
{
    public void Add(Product product) { session.Save(product); }
    public void Update(Product product) { session.Update(product); }
    public void Delete(Product product) { session.Delete(product); }
}
// 모든 Entity마다 반복...
```

## 🛠️ 1. Generic Repository 패턴

### IRepository<T> 인터페이스

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
        T GetById(long id);
        IList<T> GetAll();
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        void Delete(int id);

        // 조건 조회
        T FindOne(Expression<Func<T, bool>> predicate);
        IList<T> Find(Expression<Func<T, bool>> predicate);

        // 페이징
        PagedResult<T> GetPaged(int pageNumber, int pageSize);
        PagedResult<T> GetPaged(int pageNumber, int pageSize,
                                Expression<Func<T, bool>> predicate);

        // 카운트
        int Count();
        int Count(Expression<Func<T, bool>> predicate);
        bool Exists(Expression<Func<T, bool>> predicate);

        // 정렬
        IList<T> GetAllSorted<TKey>(Expression<Func<T, TKey>> orderBy, bool ascending = true);
    }

    public class PagedResult<T>
    {
        public IList<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
```

### Repository<T> 구현

```csharp
using NHibernate;
using NHibernate.Linq;
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

        protected ISession CurrentSession =>
            sessionFactory.GetCurrentSession();

        public virtual T GetById(int id)
        {
            return CurrentSession.Get<T>(id);
        }

        public virtual T GetById(long id)
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

        public virtual T FindOne(Expression<Func<T, bool>> predicate)
        {
            return CurrentSession.Query<T>()
                .FirstOrDefault(predicate);
        }

        public virtual IList<T> Find(Expression<Func<T, bool>> predicate)
        {
            return CurrentSession.Query<T>()
                .Where(predicate)
                .ToList();
        }

        public virtual PagedResult<T> GetPaged(int pageNumber, int pageSize)
        {
            var query = CurrentSession.Query<T>();

            var totalCount = query.Count();
            var items = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public virtual PagedResult<T> GetPaged(int pageNumber, int pageSize,
                                               Expression<Func<T, bool>> predicate)
        {
            var query = CurrentSession.Query<T>().Where(predicate);

            var totalCount = query.Count();
            var items = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public virtual int Count()
        {
            return CurrentSession.Query<T>().Count();
        }

        public virtual int Count(Expression<Func<T, bool>> predicate)
        {
            return CurrentSession.Query<T>().Count(predicate);
        }

        public virtual bool Exists(Expression<Func<T, bool>> predicate)
        {
            return CurrentSession.Query<T>().Any(predicate);
        }

        public virtual IList<T> GetAllSorted<TKey>(
            Expression<Func<T, TKey>> orderBy,
            bool ascending = true)
        {
            var query = CurrentSession.Query<T>();

            return ascending
                ? query.OrderBy(orderBy).ToList()
                : query.OrderByDescending(orderBy).ToList();
        }
    }
}
```

## 📦 2. Unit of Work 패턴

### IUnitOfWork 인터페이스

```csharp
using System;

namespace SpringNet.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        void BeginTransaction();
        void Commit();
        void Rollback();

        // Repository 접근
        IBoardRepository Boards { get; }
        IReplyRepository Replies { get; }
        IUserRepository Users { get; }
        IProductRepository Products { get; }
        IOrderRepository Orders { get; }

        // Generic Repository
        IRepository<T> Repository<T>() where T : class;
    }
}
```

### UnitOfWork 구현

```csharp
using NHibernate;
using SpringNet.Data.Repositories;
using System;

namespace SpringNet.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ISessionFactory sessionFactory;
        private ISession session;
        private ITransaction transaction;

        // Repository 캐시
        private IBoardRepository boardRepository;
        private IReplyRepository replyRepository;
        private IUserRepository userRepository;
        private IProductRepository productRepository;
        private IOrderRepository orderRepository;

        public UnitOfWork(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        private ISession Session
        {
            get
            {
                if (session == null)
                {
                    session = sessionFactory.OpenSession();
                }
                return session;
            }
        }

        public void BeginTransaction()
        {
            transaction = Session.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                transaction?.Commit();
            }
            catch
            {
                transaction?.Rollback();
                throw;
            }
            finally
            {
                transaction?.Dispose();
                transaction = null;
            }
        }

        public void Rollback()
        {
            try
            {
                transaction?.Rollback();
            }
            finally
            {
                transaction?.Dispose();
                transaction = null;
            }
        }

        public IBoardRepository Boards =>
            boardRepository ??= new BoardRepository(sessionFactory);

        public IReplyRepository Replies =>
            replyRepository ??= new ReplyRepository(sessionFactory);

        public IUserRepository Users =>
            userRepository ??= new UserRepository(sessionFactory);

        public IProductRepository Products =>
            productRepository ??= new ProductRepository(sessionFactory);

        public IOrderRepository Orders =>
            orderRepository ??= new OrderRepository(sessionFactory);

        public IRepository<T> Repository<T>() where T : class
        {
            return new Repository<T>(sessionFactory);
        }

        public void Dispose()
        {
            session?.Dispose();
            transaction?.Dispose();
        }
    }
}
```

### UnitOfWork 사용 예제

```csharp
public class OrderService : IOrderService
{
    private readonly IUnitOfWork unitOfWork;

    public OrderService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public int CreateOrder(int userId, OrderRequestDto request)
    {
        try
        {
            unitOfWork.BeginTransaction();

            // 1. 장바구니 조회
            var cart = unitOfWork.Repository<Cart>()
                .FindOne(c => c.User.Id == userId);

            // 2. 주문 생성
            var order = new Order
            {
                User = unitOfWork.Users.GetById(userId),
                ShippingAddress = request.ShippingAddress
            };

            unitOfWork.Orders.Add(order);

            // 3. 재고 차감
            foreach (var item in cart.Items)
            {
                var product = unitOfWork.Products.GetById(item.Product.Id);
                product.Stock -= item.Quantity;
                unitOfWork.Products.Update(product);
            }

            // 4. 장바구니 비우기
            unitOfWork.Repository<CartItem>()
                .Find(ci => ci.Cart.Id == cart.Id)
                .ToList()
                .ForEach(ci => unitOfWork.Repository<CartItem>().Delete(ci));

            unitOfWork.Commit();

            return order.Id;
        }
        catch
        {
            unitOfWork.Rollback();
            throw;
        }
    }
}
```

## 🎯 3. Specification 패턴

### ISpecification<T> 인터페이스

```csharp
using System;
using System.Linq.Expressions;

namespace SpringNet.Domain.Specifications
{
    public interface ISpecification<T>
    {
        Expression<Func<T, bool>> ToExpression();
        bool IsSatisfiedBy(T entity);
    }

    public abstract class Specification<T> : ISpecification<T>
    {
        public abstract Expression<Func<T, bool>> ToExpression();

        public bool IsSatisfiedBy(T entity)
        {
            var predicate = ToExpression().Compile();
            return predicate(entity);
        }

        // AND 연산
        public Specification<T> And(Specification<T> other)
        {
            return new AndSpecification<T>(this, other);
        }

        // OR 연산
        public Specification<T> Or(Specification<T> other)
        {
            return new OrSpecification<T>(this, other);
        }

        // NOT 연산
        public Specification<T> Not()
        {
            return new NotSpecification<T>(this);
        }
    }
}
```

### Specification 조합

```csharp
// AND
internal class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> left;
    private readonly Specification<T> right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        this.left = left;
        this.right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter)
        );

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

// OR, NOT도 비슷하게 구현
```

### 실제 Specification 예제

```csharp
// 인기 게시글 Specification
public class PopularBoardSpecification : Specification<Board>
{
    private readonly int minViewCount;

    public PopularBoardSpecification(int minViewCount = 100)
    {
        this.minViewCount = minViewCount;
    }

    public override Expression<Func<Board, bool>> ToExpression()
    {
        return board => board.ViewCount >= minViewCount;
    }
}

// 최근 게시글 Specification
public class RecentBoardSpecification : Specification<Board>
{
    private readonly int daysAgo;

    public RecentBoardSpecification(int daysAgo = 7)
    {
        this.daysAgo = daysAgo;
    }

    public override Expression<Func<Board, bool>> ToExpression()
    {
        var cutoffDate = DateTime.Now.AddDays(-daysAgo);
        return board => board.CreatedDate >= cutoffDate;
    }
}

// 특정 작성자 Specification
public class BoardByAuthorSpecification : Specification<Board>
{
    private readonly string author;

    public BoardByAuthorSpecification(string author)
    {
        this.author = author;
    }

    public override Expression<Func<Board, bool>> ToExpression()
    {
        return board => board.Author == author;
    }
}
```

### Repository에서 Specification 사용

```csharp
public class BoardRepository : Repository<Board>, IBoardRepository
{
    public IList<Board> GetBySpecification(ISpecification<Board> specification)
    {
        return CurrentSession.Query<Board>()
            .Where(specification.ToExpression())
            .ToList();
    }
}

// 사용 예
var popularSpec = new PopularBoardSpecification(100);
var recentSpec = new RecentBoardSpecification(7);

// 인기 있고 최근 게시글
var popularAndRecent = popularSpec.And(recentSpec);
var boards = boardRepository.GetBySpecification(popularAndRecent);

// 인기 있거나 최근 게시글
var popularOrRecent = popularSpec.Or(recentSpec);
var boards2 = boardRepository.GetBySpecification(popularOrRecent);
```

## 🗑️ 4. Soft Delete 패턴

### ISoftDeletable 인터페이스

```csharp
using System;

namespace SpringNet.Domain.Entities
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedDate { get; set; }
        string DeletedBy { get; set; }

        void Delete(string deletedBy);
        void Restore();
    }
}
```

### 엔티티에 적용

```csharp
public class Board : ISoftDeletable
{
    public virtual int Id { get; set; }
    public virtual string Title { get; set; }
    public virtual string Content { get; set; }
    public virtual string Author { get; set; }

    // Soft Delete 필드
    public virtual bool IsDeleted { get; set; }
    public virtual DateTime? DeletedDate { get; set; }
    public virtual string DeletedBy { get; set; }

    public virtual void Delete(string deletedBy)
    {
        IsDeleted = true;
        DeletedDate = DateTime.Now;
        DeletedBy = deletedBy;
    }

    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedDate = null;
        DeletedBy = null;
    }
}
```

### Repository 수정

```csharp
public class SoftDeleteRepository<T> : Repository<T>
    where T : class, ISoftDeletable
{
    public override IList<T> GetAll()
    {
        // 삭제되지 않은 것만 조회
        return CurrentSession.Query<T>()
            .Where(x => !x.IsDeleted)
            .ToList();
    }

    public override T GetById(int id)
    {
        var entity = base.GetById(id);
        return entity?.IsDeleted == false ? entity : null;
    }

    public virtual void SoftDelete(T entity, string deletedBy)
    {
        entity.Delete(deletedBy);
        Update(entity);
    }

    public virtual void Restore(T entity)
    {
        entity.Restore();
        Update(entity);
    }

    // 실제 삭제 (관리자 전용)
    public virtual void HardDelete(T entity)
    {
        base.Delete(entity);
    }

    // 삭제된 항목 조회
    public virtual IList<T> GetDeleted()
    {
        return CurrentSession.Query<T>()
            .Where(x => x.IsDeleted)
            .ToList();
    }
}
```

## 📋 5. Audit Trail (변경 이력)

### IAuditable 인터페이스

```csharp
using System;

namespace SpringNet.Domain.Entities
{
    public interface IAuditable
    {
        DateTime CreatedDate { get; set; }
        string CreatedBy { get; set; }
        DateTime? ModifiedDate { get; set; }
        string ModifiedBy { get; set; }
    }
}
```

### NHibernate Event Listener

```csharp
using NHibernate.Event;
using System;

public class AuditEventListener : IPreInsertEventListener, IPreUpdateEventListener
{
    private string CurrentUser => "System"; // 실제로는 현재 사용자 정보

    public bool OnPreInsert(PreInsertEvent @event)
    {
        var auditable = @event.Entity as IAuditable;
        if (auditable == null) return false;

        var time = DateTime.Now;
        var user = CurrentUser;

        Set(@event.Persister, @event.State, "CreatedDate", time);
        Set(@event.Persister, @event.State, "CreatedBy", user);

        auditable.CreatedDate = time;
        auditable.CreatedBy = user;

        return false;
    }

    public bool OnPreUpdate(PreUpdateEvent @event)
    {
        var auditable = @event.Entity as IAuditable;
        if (auditable == null) return false;

        var time = DateTime.Now;
        var user = CurrentUser;

        Set(@event.Persister, @event.State, "ModifiedDate", time);
        Set(@event.Persister, @event.State, "ModifiedBy", user);

        auditable.ModifiedDate = time;
        auditable.ModifiedBy = user;

        return false;
    }

    private void Set(IEntityPersister persister, object[] state,
                     string propertyName, object value)
    {
        var index = Array.IndexOf(persister.PropertyNames, propertyName);
        if (index >= 0)
        {
            state[index] = value;
        }
    }
}
```

### SessionFactory에 등록

```csharp
var configuration = new Configuration();
configuration.Configure();

configuration.EventListeners.PreInsertEventListeners =
    new IPreInsertEventListener[] { new AuditEventListener() };

configuration.EventListeners.PreUpdateEventListeners =
    new IPreUpdateEventListener[] { new AuditEventListener() };

sessionFactory = configuration.BuildSessionFactory();
```

## ⚡ 6. Bulk Operations (대량 작업)

```csharp
public class BulkOperations
{
    private readonly ISession session;

    public void BulkUpdate()
    {
        // HQL로 대량 업데이트
        var updated = session.CreateQuery(@"
            update Board b
            set b.ViewCount = b.ViewCount + 1
            where b.CreatedDate > :date")
            .SetDateTime("date", DateTime.Now.AddDays(-30))
            .ExecuteUpdate();

        Console.WriteLine($"{updated} rows updated");
    }

    public void BulkDelete()
    {
        // 대량 삭제
        var deleted = session.CreateQuery(@"
            delete from Reply r
            where r.CreatedDate < :date")
            .SetDateTime("date", DateTime.Now.AddMonths(-6))
            .ExecuteUpdate();

        Console.WriteLine($"{deleted} rows deleted");
    }

    public void BatchInsert(IList<Board> boards)
    {
        const int batchSize = 50;

        for (int i = 0; i < boards.Count; i++)
        {
            session.Save(boards[i]);

            if (i % batchSize == 0)
            {
                session.Flush();
                session.Clear();
            }
        }
    }
}
```

## 💡 핵심 정리

### 패턴 선택 가이드

| 패턴 | 사용 시기 |
|------|-----------|
| **Generic Repository** | 반복 코드 제거 |
| **Unit of Work** | 복잡한 트랜잭션 |
| **Specification** | 동적 쿼리 조합 |
| **Soft Delete** | 데이터 복구 필요 |
| **Audit Trail** | 변경 이력 추적 |

### 베스트 프랙티스

✅ Repository는 가볍게 유지
✅ 복잡한 로직은 Service Layer에
✅ Specification으로 쿼리 재사용
✅ Unit of Work로 트랜잭션 통합

## 🎓 완료!

축하합니다! Spring.NET + NHibernate 전체 튜토리얼을 완료했습니다!

이제 여러분은:
- ✅ Spring.NET IoC/DI 마스터
- ✅ NHibernate ORM 전문가
- ✅ 레이어드 아키텍처 설계
- ✅ 고급 디자인 패턴 활용
- ✅ 실전 프로젝트 구현 가능

계속 연습하고 실전 프로젝트를 만들어보세요! 🚀
