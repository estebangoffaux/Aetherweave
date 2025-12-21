using System.Collections.Immutable;
using System.Text;
using JetBrains.Annotations;
using Zwedze.Aetherweave.SharedKernel;

namespace Zwedze.Aetherweave.Application.Errors;

public static class ErrorFactory
{
    [UsedImplicitly]
    public static Error Create(string code, string message)
    {
        return new Error
        {
            Codes = [code],
            Message = message
        };
    }

    [UsedImplicitly]
    public static Error Create(Exception ex)
    {
        if (ex is BusinessException bEx)
        {
            return Create(bEx);
        }

        return new Error
        {
            Codes = [Error.UnmanagedErrorCode],
            Message = ex.Message
        };
    }

    [UsedImplicitly]
    public static Error Create(AggregateException ex)
    {
        List<string> errorCodes = new();
        StringBuilder messageBuilder = new();

        var businessExceptions = ex
            .InnerExceptions
            .OfType<BusinessException>()
            .ToImmutableArray();
        if (businessExceptions.Length != 0)
        {
            var a = ExtractCodesAndMessage(businessExceptions);
            errorCodes.AddRange(a.ErrorCodes);
            messageBuilder.Append(a.Message);
        }

        var otherExceptions = ex.InnerExceptions.Except(businessExceptions).ToArray();

        if (otherExceptions.Length > 0)
        {
            errorCodes.Add(Error.UnmanagedErrorCode);
            messageBuilder.AppendJoin(Environment.NewLine, otherExceptions.Select(x => x.Message));
        }

        return new Error
        {
            Codes = [..errorCodes],
            Message = messageBuilder.ToString()
        };
    }

    private static Error Create(BusinessException ex)
    {
        return new Error
        {
            Codes = [ex.ErrorCode],
            Message = ex.Message
        };
    }

    private static (ImmutableArray<string> ErrorCodes, string Message) ExtractCodesAndMessage(ImmutableArray<BusinessException> exceptions)
    {
        var errorCodes = exceptions.Select(x => x.ErrorCode).ToImmutableArray();
        var message = exceptions.Select(x => x.Message).Aggregate((x, y) => x + Environment.NewLine + y);

        return (errorCodes, message);
    }
}
