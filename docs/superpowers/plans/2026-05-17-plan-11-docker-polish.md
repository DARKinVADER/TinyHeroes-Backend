# TinyHeroes — Plan 11: Docker Production Polish

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix nginx configuration so avatar uploads and file serving work correctly in the Docker production environment.

**Architecture:** In Docker, nginx is the single entry point. It must proxy both `/api/` and `/uploads/` to the backend API container, and allow 5MB request bodies for avatar uploads.

**Tech Stack:** nginx, Docker Compose

---

## Context

The frontend nginx.conf currently proxies `/api/` to the backend but does NOT proxy `/uploads/`. The backend serves static uploaded files at `/uploads/avatars/{file}` via `UseStaticFiles`. In Docker, all user requests hit nginx first — without the `/uploads/` proxy, avatar images return 404.

Additionally, nginx's default `client_max_body_size` is 1MB, but the avatar upload endpoint allows up to 5MB (`[RequestSizeLimit(5 * 1024 * 1024)]`). Without raising the nginx limit, large avatar uploads will fail with `413 Request Entity Too Large`.

---

## Task Overview (1 Task)

| # | Task | Layer |
|---|------|-------|
| 1 | Fix nginx.conf: add /uploads/ proxy + client_max_body_size | Infra |

---

### Task 1: Fix nginx.conf

**Files:**
- Modify: `frontend/nginx.conf`

Replace with:
```nginx
server {
    listen 80;
    root /usr/share/nginx/html;
    index index.html;

    client_max_body_size 6m;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://api:8080/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }

    location /uploads/ {
        proxy_pass http://api:8080/uploads/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

**Verification:**
- `cd frontend && npx ng build --configuration production` — 0 errors (no code change, but confirms build still works)

**Commit:** `fix: nginx proxy for /uploads/ and 6MB body limit — Plan 11 complete`

---

## Verification Checklist

- [ ] nginx.conf has `/uploads/` location block proxying to api:8080
- [ ] nginx.conf has `client_max_body_size 6m;`
- [ ] Frontend still builds
