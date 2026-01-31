# 10. 쇼핑몰 Part 1: 상품 관리

## 📚 학습 목표

- 상품(Product) 엔티티 설계
- 카테고리(Category) 관계 매핑
- 상품 CRUD 구현
- 이미지 업로드 처리

## 🛠️ 엔티티 설계

이 튜토리얼에서는 쇼핑몰에 필요한 `Category`와 `Product` 엔티티를 정의합니다. 특히 `Product` 엔티티는 이전 튜토리얼 03에서 정의했던 내용을 확장하여 새로운 프로퍼티와 `Category`와의 관계를 추가합니다.

### Category 엔티티

`SpringNet.Domain/Entities/Category.cs` 파일을 생성합니다.

```csharp
using System.Collections.Generic;

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

### Product 엔티티 (기존 파일 수정)

`SpringNet.Domain/Entities/Product.cs` 파일을 열어 다음 내용으로 수정합니다.

```csharp
using System;
using SpringNet.Domain.Entities;

namespace SpringNet.Domain.Entities
{
    public class Product
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual decimal Price { get; set; }
        public virtual int Stock { get; set; }
        public virtual string ImageUrl { get; set; } // 상품 이미지 경로
        public virtual Category Category { get; set; } // Many-to-One 관계
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

        public override string ToString()
        {
            return $"[{Id}] {Name} - {Price:C} (재고: {Stock})";
        }
    }
}
```


## 📝 NHibernate 매핑 설정

### Category 매핑 파일

`SpringNet.Data/Mappings/Category.hbm.xml` 파일을 생성합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="Category" table="Categories">

        <!-- Primary Key -->
        <id name="Id" column="Id">
            <generator class="identity" />
        </id>

        <!-- Properties -->
        <property name="Name" column="Name" type="string"
                  length="100" not-null="true" unique="true" />

        <property name="Description" column="Description" type="string"
                  length="500" />

        <!-- One-to-Many Relationship: Category 하나에 여러 Product -->
        <bag name="Products" inverse="true" cascade="all-delete-orphan" lazy="true">
            <key column="CategoryId" />
            <one-to-many class="Product" />
        </bag>

    </class>

</hibernate-mapping>
```

### Product 매핑 파일 (기존 파일 수정)

`SpringNet.Data/Mappings/Product.hbm.xml` 파일을 열어 다음 내용으로 수정합니다.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="Product" table="Products">

        <!-- Primary Key -->
        <id name="Id" column="Id">
            <generator class="identity" />
        </id>

        <!-- Properties -->
        <property name="Name" column="Name" type="string"
                  length="100" not-null="true" />

        <property name="Description" column="Description" type="string"
                  length="500" />

        <property name="Price" column="Price" type="decimal"
                  not-null="true" />

        <property name="Stock" column="Stock" type="int"
                  not-null="true" />

        <property name="ImageUrl" column="ImageUrl" type="string"
                  length="255" />

        <property name="CreatedDate" column="CreatedDate" type="datetime"
                  not-null="true" />

        <property name="IsAvailable" column="IsAvailable" type="boolean"
                  not-null="true" />

        <!-- Many-to-One Relationship: Product 여러 개가 하나의 Category에 속함 -->
        <many-to-one name="Category" column="CategoryId"
                     class="Category" not-null="true" cascade="none" />

    </class>

</hibernate-mapping>
```

**참고**: `Product` 엔티티의 `Category` 프로퍼티는 Many-to-One 관계로 매핑됩니다. `CategoryId`라는 외래 키 컬럼을 통해 `Category` 엔티티와 연결됩니다. `cascade="none"`은 `Product`를 저장/삭제할 때 `Category`에 영향을 주지 않도록 합니다.

## 📦 Repository 패턴 구현

### ICategoryRepository 인터페이스

`SpringNet.Data/Repositories/ICategoryRepository.cs` 파일을 생성합니다.

```csharp
using SpringNet.Domain.Entities;
using System.Collections.Generic;

