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

## Lista Detallada de Edificios e Implementación

### Metal Mine (Mina de Metal)
*   **Descripción**: Extrae metal de las profundidades del planeta. Es el recurso más básico y abundante, esencial para casi todas las construcciones.
*   **Coste Base**: Metal: 60, Cristal: 15.
*   **Producción**: 30 * Nivel * 1.1^Nivel.
*   **Consumo Energía**: 10 * Nivel * 1.1^Nivel.
*   **Factor Coste**: 1.5

### Crystal Mine (Mina de Cristal)
*   **Descripción**: Extrae cristal, recurso fundamental para componentes electrónicos y aleaciones.
*   **Coste Base**: Metal: 48, Cristal: 24.
*   **Producción**: 20 * Nivel * 1.1^Nivel.
*   **Consumo Energía**: 10 * Nivel * 1.1^Nivel.
*   **Factor Coste**: 1.6

### Deuterium Synthesizer (Sintetizador de Deuterio)
*   **Descripción**: Procesa deuterio (hidrógeno pesado) del agua del planeta. Usado como combustible y para investigación avanzada.
*   **Coste Base**: Metal: 225, Cristal: 75.
*   **Producción**: 10 * Nivel * 1.1^Nivel * (-0.004 * TempMedia + 1.44). (Simplificado por ahora sin temp).
*   **Consumo Energía**: 20 * Nivel * 1.1^Nivel.
*   **Factor Coste**: 1.5

### Solar Plant (Planta de Energía Solar)
*   **Descripción**: Genera energía eléctrica a partir de la radiación solar. Es la fuente de energía básica para las minas.
*   **Coste Base**: Metal: 75, Cristal: 30.
*   **Producción Energía**: 20 * Nivel * 1.1^Nivel.
*   **Factor Coste**: 1.5

### Fusion Reactor (Reactor de Fusión)
*   **Descripción**: Genera energía mediante fusión nuclear. Consume deuterio para producir grandes cantidades de energía.
*   **Coste Base**: Metal: 900, Cristal: 360, Deuterio: 180.
*   **Producción Energía**: 30 * Nivel * (1.05 + 0.01 * TechEnergia)^Nivel.
*   **Consumo Deuterio**: 10 * Nivel * 1.1^Nivel.
*   **Factor Coste**: 1.8

### Robotics Factory (Fábrica de Robots)
*   **Descripción**: Produce robots de construcción que asisten en la edificación de estructuras, reduciendo el tiempo de construcción.
*   **Coste Base**: Metal: 400, Cristal: 120, Deuterio: 200.
*   **Efecto**: Reduce el tiempo de construcción de edificios.
*   **Factor Coste**: 2.0

### Nanite Factory (Fábrica de Nanobots)
*   **Descripción**: Produce nanobots microscópicos que aceleran masivamente la construcción de edificios, naves y defensas.
*   **Coste Base**: Metal: 1,000,000, Cristal: 500,000, Deuterio: 100,000.
*   **Requisito**: Robotics Factory Nivel 10, Computer Technology Nivel 10.
*   **Efecto**: Reduce a la mitad el tiempo de construcción por cada nivel.
*   **Factor Coste**: 2.0

### Shipyard (Hangar)
*   **Descripción**: Instalación para la construcción de naves espaciales y sistemas de defensa planetaria.
*   **Coste Base**: Metal: 400, Cristal: 200, Deuterio: 100.
*   **Requisito**: Robotics Factory Nivel 2.
*   **Efecto**: Permite construir naves y defensas. A mayor nivel, menor tiempo de construcción de unidades.
*   **Factor Coste**: 2.0

### Metal Storage (Almacén de Metal)
*   **Descripción**: Depósito blindado para almacenar el excedente de metal. Protege una pequeña cantidad de saqueos y permite almacenar más allá del límite básico.
*   **Coste Base**: Metal: 1000.
*   **Capacidad**: 5000 * 2.5^e * 20^Nivel (Fórmula OGame variable, simplificada para el clon).
*   **Factor Coste**: 2.0

### Crystal Storage (Almacén de Cristal)
*   **Descripción**: Contenedores especializados para almacenar cristal de forma segura.
*   **Coste Base**: Metal: 1000, Cristal: 500.
*   **Factor Coste**: 2.0

### Deuterium Tank (Contenedor de Deuterio)
*   **Descripción**: Tanques presurizados para almacenar deuterio volátil.
*   **Coste Base**: Metal: 1000, Cristal: 1000.
*   **Factor Coste**: 2.0

### Research Lab (Laboratorio de Investigación)
*   **Descripción**: Centro científico necesario para descubrir nuevas tecnologías.
*   **Coste Base**: Metal: 200, Cristal: 400, Deuterio: 200.
*   **Efecto**: Permite investigar tecnologías. A mayor nivel, menor tiempo de investigación.
*   **Factor Coste**: 2.0

### Terraformer (Terraformador)
*   **Descripción**: Maquinaria planetaria masiva que hace habitables zonas inhóspitas del planeta, aumentando los campos disponibles.
*   **Coste Base**: Metal: 0, Cristal: 50,000, Deuterio: 100,000, Energía: 1,000.
*   **Efecto**: +5 Campos por nivel (Ocupa 1, neto +4).
*   **Requisito**: Nanite Factory Nivel 1, Energy Technology Nivel 12.
*   **Factor Coste**: 2.0

### Alliance Depot (Depósito de la Alianza)
*   **Descripción**: Permite abastecer de combustible a flotas aliadas estacionadas en órbita.
*   **Coste Base**: Metal: 20,000, Cristal: 40,000.
*   **Factor Coste**: 2.0

### Missile Silo (Silo de Misiles)
*   **Descripción**: Estructura subterránea para almacenar y lanzar misiles interplanetarios y de intercepción.
*   **Coste Base**: Metal: 20,000, Cristal: 20,000, Deuterio: 1,000.
*   **Efecto**: Capacidad para 10 misiles interceptores o 5 interplanetarios por nivel.
*   **Factor Coste**: 2.0

## Estrategias Recomendadas
- Prioriza minas y energía inicialmente para crecimiento sostenible.
- Usa cola para eficiencia: Construye en secuencia para minimizar tiempos muertos.
- Equilibra recursos: Evita sobreproducir uno mientras faltan otros.
- Desmantela si es necesario: Para liberar campos o reasignar recursos.

Este módulo es fundamental para expandir y fortalecer la base económica del imperio.
