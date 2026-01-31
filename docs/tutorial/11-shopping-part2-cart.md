# 11. 쇼핑몰 Part 2: 장바구니 기능

## 📚 학습 목표

- 장바구니(`Cart`) 및 장바구니 항목(`CartItem`) 엔티티 설계
- DB 기반 장바구니 구현을 위한 NHibernate 매핑 설정
- 장바구니 관리를 위한 Repository 및 Service 계층 구현
- DTO를 사용한 데이터 전송 및 트랜잭션 관리

## 📖 DB 기반 장바구니

이번 튜토리얼에서는 데이터베이스에 장바구니 정보를 저장하는 방식을 선택합니다. 이 방식은 사용자가 다른 브라우저나 기기에서 로그인하더라도 장바구니 내용을 유지할 수 있는 장점이 있습니다.

- **장점**: 영구성, 여러 기기 간 동기화
- **단점**: 세션 방식에 비해 DB I/O로 인한 오버헤드 발생 가능

## 🛠️ 엔티티 설계

### Step 1: Cart 엔티티 생성

사용자별 장바구니를 나타내는 엔티티입니다.

`SpringNet.Domain/Entities/Cart.cs` 파일을 생성합니다.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Domain.Entities
{
    public class Cart
    {
        public virtual int Id { get; set; }
        public virtual User User { get; set; } // 장바구니 소유자
        public virtual DateTime ModifiedDate { get; set; }
        public virtual IList<CartItem> Items { get; set; }

        public Cart()
        {
            ModifiedDate = DateTime.Now;
            Items = new List<CartItem>();
        }

        public virtual decimal GetTotalPrice()
        {
            return Items.Sum(i => i.GetSubtotal());
        }

        public virtual void AddOrUpdateItem(Product product, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.Product.Id == product.Id);
            if (item != null)
            {
                // 이미 담긴 상품이면 수량 변경
                item.Quantity = quantity;
            }
            else
            {
                // 새로운 상품이면 추가
                Items.Add(new CartItem
                {
                    Cart = this,
                    Product = product,
                    Quantity = quantity,
                    Price = product.Price // 현재 시점의 상품 가격을 저장
                });
            }
            ModifiedDate = DateTime.Now;
        }

        public virtual void RemoveItem(int productId)
        {
            var item = Items.FirstOrDefault(i => i.Product.Id == productId);
            if (item != null)
            {
                Items.Remove(item);
                ModifiedDate = DateTime.Now;
            }
        }
    }
}
```

### Step 2: CartItem 엔티티 생성

장바구니에 담긴 개별 상품을 나타냅니다.

`SpringNet.Domain/Entities/CartItem.cs` 파일을 생성합니다.

```csharp
namespace SpringNet.Domain.Entities
{
    public class CartItem
    {
        public virtual int Id { get; set; }
        public virtual Cart Cart { get; set; }
        public virtual Product Product { get; set; }
        public virtual int Quantity { get; set; }
        public virtual decimal Price { get; set; } // 가격 변동에 대비해 담는 시점의 가격을 저장

        public virtual decimal GetSubtotal()
        {
            return Price * Quantity;
        }
    }
}
```

**핵심**: `CartItem`에 `Price`를 별도로 저장하는 이유는, 상품 가격이 나중에 변동되더라도 사용자가 장바구니에 담았던 시점의 가격으로 계산되도록 보장하기 위함입니다.

## 📝 NHibernate 매핑 설정

### Step 3: Cart 매핑 파일

`SpringNet.Data/Mappings/Cart.hbm.xml` 파일을 생성합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="Cart" table="Carts">
        <id name="Id">
            <generator class="identity" />
        </id>

        <!-- User와의 One-to-One 관계 (실제로는 Unique한 Foreign Key) -->
        <many-to-one name="User" column="UserId" class="User"
                     unique="true" not-null="true" cascade="none" />
        
        <property name="ModifiedDate" column="ModifiedDate" type="datetime" not-null="true" />

        <!-- CartItem과의 One-to-Many 관계 -->
        <bag name="Items" inverse="true" cascade="all-delete-orphan">
            <key column="CartId" />
            <one-to-many class="CartItem" />
        </bag>
    </class>
</hibernate-mapping>
```

### Step 4: CartItem 매핑 파일

`SpringNet.Data/Mappings/CartItem.hbm.xml` 파일을 생성합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="CartItem" table="CartItems">
        <id name="Id">
            <generator class="identity" />
        </id>

        <property name="Quantity" column="Quantity" type="int" not-null="true" />
        <property name="Price" column="Price" type="decimal" not-null="true" />

        <!-- Cart와의 Many-to-One 관계 -->
        <many-to-one name="Cart" column="CartId" class="Cart" not-null="true" />

        <!-- Product와의 Many-to-One 관계 -->
        <many-to-one name="Product" column="ProductId" class="Product" not-null="true" />
    </class>
