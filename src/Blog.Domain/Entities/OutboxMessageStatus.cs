namespace Blog.Domain.Entities;

public enum OutboxMessageStatus : byte
{
    Pending = 0,
    Completed = 1,
    DeadLettered = 2
}
