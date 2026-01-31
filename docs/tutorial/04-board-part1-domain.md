# 04. 게시판 Part 1: 도메인 모델 설계

## 📚 학습 목표

- 도메인 주도 설계(DDD) 기본 개념
- 게시판 엔티티 모델링
- NHibernate 매핑 (Board, Reply)
- 엔티티 간 관계 설정 (One-to-Many)

## 🎯 게시판 시스템 요구사항

### 기능 요구사항

- **게시글 (Board)**
  - 제목, 내용, 작성자, 작성일
  - 조회수 관리
  - 게시글 CRUD

- **댓글 (Reply)**
  - 댓글 내용, 작성자, 작성일
  - 게시글별 댓글 목록
  - 댓글 CRUD

### 데이터베이스 설계

```sql
-- Boards 테이블
CREATE TABLE Boards (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Author NVARCHAR(50) NOT NULL,
    ViewCount INT DEFAULT 0,
    CreatedDate DATETIME DEFAULT GETDATE(),
    ModifiedDate DATETIME
);

-- Replies 테이블
CREATE TABLE Replies (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BoardId INT NOT NULL,
    Content NVARCHAR(500) NOT NULL,
    Author NVARCHAR(50) NOT NULL,
    CreatedDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (BoardId) REFERENCES Boards(Id) ON DELETE CASCADE
);
```

## 🛠️ 도메인 엔티티 구현

### Step 1: Board 엔티티

`SpringNet.Domain/Entities/Board.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace SpringNet.Domain.Entities
{
    public class Board
    {
        public virtual int Id { get; set; }
        public virtual string Title { get; set; }
        public virtual string Content { get; set; }
        public virtual string Author { get; set; }
        public virtual int ViewCount { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime? ModifiedDate { get; set; }

        // One-to-Many: 게시글 하나에 여러 댓글
        public virtual IList<Reply> Replies { get; set; }

        public Board()
        {
            CreatedDate = DateTime.Now;
            ViewCount = 0;
            Replies = new List<Reply>();
        }

        // 비즈니스 로직
        public virtual void IncreaseViewCount()
        {
            ViewCount++;
        }

        public virtual void UpdateContent(string title, string content)
        {
            Title = title;
            Content = content;
            ModifiedDate = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Id}] {Title} by {Author} ({ViewCount} views)";
        }
    }
}
```

### Step 2: Reply 엔티티

`SpringNet.Domain/Entities/Reply.cs`:

```csharp
using System;

namespace SpringNet.Domain.Entities
{
    public class Reply
    {
        public virtual int Id { get; set; }
        public virtual string Content { get; set; }
        public virtual string Author { get; set; }
        public virtual DateTime CreatedDate { get; set; }

        // Many-to-One: 여러 댓글이 하나의 게시글에 속함
        public virtual Board Board { get; set; }

        public Reply()
        {
            CreatedDate = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[Reply {Id}] {Content} by {Author}";
        }
    }
}
```

## 📝 NHibernate 매핑 설정

### Step 3: Board 매핑 파일

`SpringNet.Data/Mappings/Board.hbm.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="Board" table="Boards">

        <!-- Primary Key -->
        <id name="Id" column="Id">
            <generator class="identity" />
        </id>

        <!-- Properties -->
        <property name="Title" column="Title" type="string"
                  length="200" not-null="true" />

        <property name="Content" column="Content" type="string"
                  not-null="true" />

        <property name="Author" column="Author" type="string"
                  length="50" not-null="true" />

        <property name="ViewCount" column="ViewCount" type="int"
                  not-null="true" />

        <property name="CreatedDate" column="CreatedDate" type="datetime"
                  not-null="true" />

        <property name="ModifiedDate" column="ModifiedDate" type="datetime" />

        <!-- One-to-Many Relationship -->
        <bag name="Replies" inverse="true" cascade="all-delete-orphan" lazy="true">
            <key column="BoardId" />
            <one-to-many class="Reply" />
        </bag>

    </class>

</hibernate-mapping>
```

