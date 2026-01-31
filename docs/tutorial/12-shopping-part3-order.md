# 12. 쇼핑몰 Part 3: 주문 처리

## 📚 학습 목표

- 주문(`Order`) 및 주문 항목(`OrderItem`) 엔티티 설계
- NHibernate를 사용한 주문 및 주문 항목 매핑
- 주문 생성, 조회, 취소 등의 비즈니스 로직을 포함하는 Service 계층 구현
- 복잡한 비즈니스 로직(장바구니에서 주문 생성, 재고 관리)에 대한 트랜잭션 처리
- 주문 상태 관리 및 DTO를 통한 데이터 전송

## 🎯 주문 처리의 복잡성

주문 처리 과정은 재고 관리, 결제 연동, 배송 정보 등 다양한 비즈니스 규칙과 연관되어 있어 높은 수준의 트랜잭션 관리가 요구됩니다. 특히 **장바구니 비우기, 재고 차감, 주문 생성**은 모두 하나의 원자적인 작업으로 처리되어야 합니다.

## 🛠️ 엔티티 설계

### Step 1: Order 엔티티 생성

주문 정보를 담는 메인 엔티티입니다.

`SpringNet.Domain/Entities/Order.cs` 파일을 생성합니다.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Domain.Entities
{
    public class Order
    {
        public virtual int Id { get; set; }
        public virtual User User { get; set; } // 주문한 사용자
        public virtual DateTime OrderDate { get; set; }
        public virtual string Status { get; set; } // Pending, Paid, Shipped, Completed, Cancelled
        public virtual decimal TotalPrice { get; set; }
        public virtual IList<OrderItem> Items { get; set; }

        // 배송 정보
        public virtual string ShippingAddress { get; set; }
        public virtual string ReceiverName { get; set; }
        public virtual string ReceiverPhone { get; set; }

        public Order()
        {
            OrderDate = DateTime.Now;
            Status = "Pending"; // 초기 상태
            Items = new List<OrderItem>();
        }

        public virtual void CalculateTotalPrice()
        {
            TotalPrice = Items.Sum(item => item.GetSubtotal());
        }

        public virtual void UpdateStatus(string newStatus)
        {
            // 실제 구현에서는 상태 전이 규칙(State Transition Rule)을 적용할 수 있습니다.
            // 예: "Pending" -> "Paid" -> "Shipped" -> "Completed"
            Status = newStatus;
        }
    }
}
```

### Step 2: OrderItem 엔티티 생성

주문 내역의 각 상품을 나타내는 엔티티입니다. 장바구니 항목과 유사하게 주문 시점의 가격을 저장합니다.

`SpringNet.Domain/Entities/OrderItem.cs` 파일을 생성합니다.

```csharp
namespace SpringNet.Domain.Entities
{
    public class OrderItem
    {
        public virtual int Id { get; set; }
        public virtual Order Order { get; set; } // 소속된 주문
        public virtual Product Product { get; set; } // 주문된 상품
        public virtual int Quantity { get; set; }
        public virtual decimal Price { get; set; } // 주문 시점의 가격

        public virtual decimal GetSubtotal()
        {
            return Price * Quantity;
        }
    }
}
```

## 📝 NHibernate 매핑 설정

### Step 3: Order 매핑 파일

`SpringNet.Data/Mappings/Order.hbm.xml` 파일을 생성합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="Order" table="Orders">
        <id name="Id">
            <generator class="identity" />
        </id>

        <many-to-one name="User" column="UserId" class="User"
                     not-null="true" cascade="none" />
        
        <property name="OrderDate" column="OrderDate" type="datetime" not-null="true" />
        <property name="Status" column="Status" type="string" length="50" not-null="true" />
        <property name="TotalPrice" column="TotalPrice" type="decimal" not-null="true" />

        <property name="ShippingAddress" column="ShippingAddress" type="string" length="500" />
        <property name="ReceiverName" column="ReceiverName" type="string" length="100" />
        <property name="ReceiverPhone" column="ReceiverPhone" type="string" length="50" />

        <!-- OrderItem과의 One-to-Many 관계 -->
        <bag name="Items" inverse="true" cascade="all-delete-orphan" lazy="true">
            <key column="OrderId" />
            <one-to-many class="OrderItem" />
        </bag>
    </class>
</hibernate-mapping>
```