namespace SpringNet.Data.Repositories
{
    public interface ICategoryRepository : IRepository<Category>
    {
        Category GetByName(string name);
        IList<Category> GetCategoriesWithProducts();
    }
}
```

### CategoryRepository 구현

`SpringNet.Data/Repositories/CategoryRepository.cs` 파일을 생성합니다.

```csharp
using NHibernate;
using NHibernate.Linq;
using SpringNet.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Data.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(ISessionFactory sessionFactory)
            : base(sessionFactory)
        {
        }

        public Category GetByName(string name)
        {
            return CurrentSession.Query<Category>().FirstOrDefault(c => c.Name == name);
        }

        public IList<Category> GetCategoriesWithProducts()
        {
            return CurrentSession.Query<Category>()
                .FetchMany(c => c.Products)
                .ToList();
        }
    }
}
```

### IProductRepository 인터페이스 (기존 파일 수정)

`SpringNet.Data/Repositories/IProductRepository.cs` 파일을 열어 다음 내용으로 수정합니다.

```csharp
using SpringNet.Domain.Entities;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SpringNet.Data.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        IList<Product> GetProductsByCategoryId(int categoryId);
        IList<Product> GetPagedProducts(int pageNumber, int pageSize, int? categoryId = null, string keyword = null);
        int CountProducts(int? categoryId = null, string keyword = null);
    }
}
```

### ProductRepository 구현 (기존 파일 수정)

`SpringNet.Data/Repositories/ProductRepository.cs` 파일을 열어 다음 내용으로 수정합니다.

```csharp
using NHibernate;
using NHibernate.Linq;
using SpringNet.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SpringNet.Data.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(ISessionFactory sessionFactory)
            : base(sessionFactory)
        {
        }

        public IList<Product> GetProductsByCategoryId(int categoryId)
        {
            return CurrentSession.Query<Product>()
                .Where(p => p.Category.Id == categoryId)
                .ToList();
        }

        public IList<Product> GetPagedProducts(int pageNumber, int pageSize, int? categoryId = null, string keyword = null)
        {
            IQueryable<Product> query = CurrentSession.Query<Product>();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.Category.Id == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword));
            }

            return query
                .OrderBy(p => p.Name) // 기본 정렬
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int CountProducts(int? categoryId = null, string keyword = null)
        {
            IQueryable<Product> query = CurrentSession.Query<Product>();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.Category.Id == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword));
            }

            return query.Count();
        }
    }
}
## 📦 Service Layer 구현

이제 상품 및 카테고리 관리를 위한 서비스 계층을 구현합니다. 서비스 계층은 비즈니스 로직, 트랜잭션 관리, DTO 변환 등을 책임집니다. `06-board-part3-service.md`에서 구현했던 `BoardService`와 유사한 패턴을 따릅니다.

### Step 1: DTO 클래스 생성

Controller와 View 사이에서 데이터를 주고받기 위한 DTO(Data Transfer Object)를 정의합니다.

`SpringNet.Service/DTOs/ShoppingDto.cs` 파일을 새로 생성하고 다음 코드를 추가합니다.

```csharp
using System;
using System.Collections.Generic;

namespace SpringNet.Service.DTOs
{
    // 카테고리 정보 DTO
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ProductCount { get; set; }
    }

    // 상품 목록에 사용될 기본 정보 DTO
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryName { get; set; }
        public bool IsInStock { get; set; }
    }

    // 상품 상세 정보 DTO
    public class ProductDetailDto : ProductDto
    {
        public string Description { get; set; }
        public int Stock { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedDate { get; set; }
        public CategoryDto Category { get; set; }
    }
}
```

### Step 2: IProductService 인터페이스

`SpringNet.Service/IProductService.cs` 파일을 열어 다음 내용으로 **수정**합니다. 이전에 `03-nhibernate-setup.md`에서 만들었던 기본 버전을 확장합니다.

```csharp
using SpringNet.Service.DTOs;
using System.Collections.Generic;

namespace SpringNet.Service
{
    public interface IProductService
    {
        ProductDetailDto GetProduct(int id);
        PagedResultDto<ProductDto> GetProducts(int pageNumber, int pageSize, int? categoryId = null, string keyword = null);
        int CreateProduct(ProductDetailDto productDto);
        void UpdateProduct(ProductDetailDto productDto);
        void DeleteProduct(int id);
    }
}
```

### Step 3: ProductService 구현

`SpringNet.Service/ProductService.cs` 파일을 열어 다음 내용으로 **수정**합니다.

