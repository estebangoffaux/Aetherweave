using JetBrains.Annotations;

namespace Zwedze.Aetherweave.Application.CQRS;

[UsedImplicitly]
public interface ICommandHandler<in TRequest, TResponse>
{
    Task<ResponseWrapper<TResponse>> Handle(TRequest request, CancellationToken cancellationToken = default);
}

[UsedImplicitly]
public interface ICommandHandler<in TRequest>
{
    Task<ResponseWrapper> Handle(TRequest request, CancellationToken cancellationToken = default);
}
