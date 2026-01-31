# 19. 고급 CRUD 패턴 및 아키텍처

## 📚 학습 목표

- Generic Repository 패턴 확장
- Unit of Work 패턴에 대한 이해 (Spring.NET 환경)
- Specification 패턴을 이용한 재사용 가능한 쿼리 정의
- Soft Delete (논리적 삭제) 구현
- Audit Trail (변경 이력 추적) 자동화

## 🎯 기본 CRUD를 넘어서

지금까지 우리는 기본적인 CRUD 기능을 제공하는 Generic Repository와 각 엔티티별 Repository를 구현했습니다. 하지만 실제 엔터프라이즈 애플리케이션에서는 더 복잡한 요구사항(재사용 가능한 쿼리, 논리적 삭제, 변경 이력 추적 등)이 발생합니다. 이 튜토리얼에서는 이러한 고급 패턴들을 우리가 구축한 Spring.NET + NHibernate 아키텍처에 어떻게 우아하게 통합하는지 알아봅니다.

## 🛠️ 1. Generic Repository 패턴 확장

`05-board-part2-repository.md`에서 구현한 `IRepository<T>`와 `Repository<T>`는 좋은 출발점입니다. 여기에 `Exists`와 같이 유용한 메서드를 추가하여 확장할 수 있습니다.

`SpringNet.Data/Repositories/IRepository.cs`에 다음 메서드 시그니처를 추가합니다.
```csharp
// IRepository<T> 인터페이스에 추가
bool Exists(Expression<Func<T, bool>> predicate);
```

`SpringNet.Data/Repositories/Repository.cs`에 다음 메서드를 구현합니다.
```csharp
// Repository<T> 클래스에 구현
public virtual bool Exists(Expression<Func<T, bool>> predicate)
{
    return CurrentSession.Query<T>().Any(predicate);
}
```
이제 모든 리포지토리에서 `Exists` 메서드를 사용하여 특정 조건을 만족하는 데이터의 존재 여부를 효율적으로 확인할 수 있습니다.

## 📦 2. Unit of Work 패턴과 Spring.NET

**Unit of Work(UoW)** 패턴은 비즈니스 트랜잭션 동안 발생하는 모든 변경 사항을 추적하고, 마지막에 하나의 단위로 데이터베이스에 커밋하는 패턴입니다. 이를 통해 데이터 일관성을 보장합니다.

수동으로 UoW 클래스를 구현할 수도 있지만, 우리가 구축한 **Spring.NET + NHibernate 아키텍처에서는 이 패턴이 이미 내장되어 있습니다.**

-   **`ISession` as Unit of Work**: NHibernate의 `ISession` 객체는 1차 캐시(Identity Map)를 통해 엔티티의 변경 사항을 추적합니다. 이것이 바로 UoW의 핵심 기능입니다.
-   **`[Transaction]` as Boundary**: 서비스 계층 메서드에 적용된 `[Transaction]` 속성은 UoW의 경계를 정의합니다.
    1.  메서드 시작 시 트랜잭션이 시작됩니다.
    2.  메서드 내에서 여러 리포지토리를 통해 데이터를 변경하면, 모든 변경 사항은 현재 `ISession`(UoW)에 의해 추적됩니다.
    3.  메서드가 성공적으로 종료되면, Spring은 트랜잭션을 커밋하고 `ISession`이 추적하던 모든 변경 사항을 데이터베이스에 한 번에 반영합니다.
    4.  예외가 발생하면 트랜잭션을 롤백하고 변경 사항을 모두 폐기합니다.

**UoW 패턴 적용 예시 (OrderService 재확인)**:
`12-shopping-part3-order.md`에서 구현한 `CreateOrderFromCart` 메서드는 이미 UoW 패턴을 따르고 있습니다.
```csharp
[Transaction] // 이 속성이 UoW의 경계를 정의
public int CreateOrderFromCart(OrderRequestDto request)
{
    // 1. 재고 확인 및 차감 (ProductRepository 사용)
    // 2. 주문 생성 (OrderRepository 사용)
    // 3. 장바구니 비우기 (CartRepository 사용)
    
    // 이 모든 작업은 하나의 트랜잭션으로 묶여 원자성(Atomicity)을 보장합니다.
    // 중간에 실패하면 모든 작업이 롤백됩니다.
    
    return order.Id;
}
```
> **결론**: Spring.NET의 선언적 트랜잭션을 사용하면, 별도의 UoW 클래스 없이도 서비스 메서드 자체가 하나의 작업 단위(Unit of Work)가 됩니다.

