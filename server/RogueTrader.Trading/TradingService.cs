using RogueTrader.Persistence;

namespace RogueTrader.Trading;

/// <summary>
/// Buy/sell at a station. Resolves docking from the ship position and hands the verdict to the transaction core, which owns
/// identity, idempotency, wallet, inventory, cargo capacity (through its <see cref="ICargoPolicy"/>) and atomic persistence.
/// The processor is always called, even when the ship is out of range, so a retry of an already applied transaction id still
/// returns its original result (the core's duplicate lookup precedes its proximity check).
/// </summary>
public sealed class TradingService(ITransactionProcessor processor, StationDirectory stations)
{
    private readonly ITransactionProcessor _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    private readonly StationDirectory _stations = stations ?? throw new ArgumentNullException(nameof(stations));

    /// <param name="authenticatedProfileId">Identity established by the session layer.</param>
    /// <param name="shipX">Authoritative ship X in world units (from the server simulation, never from the client request).</param>
    /// <param name="shipY">Authoritative ship Y in world units.</param>
    public Task<TransactionResult> BuyAsync(string authenticatedProfileId, float shipX, float shipY, BuyRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _processor.BuyAsync(new TransactionContext(authenticatedProfileId, _stations.IsDocked(request.StationId, shipX, shipY)), request, ct);
    }

    public Task<TransactionResult> SellAsync(string authenticatedProfileId, float shipX, float shipY, SellRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _processor.SellAsync(new TransactionContext(authenticatedProfileId, _stations.IsDocked(request.StationId, shipX, shipY)), request, ct);
    }
}
