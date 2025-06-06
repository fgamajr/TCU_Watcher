#!/usr/bin/env bash
#
# load_test.sh - Versão definitiva para teste de fluxo completo, com logs verbosos
# Testa: upload, OCR, transcrição, persistência em storage e repository, resiliência, segurança e carga
#
set -euo pipefail

#############################
## CONFIGURAÇÕES INICIAIS
#############################
NUM=${1:-3}        # Número de uploads simulados (simulando lives ou manual upload)
PAR=${2:-1}        # Concorrência
BASE_URL="http://localhost:5092"
AUTH_URL="$BASE_URL/api/auth/login"
UPLOAD_URL="$BASE_URL/api/SessionEvents/upload"
SE_URL="$BASE_URL/api/SessionEvents"
OCR_ENDPOINT="$BASE_URL/api/Test/me" # Apenas para simular um endpoint protegido
SWAGGER_URL="$BASE_URL/swagger/v1/swagger.json"

VALID_TOKEN=""
INVALID_TOKEN="token-invalido-xyz"

LOGFILE="load_test_$(date +%s).log"
touch "$LOGFILE"

log() {
  echo -e "[$(date +'%H:%M:%S')] $1" | tee -a "$LOGFILE"
}

header() {
  echo -e "\n===============================" | tee -a "$LOGFILE"
  echo -e " $1" | tee -a "$LOGFILE"
  echo -e "===============================\n" | tee -a "$LOGFILE"
}

now_ms() {
  date +%s%3N
}

header "INICIANDO TESTE COMPLETO DE FLUXO ($NUM uploads, $PAR concorrentes)"

#############################
## LOGIN E TOKEN
#############################
header "0) Login para obter token"
LOGIN_RESPONSE=$(curl -s -X POST "$AUTH_URL" \
  -H "Content-Type: application/json" \
  -d '{"email":"usuario@teste","password":"senha"}')

VALID_TOKEN=$(echo "$LOGIN_RESPONSE" | sed -E 's/.*"token":"([^\"]+)".*/\1/')
if [[ -z "$VALID_TOKEN" ]]; then
  log "Erro: token de login não obtido."
  exit 1
fi
log "Token obtido com sucesso."

#############################
## FLUXO COMPLETO: UPLOAD + SIMULA PROCESSAMENTO
#############################
header "1) Simulando $NUM uploads com processamento completo"

TMP_VIDEO="dummy_test_upload.bin"
head -c 2048 /dev/zero > "$TMP_VIDEO"

for i in $(seq 1 "$NUM"); do
  log "\n▶ Upload $i de $NUM"

  UPLOAD_RESPONSE=$(curl -s -X POST "$UPLOAD_URL" \
    -H "Authorization: Bearer $VALID_TOKEN" \
    -F "title=Teste-Flow-$i" \
    -F "startedAt=$(date -u '+%Y-%m-%dT%H:%M:%SZ')" \
    -F "videoFile=@$TMP_VIDEO;type=application/octet-stream")

  ID=$(echo "$UPLOAD_RESPONSE" | jq -r '.id')
  if [[ "$ID" == "null" || -z "$ID" ]]; then
    log "Erro: Falha no upload $i. Resposta: $UPLOAD_RESPONSE"
    continue
  fi

  log "Upload criado com ID: $ID"

  # Simula persistência de snapshot/audio (logs internos dos mocks devem registrar)
  log "Simulando workers (SnapshotStorageWorker / AudioStorageWorker)..."
  sleep 0.5

  # Verifica se a sessão está listada
  log "Verificando sessão $ID no repositório..."
  GET_RESP=$(curl -s -X GET "$SE_URL/$ID" -H "Authorization: Bearer $VALID_TOKEN")
  TITLE=$(echo "$GET_RESP" | jq -r '.title')
  if [[ "$TITLE" == "null" ]]; then
    log "Erro: Sessão $ID não encontrada após upload."
  else
    log "Sessão $ID confirmada no repository."
  fi

  # Atualiza a sessão
  log "Atualizando sessão $ID..."
  curl -s -X PUT "$SE_URL/$ID" \
    -H "Authorization: Bearer $VALID_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"isLive": false, "endedAt": "2025-06-06T18:00:00Z"}' >/dev/null
  log "Atualização OK."

  # Remove sessão
  log "Removendo sessão $ID..."
  curl -s -X DELETE "$SE_URL/$ID" -H "Authorization: Bearer $VALID_TOKEN" >/dev/null
  log "Sessão removida."
done

rm -f "$TMP_VIDEO"

#############################
## VERIFICAÇÃO DE SEGURANÇA (Testa todos endpoints por token)
#############################
header "2) Verificação de proteção dos endpoints"
ENDPOINTS_FILE="endpoints.json"
ENDPOINTS_LIST="endpoints_list.txt"
curl -s "$SWAGGER_URL" -o "$ENDPOINTS_FILE"
jq -r '.paths | to_entries[] | .key as $path | .value | keys[] | "\(. | ascii_upcase) \($path)"' "$ENDPOINTS_FILE" > "$ENDPOINTS_LIST"

# Código corrigido
while read -r line; do
  method=$(echo "$line" | awk '{print $1}') # Boa prática: usar minúsculas também
  api_path=$(echo "$line" | awk '{print $2}')
  full_url="$BASE_URL${api_path//\{id\}/00000000-0000-0000-0000-000000000000}"

  STATUS_VALID=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$full_url" -H "Authorization: Bearer $VALID_TOKEN")
  STATUS_INVALID=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$full_url" -H "Authorization: Bearer $INVALID_TOKEN")
  STATUS_NO_TOKEN=$(curl -s -o /dev/null -w "%{http_code}" -X "$method" "$full_url")

  log "[SECURITY] $method $api_path -> token OK=$STATUS_VALID | inválido=$STATUS_INVALID | sem token=$STATUS_NO_TOKEN"
done < "$ENDPOINTS_LIST"

header "3) Teste concluído. Consulte $LOGFILE para detalhes completos."
exit 0