### Step 4: OrderItem 매핑 파일

`SpringNet.Data/Mappings/OrderItem.hbm.xml` 파일을 생성합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="OrderItem" table="OrderItems">
        <id name="Id">
            <generator class="identity" />
        </id>

        <property name="Quantity" column="Quantity" type="int" not-null="true" />
        <property name="Price" column="Price" type="decimal" not-null="true" />

        <!-- Order와의 Many-to-One 관계 -->
        <many-to-one name="Order" column="OrderId" class="Order" not-null="true" />

        <!-- Product와의 Many-to-One 관계 -->
        <many-to-one name="Product" column="ProductId" class="Product" not-null="true" cascade="none" />
    </class>
</hibernate-mapping>
```

## 📦 Repository 패턴 구현

### Step 5: IOrderRepository 인터페이스 및 구현

`IOrderRepository`는 주문 관련 데이터 액세스 기능을 정의합니다.

`SpringNet.Data/Repositories/IOrderRepository.cs`:
```csharp
using SpringNet.Domain.Entities;
using System.Collections.Generic;

namespace SpringNet.Data.Repositories
{
    public interface IOrderRepository : IRepository<Order>
    {
        IList<Order> GetOrdersByUserId(int userId);
        Order GetOrderWithItems(int orderId);
    }
}
```

`SpringNet.Data/Repositories/OrderRepository.cs`:
```csharp
using NHibernate;
using NHibernate.Linq;
using SpringNet.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Data.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        public IList<Order> GetOrdersByUserId(int userId)
        {
            return CurrentSession.Query<Order>()
                .Where(o => o.User.Id == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
        }

        public Order GetOrderWithItems(int orderId)
        {
            // FetchMany를 사용하여 주문 조회 시 항목(Items)과 상품(Product) 정보를 함께 Eager Loading
            return CurrentSession.Query<Order>()
                .Where(o => o.Id == orderId)
                .FetchMany(o => o.Items)
                .ThenFetch(oi => oi.Product)
                .FirstOrDefault();
        }
    }
}
```

## 📦 Service Layer 구현

### Step 6: DTO 클래스 추가

`SpringNet.Service/DTOs/ShoppingDto.cs` 파일에 주문 관련 DTO들을 추가합니다.

```csharp
// 기존 DTO 아래에 추가

// 주문 요청 DTO
public class OrderRequestDto
{
    public int UserId { get; set; }
    public string ShippingAddress { get; set; }
    public string ReceiverName { get; set; }
    public string ReceiverPhone { get; set; }
}

// 주문 항목 DTO
public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

// 주문 요약 DTO (목록 조회용)
public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; }
    public decimal TotalPrice { get; set; }
    public int ItemCount { get; set; }
}

// 주문 상세 DTO (단건 조회용)
public class OrderDetailDto : OrderDto
{
    public string ShippingAddress { get; set; }
    public string ReceiverName { get; set; }
    public string ReceiverPhone { get; set; }
    public IList<OrderItemDto> Items { get; set; }
}
```

### Step 7: IOrderService 인터페이스 및 구현

`IOrderService`는 주문의 핵심 비즈니스 로직을 정의합니다.

`SpringNet.Service/IOrderService.cs`:
```csharp
using SpringNet.Service.DTOs;
using System.Collections.Generic;