**관계 설정 설명**:
- `<bag>`: 컬렉션 매핑 (순서 없는 리스트)
- `inverse="true"`: 관계의 주인이 아님 (Reply가 관계 관리)
- `cascade="all-delete-orphan"`: 게시글 삭제 시 댓글도 자동 삭제
- `lazy="true"`: 필요할 때만 댓글 로딩 (기본값)

### Step 4: Reply 매핑 파일

`SpringNet.Data/Mappings/Reply.hbm.xml`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<hibernate-mapping xmlns="urn:nhibernate-mapping-2.2"
                   assembly="SpringNet.Domain"
                   namespace="SpringNet.Domain.Entities">

    <class name="Reply" table="Replies">

        <!-- Primary Key -->
        <id name="Id" column="Id">
            <generator class="identity" />
        </id>

        <!-- Properties -->
        <property name="Content" column="Content" type="string"
                  length="500" not-null="true" />

        <property name="Author" column="Author" type="string"
                  length="50" not-null="true" />

        <property name="CreatedDate" column="CreatedDate" type="datetime"
                  not-null="true" />

        <!-- Many-to-One Relationship -->
        <many-to-one name="Board" column="BoardId"
                     class="Board" not-null="true" />

    </class>

</hibernate-mapping>
```

**관계 설정 설명**:
- `<many-to-one>`: 다대일 관계
- `column="BoardId"`: 외래 키 컬럼
- `not-null="true"`: 댓글은 반드시 게시글에 속함

**중요**: `.hbm.xml` 파일 속성을 **포함 리소스**로 설정!

### 📢 프로젝트 파일 업데이트
새로운 엔티티와 매핑 파일들을 각 프로젝트에 포함시켜야 합니다.

#### 1. `SpringNet.Domain.csproj` 업데이트
`Board.cs`와 `Reply.cs`를 `<Compile>` 아이템으로 추가합니다.

```xml
<ItemGroup>
  <Compile Include="Entities\Board.cs" />
  <Compile Include="Entities\Product.cs" />
  <Compile Include="Entities\Reply.cs" />
  <Compile Include="Properties\AssemblyInfo.cs" />
</ItemGroup>
```

#### 2. `SpringNet.Data.csproj` 업데이트
`Board.hbm.xml`과 `Reply.hbm.xml`을 `<EmbeddedResource>` 아이템으로 추가합니다.

```xml
<ItemGroup>
  <EmbeddedResource Include="Mappings\Board.hbm.xml" />
  <EmbeddedResource Include="Mappings\Product.hbm.xml" />
  <EmbeddedResource Include="Mappings\Reply.hbm.xml" />
</ItemGroup>
```

#### 매핑 파일 자동 탐지
`hibernate.cfg.xml` 파일을 수정할 필요가 없는지 궁금할 수 있습니다. 다행히도 수정할 필요가 없습니다!

이전 튜토리얼에서 `<mapping assembly="SpringNet.Domain" />` 한 줄을 추가했습니다. 이 설정은 NHibernate가 `SpringNet.Domain` 어셈블리 내에서 `.hbm.xml`로 끝나고 `포함 리소스(Embedded Resource)`로 설정된 모든 파일을 자동으로 찾아 로드하도록 지시합니다. 따라서 새 매핑 파일을 추가할 때마다 XML 파일을 수동으로 업데이트할 필요가 없습니다.

## 🔍 엔티티 관계 이해

### One-to-Many vs Many-to-One

```
Board (One)  ←→  Reply (Many)

1개 게시글     ←→  N개 댓글
```

**Board 입장**:
```csharp
// One-to-Many: 하나의 게시글이 여러 댓글을 가짐
public virtual IList<Reply> Replies { get; set; }
```

**Reply 입장**:
```csharp
// Many-to-One: 여러 댓글이 하나의 게시글에 속함
public virtual Board Board { get; set; }
```

### Cascade 옵션

| Cascade | 동작 | 사용 예 |
|---------|------|---------|
| `none` | 자동 동작 없음 | 기본값 |
| `save-update` | 부모 저장 시 자식도 저장 | 일반적 |
| `delete` | 부모 삭제 시 자식도 삭제 | 강한 관계 |
| `all` | 모든 동작 전파 | |
| `all-delete-orphan` | 고아 객체 자동 삭제 | 추천 |

```xml
<!-- 게시글 삭제 시 댓글 자동 삭제 -->
<bag name="Replies" cascade="all-delete-orphan">
    <key column="BoardId" />
    <one-to-many class="Reply" />
