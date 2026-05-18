#!/usr/bin/env bash
set -euo pipefail

api_base="${SWIFTPOS_API_BASE:-http://127.0.0.1:5022}"
email="${SWIFTPOS_DEMO_EMAIL:-admin@swiftpos.local}"
password="${SWIFTPOS_DEMO_PASSWORD:-SwiftposDemo123!}"

login_response="$(curl -fsS --retry 5 --retry-connrefused --retry-delay 1 \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"${email}\",\"password\":\"${password}\"}" \
  "${api_base}/auth/login")"

access_token="$(printf '%s' "${login_response}" | sed -n 's/.*"accessToken"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"

if [ -z "${access_token}" ]; then
  echo "Login response did not include accessToken." >&2
  exit 1
fi

curl -fsS --retry 5 --retry-connrefused --retry-delay 1 -H "Authorization: Bearer ${access_token}" "${api_base}/auth/me" >/dev/null
curl -fsS --retry 5 --retry-connrefused --retry-delay 1 -H "Authorization: Bearer ${access_token}" "${api_base}/catalog/categories" >/dev/null
curl -fsS --retry 5 --retry-connrefused --retry-delay 1 -H "Authorization: Bearer ${access_token}" "${api_base}/catalog/products" >/dev/null

echo "Smoke test passed: login, auth/me, catalog/categories, catalog/products."
