using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ADIapp.Models;

public class TicketDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("ticket_number")]
    public string TicketNumber { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "open";

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "normal";

    [JsonPropertyName("metadata")]
    public System.Text.Json.JsonElement? Metadata { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("latest_message")]
    public TicketMessageDto? LatestMessage { get; set; }

    [JsonPropertyName("messages")]
    public List<TicketMessageDto>? Messages { get; set; }
}

public class TicketMessageDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("ticket_id")]
    public int TicketId { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("sender")]
    public string Sender { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateTicketRequestDto
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "normal";

    [JsonPropertyName("metadata")]
    public object? Metadata { get; set; }
}
