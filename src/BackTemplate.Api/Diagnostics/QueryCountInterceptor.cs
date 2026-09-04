using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BackTemplate.Api.Diagnostics;

/// <summary>
/// Conta quantos comandos SQL um DbContext executa. Usado nos testes pra travar
/// regressões de N+1: um repository que deveria rodar 1 query não pode passar a rodar 1+N
/// sem quebrar o teste. Não é registrado em produção, só nos testes.
/// </summary>
public class QueryCountInterceptor : DbCommandInterceptor
{
    private int _commandCount;

    public int CommandCount => _commandCount;

    public void Reset() => _commandCount = 0;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Interlocked.Increment(ref _commandCount);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _commandCount);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
