#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# AUTO-DETECÇÃO DAS PASTAS PRINCIPAIS (assume apenas *um* match de cada)
###############################################################################
DOMAIN_DIR=$(find . -type d -name "TCUWatcher.Domain"        | head -n1)
APPLICATION_DIR=$(find . -type d -name "TCUWatcher.Application" | head -n1)
API_DIR=$(find . -type d -name "TCUWatcher.API"              | head -n1)

if [[ -z "$DOMAIN_DIR" || -z "$APPLICATION_DIR" || -z "$API_DIR" ]]; then
  echo "❌ Não encontrei uma das pastas (Domain/Application/API). Ajuste o script."
  exit 1
fi

echo "🔍 Domain  ➜ $DOMAIN_DIR"
echo "🔍 App     ➜ $APPLICATION_DIR"
echo "🔍 API     ➜ $API_DIR"

###############################################################################
# 0) COMMON / ERRORS
###############################################################################
echo "👉 Criando Common/Result.cs"
mkdir -p "$DOMAIN_DIR/Common"
cat > "$DOMAIN_DIR/Common/Result.cs" <<'EOF'
namespace TCUWatcher.Domain.Common;

public readonly record struct Result<TSuccess, TError>
{
    private readonly TSuccess? _value;
    private readonly TError?   _error;
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(TSuccess value) { IsSuccess = true;  _value = value;  _error = default; }
    private Result(TError   error) { IsSuccess = false; _error = error;  _value = default; }

    public static Result<TSuccess,TError> Success(TSuccess value) => new(value);
    public static Result<TSuccess,TError> Failure(TError error)   => new(error);

    public TSuccess Value => IsSuccess ? _value! : throw new InvalidOperationException();
    public TError   Error => IsFailure ? _error! : throw new InvalidOperationException();

    public TResult Match<TResult>(Func<TSuccess,TResult> ok, Func<TError,TResult> fail)
        => IsSuccess ? ok(Value) : fail(Error);
}
EOF

echo "👉 Criando Errors/DomainError.cs"
mkdir -p "$DOMAIN_DIR/Errors"
cat > "$DOMAIN_DIR/Errors/DomainError.cs" <<'EOF'
namespace TCUWatcher.Domain.Errors;

public abstract record DomainError(string Code, string Message);

public sealed record InvalidProcessNumberError(string Number)
    : DomainError("ProcessNumber.Invalid",
                  $"Número de processo inválido: {Number}");

public sealed record LiveAlreadyEndedError(Guid SessionId)
    : DomainError("Session.LiveAlreadyEnded",
                  $"A sessão {SessionId} já terminou.");
EOF

###############################################################################
# 1) INTERFACE
###############################################################################
SERVICE_IF=$(find "$APPLICATION_DIR" -type f -name "ISessionEventService.cs" | head -n1)
if [[ -z "$SERVICE_IF" ]]; then echo "❌ Interface não encontrada"; exit 1; fi

echo "👉 Ajustando assinatura em $SERVICE_IF"
sed -i 's#Task<[^>]*> *CreateAsync#Task<Result<SessionEventDto, DomainError>> CreateAsync#' "$SERVICE_IF"

###############################################################################
# 2) IMPLEMENTAÇÃO
###############################################################################
SERVICE_IMPL=$(find "$APPLICATION_DIR" -type f -name "SessionEventService.cs"  | head -n1)
if [[ -z "$SERVICE_IMPL" ]]; then echo "❌ Implementação não encontrada"; exit 1; fi

echo "👉 Refatorando método CreateAsync em $SERVICE_IMPL"

# altera cabeçalho
sed -i 's/public async Task<[^>]*> CreateAsync([^)]*)/public async Task<Result<SessionEventDto, DomainError>> CreateAsync(CreateSessionEventDto dto)/' "$SERVICE_IMPL"

# insere early-return para número de processo inválido
sed -i '/{/{x;p;x;:a;n;/^\s*$/b; s/.*/        if (!ProcessNumber.IsValid(dto.ProcessNumber))\n            return Result<SessionEventDto, DomainError>.Failure(new InvalidProcessNumberError(dto.ProcessNumber));\n\n&/; b};' "$SERVICE_IMPL"

# converte retorno success
sed -i 's/return _mapper.Map<SessionEventDto>(entity);/return Result<SessionEventDto, DomainError>.Success(_mapper.Map<SessionEventDto>(entity));/' "$SERVICE_IMPL"

###############################################################################
# 3) CONTROLLER
###############################################################################
CTRL=$(find "$API_DIR" -type f -name "SessionEventsController.cs" | head -n1)
if [[ -z "$CTRL" ]]; then echo "❌ Controller não encontrado"; exit 1; fi

echo "👉 Atualizando controller $CTRL"
sed -i '/\[HttpPost\]/,/^}/c\
[HttpPost]\npublic async Task<IActionResult> Create(CreateSessionEventDto dto)\n{\n    var result = await _service.CreateAsync(dto);\n\n    return result.Match<IActionResult>(\n        success => CreatedAtAction(nameof(GetById), new { id = success.Id }, success),\n        error   => error switch\n        {\n            InvalidProcessNumberError => BadRequest(ToProblem(error, 400)),\n            LiveAlreadyEndedError     => UnprocessableEntity(ToProblem(error, 422)),\n            _                         => StatusCode(500, ToProblem(error, 500))\n        });\n}\n\nprivate static ProblemDetails ToProblem(DomainError err, int status)\n    => new() { Title = err.Code, Detail = err.Message, Status = status };' "$CTRL"

###############################################################################
echo "✅  Script concluído."
echo "🏗️  Agora rode:  dotnet build"
###############################################################################
