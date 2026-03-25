# Appendix. 데이터베이스 SQL 스크립트

이 문서는 SpringNet 튜토리얼 전체에서 사용하는 테이블의 DDL 스크립트를 제공합니다.
**SQL Server** 버전과 **SQLite** 버전 모두 포함합니다.

---

## 목차

1. [SQL Server DDL (전체 초기화)](#1-sql-server-ddl-전체-초기화)
2. [SQLite DDL (전체 초기화)](#2-sqlite-ddl-전체-초기화)
3. [Tutorial 09 마이그레이션 SQL (Board 스키마 변경)](#3-tutorial-09-마이그레이션-sql-board-스키마-변경)
4. [테스트 데이터 삽입 (선택)](#4-테스트-데이터-삽입-선택)

---

## 1. SQL Server DDL (전체 초기화)

새 데이터베이스에서 처음 시작할 때 아래 스크립트를 순서대로 실행합니다.

```sql
-- 데이터베이스 생성 (필요 시)
-- CREATE DATABASE SpringNetDB;
-- GO
-- USE SpringNetDB;
-- GO

-- ============================================================
-- Tutorial 03: Products (기본 상품 테이블)
-- Tutorial 10에서 Category FK 및 신규 컬럼이 추가됩니다.
-- ============================================================
CREATE TABLE Products (
    Id          INT            PRIMARY KEY IDENTITY(1,1),
    Name        NVARCHAR(100)  NOT NULL,
    Price       DECIMAL(18,2)  NOT NULL,
    Description NVARCHAR(MAX)  NULL,
    Stock       INT            NOT NULL DEFAULT 0,
    ImageUrl    NVARCHAR(500)  NULL,
    CategoryId  INT            NULL,
    CreatedDate DATETIME       NOT NULL DEFAULT GETDATE(),
    IsAvailable BIT            NOT NULL DEFAULT 1
);
GO

-- ============================================================
-- Tutorial 04: Boards, Replies
-- ============================================================
CREATE TABLE Boards (
    Id           INT            PRIMARY KEY IDENTITY(1,1),
    Title        NVARCHAR(200)  NOT NULL,
    Content      NVARCHAR(MAX)  NOT NULL,
    AuthorId     INT            NOT NULL DEFAULT 0,
    AuthorName   NVARCHAR(50)   NOT NULL DEFAULT '',
    ViewCount    INT            NOT NULL DEFAULT 0,
    CreatedDate  DATETIME       NOT NULL DEFAULT GETDATE(),
    ModifiedDate DATETIME       NULL
);
GO

CREATE TABLE Replies (
    Id          INT            PRIMARY KEY IDENTITY(1,1),
    BoardId     INT            NOT NULL,
    Content     NVARCHAR(MAX)  NOT NULL,
    Author      NVARCHAR(50)   NOT NULL,
    CreatedDate DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Replies_Boards FOREIGN KEY (BoardId) REFERENCES Boards(Id) ON DELETE CASCADE
);
GO

-- ============================================================
-- Tutorial 08: Users
-- ============================================================
CREATE TABLE Users (
    Id            INT            PRIMARY KEY IDENTITY(1,1),
    Username      NVARCHAR(50)   NOT NULL UNIQUE,
    Email         NVARCHAR(100)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(255)  NOT NULL,
    FullName      NVARCHAR(100)  NULL,
    Role          NVARCHAR(20)   NOT NULL DEFAULT 'User',
    CreatedDate   DATETIME       NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME       NULL,
    IsActive      BIT            NOT NULL DEFAULT 1
);
GO

-- ============================================================
-- Tutorial 10: Categories (Products의 FK 대상)
-- ============================================================
CREATE TABLE Categories (
    Id          INT            PRIMARY KEY IDENTITY(1,1),
    Name        NVARCHAR(100)  NOT NULL,
    Description NVARCHAR(500)  NULL
);
GO

-- Products 테이블에 CategoryId FK 추가
ALTER TABLE Products
    ADD CONSTRAINT FK_Products_Categories
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id);
GO

-- ============================================================
-- Tutorial 11: Carts, CartItems
-- ============================================================
CREATE TABLE Carts (
    Id          INT       PRIMARY KEY IDENTITY(1,1),
    UserId      INT       NOT NULL,
    CreatedDate DATETIME  NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Carts_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

CREATE TABLE CartItems (
    Id        INT            PRIMARY KEY IDENTITY(1,1),
    CartId    INT            NOT NULL,
    ProductId INT            NOT NULL,
    Quantity  INT            NOT NULL DEFAULT 1,
    UnitPrice DECIMAL(18,2)  NOT NULL,
    CONSTRAINT FK_CartItems_Carts    FOREIGN KEY (CartId)    REFERENCES Carts(Id)    ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
GO

-- ============================================================
-- Tutorial 12: Orders, OrderItems
-- ============================================================
CREATE TABLE Orders (
    Id              INT            PRIMARY KEY IDENTITY(1,1),
    UserId          INT            NOT NULL,
    OrderDate       DATETIME       NOT NULL DEFAULT GETDATE(),
    TotalAmount     DECIMAL(18,2)  NOT NULL,
    Status          NVARCHAR(50)   NOT NULL DEFAULT 'Pending',
    ShippingAddress NVARCHAR(500)  NULL,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

CREATE TABLE OrderItems (
    Id          INT            PRIMARY KEY IDENTITY(1,1),
    OrderId     INT            NOT NULL,
    ProductId   INT            NOT NULL,
    ProductName NVARCHAR(100)  NOT NULL,
    Quantity    INT            NOT NULL,
    UnitPrice   DECIMAL(18,2)  NOT NULL,
    CONSTRAINT FK_OrderItems_Orders   FOREIGN KEY (OrderId)   REFERENCES Orders(Id)   ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
GO
```

---

## 2. SQLite DDL (전체 초기화)

`hibernate.cfg.xml`에서 SQLite를 사용하는 경우 아래 스크립트를 사용합니다.

```sql
-- SQLite는 AUTO INCREMENT 대신 INTEGER PRIMARY KEY 사용
-- SQLite는 ALTER TABLE ADD CONSTRAINT를 지원하지 않으므로
-- FK는 테이블 생성 시 인라인으로 정의합니다.

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Products (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT    NOT NULL,
    Price       REAL    NOT NULL,
    Description TEXT,
    Stock       INTEGER NOT NULL DEFAULT 0,
    ImageUrl    TEXT,
    CategoryId  INTEGER REFERENCES Categories(Id),
    CreatedDate TEXT    NOT NULL DEFAULT (datetime('now')),
    IsAvailable INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Boards (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Title        TEXT    NOT NULL,
    Content      TEXT    NOT NULL,
    AuthorId     INTEGER NOT NULL DEFAULT 0,
    AuthorName   TEXT    NOT NULL DEFAULT '',
    ViewCount    INTEGER NOT NULL DEFAULT 0,
    CreatedDate  TEXT    NOT NULL DEFAULT (datetime('now')),
    ModifiedDate TEXT
);

CREATE TABLE IF NOT EXISTS Replies (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    BoardId     INTEGER NOT NULL REFERENCES Boards(Id) ON DELETE CASCADE,
    Content     TEXT    NOT NULL,
    Author      TEXT    NOT NULL,
    CreatedDate TEXT    NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS Users (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Username      TEXT    NOT NULL UNIQUE,
    Email         TEXT    NOT NULL UNIQUE,
    PasswordHash  TEXT    NOT NULL,
    FullName      TEXT,
    Role          TEXT    NOT NULL DEFAULT 'User',
    CreatedDate   TEXT    NOT NULL DEFAULT (datetime('now')),
    LastLoginDate TEXT,
    IsActive      INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS Categories (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Name        TEXT    NOT NULL,
    Description TEXT
);

CREATE TABLE IF NOT EXISTS Carts (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId      INTEGER NOT NULL REFERENCES Users(Id),
    CreatedDate TEXT    NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS CartItems (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    CartId    INTEGER NOT NULL REFERENCES Carts(Id)    ON DELETE CASCADE,
    ProductId INTEGER NOT NULL REFERENCES Products(Id),
    Quantity  INTEGER NOT NULL DEFAULT 1,
    UnitPrice REAL    NOT NULL
);

CREATE TABLE IF NOT EXISTS Orders (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId          INTEGER NOT NULL REFERENCES Users(Id),
    OrderDate       TEXT    NOT NULL DEFAULT (datetime('now')),
    TotalAmount     REAL    NOT NULL,
    Status          TEXT    NOT NULL DEFAULT 'Pending',
    ShippingAddress TEXT
);

CREATE TABLE IF NOT EXISTS OrderItems (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId     INTEGER NOT NULL REFERENCES Orders(Id)   ON DELETE CASCADE,
    ProductId   INTEGER NOT NULL REFERENCES Products(Id),
    ProductName TEXT    NOT NULL,
    Quantity    INTEGER NOT NULL,
    UnitPrice   REAL    NOT NULL
);
```

---

## 3. Tutorial 09 마이그레이션 SQL (Board 스키마 변경)

Tutorial 04~07에서 `Boards` 테이블을 이미 생성한 경우, Tutorial 09 진행 전에 아래 마이그레이션 SQL을 실행합니다.

### SQL Server

```sql
-- 1. AuthorId, AuthorName 컬럼 추가
ALTER TABLE Boards ADD AuthorId   INT           NOT NULL DEFAULT 0;
ALTER TABLE Boards ADD AuthorName NVARCHAR(50)  NOT NULL DEFAULT '';
GO

-- 2. 기존 Author 컬럼 제거
--    (이전 튜토리얼에서 Author 컬럼이 있는 경우에만 실행)
-- ALTER TABLE Boards DROP COLUMN Author;
-- GO

-- 3. 기존 데이터가 있다면 AuthorId, AuthorName 업데이트 필요
--    (학습용이므로 테스트 데이터 삭제 후 재시작을 권장)
-- DELETE FROM Replies;
-- DELETE FROM Boards;
```

### SQLite

SQLite는 컬럼 삭제를 지원하지 않으므로, 테이블을 재생성합니다.

```sql
-- 1. 기존 데이터 백업 (필요 시)
-- CREATE TABLE Boards_backup AS SELECT * FROM Boards;

-- 2. 기존 테이블 삭제 후 재생성
DROP TABLE IF EXISTS Replies;
DROP TABLE IF EXISTS Boards;

CREATE TABLE Boards (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Title        TEXT    NOT NULL,
    Content      TEXT    NOT NULL,
    AuthorId     INTEGER NOT NULL DEFAULT 0,
    AuthorName   TEXT    NOT NULL DEFAULT '',
    ViewCount    INTEGER NOT NULL DEFAULT 0,
    CreatedDate  TEXT    NOT NULL DEFAULT (datetime('now')),
    ModifiedDate TEXT
);

CREATE TABLE Replies (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    BoardId     INTEGER NOT NULL REFERENCES Boards(Id) ON DELETE CASCADE,
    Content     TEXT    NOT NULL,
    Author      TEXT    NOT NULL,
    CreatedDate TEXT    NOT NULL DEFAULT (datetime('now'))
);
```

---

## 4. 테스트 데이터 삽입 (선택)

튜토리얼 진행 시 확인용으로 사용할 수 있는 샘플 데이터입니다.

### SQL Server

```sql
-- 카테고리
INSERT INTO Categories (Name, Description) VALUES
    (N'전자제품', N'노트북, 스마트폰 등'),
    (N'의류',     N'남성/여성 의류'),
    (N'도서',     N'IT, 소설 등');

-- 상품
INSERT INTO Products (Name, Price, Description, Stock, CategoryId, IsAvailable) VALUES
    (N'노트북 Pro',      1500000, N'고성능 개발자용 노트북', 10, 1, 1),
    (N'무선 키보드',       89000, N'블루투스 키보드',        50, 1, 1),
    (N'클린 코드 (도서)',  32000, N'로버트 마틴 저',         30, 3, 1);

-- 관리자 계정 (비밀번호: admin123 의 SHA256 Base64)
-- 실제 사용 시 AuthService.Register() 를 통해 생성하는 것을 권장합니다.
INSERT INTO Users (Username, Email, PasswordHash, FullName, Role, IsActive) VALUES
    ('admin', 'admin@example.com', 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', N'관리자', 'Admin', 1),
    ('user1', 'user1@example.com', 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', N'테스트 유저', 'User', 1);
-- ※ 위 PasswordHash는 'admin123'의 SHA256(Base64) 값입니다.

-- 게시글
INSERT INTO Boards (Title, Content, AuthorId, AuthorName, ViewCount) VALUES
    (N'첫 번째 게시글', N'Spring.NET 튜토리얼에 오신 것을 환영합니다.', 1, N'관리자', 0),
    (N'NHibernate 질문', N'NHibernate 매핑 방법이 궁금합니다.', 2, N'테스트 유저', 0);
```

### SQLite

```sql
INSERT INTO Categories (Name, Description) VALUES
    ('전자제품', '노트북, 스마트폰 등'),
    ('의류', '남성/여성 의류'),
    ('도서', 'IT, 소설 등');

INSERT INTO Products (Name, Price, Description, Stock, CategoryId, IsAvailable) VALUES
    ('노트북 Pro', 1500000, '고성능 개발자용 노트북', 10, 1, 1),
    ('무선 키보드', 89000, '블루투스 키보드', 50, 1, 1),
    ('클린 코드 (도서)', 32000, '로버트 마틴 저', 30, 3, 1);

INSERT INTO Users (Username, Email, PasswordHash, FullName, Role, IsActive) VALUES
    ('admin', 'admin@example.com', 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', '관리자', 'Admin', 1),
    ('user1', 'user1@example.com', 'jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=', '테스트 유저', 'User', 1);

INSERT INTO Boards (Title, Content, AuthorId, AuthorName, ViewCount) VALUES
    ('첫 번째 게시글', 'Spring.NET 튜토리얼에 오신 것을 환영합니다.', 1, '관리자', 0),
    ('NHibernate 질문', 'NHibernate 매핑 방법이 궁금합니다.', 2, '테스트 유저', 0);
```

---

## 관련 튜토리얼

- [Tutorial 03: NHibernate 기본 설정](./03-nhibernate-setup.md)
- [Tutorial 04: 게시판 Domain](./04-board-part1-domain.md)
- [Tutorial 08: 사용자 인증](./08-user-part1-authentication.md)
- [Tutorial 09: 사용자 인가 (Board Breaking Change)](./09-user-part2-authorization.md)
- [Tutorial 10: 쇼핑몰 상품 관리](./10-shopping-part1-products.md)
- [Tutorial 11: 장바구니](./11-shopping-part2-cart.md)
- [Tutorial 12: 주문](./12-shopping-part3-order.md)
