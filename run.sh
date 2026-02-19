#!/usr/bin/env bash
set -euo pipefail

# Prefer using the ngrok helper to start both the API and ngrok together.
ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
HELPER="$ROOT_DIR/run-with-ngrok.sh"

if [ -x "$HELPER" ]; then
	echo "Launching API with ngrok helper: $HELPER"
	exec "$HELPER"
else
	echo "Helper $HELPER not found or not executable — starting API directly"
	cd "$ROOT_DIR/src/LibraryManagement.API"
	exec dotnet run
fi