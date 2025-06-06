#!/usr/bin/env bash
#
# load_test_parallel.sh (versão final com cleanup à prova de falhas)
#
# Teste de carga paralelo para os endpoints de SessionEvents, com logs detalhados e
# cleanup seguro mesmo se o GET falhar:
#   0) Limpa tudo o que houver (DELETE em massa)
#   1) Cria N eventos em paralelo (POST)
#   2) Consulta cada evento em paralelo (GET /{id})
#   3) Lista todos em memória (GET /SessionEvents)
#   4) Atualiza em paralelo (PUT /{id})
#   5) Deleta em paralelo (DELETE /{id})
#   6) Lista novamente para confirmar (GET /SessionEvents)
#   7) Exibe resumo de tempos (total + média por requisição) e contagens
#
# Uso:
#   ./load_test_parallel.sh [NUM_EVENTS] [CONCURRENCY]
#     - NUM_EVENTS  (default = 100000)
#     - CONCURRENCY (default = 100)
#
# Pré-requisitos:
#   • API rodando em http://localhost:5092/api
#   • Token mock fixo "mock-token-abc123"
#   • uuidgen disponível (para títulos únicos)
#   • date +%s%3N disponível (para medir ms)
#   • grep, sed, xargs disponíveis

set -euo pipefail

#############################
## PARÂMETROS & VARIÁVEIS
#############################
NUM=${1:-100000}   # Quantos eventos criar
PAR=${2:-100}      # Quantos processos paralelos

BASE_URL="http://localhost:5092/api"
SE_URL="$BASE_URL/SessionEvents"
TOKEN="mock-token-abc123"

IDS_FILE="ids.txt"
TO_DELETE_FILE="to_delete.txt"

: > "$IDS_FILE"       # Gera/limpa lista de novos IDs
: > "$TO_DELETE_FILE"  # Gera/limpa lista de IDs a deletar

# Função para obter timestamp em milissegundos
now_ms() {
  date +%s%3N
}

echo "=============================================="
echo " $(date '+%Y-%m-%d %H:%M:%S')  INICIANDO TESTE DE CARGA PARA SessionEvents"
echo "   ▶ Destino:       $BASE_URL"
echo "   ▶ Criar:         $NUM eventos"
echo "   ▶ Concorrência:  $PAR processos"
echo "=============================================="
echo

#############################
## 0) CLEANUP: REMOVER TUDO
#############################
echo "[$(date '+%H:%M:%S')] 0) Iniciando cleanup: removendo todas as sessões existentes..."

# 0.1) Tentar buscar IDs; se falhar, pular cleanup
curl_output=""
if ! curl_output=$(curl -s -X GET "$SE_URL" -H "Authorization: Bearer $TOKEN"); then
  echo "   ▶ Falha ao buscar sessões (curl retornou erro). Pulando cleanup."
  existing_count=0
else
  # Extrai todos os IDs (um por linha), ignorando linhas em branco
  echo "$curl_output" \
    | grep -o '"id":"[^"]\+"' \
    | sed -E 's/"id":"([^"]+)"/\1/' \
    | grep -v '^$' \
    > "$TO_DELETE_FILE"
  existing_count=$(wc -l < "$TO_DELETE_FILE" | tr -d '[:space:]')
fi

if (( existing_count > 0 )); then
  echo "   ▶ Encontrados $existing_count IDs para remoção."
  echo "   ▶ Removendo em até $PAR processos paralelos..."

  # 0.2) Deleta todos; usamos -a e -P. Mesmo que algum delete falhe, contornamos com || true.
  xargs -a "$TO_DELETE_FILE" -P "$PAR" -I{} \
    bash -c 'curl -s -o /dev/null -X DELETE "'"$SE_URL"'/{}" -H "Authorization: Bearer '"$TOKEN"'" || true'

  echo "[$(date '+%H:%M:%S')]  → Cleanup concluído: $existing_count sessões removidas."
else
  echo "   ▶ Não há sessões para remover (ou falha na busca). Pulando cleanup."