```csharp
using NHibernate;
using SpringNet.Data.Repositories;
using SpringNet.Domain.Entities;
using SpringNet.Service.DTOs;
using System;
using System.Linq;

namespace SpringNet.Service
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository productRepository;
        private readonly ICategoryRepository categoryRepository;
        private readonly ISessionFactory sessionFactory;

        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ISessionFactory sessionFactory)
        {
            this.productRepository = productRepository;
            this.categoryRepository = categoryRepository;
            this.sessionFactory = sessionFactory;
        }

        public ProductDetailDto GetProduct(int id)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var product = productRepository.GetById(id);
                if (product == null) return null;

                var dto = MapToProductDetailDto(product);
                tx.Commit();
                return dto;
            }
        }

        public PagedResultDto<ProductDto> GetProducts(int pageNumber, int pageSize, int? categoryId = null, string keyword = null)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                var products = productRepository.GetPagedProducts(pageNumber, pageSize, categoryId, keyword);
                var totalCount = productRepository.CountProducts(categoryId, keyword);

                tx.Commit();

                return new PagedResultDto<ProductDto>
                {
                    Items = products.Select(MapToProductDto).ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };
            }
        }

        public int CreateProduct(ProductDetailDto productDto)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var category = categoryRepository.GetById(productDto.Category.Id);
                    if (category == null) throw new ArgumentException("유효하지 않은 카테고리입니다.");

                    var product = new Product
                    {
                        Name = productDto.Name,
                        Description = productDto.Description,
                        Price = productDto.Price,
                        Stock = productDto.Stock,
                        ImageUrl = productDto.ImageUrl,
                        IsAvailable = productDto.IsAvailable,
                        Category = category
                    };
                    
                    productRepository.Add(product);
                    tx.Commit();
                    return product.Id;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void UpdateProduct(ProductDetailDto productDto)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    var product = productRepository.GetById(productDto.Id);
                    if (product == null) throw new ArgumentException("상품을 찾을 수 없습니다.");

                    var category = categoryRepository.GetById(productDto.Category.Id);
                    if (category == null) throw new ArgumentException("유효하지 않은 카테고리입니다.");

                    // 매핑
                    product.Name = productDto.Name;
                    product.Description = productDto.Description;
                    product.Price = productDto.Price;
                    product.Stock = productDto.Stock;
                    product.ImageUrl = productDto.ImageUrl;
                    product.IsAvailable = productDto.IsAvailable;
                    product.Category = category;

                    productRepository.Update(product);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void DeleteProduct(int id)
        {
            using (var tx = sessionFactory.GetCurrentSession().BeginTransaction())
            {
                try
                {
                    productRepository.Delete(id);
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

        private ProductDto MapToProductDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryName = product.Category?.Name,
                IsInStock = product.IsInStock()
            };
        }

        private ProductDetailDto MapToProductDetailDto(Product product)
        {
            return new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryName = product.Category?.Name,
                IsInStock = product.IsInStock(),
                Description = product.Description,
                Stock = product.Stock,
                IsAvailable = product.IsAvailable,
                CreatedDate = product.CreatedDate,
                Category = product.Category == null ? null : new CategoryDto
                {
                    Id = product.Category.Id,
                    Name = product.Category.Name,
                    Description = product.Category.Description
                }
            };
        }

        #endregion
    }
}
```

### 📢 프로젝트 파일 및 Spring.NET 설정 업데이트

#### 1. `SpringNet.Data.csproj` 업데이트

새로 추가한 `ICategoryRepository`와 `CategoryRepository`를 프로젝트에 포함시킵니다.

```xml
<ItemGroup>
  ...
  <Compile Include="Repositories\CategoryRepository.cs" />
  <Compile Include="Repositories\ICategoryRepository.cs" />
  ...
</ItemGroup>
```

#### 2. `SpringNet.Service.csproj` 업데이트

`ProductService` 관련 파일들을 업데이트하고, `ShoppingDto.cs`를 새로 추가합니다. `DTOs` 폴더가 없다면 생성해주세요.

```xml
<ItemGroup>
  ...
  <Compile Include="DTOs\ShoppingDto.cs" />
  <Compile Include="IProductService.cs" />
  <Compile Include="ProductService.cs" />
  ...
</ItemGroup>
```

#### 3. `applicationContext.xml` 설정

`SpringNet.Web/Config/applicationContext.xml`에 새로 만든 Repository와 Service를 Bean으로 등록합니다.

```xml
    <!-- ... 기존 Bean 설정 ... -->

    <!-- Category Repository -->
    <object id="categoryRepository"
            type="SpringNet.Data.Repositories.CategoryRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <!-- Product Repository (05번 튜토리얼에서 수정됨) -->
    <object id="productRepository"
            type="SpringNet.Data.Repositories.ProductRepository, SpringNet.Data">
        <constructor-arg ref="sessionFactory" />
    </object>

    <!-- Product Service (이번에 수정됨) -->
    <object id="productService"
            type="SpringNet.Service.ProductService, SpringNet.Service">
        <constructor-arg ref="productRepository" />
        <constructor-arg ref="categoryRepository" />
        <constructor-arg ref="sessionFactory" />
    </object>

</objects>
```
**설명**:
- `categoryRepository` Bean을 새로 추가합니다.
- `productService` Bean이 `productRepository`, `categoryRepository`, `sessionFactory`를 주입받도록 생성자 인자를 설정합니다.

## 💡 핵심 정리

- **엔티티 확장**: 기존 `Product` 엔티티에 `Category`와의 관계를 추가하고, 재고 및 판매 가능 여부와 같은 속성을 더했습니다.
- **서비스 계층**: `ProductService`를 구현하여 상품 관련 비즈니스 로직과 트랜잭션 처리를 캡슐화했습니다.
- **DTO 패턴**: `ProductDto`와 `ProductDetailDto`를 사용하여 프레젠테이션 계층에 필요한 데이터만 전달하도록 설계했습니다.
- **페이징 및 검색**: Repository와 Service에 페이징 및 검색 로직을 추가하여 대량의 데이터를 효율적으로 처리할 수 있는 기반을 마련했습니다.

## 🚀 다음 단계

다음: **[11-shopping-part2-cart.md](./11-shopping-part2-cart.md)** - 장바구니 기능 구현
