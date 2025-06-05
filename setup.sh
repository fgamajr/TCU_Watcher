#!/bin/bash

# Script: inspect_project.sh
# Objetivo: listar diretórios e mostrar conteúdo de arquivos .cs e appsettings*.json,
# excluindo bin/ e obj/ para facilitar validação do código.

set -e

ROOT_DIR="."

echo "=== Diretórios (excluindo bin/ e obj/) ==="
find "$ROOT_DIR" \
  -type d \
  \( -path "*/bin" -o -path "*/obj" \) -prune \
  -o -type d -print

echo
echo "=== Arquivos de código e conteúdo ==="
# Encontrar .cs e appsettings*.json, ignorando bin/ e obj/
find "$ROOT_DIR" \
  \( -path "*/bin" -o -path "*/obj" \) -prune \
  -o -type f \( -name "*.cs" -o -name "appsettings.json" -o -name "appsettings.Development.json" \) -print |
while IFS= read -r file; do
  echo
  echo "----- $file -----"
  sed 's/^/    /' "$file"
done
