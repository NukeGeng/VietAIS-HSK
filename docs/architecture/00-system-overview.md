# System Overview

## Kiến trúc tổng thể

```text
Vue 3 Frontend
      ↓ HTTP / Realtime khi cần
ASP.NET Core API
      ↓
Wolverine Handlers
      ↓
Domain / Application
      ↓
Marten + PostgreSQL
├── Documents
└── Event Store
    ├── Inline Projections
    └── Async Projections

Wolverine Outbox
      ↓
RabbitMQ
├── AI Grading Worker
└── CosyVoice Audio Worker

Object Storage / CDN
└── generated audio / media assets
```

## Kiến trúc triển khai

Modular Monolith trước.

Không chia microservice chỉ vì:
- sidebar có nhiều nhóm;
- worker dùng process riêng;
- RabbitMQ tồn tại.

Worker có thể deploy process riêng nhưng business boundary vẫn theo documented modules.

## Data ownership

- Curriculum: knowledge/reference content;
- Learning: learning path milestones;
- Practice: attempts/deterministic grading;
- Review: review queue/mistakes;
- Exam: event-sourced exam attempt;
- Progress: projections/read models;
- Content: question/audio/extended content;
- Identity: user/profile/authorization context.
