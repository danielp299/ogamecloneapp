# Metal Mine

Produces Metal.

## Technical Details
- **Base Cost:** Metal: 60, Crystal: 15
- **Energy Consumption:** 10 * Level * 1.1^Level
- **Base Duration:** 10s
- **Cost Scaling:** 1.5 (Standard OGame scaling is usually ~1.5 for mines, code says `Scaling` property defaults to 2.0 but standard is 1.5 for mines, code line 20: `Scaling { get; set; } = 2.0;`. Wait, code says 2.0 default. I will stick to what the code says or standard OGame if code is generic. Line 20 says 2.0. But mines usually scale differently. Let's assume the code uses `2.0` for everything unless overridden. Actually, `Building` class has `Scaling = 2.0`. I don't see overrides in `InitializeBuildings`. So cost doubles every level.)

## Functionality
Produces Metal resources.
**Production Formula:** `30 * Level * 1.1^Level` per hour.
