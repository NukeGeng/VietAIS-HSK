# Storage & CDN

## Scope

Phục vụ:
- generated audio;
- story/video/resource assets khi self-host;
- static media cần tái sử dụng.

## Checklist

- provider/region;
- bucket/container naming;
- credentials/managed identity;
- public/private access;
- CORS;
- cache headers;
- CDN strategy;
- object lifecycle;
- backup/retention nếu cần;
- health/availability;
- cost monitoring.

Generated vocabulary audio phải được cache/reuse; không phụ thuộc TTS runtime cho mỗi play.