</bag>
```

### Lazy Loading vs Eager Loading

```xml
<!-- Lazy Loading (기본값): 필요할 때 로딩 -->
<bag name="Replies" lazy="true">

<!-- Eager Loading: 즉시 로딩 -->
<bag name="Replies" lazy="false">
```

**Lazy Loading 사용 시**:
```csharp
using (var session = sessionFactory.OpenSession())
{
    var board = session.Get<Board>(1);
    // 이 시점에 댓글은 로딩되지 않음

    foreach (var reply in board.Replies)
    {
        // 이 시점에 댓글 로딩 (SELECT 쿼리 실행)
        Console.WriteLine(reply.Content);
    }
}
```

## 🧪 도메인 모델 테스트

엔티티에 비즈니스 로직을 추가했다면, 단위 테스트를 통해 의도한 대로 동작하는지 검증하는 것이 매우 중요합니다.

### 🧪 테스트 프로젝트 설정
예제 코드를 실행하려면 먼저 단위 테스트 프로젝트를 설정해야 합니다.

1.  **테스트 프로젝트 생성**
    -   솔루션 우클릭 → 추가 → **새 프로젝트**
    -   **클래스 라이브러리(.NET Framework)** 템플릿 선택
    -   프로젝트 이름: `SpringNet.Tests`

2.  **프로젝트 참조 추가**
    -   `SpringNet.Tests` 프로젝트의 `참조` 우클릭 → **참조 추가**
    -   `프로젝트` 탭에서 `SpringNet.Domain`을 체크하고 확인을 누릅니다.

3.  **NuGet 패키지 설치**
    -   `SpringNet.Tests` 프로젝트를 대상으로 패키지 관리자 콘솔에서 다음 명령을 실행합니다.
    ```powershell
    Install-Package NUnit -Version 3.13.3
    Install-Package NUnit3TestAdapter -Version 4.2.1
    Install-Package Microsoft.NET.Test.Sdk -Version 17.1.0
    ```

4.  **초기 파일 삭제**
    -   `SpringNet.Tests` 프로젝트에 자동으로 생성된 `Class1.cs` 파일을 삭제합니다.

이제 테스트 코드를 작성하고 실행할 준비가 되었습니다!

### 단위 테스트 예제

`SpringNet.Tests/DomainTests/BoardEntityTests.cs`:

```csharp
using NUnit.Framework;
using SpringNet.Domain.Entities;
using System;

namespace SpringNet.Tests.DomainTests
{
    [TestFixture]
    public class BoardEntityTests
    {
        [Test]
        public void Board_Creation_SetsDefaultValues()
        {
            // Arrange & Act
            var board = new Board
            {
                Title = "테스트 제목",
                Content = "테스트 내용",
                Author = "홍길동"
            };

            // Assert
            Assert.AreEqual(0, board.ViewCount);
            Assert.IsNotNull(board.Replies);
            Assert.AreEqual(0, board.Replies.Count);
            Assert.IsTrue(board.CreatedDate <= DateTime.Now);
        }

        [Test]
        public void Board_IncreaseViewCount_IncreasesBy1()
        {
            // Arrange
            var board = new Board();
            var initialCount = board.ViewCount;

            // Act
            board.IncreaseViewCount();

            // Assert
            Assert.AreEqual(initialCount + 1, board.ViewCount);
        }