fi
echo

#############################
## 1) CRIAÇÃO EM PARALELO (POST)
#############################
echo "[$(date '+%H:%M:%S')] 1) Iniciando criação de $NUM eventos em paralelo (POST)..."
start_create=$(now_ms)

export SE_URL TOKEN IDS_FILE

# 1.1) Cria em paralelo, grava cada ID em IDS_FILE
seq 1 "$NUM" \
  | xargs -P "$PAR" -I{} bash -c '
    IDX="{}"
    TITLE="Carga-${IDX}-$(uuidgen)"
    SOURCE_TYPE="ManualUpload"
    STARTED_AT="2025-06-05T12:00:00Z"
    IS_LIVE=false

    PAYLOAD="{\"title\":\"$TITLE\",\"sourceType\":\"$SOURCE_TYPE\",\"sourceId\":null,\"startedAt\":\"$STARTED_AT\",\"isLive\":$IS_LIVE}"

    RESPONSE_JSON=$(curl -s -X POST "'"$SE_URL"'" \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer '"$TOKEN"'" \
      -d "$PAYLOAD")

    ID_NEW=$(echo "$RESPONSE_JSON" | sed -E "s/.*\"id\":\"([^\"]+)\".*/\1/")
    if [[ -n "$ID_NEW" ]]; then
      echo "$ID_NEW" >> "'"$IDS_FILE"'"
    fi
  '

end_create=$(now_ms)
elapsed_create=$(( end_create - start_create ))
created_count=$(wc -l < "$IDS_FILE" | tr -d '[:space:]')
echo "[$(date '+%H:%M:%S')]  → Criação finalizada: $created_count eventos criados."
echo "    • Tempo total: ${elapsed_create}ms   | Média ≈ $(( elapsed_create / (created_count>0?created_count:1) ))ms/req"
echo

#############################
## 2) CONSULTA EM PARALELO (GET /{id})
#############################
echo "[$(date '+%H:%M:%S')] 2) Consultando $created_count eventos em paralelo (GET /SessionEvents/{id})..."
start_getid=$(now_ms)

if (( created_count > 0 )); then
  xargs -a "$IDS_FILE" -P "$PAR" -I{} bash -c '
    ID="{}"
    if [[ -n "$ID" ]]; then
      curl -s -o /dev/null -X GET "'"$SE_URL"'/$ID" \
        -H "accept: application/json" \
        -H "Authorization: Bearer '"$TOKEN"'" || true
    fi
  '
fi

end_getid=$(now_ms)
elapsed_getid=$(( end_getid - start_getid ))
echo "[$(date '+%H:%M:%S')]  → Consulta por ID concluída."
echo "    • Tempo total: ${elapsed_getid}ms   | Média ≈ $(( elapsed_getid / (created_count>0?created_count:1) ))ms/req"
echo

#############################
## 3) LISTAR TODOS (GET /SessionEvents)
#############################
echo "[$(date '+%H:%M:%S')] 3) Listando todos os eventos criados (GET /SessionEvents)..."
start_getall=$(now_ms)

all_response=$(curl -s -X GET "$SE_URL" \
  -H "accept: application/json" \
  -H "Authorization: Bearer $TOKEN" || echo "[]")

end_getall=$(now_ms)
elapsed_getall=$(( end_getall - start_getall ))
total_after_create=$(echo "$all_response" | grep -o '"id":"[^"]\+"' | wc -l | tr -d '[:space:]')
echo "[$(date '+%H:%M:%S')]  → Listagem após criação retornou $total_after_create eventos."
echo "    • Tempo de listagem: ${elapsed_getall}ms"
echo

#############################
## 4) ATUALIZAÇÃO EM PARALELO (PUT /{id})
#############################
echo "[$(date '+%Y-%m-%d %H:%M:%S')] 4) Atualizando $created_count eventos (PUT /SessionEvents/{id})..."
start_update=$(now_ms)

if (( created_count > 0 )); then
  xargs -a "$IDS_FILE" -P "$PAR" -I{} bash -c '
    ID="{}"
    if [[ -n "$ID" ]]; then
      UPDATE_PAYLOAD="{\"isLive\":false,\"endedAt\":\"2025-06-05T23:59:59Z\"}"
      curl -s -o /dev/null -X PUT "'"$SE_URL"'/$ID" \
        -H "Content-Type: application/json" \
        -H "Authorization: Bearer '"$TOKEN"'" \
        -d "$UPDATE_PAYLOAD" || true
    fi
  '
fi

end_update=$(now_ms)
elapsed_update=$(( end_update - start_update ))
echo "[$(date '+%Y-%m-%d %H:%M:%S')]  → Atualização concluída para $created_count eventos."
echo "    • Tempo total: ${elapsed_update}ms   | Média ≈ $(( elapsed_update / (created_count>0?created_count:1) ))ms/req"
echo

#############################
## 5) REMOÇÃO EM PARALELO (DELETE /{id})
#############################
echo "[$(date '+%H:%M:%S')] 5) Removendo $created_count eventos em paralelo (DELETE /SessionEvents/{id})..."
start_delete=$(now_ms)

if (( created_count > 0 )); then
  xargs -a "$IDS_FILE" -P "$PAR" -I{} \
    curl -s -o /dev/null -X DELETE "$SE_URL/{}" \
      -H "Authorization: Bearer $TOKEN" || true
fi

end_delete=$(now_ms)
elapsed_delete=$(( end_delete - start_delete ))
echo "[$(date '+%H:%M:%S')]  → Remoção concluída para $created_count eventos."
echo "    • Tempo total: ${elapsed_delete}ms   | Média ≈ $(( elapsed_delete / (created_count>0?created_count:1) ))ms/req"
echo

#############################
## 6) LISTAR TODOS NOVAMENTE (GET /SessionEvents)
#############################
echo "[$(date '+%H:%M:%S')] 6) Listando após remoções (GET /SessionEvents) para confirmar..."
start_final=$(now_ms)

final_response=$(curl -s -w "\n" -X GET "$SE_URL" \
  -H "accept: application/json" \
  -H "Authorization: Bearer $TOKEN" || echo "[]")

end_final=$(now_ms)
elapsed_final=$(( end_final - start_final ))
remaining_count=$(echo "$final_response" | grep -o '"id":"[^"]\+"' | wc -l | tr -d '[:space:]')

echo "[$(date '+%H:%M:%S')]  → Lista final retornou $remaining_count eventos."
echo "    • Tempo do GET final: ${elapsed_final}ms"
echo

#############################
## 7) RESUMO DOS TEMPOS
#############################
echo "=============================================="
echo " $(date '+%Y-%m-%d %H:%M:%S')  RESUMO FINAL DE TEMPOS"
echo "----------------------------------------------"
printf " Criação    -> total: %8d ms   | média: %5d ms/req\n"  "$elapsed_create" \
  $(( elapsed_create / (created_count>0?created_count:1) ))
printf " GET IDs    -> total: %8d ms   | média: %5d ms/req\n"  "$elapsed_getid"  \
  $(( elapsed_getid / (created_count>0?created_count:1) ))
printf " Listar 1   -> total: %8d ms   | itens retornados: %d\n"  "$elapsed_getall" \
  "$total_after_create"
printf " Atualizar  -> total: %8d ms   | média: %5d ms/req\n"  "$elapsed_update" \
  $(( elapsed_update / (created_count>0?created_count:1) ))
printf " Remover    -> total: %8d ms   | média: %5d ms/req\n"  "$elapsed_delete" \
  $(( elapsed_delete / (created_count>0?created_count:1) ))
printf " Listar 2   -> total: %8d ms   | itens restantes: %d\n" "$elapsed_final" \
  "$remaining_count"
echo "=============================================="
echo " $(date '+%Y-%m-%d %H:%M:%S')  TESTE DE CARGA CONCLUÍDO"
echo "=============================================="
