# Módulo: Fleet (Flota)

## Descripción General
El módulo Fleet permite gestionar y enviar flotas de naves para misiones como ataque, transporte, espionaje y más. Es central para la interacción con otros jugadores, comercio y combate. Las flotas viajan entre coordenadas, consumiendo deuterio y tiempo basado en la nave más lenta.

## Características Principales
- **Tipos de Naves**:
  - **Cargueros**: Small/Large Cargo Ships – Para transporte de recursos.
  - **Cazas**: Light/Heavy Fighters – Baratos para ataques rápidos.
  - **Naves de Guerra**: Cruisers, Battleships, Destroyers, Reapers – Para combate pesado.
  - **Especiales**: Bombers *(pendiente: no implementado)*, Death Stars (para destruir lunas), Pathfinders *(pendiente: no implementado)* (exploración), Colony Ships.
- **Misiones**:
  - Attack: Atacar planetas.
  - Transport: Enviar recursos.
  - Deploy: Estacionar flota en otro planeta propio.
  - Spy: Espiar planetas (requiere Probes).
  - Colonize: Establecer colonias (con Colony Ship).
  - Recycle: Recolectar debris.
  - Destroy: Destruir lunas (con Death Stars) *(pendiente: no implementado)*.
  - Expedition: Explorar espacio profundo para recursos/bonuses.
  - Phalanx *(pendiente: no implementado)*: Consultar flotas desde lunas.
- **Cálculos**: Velocidad basada en el motor más lento; consumo de deuterio proporcional; capacidad de carga ajustada por fuel.
  - Implementado en FleetService.cs:345-391 con fórmulas para fuel y flight time.
- **Actividad de Flota**: Pantalla para monitorear flotas en vuelo, con tiempos de llegada/regreso.
  - Implementado en FleetService.cs:24-36 con FleetMission y FleetStatus (Flight/Return/Holding).

## Mecánicas de Juego
- **Combate**: Resuelto automáticamente al llegar; incluye rapid fire, shields y armor.
- **Debris Fields**: Generados tras batallas; recolectables con Recyclers.
- **ACS (Ataque Coordinado)**: *(pendiente: no implementado)* Ataques grupales con aliados.
- **Expediciones**: Riesgo-recompensa; pueden fallar o dar recursos/naves.
  - Implementado en FleetService.cs:713-778 con posibilidades de: Black Hole (1%), Combate Aliens/Pirates (9%), Nada/Retraso (30%), Recursos (30%), Naves abandoneadas (20%), Dark Matter (10%)
- **Fuel y Tiempo**: Afectados por distancias y universos (velocidad variable).

## Estrategias Recomendadas
- Composiciones óptimas: Cargueros para farming, Cruisers/Destroyers para defensa.
- Salvaguarda flotas: Envía a expediciones o estaciona en lunas durante ausencias.
- Calcula tiempos: Usa herramientas para predecir vulnerabilidades.
- Diversifica: Mezcla naves para misiones variadas.

Este módulo es clave para la expansión, guerra y economía en OGame.