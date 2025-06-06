#!/usr/bin/env bash
#
# test_suite_complete.sh - Suíte de Testes Definitiva para a API TCUWatcher
#
# Fases de Execução:
# 1. Verificação de Saúde (Health Check)
# 2. Auditoria de Segurança (Autorização Tripla)
# 3. Teste de Fluxo Funcional (E2E Smoke Test)
# 4. Teste de Cenários Negativos (Validação de Entrada)
# 5. Teste de Carga e Estresse (Load Test)
#
# Pré-requisitos:
#  • API rodando em http://localhost:5092
#  • curl, jq, uuidgen, date, grep, sed, xargs, bc
#
set -euo pipefail

#############################
## CONFIGURAÇÕES E PARÂMETROS
#############################
NUM_LOAD=${1:-1000}        # Itens para o TESTE DE CARGA
PAR_LOAD=${2:-10}          # Concorrência do TESTE DE CARGA
BASE_URL="http://localhost:5092"
AUTH_URL="$BASE_URL/api/auth/login"
UPLOAD_URL="$BASE_URL/api/SessionEvents/upload"
SE_URL="$BASE_URL/api/SessionEvents"
SWAGGER_URL="$BASE_URL/swagger/v1/swagger.json"

VALID_TOKEN=""
INVALID_TOKEN="token-invalido-que-nao-deve-funcionar"

LOGFILE="test_suite_$(date +%Y%m%d_%H%M%S).log"
IDS_FILE="load_test_ids.txt"
ENDPOINTS_FILE="endpoints.json"
ENDPOINTS_LIST="endpoints_list.txt"
: > "$IDS_FILE"

# Funções de Logging
log() { echo -e "[$(date +'%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOGFILE"; }
header() { log "\n======================================================\n# $1\n======================================================"; }

header "INICIANDO SUÍTE DE TESTES COMPLETA"
log "Parâmetros de Carga: $NUM_LOAD itens, $PAR_LOAD processos paralelos."
log "Resultados detalhados serão salvos em: $LOGFILE"

################################################################################
# FASE 1: PREPARAÇÃO E VERIFICAÇÃO DE SAÚDE
################################################################################
header "FASE 1: PREPARAÇÃO E VERIFICAÇÃO DE SAÚDE"

log "Verificando se a API está online em $BASE_URL..."
HEALTH_CHECK_STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/swagger/index.html")
if [[ "$HEALTH_CHECK_STATUS_CODE" != "200" ]]; then
  log "FALHA NO HEALTH CHECK! A API parece estar offline (status: $HEALTH_CHECK_STATUS_CODE). Abortando."
  exit 1
fi
HEALTH_CHECK_STATUS="PASS"
log "Health Check OK (Status 200)."

log "Realizando login para obter token válido..."
LOGIN_RESPONSE=$(curl -s -X POST "$AUTH_URL" -H "Content-Type: application/json" -d '{"email":"usuario@teste","password":"senha"}')
VALID_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r .token)
if [[ -z "$VALID_TOKEN" || "$VALID_TOKEN" == "null" ]]; then
  log "FALHA CRÍTICA: Não foi possível obter o token de login."
  exit 1
fi
log "Token válido obtido com sucesso."

################################################################################
# FASE 2: AUDITORIA DE SEGURANÇA (AUTORIZAÇÃO TRIPLA)
################################################################################
header "FASE 2: AUDITORIA DE SEGURANÇA (AUTORIZAÇÃO TRIPLA)"
AUTH_RESULTS=()
AUTH_TEST_STATUS="PASS"

log "Descobrindo endpoints via Swagger..."
curl -s -o "$ENDPOINTS_FILE" "$SWAGGER_URL"
jq -r '.paths | to_entries[] | .key as $path | .value | keys[] | "\(. | ascii_upcase) \($path)"' "$ENDPOINTS_FILE" > "$ENDPOINTS_LIST"
log "$(wc -l < "$ENDPOINTS_LIST" | tr -d ' ') endpoints encontrados. Testando..."

AUTH_RESULTS+=("$(printf '%-8s %-35s %-15s %-15s %-15s' 'MÉTODO' 'ENDPOINT' 'COM TOKEN' 'SEM TOKEN' 'TOKEN INVÁLIDO')")
AUTH_RESULTS+=("$(printf '%s' '---------------------------------------------------------------------------------------------')")