## 🎯 3. Specification 패턴

Specification 패턴은 비즈니스 규칙(쿼리 조건)을 재사용 가능한 객체로 캡슐화하는 패턴입니다. LINQ의 `Expression`을 활용하여 타입 안정적인 쿼리 명세를 만들 수 있습니다.

### Specification 기본 인터페이스 및 클래스

`SpringNet.Domain/Specifications/` 폴더를 생성하고 다음 파일들을 추가합니다.

`ISpecification.cs`:
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
}
```

`Specification.cs` (조합을 위한 도우미):
```csharp
// (ExpressionVisitor를 사용한 올바른 조합 로직 포함)
// ExpressionCombiner.cs
using System.Linq.Expressions;

namespace SpringNet.Domain.Specifications
{
    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return base.VisitParameter(_parameter);
        }

        internal ParameterReplacer(ParameterExpression parameter)
        {
            _parameter = parameter;
        }
    }
    
    public static class Specification
    {
        public static ISpecification<T> All<T>() => new IdentitySpecification<T>();

        public static AndSpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
        {
            return new AndSpecification<T>(left, right);
        }

        public static OrSpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
        {
            return new OrSpecification<T>(left, right);
        }

        public static NotSpecification<T> Not<T>(this ISpecification<T> spec)
        {
            return new NotSpecification<T>(spec);
        }
    }
    
    // Specifications
    public abstract class AbstractSpecification<T> : ISpecification<T>
    {
        public abstract Expression<Func<T, bool>> ToExpression();
        public bool IsSatisfiedBy(T entity) => ToExpression().Compile().Invoke(entity);
    }
    
    internal sealed class IdentitySpecification<T> : AbstractSpecification<T>
    {
        public override Expression<Func<T, bool>> ToExpression() => _ => true;
    }
    
    public sealed class AndSpecification<T> : AbstractSpecification<T>
    {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;
        public AndSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            _left = left;
            _right = right;
        }
        public override Expression<Func<T, bool>> ToExpression()
        {
            var leftExpression = _left.ToExpression();
            var rightExpression = _right.ToExpression();
            var andExpression = Expression.AndAlso(leftExpression.Body, new ParameterReplacer(leftExpression.Parameters[0]).Visit(rightExpression.Body));
            return Expression.Lambda<Func<T, bool>>(andExpression, leftExpression.Parameters);
        }
    }
    
    public sealed class OrSpecification<T> : AbstractSpecification<T>
    {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;
        public OrSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            _left = left;
            _right = right;
        }
        public override Expression<Func<T, bool>> ToExpression()
        {
            var leftExpression = _left.ToExpression();
            var rightExpression = _right.ToExpression();
            var orExpression = Expression.OrElse(leftExpression.Body, new ParameterReplacer(leftExpression.Parameters[0]).Visit(rightExpression.Body));
            return Expression.Lambda<Func<T, bool>>(orExpression, leftExpression.Parameters);
        }
    }
    
    public sealed class NotSpecification<T> : AbstractSpecification<T>
    {
        private readonly ISpecification<T> _specification;
        public NotSpecification(ISpecification<T> specification)
        {
            _specification = specification;
        }
        public override Expression<Func<T, bool>> ToExpression()
        {
            var expression = _specification.ToExpression();
            return Expression.Lambda<Func<T, bool>>(Expression.Not(expression.Body), expression.Parameters);
        }
    }
}
```

### 실제 Specification 예제

`SpringNet.Domain/Specifications/ProductSpecifications.cs`:
```csharp
// 인기 상품 Specification
public class PopularProductSpecification : AbstractSpecification<Product>
{
    private readonly int minStock;
    public PopularProductSpecification(int minStock = 10)
    {
        this.minStock = minStock;
    }
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.Stock >= minStock;
    }
}

