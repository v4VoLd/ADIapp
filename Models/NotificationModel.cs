using System;
using System.Text.Json.Serialization;

namespace ADIapp.Models;

public class NotificationModel
{
    public string Id { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }

    [JsonPropertyName("ticket_id")]
    public int? TicketId { get; set; }

    [JsonPropertyName("ticket_number")]
    public string? TicketNumber { get; set; }
}
