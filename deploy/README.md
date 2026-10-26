# Deploy — https://xanthos.tugrul.net

Goal: the demo lives behind Hestia (panel-visible web domain, HTTPS) and
proxies to the Blazor app. Agent and photo pipeline stay localhost-only.

## Port map (server)

| Service | Port | Exposed |
|---|---|---|
| Blazor web (`xanthos-web`) | 127.0.0.1:**5010** | via nginx/HTTPS only |
| buyer agent (`xanthos-agent`) | 127.0.0.1:5011 | never |
| photo pipeline (`xanthos-pipeline`) | 127.0.0.1:5012 | never |
| Chim app | **5005** | do not touch, do not reuse |

## 1. Hestia panel (owner) — domain must exist in the panel

1. Log in to Hestia → the target user → **Web** → **Add Web Domain**:
   `xanthos.tugrul.net`, template `default`.
2. Domain → **SSL** tab → **Let's Encrypt** → enable (expires auto-renews in Hestia).
3. Domain → backend/proxy → `127.0.0.1:5010` (panel's Backend field, or
   "Shared/Proxy" template). This makes the domain panel-managed, as required.

## 2. Upload (from the dev machine, after `main` merge)

```powershell
dotnet publish src/XanthosResidentialAI/XanthosResidentialAI.csproj -c Release -o publish/web
scp -r publish/web/*            SERVER:/opt/xanthos/web/
scp -r python/buyer-agent       SERVER:/opt/xanthos/buyer-agent      # without .venv
scp -r python/photo-pipeline    SERVER:/opt/xanthos/photo-pipeline   # without .venv
scp -r deploy                   SERVER:/opt/xanthos/deploy
```

## 3. Server (root)

```bash
bash /opt/xanthos/deploy/setup.sh     # user, venvs, systemd, health checks
sudo nano /etc/xanthos/agent.env      # MODEL_API_KEY=... when credit lands
systemctl restart xanthos-agent       # until then the agent runs rules-only
```

`setup.sh` only creates `/etc/xanthos/agent.env` if it does not exist; the
key itself is entered on the server and never leaves it.

## 4. WebSocket proxy (required — Blazor Server fails silently without it)

Apply `deploy/nginx-ws-proxy.conf` to the domain's nginx conf (verify the real
path on the server first — Hestia versions differ:

```bash
ls /etc/nginx/conf.d/ | grep xanthos
ls /usr/local/hestia/data/users/<user>/conf/web/nginx/ | grep xanthos
```

Edit the generated conf and mirror the change into Hestia's template source so
`v-rebuild` does not lose it, then:

```bash
nginx -t && systemctl reload nginx
```

## 5. Verify

- `curl -I https://xanthos.tugrul.net/` → 200
- Browser DevTools → Network → WS: `wss://xanthos.tugrul.net/_blazor` connects
- `/` shows the one flow; agent badge shows `rules-only fallback` until the key
  is set, then `LangGraph · Muse Spark`
- `curl http://127.0.0.1:5011/health` and `:5012/health` → ok

## Rollback

```bash
systemctl disable --now xanthos-web xanthos-agent xanthos-pipeline
```
Delete the domain in the panel if needed. Repo data is never touched by any
of this (demo results live outside the live DB by rule).

## Security rules applied here

- Secrets (`MODEL_API_KEY`) exist only in `/etc/xanthos/*.env` (root, 600).
  Nothing in this folder contains a secret; run the pre-push scan before
  every push and the post-push scan after, as always.
- Ports 5011/5012 are bound to 127.0.0.1 and are not proxied publicly.
- Do not expose the Hestia panel port in demo screenshots.