while read -r line; do
  method=$(echo "$line" | awk '{print $1}') && path=$(echo "$line" | awk '{print $2}')
  
  # LÓGICA CORRIGIDA: Ignora completamente o endpoint de login, pois ele não segue as regras de autorização padrão
  if [[ "$path" == "/api/auth/login" ]]; then
    result_row=$(printf '%-8s %-35s %-15s %-15s %-15s' "$method" "$path" "IGNORADO" "IGNORADO" "IGNORADO")
    AUTH_RESULTS+=("$result_row")
    continue
  fi

  full_url="$BASE_URL${path//\{id\}/00000000-0000-0000-0000-000000000000}"

  # Teste 1: Com token válido
  status_valid=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$full_url" -H "Authorization: Bearer $VALID_TOKEN")
  [[ "$status_valid" == "401" || "$status_valid" == "403" ]] && result_valid="FALHA($status_valid)" && AUTH_TEST_STATUS="FAIL" || result_valid="OK($status_valid)"

  # Teste 2: Sem token
  status_no_token=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$full_url")
  [[ "$status_no_token" != "401" ]] && result_no_token="FALHA($status_no_token)" && AUTH_TEST_STATUS="FAIL" || result_no_token="OK($status_no_token)"

  # Teste 3: Com token inválido
  status_invalid=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$full_url" -H "Authorization: Bearer $INVALID_TOKEN")
  [[ "$status_invalid" != "401" ]] && result_invalid="FALHA($status_invalid)" && AUTH_TEST_STATUS="FAIL" || result_invalid="OK($status_invalid)"

  AUTH_RESULTS+=("$(printf '%-8s %-35s %-15s %-15s %-15s' "$method" "$path" "$result_valid" "$result_no_token" "$result_invalid")")
done < "$ENDPOINTS_LIST"

log "Auditoria de Segurança concluída. Status: $AUTH_TEST_STATUS"
if [[ "$AUTH_TEST_STATUS" == "FAIL" ]]; then
  log "FALHA CRÍTICA na auditoria de segurança. Abortando. Revise a tabela de resultados."
  for entry in "${AUTH_RESULTS[@]}"; do log "  $entry"; done
  exit 1
fi

################################################################################
# FASE 3: TESTE DE FLUXO FUNCIONAL (E2E SMOKE TEST)
################################################################################
header "FASE 3: TESTE DE FLUXO FUNCIONAL (E2E SMOKE TEST)"

SMOKE_TEST_STATUS="FAIL"
TMP_VIDEO_SMOKE="dummy_smoke.bin"
head -c 1024 /dev/zero > "$TMP_VIDEO_SMOKE"

log "Executando um ciclo de vida completo (POST -> GET -> PUT -> DELETE -> GET)..."
upload_response=$(curl -s -w "\n%{http_code}" -X POST "$UPLOAD_URL" -H "Authorization: Bearer $VALID_TOKEN" -F "title=SmokeTest-$(uuidgen)" -F "startedAt=$(date -u '+%Y-%m-%dT%H:%M:%SZ')" -F "videoFile=@$TMP_VIDEO_SMOKE;type=application/octet-stream")
upload_body=$(echo "$upload_response" | head -n -1) && upload_status=$(echo "$upload_response" | tail -n 1)
item_id=$(echo "$upload_body" | jq -r '.id')

if [[ "$upload_status" == "201" && "$item_id" != "null" && -n "$item_id" ]]; then
  log "  [PASS] Upload (POST) criou o item: $item_id"
  get_status=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$SE_URL/$item_id" -H "Authorization: Bearer $VALID_TOKEN")
  if [[ "$get_status" == "200" ]]; then
    log "  [PASS] Verificação (GET) encontrou o item."
    put_status=$(curl -s -o /dev/null -w "%{http_code}" -X PUT "$SE_URL/$item_id" -H "Authorization: Bearer $VALID_TOKEN" -H "Content-Type: application/json" -d '{"isLive": false}')
    if [[ "$put_status" == "200" || "$put_status" == "204" ]]; then
      log "  [PASS] Atualização (PUT) bem-sucedida."
      delete_status=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$SE_URL/$item_id" -H "Authorization: Bearer $VALID_TOKEN")
      if [[ "$delete_status" == "200" || "$delete_status" == "204" ]]; then
        log "  [PASS] Remoção (DELETE) bem-sucedida."
        final_get_status=$(curl -s -o /dev/null -w "%{http_code}" -X GET "$SE_URL/$item_id" -H "Authorization: Bearer $VALID_TOKEN")
        if [[ "$final_get_status" == "404" ]]; then
          log "  [PASS] Item corretamente não foi encontrado após remoção (GET retornou 404)."
          SMOKE_TEST_STATUS="PASS"
        else log "  [FAIL] Item foi encontrado após a remoção (esperava 404, recebeu $final_get_status)."; fi
      else log "  [FAIL] Falha ao remover o item (status: $delete_status)."; fi
    else log "  [FAIL] Falha ao atualizar o item (status: $put_status)."; fi
  else log "  [FAIL] Falha ao buscar o item recém-criado (status: $get_status)."; fi
else log "  [FAIL] Falha ao criar o item no upload (status: $upload_status). Resposta: $upload_body"; fi
rm -f "$TMP_VIDEO_SMOKE"

if [[ "$SMOKE_TEST_STATUS" == "FAIL" ]]; then
  log "FALHA CRÍTICA no teste de fluxo funcional. Abortando teste de carga."
  exit 1
fi