</hibernate-mapping>
```

## 📦 Repository 패턴 구현

### Step 5: ICartRepository 인터페이스 및 구현

`ICartRepository`는 `Cart`에 특화된 메서드를 정의합니다. `GetByUserId`는 가장 핵심적인 조회 기능입니다.

`SpringNet.Data/Repositories/ICartRepository.cs`:
```csharp
using SpringNet.Domain.Entities;

namespace SpringNet.Data.Repositories
{
    public interface ICartRepository : IRepository<Cart>
    {
        Cart GetByUserId(int userId);
    }
}
```

`SpringNet.Data/Repositories/CartRepository.cs`:
```csharp
using NHibernate;
using NHibernate.Linq;
using SpringNet.Domain.Entities;
using System.Linq;

namespace SpringNet.Data.Repositories
{
    public class CartRepository : Repository<Cart>, ICartRepository
    {
        public CartRepository(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        public Cart GetByUserId(int userId)
        {
            // FetchMany를 사용하여 장바구니 조회 시 항목(Items)과 상품(Product) 정보를 함께 Eager Loading
            return CurrentSession.Query<Cart>()
                .Where(c => c.User.Id == userId)
                .FetchMany(c => c.Items)
                .ThenFetch(ci => ci.Product)
                .FirstOrDefault();
        }
    }
}
```

## 📦 Service Layer 구현

### Step 6: DTO 클래스 추가

`SpringNet.Service/DTOs/ShoppingDto.cs` 파일에 장바구니 DTO들을 추가합니다.

```csharp
// 기존 DTO 아래에 추가
public class CartDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime ModifiedDate { get; set; }
    public decimal TotalPrice { get; set; }
    public IList<CartItemDto> Items { get; set; }
}

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}
```

### Step 7: ICartService 인터페이스 및 구현

`SpringNet.Service/ICartService.cs`:
```csharp
using SpringNet.Service.DTOs;

namespace SpringNet.Service
{
    public interface ICartService
    {
        CartDto GetCartByUserId(int userId);
        void AddToCart(int userId, int productId, int quantity);
        void UpdateCartItem(int userId, int productId, int quantity);
        void RemoveFromCart(int userId, int productId);
        void ClearCart(int userId);
    }
}
```

`SpringNet.Service/CartService.cs`:
```csharp
using NHibernate;
using SpringNet.Data.Repositories;
using SpringNet.Domain.Entities;
using SpringNet.Service.DTOs;
using System;
using System.Linq;

namespace SpringNet.Service
{
    public class CartService : ICartService
    {
        private readonly ICartRepository cartRepository;
        private readonly IUserRepository userRepository; // User 정보 조회를 위해 추가
        private readonly IProductRepository productRepository; // Product 정보 조회를 위해 추가
        private readonly ISessionFactory sessionFactory;

        public CartService(
            ICartRepository cartRepository,
            IUserRepository userRepository,
            IProductRepository productRepository,
            ISessionFactory sessionFactory)
        {
            this.cartRepository = cartRepository;
            this.userRepository = userRepository;
            this.productRepository = productRepository;
            this.sessionFactory = sessionFactory;
        }
        
        // 비공개 헬퍼 메서드: 사용자 ID로 장바구니를 가져오거나 없으면 새로 생성
        private Cart GetOrCreateCartByUserId(int userId)
        {
            var cart = cartRepository.GetByUserId(userId);
            if (cart == null)
            {
                var user = userRepository.GetById(userId);
                if (user == null) throw new ArgumentException("존재하지 않는 사용자입니다.");
                
                cart = new Cart { User = user };
                cartRepository.Add(cart);
            }
            return cart;
        }

        public CartDto GetCartByUserId(int userId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var cart = cartRepository.GetByUserId(userId);
                tx.Commit();
                return cart == null ? null : MapToCartDto(cart);
            }
        }

        public void AddToCart(int userId, int productId, int quantity)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var cart = GetOrCreateCartByUserId(userId);
                    var product = productRepository.GetById(productId);
                    if (product == null || !product.IsInStock()) throw new ArgumentException("구매할 수 없는 상품입니다.");

