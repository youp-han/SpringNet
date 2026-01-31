## 📚 학습 목표

- 트랜잭션 ACID 속성 이해
- Spring.NET 선언적 트랜잭션 설정 및 사용
- `[Transaction]` 속성 활용
- 트랜잭션 전파 (`Propagation`) 및 격리 수준 (`Isolation Level`) 이해
- 동시성 문제 (Lost Update, Dirty Read 등) 및 해결 방안 (Optimistic/Pessimistic Locking)

## 💡 ACID 속성

트랜잭션은 데이터베이스 시스템에서 데이터의 일관성과 신뢰성을 보장하기 위한 일련의 작업 단위입니다. 다음 네 가지 속성을 만족해야 합니다.

-   **Atomicity (원자성)**: 트랜잭션 내의 모든 작업은 전부 성공하거나 전부 실패하여 롤백되어야 합니다. (All or Nothing)
-   **Consistency (일관성)**: 트랜잭션이 성공적으로 완료되면 데이터베이스는 항상 일관된 상태를 유지해야 합니다.
-   **Isolation (격리성)**: 여러 트랜잭션이 동시에 실행될 때, 각 트랜잭션은 마치 다른 트랜잭션의 영향을 받지 않고 단독으로 실행되는 것처럼 동작해야 합니다.
-   **Durability (지속성)**: 트랜잭션이 한 번 커밋되면, 시스템 오류가 발생하더라도 해당 변경 내용은 영구적으로 보존되어야 합니다.

## 🔧 Spring.NET 선언적 트랜잭션

이전 튜토리얼들에서는 `using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())` 형태의 코드를 사용하여 **프로그래밍 방식(수동) 트랜잭션**을 처리했습니다. 하지만 Spring.NET은 AOP(Aspect-Oriented Programming) 기반의 **선언적 트랜잭션 관리**를 제공하여 비즈니스 로직과 트랜잭션 코드를 분리하고, 코드를 훨씬 간결하게 만들 수 있습니다.

선언적 트랜잭션을 사용하려면 다음 단계를 따릅니다.

### Step 1: `hibernate.cfg.xml` 설정 업데이트

`Spring.Data.NHibernate` 어셈블리의 `SpringSessionContext`를 사용하여 Spring이 NHibernate 세션의 라이프사이클을 관리하도록 합니다. 이 방식은 Spring.NET의 선언적 트랜잭션을 위해 권장됩니다.

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

### Step 2: 필요한 NuGet 패키지 설치

선언적 트랜잭션을 사용하기 위해서는 `Spring.Transaction.Interceptor` 패키지가 필요합니다. `SpringNet.Service` 프로젝트에 설치합니다.

```
PM> Install-Package Spring.Transaction.Interceptor
```

### Step 3: `applicationContext.xml` 설정

트랜잭션 매니저를 정의하고, `@Transaction` 속성을 활성화합니다.

`SpringNet.Web/Config/services.xml` 또는 `applicationContext.xml` 파일을 열어 다음 내용을 추가합니다. (이전 튜토리얼에서 `applicationContext.xml`이 여러 파일로 분리되었다고 가정합니다.)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<objects xmlns="http://www.springframework.net"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xmlns:tx="http://www.springframework.net/tx"
         xsi:schemaLocation="http://www.springframework.net
         http://www.springframework.net/xsd/spring/spring-objects.xsd
         http://www.springframework.net/tx
         http://www.springframework.net/xsd/spring/spring-tx.xsd">

    <!-- Transaction Manager: NHibernate 세션 팩토리를 사용하여 트랜잭션 관리 -->
    <object id="transactionManager"
            type="Spring.Data.NHibernate.HibernateTransactionManager, Spring.Data.NHibernate">
        <property name="SessionFactory" ref="sessionFactory" />
    </object>

    <!-- @Transaction 속성을 사용하여 선언적 트랜잭션을 활성화 -->
    <tx:attribute-driven transaction-manager="transactionManager" />

    <!--
        이제 서비스 빈들을 정의할 때, 의존성 주입은 동일하게 유지하되
        해당 서비스 클래스나 메서드에 [Transaction] 속성을 적용할 수 있습니다.
        예: OrderService는 여러 Repository와 SessionFactory에 의존합니다.
        (이전 튜토리얼에서 OrderService는 services.xml에 정의되었습니다.)
    -->
    <object id="orderService"
            type="SpringNet.Service.OrderService, SpringNet.Service">
        <constructor-arg ref="orderRepository" />
        <constructor-arg ref="cartRepository" />
        <constructor-arg ref="userRepository" />
        <constructor-arg ref="productRepository" />
        <constructor-arg ref="sessionFactory" />
    </object>

    <!-- 다른 서비스 빈들도 유사하게 정의 -->