################################################################################
# FASE 4: TESTES DE CENÁRIOS NEGATIVOS
################################################################################
header "FASE 4: TESTES DE CENÁRIOS NEGATIVOS"
NEGATIVE_TEST_STATUS="PASS"
log "Executando testes de validação de entrada..."

log "  Testando POST /upload sem arquivo..."
upload_no_file_status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$UPLOAD_URL" -H "Authorization: Bearer $VALID_TOKEN" -F "title=TesteSemArquivo")
if [[ "$upload_no_file_status" == "400" ]]; then
    log "    [PASS] API retornou 400 Bad Request como esperado."
else
    log "    [FAIL] API retornou $upload_no_file_status em vez de 400."
    NEGATIVE_TEST_STATUS="FAIL"
fi

log "Testes de Cenários Negativos concluídos. Status: $NEGATIVE_TEST_STATUS"
if [[ "$NEGATIVE_TEST_STATUS" == "FAIL" ]]; then
  log "FALHA nos testes de cenário negativo. Abortando teste de carga."
  exit 1
fi

################################################################################
# FASE 5: TESTE DE CARGA E ESTRESSE
################################################################################
header "FASE 5: TESTE DE CARGA E ESTRESSE"

log "Limpando dados remanescentes antes do teste de carga..."
curl_output=$(curl -s -X GET "$SE_URL" -H "Authorization: Bearer $VALID_TOKEN")
( echo "$curl_output" | jq -r '.[].id' | grep -v '^$' ) > "$TO_DELETE_FILE" 2>/dev/null || true
existing_count=$(wc -l < "$TO_DELETE_FILE" | tr -d '[:space:]')
if (( existing_count > 0 )); then
    log "  Limpando $existing_count itens..."
    xargs -a "$TO_DELETE_FILE" -P "$PAR_LOAD" -I{} curl -s -o /dev/null -X DELETE "$SE_URL/{}" -H "Authorization: Bearer $VALID_TOKEN" || true
fi
: > "$IDS_FILE"

log "Iniciando criação de $NUM_LOAD eventos com concorrência de $PAR_LOAD..."
start_create=$(now_ms)
export SE_URL VALID_TOKEN IDS_FILE
seq 1 "$NUM_LOAD" | xargs -P "$PAR_LOAD" -I{} bash -c '
  PAYLOAD="{\"title\":\"Carga-$(uuidgen)\",\"sourceType\":\"LoadTest\",\"startedAt\":\"2025-06-06T12:00:00Z\",\"isLive\":true}"
  ID_NEW=$(curl -s -X POST "'"$SE_URL"'" -H "Content-Type: application/json" -H "Authorization: Bearer '"$VALID_TOKEN"'" -d "$PAYLOAD" | jq -r ".id")
  if [[ "$ID_NEW" != "null" && -n "$ID_NEW" ]]; then echo "$ID_NEW" >> "'"$IDS_FILE"'"; fi
'
end_create=$(now_ms)
created_count=$(wc -l < "$IDS_FILE" | tr -d '[:space:]')
elapsed_create=$(( end_create - start_create ))
log "Criação em carga concluída."

log "Limpando $created_count itens criados no teste de carga..."
if (( created_count > 0 )); then
    xargs -a "$IDS_FILE" -P "$PAR_LOAD" -I{} curl -s -o /dev/null -X DELETE "$SE_URL/{}" -H "Authorization: Bearer $VALID_TOKEN" || true
fi
log "Limpeza pós-carga concluída."

################################################################################
# FASE 6: RELATÓRIO FINAL CONSOLIDADO
################################################################################
header "FASE 6: RELATÓRIO FINAL CONSOLIDADO"

log "STATUS DE SAÚDE DA API: $HEALTH_CHECK_STATUS"
log "STATUS DA AUDITORIA DE SEGURANÇA: $AUTH_TEST_STATUS"
log "STATUS DO TESTE DE FLUXO FUNCIONAL: $SMOKE_TEST_STATUS"
log "STATUS DOS TESTES NEGATIVOS: $NEGATIVE_TEST_STATUS"
log ""

log "------------------- DETALHES DA AUDITORIA DE SEGURANÇA -------------------"
for entry in "${AUTH_RESULTS[@]}"; do log "  $entry"; done
log "--------------------------------------------------------------------------"
log ""

log "------------------- MÉTRICAS DO TESTE DE CARGA ---------------------------"
calculate_avg() {
  local elapsed=$1 && local count=$2 && local avg="0.00"
  if (( count > 0 )); then avg=$(echo "scale=2; $elapsed / $count" | bc); fi
  echo "$avg"
}
avg_create=$(calculate_avg "$elapsed_create" "$created_count")

log "ITENS CRIADOS NA CARGA: $created_count de $NUM_LOAD solicitados."
printf "[PERFORMANCE] Criação (POST): Total: %d ms | Média: %.2f ms/req\n" "$elapsed_create" "$avg_create" | tee -a "$LOGFILE"
log "--------------------------------------------------------------------------"

header "SUÍTE DE TESTES CONCLUÍDA"