                    cart.AddOrUpdateItem(product, quantity);
                    // cartRepository.Update(cart); // 변경 추적으로 인해 명시적 Update는 불필요할 수 있음
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        public void UpdateCartItem(int userId, int productId, int quantity)
        {
            if (quantity <= 0)
            {
                RemoveFromCart(userId, productId);
                return;
            }

            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var cart = cartRepository.GetByUserId(userId);
                    if (cart == null) throw new InvalidOperationException("장바구니가 없습니다.");
                    
                    var product = productRepository.GetById(productId);
                    if (product == null) throw new ArgumentException("존재하지 않는 상품입니다.");

                    cart.AddOrUpdateItem(product, quantity);
                    tx.Commit();
                }
                catch { tx.Rollback(); throw; }
            }
        }

        public void RemoveFromCart(int userId, int productId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var cart = cartRepository.GetByUserId(userId);
                    if (cart != null)
                    {
                        cart.RemoveItem(productId);
                        tx.Commit();
                    }
                }
                catch { tx.Rollback(); throw; }
            }
        }

        public void ClearCart(int userId)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var cart = cartRepository.GetByUserId(userId);
                    if (cart != null)
                    {
                        cart.Items.Clear();
                        cart.ModifiedDate = DateTime.Now;
                        tx.Commit();
                    }
                }
                catch { tx.Rollback(); throw; }
            }
        }

        #region DTO Mappers
        private CartDto MapToCartDto(Cart cart)
        {
            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.User.Id,
                ModifiedDate = cart.ModifiedDate,
                TotalPrice = cart.GetTotalPrice(),
                Items = cart.Items.Select(MapToCartItemDto).ToList()
            };
        }

        private CartItemDto MapToCartItemDto(CartItem item)
        {
            return new CartItemDto
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
    - `Cart.cs`와 `CartItem.cs`를 `<Compile>` 아이템으로 추가합니다.
    ```xml
    <ItemGroup>
      ...
      <Compile Include="Entities\Cart.cs" />
      <Compile Include="Entities\CartItem.cs" />
      ...
    </ItemGroup>
    ```

2.  **`SpringNet.Data.csproj`**
    - 매핑 파일들을 `<EmbeddedResource>`로 추가하고, Repository를 `<Compile>` 아이템으로 추가합니다.
    ```xml
    <ItemGroup>
      ...
      <EmbeddedResource Include="Mappings\Cart.hbm.xml" />
      <EmbeddedResource Include="Mappings\CartItem.hbm.xml" />
      ...
    </ItemGroup>
    <ItemGroup>
      ...
      <Compile Include="Repositories\CartRepository.cs" />
      <Compile Include="Repositories\ICartRepository.cs" />
      ...
    </ItemGroup>
    ```

3.  **`SpringNet.Service.csproj`**
    - `ICartService.cs`와 `CartService.cs`를 `<Compile>` 아이템으로 추가합니다.
    ```xml
    <ItemGroup>
      ...
      <Compile Include="CartService.cs" />
      <Compile Include="ICartService.cs" />
      ...
    </ItemGroup>
    ```

### Step 9: `applicationContext.xml` 설정

`SpringNet.Web/Config/applicationContext.xml`에 새로운 Repository와 Service를 Bean으로 등록합니다.

```xml
    <!-- ... 기존 Bean 설정 ... -->

    <!-- Cart Repository -->
    <object id="cartRepository"
            type="SpringNet.Data.Repositories.CartRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <!-- Cart Service -->
    <object id="cartService"
            type="SpringNet.Service.CartService, SpringNet.Service">
        <constructor-arg ref="cartRepository" />
        <constructor-arg ref="userRepository" />
        <constructor-arg ref="productRepository" />
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```
**참고**: `CartService`는 `IUserRepository`와 `IProductRepository`에도 의존하므로, 생성자 인자로 함께 주입해줍니다. (이전 튜토리얼에서 `userRepository`와 `productRepository`가 이미 Bean으로 등록되었다고 가정합니다.)

## 💡 핵심 정리

- **DB 기반 장바구니**: 사용자의 장바구니 정보를 DB에 영구적으로 저장하여 여러 기기에서 접근할 수 있도록 설계했습니다.
- **엔티티 관계**: `Cart`는 `User`와 `CartItem`에, `CartItem`은 `Cart`와 `Product`에 관계를 맺습니다.
- **Service 계층 로직**: 장바구니가 없는 사용자를 위해 자동으로 생성해주는 로직, 장바구니에 상품을 추가/수정/삭제하는 비즈니스 로직을 `CartService`에 구현했습니다.
- **Eager Loading**: `CartRepository`에서 `FetchMany`/`ThenFetch`를 사용하여 장바구니와 관련된 항목, 상품 정보를 한 번의 쿼리로 가져와 N+1 문제를 방지했습니다.

## 🚀 다음 단계

다음: **[12-shopping-part3-order.md](./12-shopping-part3-order.md)** - 주문 처리