// 특정 카테고리 상품 Specification
public class ProductsByCategorySpecification : AbstractSpecification<Product>
{
    private readonly int categoryId;
    public ProductsByCategorySpecification(int categoryId)
    {
        this.categoryId = categoryId;
    }
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.Category.Id == categoryId;
    }
}
```

### Repository에서 Specification 사용

`IRepository<T>` 와 `Repository<T>`를 수정하여 Specification을 지원하도록 합니다.

`IRepository<T>`에 추가:
```csharp
IList<T> Find(ISpecification<T> specification);
```

`Repository<T>`에 구현:
```csharp
public virtual IList<T> Find(ISpecification<T> specification)
{
    return CurrentSession.Query<T>()
        .Where(specification.ToExpression())
        .ToList();
}
```

**서비스 계층에서 사용 예**:
```csharp
var popularSpec = new PopularProductSpecification(50);
var electronicsSpec = new ProductsByCategorySpecification(1); // 1번: 전자제품 카테고리

// 50개 이상 재고가 있는 전자제품 조회
var spec = popularSpec.And(electronicsSpec);
var products = productRepository.Find(spec);
```

## 🗑️ 4. Soft Delete (논리적 삭제) 패턴

사용자 실수나 규제 요건으로 인해 데이터를 실제로 삭제하는 대신 '삭제됨' 상태로 표시해야 할 때 사용합니다. NHibernate의 **Filter** 기능을 사용하면 이를 매우 우아하게 처리할 수 있습니다.

### `ISoftDeletable` 인터페이스 및 엔티티 수정

`SpringNet.Domain/Entities/ISoftDeletable.cs`:
```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
```
`Board`, `Product` 등 필요한 엔티티에 `ISoftDeletable`을 구현하고 `IsDeleted` 속성을 추가합니다.
```csharp
public class Board : ISoftDeletable
{
    // ...
    public virtual bool IsDeleted { get; set; }
}
```
매핑 파일(`.hbm.xml`)에도 `<property name="IsDeleted" column="IsDeleted" type="boolean" not-null="true" />`를 추가합니다.

### NHibernate Filter 정의

`SpringNet.Data/Mappings/` 폴더에 `Filters.hbm.xml` 파일을 새로 생성하고 포함 리소스(Embedded Resource)로 설정합니다.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2">
    <filter-def name="SoftDeleteFilter">
        <filter-param name="isDeleted" type="System.Boolean"/>
    </filter-def>
</hibernate-mapping>
```
`Board.hbm.xml` 등 Soft Delete를 적용할 엔티티의 `<class>` 태그 내에 필터를 적용합니다.
```xml
<class name="Board" table="Boards">
    <!-- ... -->
    <filter name="SoftDeleteFilter" condition="IsDeleted = :isDeleted"/>
</class>
```

### Spring.NET과 Filter 통합

Spring의 `OpenSessionInView` 패턴과 함께 필터를 자동으로 활성화하도록 `Global.asax.cs`를 수정합니다.

`SpringNet.Web/Global.asax.cs`:
```csharp
public class MvcApplication : SpringMvcApplication
{
    // ...
    protected override void OnBeginRequest(object sender, EventArgs e)
    {
        base.OnBeginRequest(sender, e);
        // 모든 세션에 SoftDeleteFilter를 기본으로 활성화
        var session = GetCurrentSession();
        if (session != null)
        {
            session.EnableFilter("SoftDeleteFilter").SetParameter("isDeleted", false);
        }
    }
}
```
이제 모든 `session.Query<T>()`나 `session.Get<T>()`는 `IsDeleted = false`인 데이터만 자동으로 가져옵니다.

**Soft Delete 수행**:
```csharp
[Transaction]
public void SoftDeleteBoard(int boardId)
{
    var board = boardRepository.GetById(boardId);
    if (board != null)
    {
        board.IsDeleted = true;
        boardRepository.Update(board); // Update 호출
    }
}
```
**삭제된 데이터 조회**:
```csharp
[Transaction(ReadOnly = true)]
public IList<Board> GetDeletedBoards()
{
    // 필터를 일시적으로 비활성화하여 삭제된 데이터 조회
    using (var session = sessionFactory.GetCurrentSession().SessionWithOptions().DisableFilter("SoftDeleteFilter").OpenSession())
    {
        return session.Query<Board>().Where(b => b.IsDeleted).ToList();
    }
}
```

## 📋 5. Audit Trail (변경 이력 추적)

