# Spring.NET + NHibernate 학습 로드맵

## 📚 학습 개요

이 튜토리얼은 Spring.NET과 NHibernate를 사용하여 실전 웹 애플리케이션을 개발하는 방법을 단계별로 학습합니다.

### 학습 목표

- **Spring.NET의 핵심 개념** (IoC, DI, AOP) 이해 및 실습
- **NHibernate ORM**을 통한 데이터베이스 연동
- **레이어드 아키텍처** 설계 및 구현
- **실전 프로젝트** 3개를 통한 실무 역량 강화

## 🎯 프로젝트 구조

```
SpringNet/
├── SpringNet.Domain/        # 도메인 모델 (엔티티)
├── SpringNet.Data/          # 데이터 액세스 (Repository, NHibernate)
├── SpringNet.Service/       # 비즈니스 로직 (Service Layer)
├── SpringNet.Infrastructure/ # 공통 인프라 (Helpers, Utilities)
└── SpringNet.Web/           # 웹 프레젠테이션 (MVC)
```

## 📖 학습 단계

### Phase 1: 기초 개념 (1-3단계)

| 단계 | 문서 | 내용 | 난이도 |
|------|------|------|--------|
| 1 | [01-springnet-basics.md](./01-springnet-basics.md) | Spring.NET 기본 개념 (IoC, DI) | ⭐ |
| 2 | [02-dependency-injection.md](./02-dependency-injection.md) | 의존성 주입 실습 | ⭐ |
| 3 | [03-nhibernate-setup.md](./03-nhibernate-setup.md) | NHibernate 설정 및 기본 | ⭐⭐ |

### Phase 2: 게시판 시스템 (4-7단계)

| 단계 | 문서 | 내용 | 난이도 |
|------|------|------|--------|
| 4 | [04-board-part1-domain.md](./04-board-part1-domain.md) | 도메인 모델 설계 | ⭐⭐ |
| 5 | [05-board-part2-repository.md](./05-board-part2-repository.md) | Repository 패턴 구현 | ⭐⭐ |
| 6 | [06-board-part3-service.md](./06-board-part3-service.md) | Service Layer 구현 | ⭐⭐⭐ |
| 7 | [07-board-part4-mvc.md](./07-board-part4-mvc.md) | MVC 컨트롤러 및 뷰 | ⭐⭐⭐ |

### Phase 3: 사용자 관리 (8-9단계)

| 단계 | 문서 | 내용 | 난이도 |
|------|------|------|--------|
| 8 | [08-user-part1-authentication.md](./08-user-part1-authentication.md) | 회원가입, 로그인 구현 | ⭐⭐⭐ |
| 9 | [09-user-part2-authorization.md](./09-user-part2-authorization.md) | 권한 관리 및 보안 | ⭐⭐⭐⭐ |

### Phase 4: 쇼핑몰 시스템 (10-12단계)

| 단계 | 문서 | 내용 | 난이도 |
|------|------|------|--------|
| 10 | [10-shopping-part1-products.md](./10-shopping-part1-products.md) | 상품 관리 | ⭐⭐⭐ |
| 11 | [11-shopping-part2-cart.md](./11-shopping-part2-cart.md) | 장바구니 기능 | ⭐⭐⭐⭐ |
| 12 | [12-shopping-part3-order.md](./12-shopping-part3-order.md) | 주문 처리 및 결제 | ⭐⭐⭐⭐ |

### Phase 5: 고급 주제 (13-14단계)

| 단계 | 문서 | 내용 | 난이도 |
|------|------|------|--------|
| 13 | [13-transaction-management.md](./13-transaction-management.md) | 트랜잭션 관리 | ⭐⭐⭐⭐ |
| 14 | [14-best-practices.md](./14-best-practices.md) | 베스트 프랙티스 | ⭐⭐⭐⭐⭐ |

### Phase 6: 실무 심화 (15-19단계)

| 단계 | 문서 | 내용 | 난이도 |
|------|------|------|--------|
| 15 | [15-advanced-nhibernate-queries.md](./15-advanced-nhibernate-queries.md) | NHibernate 고급 쿼리 (HQL, LINQ, Criteria) | ⭐⭐⭐⭐ |
| 16 | [16-stored-procedures.md](./16-stored-procedures.md) | Stored Procedure 사용법 | ⭐⭐⭐ |
| 17 | [17-session-management.md](./17-session-management.md) | 세션 관리 (NHibernate & Web Session) | ⭐⭐⭐⭐ |
| 18 | [18-webapi-integration.md](./18-webapi-integration.md) | ASP.NET Web API 통합 | ⭐⭐⭐⭐ |
| 19 | [19-advanced-crud-patterns.md](./19-advanced-crud-patterns.md) | 고급 CRUD 패턴 (UoW, Specification) | ⭐⭐⭐⭐⭐ |