namespace SpringNet.Service
{
    public interface IOrderService
    {
        int CreateOrderFromCart(OrderRequestDto request);
        OrderDetailDto GetOrderDetails(int orderId);
        IList<OrderDto> GetUserOrders(int userId);
        void CancelOrder(int orderId, int userId); // 사용자 검증 추가
        void UpdateOrderStatus(int orderId, string newStatus); // 관리자용
    }
}
```

`SpringNet.Service/OrderService.cs`:
```csharp
using NHibernate;
using SpringNet.Data.Repositories;
using SpringNet.Domain.Entities;
using SpringNet.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Service
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository orderRepository;
        private readonly ICartRepository cartRepository;
        private readonly IUserRepository userRepository; // User 정보 조회를 위해 추가
        private readonly IProductRepository productRepository; // Product 재고 관리를 위해 추가
        private readonly ISessionFactory sessionFactory;

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

        public int CreateOrderFromCart(OrderRequestDto request)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
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
                        
                        // 재고 차감
                        product.Stock -= cartItem.Quantity;
                        productRepository.Update(product); // Product 엔티티의 변경 사항을 저장
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
                            Price = cartItem.Price // 장바구니에 저장된 시점의 가격 사용
                        });
                    }

                    order.CalculateTotalPrice(); // 총 가격 계산

                    // 5. 주문 저장
                    orderRepository.Add(order);

                    // 6. 장바구니 비우기
                    cart.Items.Clear();
                    cart.ModifiedDate = DateTime.Now;
                    cartRepository.Update(cart); // 장바구니 변경 사항 저장 (또는 명시적 SaveOrUpdate)

                    tx.Commit();
                    return order.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public OrderDetailDto GetOrderDetails(int orderId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var order = orderRepository.GetOrderWithItems(orderId);
                tx.Commit();
                return order == null ? null : MapToOrderDetailDto(order);
            }
        }

        public IList<OrderDto> GetUserOrders(int userId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var orders = orderRepository.GetOrdersByUserId(userId);
                tx.Commit();
                return orders.Select(MapToOrderDto).ToList();
            }
        }

        public void CancelOrder(int orderId, int userId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var order = orderRepository.GetById(orderId);
                    if (order == null) throw new ArgumentException("주문을 찾을 수 없습니다.");
                    if (order.User.Id != userId) throw new UnauthorizedAccessException("주문 취소 권한이 없습니다.");
                    if (order.Status == "Cancelled" || order.Status == "Completed")
                        throw new InvalidOperationException("이미 취소되었거나 완료된 주문은 취소할 수 없습니다.");

                    // 1. 재고 복구
                    foreach (var item in order.Items)
                    {
                        var product = productRepository.GetById(item.Product.Id);
                        if (product != null)
                        {
                            product.Stock += item.Quantity;
                            productRepository.Update(product);
                        }
                    }

                    // 2. 주문 상태 변경
                    order.UpdateStatus("Cancelled");
                    orderRepository.Update(order);

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void UpdateOrderStatus(int orderId, string newStatus)
        {
            // 이 메서드는 관리자 권한으로만 호출되어야 합니다.
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var order = orderRepository.GetById(orderId);
                    if (order == null) throw new ArgumentException("주문을 찾을 수 없습니다.");

                    order.UpdateStatus(newStatus);
                    orderRepository.Update(order);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        #region DTO Mappers
        private OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                UserId = order.User.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalPrice = order.TotalPrice,
                ItemCount = order.Items.Count
            };
        }

        private OrderDetailDto MapToOrderDetailDto(Order order)
        {
            return new OrderDetailDto
            {
                Id = order.Id,
                UserId = order.User.Id,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalPrice = order.TotalPrice,
                ItemCount = order.Items.Count,
                ShippingAddress = order.ShippingAddress,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                Items = order.Items.Select(MapToOrderItemDto).ToList()
            };
        }

        private OrderItemDto MapToOrderItemDto(OrderItem item)
        {
            return new OrderItemDto
            {
                Id = item.Id,
                ProductId = item.Product.Id,
                ProductName = item.Product.Name,
                ProductImageUrl = item.Product.ImageUrl,
                Quantity = item.Quantity,
                UnitPrice = item.Price,
                Subtotal = item.GetSubtotal()
            };
        }
        #endregion
    }
}
```

## 📢 프로젝트 파일 및 Spring.NET 설정

### Step 8: 프로젝트 파일 업데이트

1.  **`SpringNet.Domain.csproj`**
    - `Order.cs`와 `OrderItem.cs`를 `<Compile>` 아이템으로 추가합니다.
    ```xml
    <ItemGroup>
      ...
      <Compile Include="Entities\Order.cs" />
      <Compile Include="Entities\OrderItem.cs" />
      ...
    </ItemGroup>
    ```

2.  **`SpringNet.Data.csproj`**
    - 매핑 파일들을 `<EmbeddedResource>`로 추가하고, Repository를 `<Compile>` 아이템으로 추가합니다.
    ```xml
    <ItemGroup>
      ...
      <EmbeddedResource Include="Mappings\Order.hbm.xml" />
      <EmbeddedResource Include="Mappings\OrderItem.hbm.xml" />
      ...
    </ItemGroup>
    <ItemGroup>
      ...
      <Compile Include="Repositories\OrderRepository.cs" />
      <Compile Include="Repositories\IOrderRepository.cs" />
      ...
    </ItemGroup>
    ```

3.  **`SpringNet.Service.csproj`**
    - `IOrderService.cs`와 `OrderService.cs`를 `<Compile>` 아이템으로 추가합니다.
    ```xml
    <ItemGroup>
      ...
      <Compile Include="OrderService.cs" />
      <Compile Include="IOrderService.cs" />
      ...
    </ItemGroup>
    ```

### Step 9: `applicationContext.xml` 설정

`SpringNet.Web/Config/dataAccess.xml`과 `SpringNet.Web/Config/services.xml`에 새로운 Repository와 Service를 Bean으로 등록합니다.

**`dataAccess.xml`에 추가**:
```xml
    <!-- ... 기존 Repository Bean 설정 ... -->

    <!-- Order Repository -->
    <object id="orderRepository"
            type="SpringNet.Data.Repositories.OrderRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```

