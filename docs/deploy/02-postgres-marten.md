# PostgreSQL & Marten

API nhận `ConnectionStrings__Postgres` từ environment. Khi connection string có mặt, Marten
đăng ký schema `vietais_hsk` và các document/event stream đã được khai báo trong backend.

Local compose dùng PostgreSQL 16 với volume `vietais-postgres`; `AutoCreateSchemaObjects` chỉ phù
hợp local/development. Staging/production phải pin migration/schema rollout, backup, restore và
rollback riêng trước khi promotion.

Verification 2026-09-23: stack compose dữ liệu sạch đã ghi profile/curriculum/learning/practice/
review/progress/translation/speaking, restart API container hai lần, rồi đọc lại và replay completion
thành công bằng `scripts/marten-persistence-smoke.mjs`. Cổng kiểm chứng dùng Postgres host `55434`;
dữ liệu này
thuộc project QA tạm, không phải database production.

Verification bổ sung 2026-09-23: project `vietais-hsk-curriculum-guard-qa` dùng Postgres host
`55436`, API `6059`, frontend `6175`; cả bốn container healthy. Smoke write/read/verify pass,
bao gồm import version mới dùng lại HSK level Id đã publish trả `409` và không đổi version cũ.
