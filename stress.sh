#!/bin/bash

# --- CONFIGURAÇÃO ---
URL="http://localhost:5092/api/validation/title"
LOG_FILE="validation_test_report.log"
ITERATIONS=100000

# --- PADRÃO DE COMPARAÇÃO (GROUND TRUTH) CORRIGIDO ---

# Lista de títulos que DEVEM retornar TRUE
VALID_TITLES=(
    # Casos explícitos
    "Sessão Plenária do TCU"
    "Sessão da 1ª Câmara do TCU"
    "Plenario do TCU"
    "tribunal de contas da uniao sessão"
    "TCU: Sessão de hoje"

    # Casos que costumavam ser considerados "genéricos", mas que nossa API agora identifica corretamente
    "julgamento da primeira câmara" # <-- PROMOVIDO A VÁLIDO
    "camara plenaria"               # <-- PROMOVIDO A VÁLIDO
    "Sessão Ordinária da 2a Camara"
    "reuniao da 1 camara do tribunal de contas"
    
    # Casos com Erros de Digitação
    "sessao do plenaruio do tcu"
    "sessao do plenerio do tcú"
    "pleenario do tcu"
    "2ª Câmaa - 25/02/2025"

    # Casos para Lógica Híbrida
    "tcu camara ao vivo"
    "ao vivo tcu plenario"
)

# Lista de títulos que DEVEM retornar FALSE
# Esta lista agora contém apenas casos que são inequivocamente errados.
INVALID_TITLES=(
    # Outros órgãos ou contextos explícitos
    "plano diretor do stf"
    "sessão da câmara municipal"
    "audiência criminal do tjmg"
    "reuniao plenaria do senado"
    "Sessão do Pleno do STF"
    "1ª Câmara Cível do TJ"

    # Contexto claramente errado (agora com a blocklist mais forte)
    "podcast sobre o plenario"
    "show plenário samba"
    "sessão aleatória de fotos"
)

# --- INICIALIZAÇÃO ---
echo "Iniciando teste de validação automatizada em $(date)" > "$LOG_FILE"
echo "----------------------------------------------------" >> "$LOG_FILE"

# Contadores
PASSED=0
FAILED=0

# Cores para o output
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color


# --- EXECUÇÃO DOS TESTES ---
echo "Iniciando a execução de $ITERATIONS testes com o gabarito atualizado..."

for ((i = 1; i <= $ITERATIONS; i++)); do
    
    # Sorteia se vamos testar um caso válido ou inválido
    if (( RANDOM % 2 == 0 )); then
        EXPECTED_RESULT="true"
        TITLE=${VALID_TITLES[$((RANDOM % ${#VALID_TITLES[@]}))]}
    else
        EXPECTED_RESULT="false"
        TITLE=${INVALID_TITLES[$((RANDOM % ${#INVALID_TITLES[@]}))]}
    fi

    # Faz a chamada à API e extrai apenas o valor booleano
    RESPONSE=$(curl -s -X POST "$URL" -H "Content-Type: application/json" -d "\"$TITLE\"")
    ACTUAL_RESULT=$(echo "$RESPONSE" | grep -o '"isRelevant":\s*\w*' | cut -d':' -f2 | tr -d '[:space:]')

    # Validação da resposta
    if [ "$ACTUAL_RESULT" == "$EXPECTED_RESULT" ]; then
        ((PASSED++))
    else
        ((FAILED++))
        # Loga a falha completa para análise posterior
        echo "----------------- FALHA DETECTADA -----------------" >> "$LOG_FILE"
        printf "[%06d] [FAIL] Título: '%s'\n" "$i" "$TITLE" >> "$LOG_FILE"
        printf "                 => Esperado: %s, Recebido: %s\n" "$EXPECTED_RESULT" "$ACTUAL_RESULT" >> "$LOG_FILE"
        echo "-----------------------------------------------------" >> "$LOG_FILE"
    fi

    # Imprime o progresso no console
    if (( $i % 1000 == 0 )); then
        echo "Progresso: $i/$ITERATIONS testes completados... (Sucessos: $PASSED, Falhas: $FAILED)"
    fi
done


# --- RELATÓRIO FINAL ---

echo "----------------------------------------------------"
echo "Teste finalizado!"
echo ""
echo "=============== RELATÓRIO DE TESTE ==============="
printf "Testes Totais: %d\n" "$ITERATIONS"
printf "${GREEN}Sucessos (PASS): %d${NC}\n" "$PASSED"
printf "${RED}Falhas (FAIL): %d${NC}\n" "$FAILED"
echo "===================================================="
echo ""
if [ $FAILED -gt 0 ]; then
    echo "🔴 Foram encontradas falhas! Verifique o arquivo '$LOG_FILE' para detalhes."
else
    echo "🟢 Todos os testes passaram com sucesso!"
fi


# Adiciona o resumo ao final do arquivo de log
echo "" >> "$LOG_FILE"
echo "=============== RESUMO DO RELATÓRIO ===============" >> "$LOG_FILE"
printf "Testes Totais: %d\n" "$ITERATIONS" >> "$LOG_FILE"
printf "Sucessos (PASS): %d\n" "$PASSED" >> "$LOG_FILE"
printf "Falhas (FAIL): %d\n" "$FAILED" >> "$LOG_FILE"
echo "===================================================" >> "$LOG_FILE"