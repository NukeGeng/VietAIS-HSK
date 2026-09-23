# CI/CD

Pipeline tối thiểu:

```text
restore -> dotnet build/test -> npm build -> docker compose config
-> build API/frontend images -> deploy staging -> health/smoke -> production approval
```

Không dùng `design-template` làm production image; template chỉ được preview độc lập trên cổng
8766 để đối chiếu visual.