</objects>
```
**참고**: `xmlns:tx` 및 `xsi:schemaLocation`에 `spring-tx.xsd`를 추가해야 `tx:attribute-driven`을 사용할 수 있습니다.

### Step 4: Service에 `[Transaction]` 속성 적용

이제 서비스 클래스나 메서드에 `[Transaction]` 속성을 적용하여 트랜잭션 동작을 선언할 수 있습니다. Spring.NET AOP가 이 속성을 가로채어 트랜잭션을 자동으로 시작하고 커밋/롤백합니다.

`SpringNet.Service/OrderService.cs`를 열어 다음 내용을 참고하여 트랜잭션 코드를 수정합니다.

```csharp
using NHibernate;
using Spring.Transaction.Interceptor; // [Transaction] 속성을 위해 필요
using SpringNet.Data.Repositories;
using SpringNet.Domain.Entities;
using SpringNet.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Service
{
    // 클래스 레벨에 적용하여 모든 public 메서드에 트랜잭션 적용 가능
    [Transaction]
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository orderRepository;
        private readonly ICartRepository cartRepository;
        private readonly IUserRepository userRepository;
        private readonly IProductRepository productRepository;
        private readonly ISessionFactory sessionFactory; // 수동 세션 관리가 필요할 경우 대비

        public OrderService(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IUserRepository userRepository,
            IProductRepository productRepository,
            ISessionFactory sessionFactory)
        {
            this.orderRepository = orderRepository;
            this.cartRepository = cartRepository;
            this.userRepository = userRepository;
            this.productRepository = productRepository;
            this.sessionFactory = sessionFactory;
        }

        [Transaction] // 개별 메서드에 적용 (클래스 레벨 속성보다 우선순위 높음)
        public int CreateOrderFromCart(OrderRequestDto request)
        {
            // 더 이상 수동으로 BeginTransaction(), Commit(), Rollback()을 호출할 필요가 없습니다.
            // Spring.NET이 [Transaction] 속성을 보고 트랜잭션을 관리해줍니다.

            // 1. 사용자 및 장바구니 유효성 검사
            var user = userRepository.GetById(request.UserId);
            if (user == null) throw new ArgumentException("유효하지 않은 사용자입니다.");

            var cart = cartRepository.GetByUserId(request.UserId);
            if (cart == null || !cart.Items.Any())
                throw new InvalidOperationException("장바구니가 비어있습니다.");

            // 2. 재고 확인 및 재고 차감 (트랜잭션 내에서 처리)
            foreach (var cartItem in cart.Items)
            {
                var product = productRepository.GetById(cartItem.Product.Id);
                if (product == null || !product.IsInStock() || product.Stock < cartItem.Quantity)
                    throw new InvalidOperationException(
                        $"'{cartItem.Product.Name}' 상품의 재고가 부족하거나 유효하지 않습니다.");
                
                product.Stock -= cartItem.Quantity;
                productRepository.Update(product); // 변경 추적으로 인해 명시적 Update는 불필요할 수 있으나, 안전을 위해 호출
            }

            // 3. 주문 생성
            var order = new Order
            {
                User = user,
                ShippingAddress = request.ShippingAddress,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.ReceiverPhone,
                OrderDate = DateTime.Now,
                Status = "Pending" // 초기 주문 상태
            };

            // 4. 주문 항목 생성
            foreach (var cartItem in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    Order = order,
                    Product = cartItem.Product,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Price
                });
            }

            order.CalculateTotalPrice();

            // 5. 주문 저장
            orderRepository.Add(order);

            // 6. 장바구니 비우기
            cart.Items.Clear();
            cart.ModifiedDate = DateTime.Now;
            cartRepository.Update(cart);

            return order.Id;
        }

        [Transaction(ReadOnly = true)] // 읽기 전용 트랜잭션, 성능 최적화
        public OrderDetailDto GetOrderDetails(int orderId)
        {
            var order = orderRepository.GetOrderWithItems(orderId);
            return order == null ? null : MapToOrderDetailDto(order);
        }
        
        // ... 다른 OrderService 메서드들도 [Transaction] 속성으로 변경 ...
        // 예를 들어, GetUserOrders, CancelOrder 등도 [Transaction] 속성을 적용하고
        // 기존의 using(var tx = ...) 코드를 제거합니다.
        // GetUserOrders는 ReadOnly = true로 설정하는 것이 좋습니다.
    }
}
```

**참고**: `Repository<T>`의 `Add`, `Update`, `Delete` 메서드는 이미 내부적으로 NHibernate의 세션을 사용하고 있으며, 선언적 트랜잭션 하에서는 현재 트랜잭션에 참여하게 됩니다. 따라서 서비스 계층에서 명시적으로 `session.Save(entity)`, `session.Update(entity)` 등을 호출할 필요가 없습니다. 하지만 변경 추적(dirty checking)이 동작하더라도 안전을 위해 `Update`를 호출하는 것을 예제에서는 유지합니다.

## 📊 트랜잭션 전파 (Propagation)

`[Transaction]` 속성은 `TransactionPropagation` 옵션을 통해 트랜잭션의 전파 방식을 제어할 수 있습니다. 이는 한 트랜잭션이 이미 진행 중일 때 다른 트랜잭션 메서드가 호출될 경우 어떻게 동작할지 정의합니다.

-   `Required` (기본값): 기존 트랜잭션이 있으면 참여하고, 없으면 새로운 트랜잭션을 생성합니다. 대부분의 경우에 적합합니다.
    ```csharp
    [Transaction(TransactionPropagation.Required)]
    public void Method1() { }
    ```
-   `RequiresNew`: 항상 새로운 트랜잭션을 생성합니다. 기존 트랜잭션이 있다면 잠시 중단시키고 새 트랜잭션을 실행합니다.
    ```csharp
    [Transaction(TransactionPropagation.RequiresNew)]
    public void Method2() { }
    ```
-   `Supports`: 기존 트랜잭션이 있으면 참여하고, 없어도 트랜잭션 없이 실행됩니다. 읽기 전용 작업에 유용합니다.
    ```csharp
    [Transaction(TransactionPropagation.Supports)]
    public void Method3() { }
    ```
-   `Mandatory`: 기존 트랜잭션이 반드시 있어야 합니다. 없으면 예외를 발생시킵니다.
    ```csharp
    [Transaction(TransactionPropagation.Mandatory)]
    public void Method4() { }
    ```
-   `NotSupported`: 트랜잭션 없이 실행됩니다. 기존 트랜잭션이 있다면 잠시 중단시킵니다.
    ```csharp
    [Transaction(TransactionPropagation.NotSupported)]
    public void Method5() { }
    ```
-   `Never`: 트랜잭션 없이 실행되어야 합니다. 기존 트랜잭션이 있다면 예외를 발생시킵니다.
    ```csharp
    [Transaction(TransactionPropagation.Never)]
    public void Method6() { }
    ```

## 🔒 격리 수준 (Isolation Level)

`[Transaction]` 속성은 `IsolationLevel` 옵션을 통해 트랜잭션의 격리 수준을 설정할 수 있습니다. 이는 동시성 문제(Dirty Read, Non-Repeatable Read, Phantom Read)를 얼마나 허용할지 정의합니다.

-   `ReadUncommitted`: 커밋되지 않은 데이터도 읽을 수 있습니다. (가장 낮은 격리, Dirty Read 발생 가능)
    ```csharp
    [Transaction(IsolationLevel = IsolationLevel.ReadUncommitted)]
    ```
-   `ReadCommitted` (대부분 DB의 기본값): 커밋된 데이터만 읽을 수 있습니다. (Dirty Read 방지, Non-Repeatable Read, Phantom Read 발생 가능)
    ```csharp
    [Transaction(IsolationLevel = IsolationLevel.ReadCommitted)]
    ```
-   `RepeatableRead`: 트랜잭션 내에서 한 번 읽은 데이터는 다시 읽어도 항상 같은 값을 반환합니다. (Dirty Read, Non-Repeatable Read 방지, Phantom Read 발생 가능)
    ```csharp
    [Transaction(IsolationLevel = IsolationLevel.RepeatableRead)]
    ```
-   `Serializable`: 가장 높은 격리 수준으로, 트랜잭션을 순차적으로 실행하는 것과 같은 효과를 냅니다. 모든 동시성 문제를 방지하지만, 성능 저하가 가장 큽니다.
    ```csharp
    [Transaction(IsolationLevel = IsolationLevel.Serializable)]
    ```

## 🎯 동시성 문제 및 해결 방안

여러 트랜잭션이 동시에 데이터를 조작할 때 발생할 수 있는 일반적인 동시성 문제와 해결책입니다.

### 1. Lost Update (갱신 손실)

두 개 이상의 트랜잭션이 같은 데이터를 읽고 수정한 후, 마지막에 커밋된 트랜잭션의 변경 사항만 반영되고 이전 트랜잭션의 변경 사항이 손실되는 문제입니다.

**문제 상황 예시**:
```csharp
// (Service Layer 메서드)
[Transaction] // 트랜잭션 A, B가 동시에 이 메서드를 호출
public void UpdateStock(int productId)
{
    var product = productRepository.GetById(productId); // A: 재고 100 읽음, B: 재고 100 읽음
    product.Stock -= 10;                               // A: 재고 90으로 변경, B: 재고 90으로 변경
    productRepository.Update(product);                 // A: 재고 90으로 커밋, B: 재고 90으로 커밋 -> 최종 재고 90 (기대값: 80)
}
```

**해결 방안**:

-   **Pessimistic Locking (비관적 잠금)**:
    데이터를 읽을 때 다른 트랜잭션이 접근하지 못하도록 잠금을 설정합니다. NHibernate에서는 `LockMode`를 사용하여 구현할 수 있습니다.
    ```csharp
    // (Service Layer 메서드)
    [Transaction]
    public void UpdateStockWithPessimisticLock(int productId)
    {
        // LockMode.Upgrade (SQL의 SELECT ... FOR UPDATE)를 사용하여 배타적 잠금 획득
        var product = sessionFactory.GetCurrentSession().Get<Product>(productId, LockMode.Upgrade);
        if (product == null) throw new ArgumentException("상품을 찾을 수 없습니다.");

        product.Stock -= 10;
        productRepository.Update(product); // 트랜잭션 커밋 시 잠금 해제
    }
    ```
    **참고**: `Repository<T>`는 기본적으로 `LockMode`를 제공하지 않습니다. 위 예시처럼 `GetCurrentSession().Get<T>(id, LockMode)`를 직접 사용하거나, `IRepository` 인터페이스에 `GetByIdWithLock(int id, LockMode lockMode)`와 같은 메서드를 추가하여 캡슐화할 수 있습니다.

-   **Optimistic Locking (낙관적 잠금)**:
    데이터를 읽을 때 잠금을 걸지 않고, 업데이트 시점에 다른 트랜잭션에 의해 데이터가 변경되었는지 확인합니다. 주로 `Version` 컬럼이나 `Timestamp` 컬럼을 사용하여 구현합니다. NHibernate는 엔티티에 `Version` 프로퍼티를 추가하고 매핑 파일에 `<version>` 태그를 설정하면 자동 지원합니다.

    `SpringNet.Domain/Entities/Product.cs`에 `Version` 속성 추가:
    ```csharp
    public class Product
    {
        // ... 기존 속성 ...
        public virtual int Version { get; set; } // 낙관적 잠금을 위한 버전 필드
    }
    ```
    `SpringNet.Data/Mappings/Product.hbm.xml`에 `<version>` 태그 추가:
    ```xml
    <class name="Product" table="Products">
        <!-- ... 기존 매핑 ... -->

        <!-- 낙관적 잠금을 위한 버전 매핑 -->
        <version name="Version" column="Version" unsaved-value="null" />

    </class>
    ```
    이후 `productRepository.Update(product)` 호출 시, NHibernate는 `Version` 값을 비교하여 다른 트랜잭션에 의해 변경되었다면 `StaleObjectStateException`을 발생시킵니다.

### 2. Dirty Read (더티 읽기)

아직 커밋되지 않은 다른 트랜잭션의 변경 사항을 읽는 문제입니다.

**문제 상황 예시**:
-   트랜잭션 A: `Product` 재고를 100에서 90으로 변경 (아직 커밋 안 함).
-   트랜잭션 B: `Product` 재고를 90으로 읽고 주문 생성.
-   트랜잭션 A: 롤백하여 `Product` 재고가 다시 100이 됨.
-   결과: 트랜잭션 B는 존재하지 않는 데이터를 기반으로 잘못된 작업을 수행했습니다.

**해결 방안**:
-   `ReadCommitted` 이상의 격리 수준을 사용합니다. (`[Transaction(IsolationLevel = IsolationLevel.ReadCommitted)]`)

### 3. Non-Repeatable Read (반복 불가능 읽기)

한 트랜잭션 내에서 같은 데이터를 두 번 읽었을 때, 그 사이에 다른 트랜잭션이 데이터를 수정하고 커밋하여 두 번의 읽기 결과가 달라지는 문제입니다.

**문제 상황 예시**:
-   트랜잭션 A: `Product` 재고를 100으로 읽음.
-   트랜잭션 B: `Product` 재고를 100에서 90으로 변경하고 커밋.
-   트랜잭션 A: `Product` 재고를 다시 읽으니 90이 됨.

**해결 방안**:
-   `RepeatableRead` 이상의 격리 수준을 사용합니다. (`[Transaction(IsolationLevel = IsolationLevel.RepeatableRead)]`)

### 4. Phantom Read (환영 읽기)

한 트랜잭션 내에서 동일한 쿼리를 두 번 실행했을 때, 그 사이에 다른 트랜잭션이 새 레코드를 추가하거나 삭제하여 두 번의 쿼리 결과 집합이 달라지는 문제입니다.

**문제 상황 예시**:
-   트랜잭션 A: 특정 카테고리의 `Product` 목록 10개를 조회.
-   트랜잭션 B: 같은 카테고리에 새 `Product`를 추가하고 커밋.
-   트랜잭션 A: 같은 쿼리를 다시 실행하니 11개의 `Product`가 조회됨.

**해결 방안**:
-   `Serializable` 격리 수준을 사용합니다. (`[Transaction(IsolationLevel = IsolationLevel.Serializable)]`)

## 💡 핵심 정리

### 트랜잭션 사용 시기

✅ **데이터 변경 작업** (INSERT, UPDATE, DELETE)이 포함될 때
✅ **여러 테이블의 데이터가 동시에 변경**되어야 할 때
✅ **데이터의 일관성이 매우 중요한 작업** (예: 계좌 이체, 주문 처리)

### 트랜잭션 미사용 시기

-   단순 조회 (SELECT)
-   읽기 전용 작업 (이 경우 `[Transaction(ReadOnly = true)]`를 사용하여 성능 최적화)

### 베스트 프랙티스

✅ **선언적 트랜잭션 활용**: `[Transaction]` 속성을 사용하여 비즈니스 로직과 트랜잭션 로직을 분리하고 코드 가독성을 높입니다.
✅ **트랜잭션 범위 최소화**: 트랜잭션은 필요한 최소한의 범위 내에서만 사용하며, 불필요하게 긴 트랜잭션은 동시성을 저해하고 데드락 발생 가능성을 높입니다.
✅ **읽기 전용 트랜잭션**: 단순 조회 메서드에는 `[Transaction(ReadOnly = true)]`를 적용하여 데이터베이스 오버헤드를 줄이고 성능을 향상시킵니다.
✅ **예외 처리 명확히**: 트랜잭션 내부에서 예외 발생 시 적절한 롤백이 이루어지도록 설계합니다. Spring.NET은 언체크 예외(Runtime Exception) 발생 시 기본적으로 롤백을 수행합니다.
✅ **동시성 문제 해결**: Lost Update, Dirty Read 등 동시성 문제가 발생할 수 있는 시나리오를 식별하고, 비관적/낙관적 잠금 또는 적절한 격리 수준을 통해 해결합니다. 특히 재고 관리와 같은 민감한 작업에는 낙관적 잠금(`Version`)을 우선적으로 고려합니다.

## 🚀 다음 단계

다음: **[14-best-practices.md](./14-best-practices.md)** - 베스트 프랙티스
