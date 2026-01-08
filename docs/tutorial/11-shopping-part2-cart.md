# 11. 쇼핑몰 Part 2: 장바구니

## 📚 학습 목표

- 장바구니(Cart) 엔티티 설계
- 세션 기반 장바구니 vs DB 장바구니
- 장바구니 항목 관리
- 주문으로 전환

## 🛠️ Cart 엔티티 설계

### Cart 엔티티

```csharp
namespace SpringNet.Domain.Entities
{
    public class Cart
    {
        public virtual int Id { get; set; }
        public virtual User User { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual IList<CartItem> Items { get; set; }

        public Cart()
        {
            CreatedDate = DateTime.Now;
            Items = new List<CartItem>();
        }

        public virtual decimal GetTotalPrice()
        {
            return Items.Sum(i => i.Price * i.Quantity);
        }
    }
}
```

### CartItem 엔티티

```csharp
public class CartItem
{
    public virtual int Id { get; set; }
    public virtual Cart Cart { get; set; }
    public virtual Product Product { get; set; }
    public virtual int Quantity { get; set; }
    public virtual decimal Price { get; set; } // 가격 변동 방지

    public virtual decimal GetSubtotal()
    {
        return Price * Quantity;
    }
}
```

## 📦 CartService

```csharp
public interface ICartService
{
    void AddToCart(int userId, int productId, int quantity);
    void UpdateQuantity(int userId, int productId, int quantity);
    void RemoveFromCart(int userId, int productId);
    CartDto GetCart(int userId);
    void ClearCart(int userId);
}
```

## 💡 핵심 정리

### 장바구니 방식

- **세션 기반**: 임시, 빠름, 로그아웃 시 사라짐
- **DB 기반**: 영구, 여러 기기에서 동기화

## 🚀 다음 단계

다음: **[12-shopping-part3-order.md](./12-shopping-part3-order.md)** - 주문 처리
