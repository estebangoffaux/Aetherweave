using JetBrains.Annotations;

namespace Zwedze.Aetherweave.Application.CQRS;

[UsedImplicitly]
public interface IQueryHandler<in TRequest, TResponse>
{
    Task<ResponseWrapper<TResponse>> Handle(TRequest request, CancellationToken cancellationToken = default);
}

[UsedImplicitly]
public interface IQueryHandler<TResponse>
{
    Task<ResponseWrapper<TResponse>> Handle(CancellationToken cancellationToken = default);
}
