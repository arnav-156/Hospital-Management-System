# API reference

All API routes are rooted at `/api`; protected routes require `Authorization: Bearer <JWT>`. List routes accept `page` and `pageSize` (default 25, maximum 100).

| Area | Routes | Access |
|---|---|---|
| Health | `GET /health` | Public |
| Auth | `POST /auth/register`, `POST /auth/login`, `GET /auth/me` | Register/login public; me authenticated |
| Profile | `GET/PUT /profile/me` | Authenticated; PUT patient only |
| Catalog | `GET /departments`, `/departments/{id}`, `/doctors`, `/doctors/{id}`, `/departments/{id}/doctors` | Public |
| Departments | `POST/PUT /departments` | Administrator |
| Appointments | `GET /doctors/{id}/slots`, `POST /appointments`, `GET /appointments/my`, `GET /appointments/{id}` | Slots public; patient routes patient only |
| Doctor appointments | `GET /doctor/appointments/pending`, `/today`, `PUT /appointments/{id}/accept`, `/reject` | Doctor only |
| Treatment/history | `POST /appointments/{id}/treatment`, `GET /patients/{id}/history` | Doctor treatment; patient own or related doctor history |
| Billing | `POST /appointments/{id}/bill`, `GET /bills/my`, `GET /bills/{id}` | Doctor creation; patient own reads |
| Notifications | `GET /notifications`, `PUT /notifications/{id}/read` | Authenticated, own only |
| Feedback | `POST/GET /feedback` | Patient, own completed appointments |
| Administration | `GET /admin/patients`, `/doctors`, `/staff`, `PATCH /admin/accounts/{id}/status` | Administrator |
| AI summary | `POST /patients/{id}/history-summary` | Related doctor only |

Validation errors return 400; missing or non-owned resources return 404; invalid lifecycle/duplicate operations return 409; unauthenticated requests return 401; role violations return 403. Responses include `Cache-Control: no-store` and standard security headers.
