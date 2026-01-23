# Módulo: Buildings (Edificios)

## Descripción General
El módulo Buildings permite a los jugadores construir y mejorar estructuras en sus planetas. Incluye minas de recursos, plantas de energía, fábricas y otros edificios que aumentan la producción, capacidad de almacenamiento y eficiencia. Cada edificio ocupa un "campo" en el planeta, y hay un límite basado en el tamaño del planeta (medido en campos).

## Características Principales
- **Tipos de Edificios**:
  - **Mines de Recursos**: Metal Mine, Crystal Mine, Deuterium Synthesizer – Producen recursos básicos.
  - **Plantas de Energía**: Solar Plant, Fusion Reactor – Generan energía para alimentar otras estructuras.
  - **Almacenes**: Metal Storage, Crystal Storage, Deuterium Tank – Aumentan la capacidad de almacenamiento.
  - **Fábricas**: Robotics Factory, Shipyard, Nanite Factory – Aceleran la construcción y producción de flotas/defensas.
  - **Otros**: Research Lab (para investigación), Terraformer (aumenta campos), Alliance Depot, Missile Silo.
- **Cola de Construcción**: Permite poner hasta 5 edificios en cola secuencialmente. Los costos se duplican por nivel (o siguen fórmulas específicas como 1.5x para minas).
- **Tiempos de Construcción**: Calculados por la fórmula: Tiempo = (Metal + Cristal) / (2500 * (1 + Robotics Factory) * 2^Nível Nanite Factory * Velocidad Universo).
- **Cancelación**: Posible cancelar construcciones, recuperando recursos, pero no defensas o naves.

## Mecánicas de Juego
- **Dependencias**: Algunos edificios requieren niveles previos (ej. Shipyard necesita Robotics Factory 2).
- **Campos Ocupados**: Edificios consumen campos; defensas no lo hacen.
- **Bonos por Forma de Vida**: En universos recientes, formas de vida como Humanos o Rock'tal otorgan bonos a edificios específicos.
- **Mejoras**: Cada nivel aumenta producción o eficiencia, pero con costos exponenciales.

## Estrategias Recomendadas
- Prioriza minas y energía inicialmente para crecimiento sostenible.
- Usa cola para eficiencia: Construye en secuencia para minimizar tiempos muertos.
- Equilibra recursos: Evita sobreproducir uno mientras faltan otros.
- Desmantela si es necesario: Para liberar campos o reasignar recursos.

Este módulo es fundamental para expandir y fortalecer la base económica del imperio.