**`services.xml`에 추가**:
```xml
    <!-- ... 기존 Service Bean 설정 ... -->

    <!-- Order Service -->
    <object id="orderService"
            type="SpringNet.Service.OrderService, SpringNet.Service">
        <constructor-arg ref="orderRepository" />
        <constructor-arg ref="cartRepository" />
        <constructor-arg ref="userRepository" />
        <constructor-arg ref="productRepository" />
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```
**참고**: `OrderService`는 `IUserRepository`, `ICartRepository`, `IProductRepository`에도 의존하므로, 생성자 인자로 함께 주입해줍니다. 이들 리포지토리는 이미 `dataAccess.xml`에 Bean으로 등록되었다고 가정합니다.

## 💡 핵심 정리

- **복합적인 트랜잭션**: `CreateOrderFromCart` 메서드에서 재고 차감, 주문 생성, 장바구니 비우기 등 여러 단계의 작업을 하나의 트랜잭션으로 처리하여 데이터의 일관성과 무결성을 보장했습니다.
- **재고 관리**: 주문 시점에 상품 재고를 확인하고 차감하며, 주문 취소 시 재고를 복구하는 로직을 구현했습니다.
- **주문 상태**: 주문의 생명 주기를 관리하기 위한 `Status` 속성과 `UpdateStatus` 메서드를 제공했습니다.
- **Eager Loading**: `OrderRepository`에서 `FetchMany`/`ThenFetch`를 사용하여 주문 상세 조회 시 `OrderItem` 및 `Product` 정보를 한 번의 쿼리로 가져와 N+1 문제를 방지했습니다.

## 🚀 다음 단계

다음: **[13-transaction-management.md](./13-transaction-management.md)** - 트랜잭션 심화
