#!/usr/bin/env bash
#
# setup.sh (versão simplificada, sem read heredoc)
#
# Script para testar automaticamente os endpoints de SessionEvents com saída completa:
# - Token mock fixo
# - Criar 3 sessões (POST)
# - Listar todas (GET)
# - Buscar cada sessão por ID (GET)
# - Atualizar cada sessão (PUT)
# - Deletar cada sessão (DELETE)
# - Listar novamente para confirmar remoções
#
# Ajuste BASE_URL conforme a porta usada no seu ambiente.

set -euo pipefail
set -x  # ativa “echo” de cada comando antes de executá-lo

# ========== CONFIGURAÇÃO ==========
# Se sua API estiver em outra porta, ajuste aqui:
BASE_URL="http://localhost:5092/api"
SE_URL="$BASE_URL/SessionEvents"
TOKEN="mock-token-abc123"

# Função para extrair o "id" de um JSON simples:
# Exemplo: {"id":"abc123", ...} → abc123
extract_id() {
  local json="$1"
  echo "$json" | sed -E 's/.*"id":"([^"]+)".*/\1/'
}

echo "=== Teste automático de SessionEvents ==="
echo

# 1) Token mock (serviço sempre retorna mock-token-abc123)
echo "1) (Token fixo) -> $TOKEN"
echo

# 2) CRIAR 3 SESSÕES VIA POST (cada payload direto em variável)
echo "2) Criando 3 sessões distintas via POST..."
IDS=()

for i in 1 2 3; do
  TITLE="Sessão Teste #$i"
  if [ "$i" -eq 1 ]; then
    SOURCE_TYPE="YouTube"
    SOURCE_ID="yt-video-$(date +%s)-$i"
    STARTED_AT="2025-06-05T10:0${i}:00Z"
    IS_LIVE=true
  else
    SOURCE_TYPE="ManualUpload"
    SOURCE_ID=null
    STARTED_AT="2025-06-05T11:0${i}:00Z"
    IS_LIVE=false
  fi

  # Monta JSON de criação direto em string
  if [ "$SOURCE_ID" = null ]; then
    PAYLOAD="{ \
      \"title\":\"$TITLE\", \
      \"sourceType\":\"$SOURCE_TYPE\", \
      \"sourceId\":null, \
      \"startedAt\":\"$STARTED_AT\", \
      \"isLive\":$IS_LIVE \
    }"
  else
    PAYLOAD="{ \
      \"title\":\"$TITLE\", \
      \"sourceType\":\"$SOURCE_TYPE\", \
      \"sourceId\":\"$SOURCE_ID\", \
      \"startedAt\":\"$STARTED_AT\", \
      \"isLive\":$IS_LIVE \
    }"
  fi

  echo ">>> Payload para sessão $i:"
  echo "$PAYLOAD"
  echo

  # Faz POST com verbosidade e captura o JSON de resposta
  RESPONSE_JSON=$(
    curl -v -s -w "\n" -X POST "$SE_URL" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $TOKEN" \
      -d "$PAYLOAD"
  )

  echo ">>> Resposta JSON pura (sessão $i):"
  echo "$RESPONSE_JSON"
  echo

  NEW_ID=$(extract_id "$RESPONSE_JSON")
  IDS+=("$NEW_ID")
  echo "  • Criada sessão $i -> ID = $NEW_ID"
  echo
done

echo
# 3) LISTAR TODAS AS SESSÕES (GET)
echo "3) Listando todas as sessões com GET..."
curl -v -s -X GET "$SE_URL" \
  -H "accept: application/json" \
  -H "Authorization: Bearer $TOKEN"
echo -e "\n"

echo
# 4) BUSCAR CADA SESSÃO POR ID (GET /SessionEvents/{id})
echo "4) Buscando cada sessão pelo ID..."
for id in "${IDS[@]}"; do
  echo -n "  • ID = $id -> "
  curl -v -s -X GET "$SE_URL/$id" \
    -H "accept: application/json" \
    -H "Authorization: Bearer $TOKEN"
  echo
done

echo
# 5) ATUALIZAR CADA SESSÃO (PUT /SessionEvents/{id})
echo "5) Atualizando cada sessão (PUT)..."
for id in "${IDS[@]}"; do
  UPDATE_PAYLOAD="{ \
    \"isLive\":false, \
    \"endedAt\":\"2025-06-05T12:00:00Z\" \
  }"
  echo -n "  • Atualizando ID = $id ... "
  STATUS_CODE=$(
    curl -v -s -o /dev/null -w "%{http_code}" -X PUT "$SE_URL/$id" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $TOKEN" \
      -d "$UPDATE_PAYLOAD"
  )
  echo "HTTP $STATUS_CODE"

  # Exibe o JSON atualizado
  echo -n "    → Atualizado: "
  curl -v -s -X GET "$SE_URL/$id" \
    -H "accept: application/json" \
    -H "Authorization: Bearer $TOKEN"
  echo
done

echo
# 6) DELETE CADA SESSÃO (DELETE /SessionEvents/{id})
echo "6) Removendo cada sessão (DELETE)..."
for id in "${IDS[@]}"; do
  echo -n "  • Deletando ID = $id ... "
  STATUS_CODE=$(
    curl -v -s -o /dev/null -w "%{http_code}" -X DELETE "$SE_URL/$id" \
      -H "Authorization: Bearer $TOKEN"
  )
  echo "HTTP $STATUS_CODE"
done

echo
# 7) LISTAR NOVAMENTE (GET /SessionEvents) PARA CONFIRMAR REMOÇÕES
echo "7) Listando todas as sessões após remoções (deve ser vazio)..."
curl -v -s -X GET "$SE_URL" \
  -H "accept: application/json" \
  -H "Authorization: Bearer $TOKEN"
echo -e "\n"

echo "=== FIM DO TESTE AUTOMÁTICO ==="