        [Test]
        public void Board_UpdateContent_SetsModifiedDate()
        {
            // Arrange
            var board = new Board
            {
                Title = "원래 제목",
                Content = "원래 내용"
            };

            // Act
            board.UpdateContent("새 제목", "새 내용");

            // Assert
            Assert.AreEqual("새 제목", board.Title);
            Assert.AreEqual("새 내용", board.Content);
            Assert.IsNotNull(board.ModifiedDate);
        }

        [Test]
        public void Reply_AssignToBoard_EstablishesRelationship()
        {
            // Arrange
            var board = new Board
            {
                Title = "게시글",
                Content = "내용",
                Author = "작성자"
            };

            var reply = new Reply
            {
                Content = "댓글 내용",
                Author = "댓글 작성자",
                Board = board
            };

            // Act
            board.Replies.Add(reply);

            // Assert
            Assert.AreEqual(1, board.Replies.Count);
            Assert.AreEqual(board, reply.Board);
        }
    }
}
```

## 💡 도메인 주도 설계 원칙

### 1. 엔티티에 비즈니스 로직 포함

```csharp
// ❌ 나쁜 예: 빈약한 도메인 모델
public class Board
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int ViewCount { get; set; }
}

// 외부에서 직접 조작
board.ViewCount++;

// ✅ 좋은 예: 풍부한 도메인 모델
public class Board
{
    public virtual int Id { get; set; }
    public virtual string Title { get; set; }
    public virtual int ViewCount { get; set; }

    // 비즈니스 로직 캡슐화
    public virtual void IncreaseViewCount()
    {
        ViewCount++;
        // 추가 로직 (로그, 이벤트 등)
    }
}
```

### 2. 불변성 유지

```csharp
public class Board
{
    private readonly DateTime createdDate;

    public virtual DateTime CreatedDate => createdDate;

    public Board()
    {
        createdDate = DateTime.Now;
    }

    // CreatedDate는 변경 불가
}
```

### 3. 유효성 검증

```csharp
public class Board
{
    private string title;

    public virtual string Title
    {
        get => title;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("제목은 필수입니다.");

            if (value.Length > 200)
                throw new ArgumentException("제목은 200자 이내여야 합니다.");

            title = value;
        }
    }
}
```

## 🎯 연습 문제

### 문제 1: 첨부파일 엔티티 추가

다음 요구사항을 구현하세요:

1. `Attachment` 엔티티 생성 (FileName, FileSize, FilePath)
2. Board와 One-to-Many 관계 설정
3. 매핑 파일 작성

### 문제 2: 게시글 카테고리

1. `Category` 엔티티 생성
2. Board와 Many-to-One 관계 설정
3. 카테고리별 게시글 조회 기능

### 문제 3: 도메인 로직 추가

다음 메서드를 Board 엔티티에 추가:

1. `IsModified()`: 수정 여부 확인
2. `GetReplyCount()`: 댓글 수 반환
3. `CanDelete(string userId)`: 삭제 권한 확인

## 💡 핵심 정리

### 도메인 모델 설계 원칙

✅ **엔티티에 비즈니스 로직 포함**
✅ **virtual 키워드 사용** (NHibernate Proxy)
✅ **기본 생성자 제공**
✅ **컬렉션 초기화** (생성자에서)
✅ **불변성 고려**

### NHibernate 관계 매핑

**One-to-Many**:
```xml
<bag name="Replies" inverse="true" cascade="all-delete-orphan">
    <key column="ForeignKey" />
    <one-to-many class="ChildEntity" />
</bag>
```

**Many-to-One**:
```xml
<many-to-one name="Parent" column="ForeignKey"
             class="ParentEntity" not-null="true" />
```

### Cascade 옵션

- `all-delete-orphan`: 부모 삭제 시 자식도 삭제 (추천)
- `save-update`: 부모 저장 시 자식도 저장
- `none`: 자동 동작 없음

## 🚀 다음 단계

도메인 모델 설계 완료!

다음 단계: **[05-board-part2-repository.md](./05-board-part2-repository.md)**에서 Repository 패턴을 구현합니다.

---

**도메인 모델은 애플리케이션의 핵심입니다. 비즈니스 로직을 잘 표현하세요!**
