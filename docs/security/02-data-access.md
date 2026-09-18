# Data Access

## Learner data isolation

Learner chỉ truy cập dữ liệu của chính mình:
- profile;
- learning state;
- practice attempts;
- review queue/mistakes;
- exam attempts/results;
- translation attempts;
- speaking sessions;
- progress/history/streak.

Handler/query phải filter/resolve UserId từ trusted auth context, không nhận UserId tùy ý từ client cho self-service endpoint.

## Public/published content

Learner/Guest chỉ đọc content đã Published theo rule product:
- curriculum;
- question/sample content khi exposed;
- stories/videos/resources/tools.

Draft content chỉ admin có permission phù hợp.

## Admin

Admin access theo permission, không chỉ role name.

## AI/config

Không expose:
- provider secrets;
- internal prompt secrets/config không cần thiết;
- raw operational metadata;
- other users' AI usage.
