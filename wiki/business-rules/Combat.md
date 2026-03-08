# Modulo: Combat (Combate)

## Descripcion General
El combate en este proyecto se resuelve automaticamente cuando una mision de ataque llega al planeta objetivo.
La logica vive en `FleetService.HandleCombat` y genera un mensaje tipo `Combat` en el sistema de mensajes.

## Reporte de Combate (Implementado)
Cuando ocurre un combate, el reporte incluye:
- Flota atacante enviada (con nombres de naves y cantidades).
- Defensas enemigas detectadas antes del combate.
- Flota enemiga detectada antes del combate.
- Resultado final: `VICTORY` o `DEFEAT`.
- Comparacion de poder (ataque y vida total de ambos bandos).
- Perdidas propias (naves perdidas del atacante).
- Perdidas del enemigo en defensas (segun `ApplyDefenseCombatLosses`).
- Perdidas de flota enemiga estimadas en victoria (segun la resolucion simplificada actual).
- Campo de escombros generado (metal/cristal) y estado (`Created` o `No debris`).
- Botin capturado (metal/cristal/deuterio) cuando el atacante gana.

## Reglas actuales relevantes
- Resolucion simplificada por score:
  - `attackerScore = attackerAttack / defenderHealth`
  - `defenderScore = defenderAttack / attackerHealth`
- Si gana el atacante:
  - Captura botin limitado por capacidad de carga.
  - Se genera escombro por destruccion aplicada en la resolucion actual.
- Si pierde el atacante:
  - Pierde 50% de cada tipo de nave enviada.
  - Se genera escombro por sus perdidas.
- Las defensas del defensor se reducen con porcentaje configurable (`CombatDefenseLossMinPercentage`/`MaxPercentage`).

## Estado de documentacion
Esta estructura detallada del reporte de combate no estaba documentada de forma explicita en la wiki.
Queda documentada en este archivo.
