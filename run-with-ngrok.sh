#!/usr/bin/env bash
set -euo pipefail

# Starts the API and ngrok, then prints the public ngrok URL.
# Usage: ./run-with-ngrok.sh

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_DIR="$ROOT_DIR/src/LibraryManagement.API"
PORT=5252

cd "$API_DIR"

echo "Starting .NET API in background (port $PORT)..."
if command -v dotnet >/dev/null 2>&1; then
  # try dotnet watch run for developer convenience, fall back to dotnet run
  (dotnet watch run --urls http://localhost:$PORT) &
  DOTNET_PID=$!
  # give it a few seconds to start
  sleep 1
  # if process died quickly, fallback
  if ! kill -0 "$DOTNET_PID" 2>/dev/null; then
    echo "dotnet watch did not start; falling back to dotnet run"
    (dotnet run --urls http://localhost:$PORT) &
    DOTNET_PID=$!
  fi
else
  echo "dotnet not found on PATH; please install .NET SDK"
  exit 1
fi

echo "Started dotnet (pid $DOTNET_PID)."

if ! command -v ngrok >/dev/null 2>&1; then
  echo "ngrok not found on PATH. Install ngrok and ensure it's available." >&2
  exit 1
fi

echo "Starting ngrok (http -> localhost:$PORT) in background..."
(ngrok http $PORT --log=stdout) &
NGROK_PID=$!

echo "Waiting for ngrok to publish a tunnel (this may take a few seconds)..."
NGROK_API="http://127.0.0.1:4040/api/tunnels"
URL=""
for i in $(seq 1 30); do
  sleep 1
  if curl -s "$NGROK_API" >/dev/null 2>&1; then
    URL=$(curl -s "$NGROK_API" | python3 -c "import sys,json
d=json.load(sys.stdin)
ts=d.get('tunnels') or []
print(ts[0]['public_url'] if ts else '')" 2>/dev/null || true)
    if [ -n "$URL" ]; then
      echo "ngrok public URL: $URL"
      echo "$URL" > /tmp/librarymanagement_ngrok_url.txt
      break
    fi
  fi
done

if [ -z "$URL" ]; then
  echo "Failed to detect ngrok URL. Check ngrok output or run 'ngrok http $PORT' manually." >&2
fi

echo "To stop: kill $DOTNET_PID and $NGROK_PID or Ctrl-C this script." 

# wait for dotnet process so the script stays alive
wait "$DOTNET_PID"