NHibernate의 **Event Listener**를 사용하여 엔티티가 생성되거나 수정될 때 특정 속성(`CreatedDate`, `ModifiedBy` 등)을 자동으로 채웁니다.

### `IAuditable` 인터페이스 및 엔티티 수정

`SpringNet.Domain/Entities/IAuditable.cs`:
```csharp
public interface IAuditable
{
    DateTime CreatedDate { get; set; }
    string CreatedBy { get; set; }
    DateTime? ModifiedDate { get; set; }
    string ModifiedBy { get; set; }
}
```
엔티티에 인터페이스를 구현하고 속성 및 매핑을 추가합니다.

### `AuditEventListener` 구현

`SpringNet.Data/Listeners/AuditEventListener.cs`:
```csharp
public class AuditEventListener : IPreInsertEventListener, IPreUpdateEventListener
{
    // 실제로는 IWebUserSession을 주입받아 현재 사용자 정보를 가져와야 함
    private string GetCurrentUser() => "System"; 

    public bool OnPreInsert(PreInsertEvent e)
    {
        if (e.Entity is IAuditable auditable)
        {
            var now = DateTime.Now;
            var user = GetCurrentUser();
            auditable.CreatedDate = now;
            auditable.CreatedBy = user;
            SetState(e.Persister, e.State, "CreatedDate", now);
            SetState(e.Persister, e.State, "CreatedBy", user);
        }
        return false;
    }

    public bool OnPreUpdate(PreUpdateEvent e)
    {
        if (e.Entity is IAuditable auditable)
        {
            var now = DateTime.Now;
            var user = GetCurrentUser();
            auditable.ModifiedDate = now;
            auditable.ModifiedBy = user;
            SetState(e.Persister, e.State, "ModifiedDate", now);
            SetState(e.Persister, e.State, "ModifiedBy", user);
        }
        return false;
    }
    
    private void SetState(IEntityPersister p, object[] state, string name, object val)
    {
        var i = Array.IndexOf(p.PropertyNames, name);
        if (i != -1) state[i] = val;
    }
}
```

### Spring.NET XML로 Event Listener 등록

`dataAccess.xml`의 `sessionFactory` Bean 정의를 수정하여 Event Listener를 등록합니다.

`SpringNet.Web/Config/dataAccess.xml`:
```xml
    <!-- Audit Event Listener Bean -->
    <object id="auditEventListener" type="SpringNet.Data.Listeners.AuditEventListener, SpringNet.Data" />

    <!-- SessionFactory Bean 수정 -->
    <object id="sessionFactory" type="Spring.Data.NHibernate.LocalSessionFactoryObject, Spring.Data.NHibernate5">
        <property name="DbProvider" ref="dbProvider" />
        <property name="MappingAssemblies">
            <list>
                <value>SpringNet.Domain</value>
            </list>
        </property>
        <property name="HibernateProperties">
            <dictionary>
                <!-- ... 기존 프로퍼티들 ... -->
            </dictionary>
        </property>
        <!-- Event Listeners 등록 -->
        <property name="EventListeners">
            <dictionary>
                <entry key="pre-insert">
                    <list>
                        <ref object="auditEventListener" />
                    </list>
                </entry>
                <entry key="pre-update">
                    <list>
                        <ref object="auditEventListener" />
                    </list>
                </entry>
            </dictionary>
        </property>
    </object>
```
**참고**: 위 예시는 `LocalSessionFactoryObject`를 사용하는 방법입니다. 기존 `NHibernateHelper`를 사용하고 있다면, Helper 클래스 내에서 프로그래밍 방식으로 리스너를 등록해야 합니다. Spring.NET 환경에서는 `LocalSessionFactoryObject`를 사용하는 것이 더 유연합니다.

## 🎓 축하합니다!

Spring.NET + NHibernate 전체 튜토리얼 시리즈를 완료했습니다. 이제 기본적인 CRUD부터 고급 아키텍처 패턴까지, 견고하고 유지보수 가능한 엔터프라이즈 애플리케이션을 구축할 수 있는 탄탄한 기반을 갖추게 되었습니다.

이 튜토리얼에서 배운 개념들을 바탕으로 자신만의 프로젝트를 시작하고, 실제 문제를 해결하며 실력을 더욱 발전시켜 나가시길 바랍니다! 🚀
