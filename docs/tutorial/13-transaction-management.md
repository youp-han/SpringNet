# 13. 트랜잭션 관리

## 📚 학습 목표

- 트랜잭션 ACID 속성 이해
- Spring.NET 선언적 트랜잭션
- 트랜잭션 전파 (Propagation)
- 격리 수준 (Isolation Level)

## 💡 ACID 속성

- **Atomicity (원자성)**: 전체 성공 또는 전체 실패
- **Consistency (일관성)**: 데이터 무결성 유지
- **Isolation (격리성)**: 동시 트랜잭션 독립
- **Durability (지속성)**: 커밋 후 영구 저장

## 🔧 Spring.NET 선언적 트랜잭션

### hibernate.cfg.xml 설정

```xml
<property name="current_session_context_class">
    Spring.Data.NHibernate.SpringSessionContext, Spring.Data.NHibernate
</property>
```

### applicationContext.xml 설정

```xml
<!-- Transaction Manager -->
<object id="transactionManager"
        type="Spring.Data.NHibernate.HibernateTransactionManager, Spring.Data.NHibernate">
    <property name="SessionFactory" ref="sessionFactory" />
</object>

<!-- Transaction Attribute Source -->
<tx:attribute-driven transaction-manager="transactionManager" />

<!-- Service with Transaction -->
<object id="orderService"
        type="SpringNet.Service.OrderService, SpringNet.Service">
    <constructor-arg ref="orderRepository" />
    <constructor-arg ref="productRepository" />
    <constructor-arg ref="cartRepository" />
</object>
```

### Service에 Attribute 적용

```csharp
using Spring.Transaction.Interceptor;

namespace SpringNet.Service
{
    public class OrderService : IOrderService
    {
        [Transaction]
        public int CreateOrder(int userId, OrderRequestDto request)
        {
            // 트랜잭션 자동 관리!
            // - 메서드 시작 시 트랜잭션 시작
            // - 메서드 정상 종료 시 커밋
            // - 예외 발생 시 롤백
        }

        [Transaction(ReadOnly = true)]
        public OrderDto GetOrder(int id)
        {
            // 읽기 전용 트랜잭션 (성능 최적화)
        }
    }
}
```

## 📊 트랜잭션 전파 (Propagation)

```csharp
// REQUIRED (기본값): 기존 트랜잭션 있으면 참여, 없으면 새로 생성
[Transaction(TransactionPropagation.Required)]
public void Method1() { }

// REQUIRES_NEW: 항상 새 트랜잭션 생성
[Transaction(TransactionPropagation.RequiresNew)]
public void Method2() { }

// SUPPORTS: 트랜잭션 있으면 참여, 없어도 실행
[Transaction(TransactionPropagation.Supports)]
public void Method3() { }

// MANDATORY: 트랜잭션 필수 (없으면 예외)
[Transaction(TransactionPropagation.Mandatory)]
public void Method4() { }

// NEVER: 트랜잭션 금지 (있으면 예외)
[Transaction(TransactionPropagation.Never)]
public void Method5() { }
```

## 🔒 격리 수준 (Isolation Level)

```csharp
// READ_UNCOMMITTED: 커밋 안 된 데이터 읽기 가능 (Dirty Read)
[Transaction(IsolationLevel = IsolationLevel.ReadUncommitted)]

// READ_COMMITTED: 커밋된 데이터만 읽기 (기본값)
[Transaction(IsolationLevel = IsolationLevel.ReadCommitted)]

// REPEATABLE_READ: 반복 읽기 가능
[Transaction(IsolationLevel = IsolationLevel.RepeatableRead)]

// SERIALIZABLE: 가장 높은 격리 (성능 저하)
[Transaction(IsolationLevel = IsolationLevel.Serializable)]
```

## 🎯 동시성 문제 예제

### Lost Update (갱신 손실)

```csharp
// 문제 상황
[Transaction]
public void UpdateStock(int productId)
{
    var product = repository.GetById(productId);
    product.Stock -= 1;
    repository.Update(product);
    // 두 트랜잭션이 동시에 실행되면 한 번의 차감만 반영됨!
}

// 해결: 비관적 잠금 (Pessimistic Lock)
[Transaction]
public void UpdateStockWithLock(int productId)
{
    var product = session.Get<Product>(productId, LockMode.Upgrade);
    product.Stock -= 1;
    session.Update(product);
}

// 해결: 낙관적 잠금 (Optimistic Lock)
public class Product
{
    public virtual int Version { get; set; } // NHibernate가 자동 관리

    <version name="Version" column="Version" />
}
```

## 💡 핵심 정리

### 트랜잭션 사용 시기

✅ **데이터 변경 작업** (INSERT, UPDATE, DELETE)
✅ **여러 테이블 동시 변경**
✅ **일관성이 중요한 작업**

### 트랜잭션 미사용 시기

- 단순 조회 (SELECT)
- 읽기 전용 작업

### 베스트 프랙티스

✅ 트랜잭션 범위 최소화
✅ 읽기 전용은 `ReadOnly = true`
✅ 긴 작업은 트랜잭션 분리
✅ 예외 처리 명확히

## 🚀 다음 단계

다음: **[14-best-practices.md](./14-best-practices.md)** - 베스트 프랙티스
