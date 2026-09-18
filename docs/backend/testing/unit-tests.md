# Unit Tests

Test business rules và deterministic logic của từng module.

Test project hiện tại: `tests/VietAisHsk.Api.Tests/`.

Coverage bootstrap:

- Practice grading và normalization;
- Identity profile/learning target validation;
- Curriculum import provenance/duplicate level validation;
- Progress weak point projection và activity idempotency.

Chạy: `dotnet test VietAisHsk.slnx --no-restore -m:1`.
