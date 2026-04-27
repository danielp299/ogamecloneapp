# Módulo: Combat (Combate)

## Descripción General
El combate en OGame es un sistema automático que resuelve batallas entre flotas atacantes y defensoras (incluyendo defensas planetarias). Se basa en rounds secuenciales donde naves y defensas causan daño, considerando shields, armor y rapid fire. No es en tiempo real; se calcula instantáneamente al llegar la flota.

## Características Principales
- **Tipos de Combate**:
  - **Ataque Planetario**: Flota atacante vs. flota defensora + defensas fijas.
  - **Ataque a Flota**: Destrucción de flotas estacionadas (sin defensas).
  - **ACS (Ataque Coordinado)**: Múltiples jugadores atacan juntos.
  - **Espionaje y Misiles**: No combate directo, pero interplanetarios pueden dañar defensas.

- **Mecánicas de Resolución**:
  - **Rounds**: Máximo 6 rounds; termina si un lado pierde todo.
  - **Daño**: Cada unidad ataca aleatoriamente; daño = Ataque - Defensa (shields absorben primero).
  - **Shields**: Absorben daño inicial; se regeneran parcialmente por round.
  - **Armor**: Reduce daño recibido.
  - **Rapid Fire**: Probabilidad de disparar extra contra ciertos tipos (ej. Cazadores ligeros vs. Cargueros pequeños).
  - **Orden de Ataque**: Defensas disparan primero, luego flotas.

- **Resultados**:
  - **Ganador**: Toma recursos del perdedor (si ataca).
  - **Debris Field**: Generado por naves destruidas (70% metal/cristal reciclable).
  - **Pérdidas**: Naves destruidas no se recuperan automáticamente (excepto defensas al 70%).

- **Factores Adicionales**:
  - **Tecnologías**: Aumentan ataque/defensa (Weapons, Shielding, Armor Tech).
  - **Formas de Vida**: Bonos específicos (ej. Mejora combate).
  - **Estadísticas de Unidades**: Ataque, Shields, Armor, Rapid Fire por nave/defensa.

## Estrategias Recomendadas
- Balancea flota: Mezcla cazadores para rapid fire y acorazados para daño.
- Usa espía para evaluar defensas.
- Evita ataques innecesarios; calcula pérdidas vs. ganancias.
- Defiende con mix de torretas y escudos.

Este módulo es clave para PvP y saqueo, haciendo OGame competitivo.