## 🛠️ 필요한 도구

- **Visual Studio 2022**
- **.NET Framework 4.8**
- **SQL Server 2019+** 또는 **SQLite** (학습용)
- **NuGet 패키지**:
  - Spring.Core (>= 3.0.0)
  - Spring.Web.Mvc5 (>= 3.0.0)
  - NHibernate (>= 5.3.0)
  - FluentNHibernate (>= 3.1.0) - 선택사항

## 📋 학습 전 체크리스트

- [ ] Visual Studio 2022 설치 완료
- [ ] SQL Server 설치 및 실행 (또는 SQLite)
- [ ] SpringNet 솔루션 생성 (5개 프로젝트)
- [ ] NuGet 패키지 설치 완료
- [ ] Git 저장소 초기화 (선택사항)

## 🚀 학습 방법

### 권장 학습 순서

1. **순서대로 학습**: 01번부터 19번까지 순서대로 진행하세요
2. **코드 직접 작성**: 복사/붙여넣기보다 직접 타이핑하면서 이해하세요
3. **실습 후 실행**: 각 단계마다 반드시 실행해서 동작을 확인하세요
4. **에러 해결**: 에러가 발생하면 스택 트레이스를 읽고 해결해보세요
5. **복습**: 이해가 안 되는 부분은 이전 단계로 돌아가 복습하세요

### 각 문서의 구성

각 튜토리얼 문서는 다음 구조로 되어 있습니다:

```
1. 학습 목표           # 이번 단계에서 배울 내용
2. 개념 설명           # 필요한 이론 설명
3. 코드 작성           # 단계별 코드 작성
4. 실행 및 테스트      # 동작 확인
5. 핵심 정리           # 배운 내용 요약
6. 연습 문제           # 스스로 해결해보기
7. 다음 단계           # 다음에 배울 내용
```

## 💡 학습 팁

### Spring.NET 핵심 개념
- **IoC (Inversion of Control)**: 제어의 역전 - 객체 생성/관리를 프레임워크에 위임
- **DI (Dependency Injection)**: 의존성 주입 - 필요한 객체를 외부에서 주입
- **Bean**: Spring이 관리하는 객체 (XML 또는 Attribute로 정의)

### NHibernate 핵심 개념
- **ORM**: 객체-관계 매핑 - C# 객체를 DB 테이블로 매핑
- **Session**: DB 연결을 관리하는 객체
- **HQL**: 객체 지향 쿼리 언어
- **Lazy Loading**: 필요할 때만 데이터 로딩

### 자주 하는 실수
1. ❌ Spring 설정 파일 경로 오류
2. ❌ NHibernate 매핑 오류
3. ❌ 순환 참조 문제
4. ❌ 트랜잭션 미적용
5. ❌ Session 관리 오류

## 📚 참고 자료

- [Spring.NET 공식 문서](http://springframework.net/doc-latest/reference/html/)
- [NHibernate 공식 문서](https://nhibernate.info/doc/)
- [Spring.NET GitHub](https://github.com/spring-projects/spring-net)

## 🎓 학습 후 기대 효과

이 튜토리얼을 완료하면:

✅ **엔터프라이즈 아키텍처** 설계 능력
✅ **Spring.NET IoC/DI** 완벽 이해
✅ **NHibernate ORM** 실무 활용 (HQL, LINQ, Criteria, Stored Procedure)
✅ **레이어드 패턴** 구현 능력
✅ **실전 프로젝트** 3개 포트폴리오
✅ **Web API 개발** 능력
✅ **고급 디자인 패턴** 활용 (UoW, Specification, Repository)
✅ **세션 관리** 마스터

## 🤝 도움이 필요하면

각 단계에서 막히는 부분이 있으면:
1. 에러 메시지를 자세히 읽어보세요
2. 설정 파일(XML, Web.config)을 다시 확인하세요
3. 이전 단계로 돌아가 누락된 부분이 없는지 확인하세요

---

**준비되셨나요? 그럼 [01-springnet-basics.md](./01-springnet-basics.md)로 시작하세요!** 🚀
