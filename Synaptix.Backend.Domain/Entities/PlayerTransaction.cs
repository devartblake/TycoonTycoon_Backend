namespace Synaptix.Backend.Domain.Entities
{
    public enum PlayerTransactionStatus
    {
        Pending = 1,
        Applied = 2,
        Disputed = 3,
        Reversed = 4,
        Failed = 5
    }

    public enum PlayerTransactionActorRole
    {
        System = 1,
        Buyer = 2,
        Seller = 3,
        Recipient = 4,
        Sender = 5
    }

    public enum ItemOperation
    {
        Grant = 1,
        Revoke = 2,
        Swap = 3
    }

    /// <summary>
    /// Higher-level aggregate that wraps one or more EconomyTransaction ledger entries
    /// plus optional inventory changes into a single atomic business operation.
    /// </summary>
    public sealed class PlayerTransaction
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid EventId { get; private set; }               // idempotency key
        public Guid? CorrelatedEventId { get; private set; }    // links to match, game event, etc.

        public string Kind { get; private set; } = string.Empty; // "match-completion", "purchase", "game-event-entry", etc.
        public PlayerTransactionStatus Status { get; private set; } = PlayerTransactionStatus.Pending;

        public string? Receipt { get; private set; }             // IAP receipt linking
        public string? DisputeReason { get; private set; }
        public Guid? DisputeLinkedToTransactionId { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAtUtc { get; private set; }

        public List<PlayerTransactionActor> Actors { get; private set; } = new();
        public List<PlayerTransactionItem> ItemChanges { get; private set; } = new();

        // Navigation: child EconomyTransactions that were created as part of this aggregate
        public List<EconomyTransaction> EconomyTransactions { get; private set; } = new();

        private PlayerTransaction() { } // EF

        public PlayerTransaction(Guid eventId, string kind, Guid? correlatedEventId = null, string? receipt = null)
        {
            EventId = eventId;
            Kind = (kind ?? "").Trim();
            CorrelatedEventId = correlatedEventId;
            Receipt = receipt;
        }

        public void MarkApplied()
        {
            Status = PlayerTransactionStatus.Applied;
            CompletedAtUtc = DateTimeOffset.UtcNow;
        }

        public void MarkFailed()
        {
            Status = PlayerTransactionStatus.Failed;
            CompletedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Dispute(string reason, Guid? linkedTransactionId = null)
        {
            if (Status != PlayerTransactionStatus.Applied)
                throw new InvalidOperationException($"Cannot dispute a transaction with status '{Status}'.");

            Status = PlayerTransactionStatus.Disputed;
            DisputeReason = reason;
            DisputeLinkedToTransactionId = linkedTransactionId;
        }

        public void MarkReversed()
        {
            Status = PlayerTransactionStatus.Reversed;
        }

        public void AddActor(Guid playerId, PlayerTransactionActorRole role, int allocationPercent = 100)
        {
            Actors.Add(new PlayerTransactionActor(Id, playerId, role, allocationPercent));
        }

        public void AddItemChange(string itemType, int quantity, ItemOperation operation)
        {
            ItemChanges.Add(new PlayerTransactionItem(Id, itemType, quantity, operation));
        }
    }

    public sealed class PlayerTransactionActor
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerTransactionId { get; private set; }
        public Guid PlayerId { get; private set; }
        public PlayerTransactionActorRole Role { get; private set; }
        public int AllocationPercent { get; private set; }

        private PlayerTransactionActor() { } // EF

        public PlayerTransactionActor(Guid playerTransactionId, Guid playerId, PlayerTransactionActorRole role, int allocationPercent)
        {
            PlayerTransactionId = playerTransactionId;
            PlayerId = playerId;
            Role = role;
            AllocationPercent = allocationPercent;
        }
    }

    public sealed class PlayerTransactionItem
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerTransactionId { get; private set; }
        public string ItemType { get; private set; } = string.Empty; // "powerup:skip", "cosmetic:hat"
        public int Quantity { get; private set; }
        public ItemOperation Operation { get; private set; }

        private PlayerTransactionItem() { } // EF

        public PlayerTransactionItem(Guid playerTransactionId, string itemType, int quantity, ItemOperation operation)
        {
            PlayerTransactionId = playerTransactionId;
            ItemType = (itemType ?? "").Trim();
            Quantity = quantity;
            Operation = operation;
        }
    }
}
