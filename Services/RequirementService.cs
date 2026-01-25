using System;
using System.Collections.Generic;
using System.Linq;

namespace myapp.Services;

public class RequirementService
{
    private readonly BuildingService _buildingService;
    private readonly TechnologyService _technologyService;

    public RequirementService(BuildingService buildingService, TechnologyService technologyService)
    {
        _buildingService = buildingService;
        _technologyService = technologyService;
    }

    public bool IsUnlocked(Dictionary<string, int> requirements)
    {
        if (requirements == null || !requirements.Any()) return true;

        foreach (var req in requirements)
        {
            string name = req.Key;
            int levelNeeded = req.Value;
            int currentLevel = 0;

            // Check Buildings
            var building = _buildingService.Buildings.FirstOrDefault(b => b.Title == name);
            if (building != null)
            {
                currentLevel = building.Level;
            }
            // Check Technologies
            else
            {
                var tech = _technologyService.Technologies.FirstOrDefault(t => t.Title == name);
                if (tech != null)
                {
                    currentLevel = tech.Level;
                }
            }

            if (currentLevel < levelNeeded)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsUnlocked(List<string> requirements)
    {
        // Keep this for backward compatibility if needed, but usually we use Dictionary
        // If List is just names, we assume Level 1? Or we just return true?
        // Let's assume List<string> implies a check for existence/level 1, 
        // OR simply return true if we migrated everything to Dictionary.
        return true; 
    }
}
