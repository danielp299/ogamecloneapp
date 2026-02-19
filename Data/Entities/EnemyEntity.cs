using System;
using System.ComponentModel.DataAnnotations;

namespace myapp.Data.Entities;

public class EnemyEntity
{
    [Key]
    public Guid Id { get; set; }
    
    public string Name { get; set; } = "";
    public int Galaxy { get; set; }
    public int System { get; set; }
    public int Position { get; set; }

    // Empire linkage
    public Guid EmpireId { get; set; }
    public bool IsHomeworld { get; set; } = false;
    
    // Resources
    public long Metal { get; set; } = 1000;
    public long Crystal { get; set; } = 500;
    public long Deuterium { get; set; } = 200;
    public long Energy { get; set; } = 0;
    
    // Timestamps
    public DateTime LastResourceUpdate { get; set; } = DateTime.Now;
    public DateTime LastActivity { get; set; } = DateTime.Now;
    
    // Flags
    public bool IsBot { get; set; } = false;
    
    // Colony tracking
    public int ColonyCount { get; set; } = 0;
    
    // Serialized data for buildings, technologies, defenses, ships
    public string BuildingsJson { get; set; } = "{}";
    public string TechnologiesJson { get; set; } = "{}";
    public string DefensesJson { get; set; } = "{}";
    public string ShipsJson { get; set; } = "{}";
}
