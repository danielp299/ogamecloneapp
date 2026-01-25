using System;
using System.Collections.Generic;

namespace myapp.Services;

public class GameMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; }
    public string Body { get; set; } // Can be HTML
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Type { get; set; } = "General"; // Combat, Espionage, Expedition, General
    public bool IsRead { get; set; } = false;
}

public class MessageService
{
    public List<GameMessage> Messages { get; private set; } = new();
    
    public event Action OnChange;

    public void AddMessage(string subject, string body, string type = "General")
    {
        Messages.Insert(0, new GameMessage // Newest first
        {
            Subject = subject,
            Body = body,
            Type = type
        });
        NotifyStateChanged();
    }
    
    public void DeleteMessage(Guid id)
    {
        var msg = Messages.FirstOrDefault(m => m.Id == id);
        if (msg != null)
        {
            Messages.Remove(msg);
            NotifyStateChanged();
        }
    }
    
    public void DeleteAll()
    {
        Messages.Clear();
        NotifyStateChanged();
    }
    
    public int GetUnreadCount()
    {
        return Messages.Count(m => !m.IsRead);
    }

    public void MarkAsRead(Guid id)
    {
        var msg = Messages.FirstOrDefault(m => m.Id == id);
        if (msg != null && !msg.IsRead)
        {
            msg.IsRead = true;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
