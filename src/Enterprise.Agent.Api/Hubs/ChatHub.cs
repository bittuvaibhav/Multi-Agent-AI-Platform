using Enterprise.Agent.Core.Application.Chat;
using Enterprise.Agent.Models.Chat;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Enterprise.Agent.Api.Hubs;

/// <summary>
/// Real-time chat hub. Clients invoke <see cref="SendMessage"/>; the resulting answer is
/// streamed back to the caller (and conversation group) via the <c>ReceiveMessage</c> event.
/// </summary>
public sealed class ChatHub : Hub
{
    private readonly ISender _sender;

    public ChatHub(ISender sender) => _sender = sender;

    public async Task SendMessage(ChatRequest request)
    {
        await Clients.Caller.SendAsync("Acknowledged", request.ConversationId ?? string.Empty);

        var response = await _sender.Send(new ProcessChatCommand(request), Context.ConnectionAborted);

        if (!string.IsNullOrWhiteSpace(response.ConversationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, response.ConversationId);
        }

        await Clients.Caller.SendAsync("ReceiveMessage", response, Context.ConnectionAborted);
    }

    public Task JoinConversation(string conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, conversationId);

    public Task LeaveConversation(string conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
}
