#!/usr/bin/env bash
# Xanthos Residential AI — demo server setup (xanthos.tugrul.net)
# Run as root/sudo AFTER the code has been uploaded to /opt/xanthos:
#   /opt/xanthos/web/                 <- dotnet publish output
#   /opt/xanthos/buyer-agent/         <- python/buyer-agent (without .venv)
#   /opt/xanthos/photo-pipeline/      <- python/photo-pipeline (without .venv)
#   /opt/xanthos/deploy/              <- this folder (units + proxy snippet)
#
# Ports (server): web 5010, agent 5011, pipeline 5012 — localhost only.
# NEVER 5005 (Chim app owns it and hijacks tugrul.net today).
set -euo pipefail

APP_USER=xanthosapp
ROOT=/opt/xanthos
VENVS=$ROOT/venvs

echo "== 1/6 checks =="
command -v dotnet >/dev/null || { echo "FATAL: dotnet runtime missing"; exit 1; }
dotnet --list-runtimes | grep -q "Microsoft.NETCore.App 10\." \
  || { echo "FATAL: .NET 10 runtime missing (need 10.x)"; exit 1; }
command -v python3 >/dev/null || { echo "FATAL: python3 missing"; exit 1; }
echo "dotnet runtimes OK, python3 OK"

echo "== 2/6 app user + dirs =="
id -u "$APP_USER" >/dev/null 2>&1 || useradd --system --home /var/lib/xanthos --create-home --shell /usr/sbin/nologin "$APP_USER"
mkdir -p /etc/xanthos /var/cache/xanthos/hf
chown -R "$APP_USER:$APP_USER" /var/cache/xanthos /var/lib/xanthos

echo "== 3/6 python venvs =="
if [ ! -x "$VENVS/agent/bin/python" ]; then
  python3 -m venv "$VENVS/agent"
  "$VENVS/agent/bin/pip" install --quiet --upgrade pip
  "$VENVS/agent/bin/pip" install --quiet -r "$ROOT/buyer-agent/requirements.txt"
fi
if [ ! -x "$VENVS/pipeline/bin/python" ]; then
  python3 -m venv "$VENVS/pipeline"
  "$VENVS/pipeline/bin/pip" install --quiet --upgrade pip
  "$VENVS/pipeline/bin/pip" install --quiet -r "$ROOT/photo-pipeline/requirements.txt"
  # torch CPU (no GPU on this server); CLIP downloads on first /warmup
  "$VENVS/pipeline/bin/pip" install --quiet torch==2.5.1 --index-url https://download.pytorch.org/whl/cpu
fi
echo "venvs ready"

echo "== 4/6 server-only env files (secrets stay here, never in git) =="
if [ ! -f /etc/xanthos/agent.env ]; then
  cat > /etc/xanthos/agent.env <<'EOF'
# Server-only secrets — never committed anywhere.
# Fill in the Meta Model API key (dashboard: meta.ai) then:
#   systemctl restart xanthos-agent
MODEL_API_KEY=
EOF
  chmod 600 /etc/xanthos/agent.env
  echo "NOTE: created /etc/xanthos/agent.env — put MODEL_API_KEY there when the credit lands."
fi

echo "== 5/6 systemd units =="
install -m 644 "$ROOT/deploy/xanthos-web.service"      /etc/systemd/system/
install -m 644 "$ROOT/deploy/xanthos-agent.service"    /etc/systemd/system/
install -m 644 "$ROOT/deploy/xanthos-pipeline.service" /etc/systemd/system/
# pipeline needs the HF cache dir for CLIP
grep -q HF_HOME /etc/systemd/system/xanthos-pipeline.service \
  || sed -i '/EnvironmentFile/i Environment=HF_HOME=/var/cache/xanthos/hf' /etc/systemd/system/xanthos-pipeline.service
systemctl daemon-reload
systemctl enable --now xanthos-pipeline xanthos-agent xanthos-web

echo "== 6/6 health checks =="
sleep 3
for target in "5012/health pipeline" "5011/health agent" "5010/ web"; do
  port="${target%%/*}"; name="${target##* }"
  if curl -fsS --max-time 10 "http://127.0.0.1:$port/" >/dev/null; then
    echo "OK   $name (127.0.0.1:$port)"
  else
    echo "FAIL $name (127.0.0.1:$port) — check: systemctl status xanthos-$name"
  fi
done

cat <<'EOF'

Next steps (see deploy/README.md):
  1. Hestia panel: web domain xanthos.tugrul.net under the user, SSL (Let's Encrypt),
     backend proxy -> 127.0.0.1:5010
  2. Apply deploy/nginx-ws-proxy.conf to that domain's nginx conf (Blazor needs
     WebSocket upgrade headers), then: nginx -t && systemctl reload nginx
  3. When the Meta credit lands: sudo nano /etc/xanthos/agent.env (MODEL_API_KEY=...)
     && systemctl restart xanthos-agent
EOF
