#!/bin/sh
set -eu

echo "Commerce host starting (ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production})"

exec "$@"
