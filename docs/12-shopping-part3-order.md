# 12. 쇼핑몰 Part 3: 주문 처리

## 📚 학습 목표

- 주문(Order) 엔티티 설계
- 복잡한 트랜잭션 처리
- 재고 관리
- 주문 상태 관리

## 🛠️ Order 엔티티 설계

### Order 엔티티

```csharp
namespace SpringNet.Domain.Entities
{
    public class Order
    {
        public virtual int Id { get; set; }
        public virtual User User { get; set; }
        public virtual DateTime OrderDate { get; set; }
        public virtual string Status { get; set; } // Pending, Paid, Shipped, Completed
        public virtual decimal TotalPrice { get; set; }
        public virtual IList<OrderItem> Items { get; set; }

        // 배송 정보
        public virtual string ShippingAddress { get; set; }
        public virtual string ReceiverName { get; set; }
        public virtual string ReceiverPhone { get; set; }

        public Order()
        {
            OrderDate = DateTime.Now;
            Status = "Pending";
            Items = new List<OrderItem>();
        }
    }
}
```

### OrderItem 엔티티

```csharp
public class OrderItem
{
    public virtual int Id { get; set; }
    public virtual Order Order { get; set; }
    public virtual Product Product { get; set; }
    public virtual int Quantity { get; set; }
    public virtual decimal Price { get; set; }

    public virtual decimal GetSubtotal()
    {
        return Price * Quantity;
    }
}
```

## 📦 OrderService - 트랜잭션 핵심

```csharp
public class OrderService : IOrderService
{
    public int CreateOrder(int userId, OrderRequestDto request)
    {
        using (var session = sessionFactory.OpenSession())
        using (var tx = session.BeginTransaction())
        {
            try
            {
                // 1. 장바구니 조회
                var cart = session.Query<Cart>()
                    .Fetch(c => c.Items)
                    .FirstOrDefault(c => c.User.Id == userId);

                if (cart == null || !cart.Items.Any())
                    throw new InvalidOperationException("장바구니가 비어있습니다.");

                // 2. 재고 확인
                foreach (var item in cart.Items)
                {
                    if (item.Product.Stock < item.Quantity)
                        throw new InvalidOperationException(
                            $"{item.Product.Name} 재고가 부족합니다.");
                }

                // 3. 주문 생성
                var order = new Order
                {
                    User = cart.User,
                    ShippingAddress = request.ShippingAddress,
                    ReceiverName = request.ReceiverName,
                    ReceiverPhone = request.ReceiverPhone
                };

                // 4. 주문 항목 생성 & 재고 차감
                foreach (var cartItem in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        Order = order,
                        Product = cartItem.Product,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Price
                    };

                    order.Items.Add(orderItem);

                    // 재고 차감 (중요!)
                    cartItem.Product.Stock -= cartItem.Quantity;
                    session.Update(cartItem.Product);
                }

                order.TotalPrice = order.Items.Sum(i => i.GetSubtotal());

                // 5. 저장
                session.Save(order);

                // 6. 장바구니 비우기
                foreach (var item in cart.Items.ToList())
                {
                    session.Delete(item);
                }

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
}
```

## 💡 핵심 정리

### 트랜잭션 중요성

✅ **재고 차감** - 동시성 문제 방지
✅ **원자성** - 전체 성공 또는 전체 실패
✅ **격리성** - 다른 트랜잭션과 독립

### 주문 상태 관리

- `Pending`: 결제 대기
- `Paid`: 결제 완료
- `Shipped`: 배송 중
- `Completed`: 완료
- `Cancelled`: 취소

## 🚀 다음 단계

다음: **[13-transaction-management.md](./13-transaction-management.md)** - 트랜잭션 심화
