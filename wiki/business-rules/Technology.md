# Módulo: Technology (Tecnologías de Investigación)

## Descripción General
El módulo Technology maneja la investigación de tecnologías avanzadas, esenciales para desbloquear edificios, naves, defensas y mejoras. Se realiza en el Research Lab, y solo una investigación puede hacerse a la vez por imperio (o por planeta con Intergalactic Research Network).

## Características Principales
- **Árbol de Tecnologías**: Incluye 16 tecnologías implementadas:
  - Espionage Technology (req: Lab Nivel 3)
  - Computer Technology (req: Lab Nivel 1) - Aumenta slots de flota
  - Weapons Technology (req: Lab Nivel 4) - Mejora ataque
  - Shielding Technology (req: Lab Nivel 6, Energy Tech 3) - Mejora escudos
  - Armour Technology (req: Lab Nivel 2) - Mejora estructura
  - Energy Technology (req: Lab Nivel 1)
  - Hyperspace Technology (req: Lab Nivel 7, Energy 5, Shielding 5)
  - Combustion Drive (req: Lab Nivel 1, Energy 1) - Propulsión básica
  - Impulse Drive (req: Lab Nivel 2, Energy 1) - Para Cruisers/Heavy Fighters
  - Hyperspace Drive (req: Lab Nivel 7, Hyperspace 3)
  - Laser Technology (req: Lab Nivel 1, Energy 2)
  - Ion Technology (req: Lab Nivel 4, Laser 5, Energy 4) - Para Ion Cannons
  - Plasma Technology (req: Lab Nivel 4, Energy 8, Laser 10, Ion 5)
  - Intergalactic Research Network (req: Lab Nivel 10, Computer 8, Hyperspace 8) - Reduce tiempos de investigación
  - Astrophysics (req: Lab Nivel 3, Espionage 4, Impulse 3) - Colonización y expediciones
  - Graviton Technology (req: Lab Nivel 12) - Para Death Star (consumo energía: 300,000)
- **Costos**: Duplican por nivel (Scaling = 2.0), excepto:
  - Astrophysics: 1.75x (ver TechnologyService.cs:369)
  - Graviton: 3.0x (ver TechnologyService.cs:389)
- **Tiempos**: Calculados con fórmula base, ajustados por Research Lab y DevMode
- **Requisitos**: Validados en RequirementsMet() (TechnologyService.cs:397-414)
- **Graviton Instantáneo**: Si hay suficiente energía, se investiga instantáneamente sin consumir recursos (TechnologyService.cs:428-437)
- **Proceso de Investigación**: Los tiempos se calculan con BaseDuration * 1.1^Level y se dividen por factores como Research Lab y DevMode

## Mecánicas de Juego
- **Investigación Global**: Una vez completada, aplica a todo el imperio.
- **Cancelación**: Posible cancelar, recuperando recursos (ver TechnologyService.cs:463-478).
- **Proceso Asincrónico**: Las investigaciones se procesan en ProcessResearchQueue() (TechnologyService.cs:480-536), actualizando TimeRemaining cada segundo hasta completar.

## Estrategias Recomendadas
- Enfócate en tecnologías clave temprano: Energy, Computer, Combustion para crecimiento inicial.
- Usa IRN para acelerar: Únelo en planetas con altos labs.
- Prioriza según estrategia: Combate para guerra, Astrophysics para colonización.
- Equilibra ramas: Evita descuidar una área (ej. sin energía, no puedes operar eficientemente).

Este módulo impulsa el progreso tecnológico, permitiendo avances en flota y defensas.