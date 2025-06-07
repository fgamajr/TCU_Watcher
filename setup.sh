#!/usr/bin/env bash
#
# create_missing_errors.sh - Cria os arquivos de DomainError faltantes
# para o projeto TCUWatcher, conforme solicitado.
#
set -euo pipefail

# Define o diretório raiz como o diretório atual onde o script é executado.
ROOT_DIR="$(pwd)"

echo "======================================================="
echo "# Criando os arquivos de DomainError faltantes..."
echo "# Local de destino: TCUWatcher.Domain/Errors/"
echo "======================================================="

# Garante que a pasta de destino exista, sem dar erro se já existir.
mkdir -p "$ROOT_DIR/TCUWatcher.Domain/Errors"

# --- Criar InvalidProcessNumberError.cs ---
cat > "$ROOT_DIR/TCUWatcher.Domain/Errors/InvalidProcessNumberError.cs" << 'EOF'
namespace TCUWatcher.Domain.Errors;

public sealed record InvalidProcessNumberError(string Number)
    : DomainError("ProcessNumber.Invalid",
                  $"Número de processo inválido: {Number}");
EOF
echo "  -> Arquivo 'InvalidProcessNumberError.cs' criado com sucesso."

# --- Criar LiveAlreadyEndedError.cs ---
cat > "$ROOT_DIR/TCUWatcher.Domain/Errors/LiveAlreadyEndedError.cs" << 'EOF'
using System;

namespace TCUWatcher.Domain.Errors;

public sealed record LiveAlreadyEndedError(Guid SessionId)
    : DomainError("Session.LiveAlreadyEnded",
                  $"A sessão {SessionId} já terminou.");
EOF
echo "  -> Arquivo 'LiveAlreadyEndedError.cs' criado com sucesso."

echo -e "\n# Processo concluído."
echo "# Agora, por favor, tente compilar o projeto novamente com 'dotnet build'."
echo "======================================================="

exit 0