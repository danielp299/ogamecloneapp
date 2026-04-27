# Plan de Implementación de Consumo de Recursos Globales

## Objetivo
Implementar un sistema de gestión de recursos centralizado (`ResourceService`) y asegurar que todas las páginas (`BuildingsPage`, `FactoryPage`, `FleetPage`, `DefensePage`, `TechnologyPage`) consuman recursos de este estado global validando la disponibilidad antes de cualquier acción.

## 1. Servicio Global (`ResourceService`)
**Estado:** ✅ Implementado
- Clase Singleton creada en `Services/ResourceService.cs`.
- Maneja `Metal`, `Crystal`, `Deuterium`.
- Métodos `HasResources(m, c, d)` y `ConsumeResources(m, c, d)` implementados.
- Evento `OnChange` para notificar actualizaciones a la UI.
- Registrado en `Program.cs`.

## 2. Integración por Página

### 2.1. `BuildingsPage.razor`
**Estado Actual:**
- Variables locales `UsedFields`, `MaxFields`.
- No valida ni consume recursos globales al construir.
- Lógica de construcción incompleta (no sube nivel).

**Cambios Requeridos:**
- Inyectar `ResourceService`.
- Reemplazar la lógica de validación de costos en `AddToQueue`.
- Llamar a `ResourceService.ConsumeResources(...)` antes de agregar a la cola.
- Suscribirse a `OnChange` para actualizar la vista cuando cambien los recursos (opcional, pero recomendado para mostrar recursos actualizados).

### 2.2. `FactoryPage.razor`
**Estado Actual:**
- Variables locales `Metal`, `Crystal`, `Deuterium` hardcodeadas (10000).
- Valida contra estas variables locales.

**Cambios Requeridos:**
- Inyectar `ResourceService`.
- Eliminar variables locales de recursos.
- Actualizar `AddToQueueQuantity` para usar `ResourceService.ConsumeResources`.

### 2.3. `DefensePage.razor`
**Estado Actual:**
- (Pendiente de análisis detallado, pero se asume similar a Factory).
- Probablemente use variables locales.

**Cambios Requeridos:**
- Inyectar `ResourceService`.
- Validar y consumir recursos al construir defensas.

### 2.4. `FleetPage.razor`
**Estado Actual:**
- (Pendiente de análisis detallado).
- El envío de flotas consume **combustible (Deuterio)**.

**Cambios Requeridos:**
- Inyectar `ResourceService`.
- Validar que haya suficiente Deuterio para el vuelo (`FuelConsumption`).
- Consumir el deuterio al enviar la flota (`SendFleet`).

### 2.5. `TechnologyPage.razor`
**Estado Actual:**
- (Pendiente de análisis detallado).

**Cambios Requeridos:**
- Inyectar `ResourceService`.
- Validar y consumir recursos al iniciar investigaciones.

## 3. Visualización Global
- Asegurar que el componente de cabecera (`Home.razor` o el layout principal) muestre los recursos del `ResourceService` para que el usuario vea su saldo actual en todo momento.

---
*Este documento se actualizará a medida que progrese la implementación.*
