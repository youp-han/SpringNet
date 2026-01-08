# 10. 쇼핑몰 Part 1: 상품 관리

## 📚 학습 목표

- 상품(Product) 엔티티 설계
- 카테고리(Category) 관계 매핑
- 상품 CRUD 구현
- 이미지 업로드 처리

## 🛠️ 엔티티 설계

### Category 엔티티

```csharp
namespace SpringNet.Domain.Entities
{
    public class Category
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual IList<Product> Products { get; set; }

        public Category()
        {
            Products = new List<Product>();
        }
    }
}
```

### Product 엔티티

```csharp
namespace SpringNet.Domain.Entities
{
    public class Product
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual decimal Price { get; set; }
        public virtual int Stock { get; set; }
        public virtual string ImageUrl { get; set; }
        public virtual Category Category { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual bool IsAvailable { get; set; }

        public Product()
        {
            CreatedDate = DateTime.Now;
            IsAvailable = true;
        }

        public virtual bool IsInStock()
        {
            return Stock > 0 && IsAvailable;
        }
    }
}
```

## 📦 ProductService

```csharp
public interface IProductService
{
    PagedResultDto<ProductDto> GetProducts(int categoryId, int page, int pageSize);
    ProductDto GetProduct(int id);
    int CreateProduct(CreateProductDto dto);
    void UpdateProduct(int id, UpdateProductDto dto);
    void DeleteProduct(int id);
    IList<CategoryDto> GetCategories();
}
```

## 💡 핵심 정리

### Many-to-One 관계

- **Product** (Many) ←→ **Category** (One)
- 한 카테고리에 여러 상품

## 🚀 다음 단계

다음: **[11-shopping-part2-cart.md](./11-shopping-part2-cart.md)** - 장바구니 기능
