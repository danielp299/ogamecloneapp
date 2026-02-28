using Microsoft.EntityFrameworkCore;
using myapp.Data;
using myapp.Data.Entities;

namespace myapp.Services;

public class GameMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Subject { get; set; } = "";
    public string Body { get; set; } = ""; // Can be HTML
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Type { get; set; } = "General"; // Combat, Espionage, Expedition, General
    public bool IsRead { get; set; } = false;
}

public class MessageService
{
    private readonly GameDbContext _dbContext;
    public List<GameMessage> Messages { get; private set; } = new();
    
    public event Action? OnChange;
    private bool _isInitialized = false;

    public MessageService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await LoadFromDatabaseAsync();
        _isInitialized = true;
    }

    private async Task LoadFromDatabaseAsync()
    {
        var dbMessages = await _dbContext.Messages.OrderByDescending(m => m.Timestamp).ToListAsync();
        Messages = dbMessages.Select(dbMsg => new GameMessage
        {
            Id = dbMsg.Id,
            Subject = dbMsg.Subject,
            Body = dbMsg.Body,
            Timestamp = dbMsg.Timestamp,
            Type = dbMsg.Type,
            IsRead = dbMsg.IsRead
        }).ToList();
    }

    private async Task SaveToDatabaseAsync(GameMessage message)
    {
        var dbMessage = new Data.Entities.GameMessageEntity
        {
            Id = message.Id,
            Subject = message.Subject,
            Body = message.Body,
            Timestamp = message.Timestamp,
            Type = message.Type,
            IsRead = message.IsRead
        };
        
        _dbContext.Messages.Add(dbMessage);
        await _dbContext.SaveChangesAsync();
    }

    private async Task UpdateDatabaseAsync(GameMessage message)
    {
        var dbMessage = await _dbContext.Messages.FindAsync(message.Id);
        if (dbMessage != null)
        {
            dbMessage.IsRead = message.IsRead;
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task AddMessage(string subject, string body, string type = "General")
    {
        var message = new GameMessage
        {
            Subject = subject,
            Body = body,
            Type = type,
            Timestamp = DateTime.Now
        };
        
        Messages.Insert(0, message); // Newest first
        await SaveToDatabaseAsync(message);
        NotifyStateChanged();
    }
    
    public async Task DeleteMessage(Guid id)
    {
        var msg = Messages.FirstOrDefault(m => m.Id == id);
        if (msg != null)
        {
            Messages.Remove(msg);
            
            var dbMsg = await _dbContext.Messages.FindAsync(id);
            if (dbMsg != null)
            {
                _dbContext.Messages.Remove(dbMsg);
                await _dbContext.SaveChangesAsync();
            }
            
            NotifyStateChanged();
        }
    }
    
    public async Task DeleteAll()
    {
        Messages.Clear();
        
        _dbContext.Messages.RemoveRange(_dbContext.Messages);
        await _dbContext.SaveChangesAsync();
        
        NotifyStateChanged();
    }
    
    public int GetUnreadCount()
    {
        return Messages.Count(m => !m.IsRead);
    }

    public async Task MarkAsRead(Guid id)
    {
        var msg = Messages.FirstOrDefault(m => m.Id == id);
        if (msg != null && !msg.IsRead)
        {
            msg.IsRead = true;
            await UpdateDatabaseAsync(msg);
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
