# Sistema de Diseño OGame Clone

## 📋 Resumen

Este documento describe el sistema de diseño unificado para el OGame Clone. Todas las páginas deben seguir estos estándares para mantener consistencia visual y experiencia de usuario.

## 🎨 Paleta de Colores

### Fondos
- **Primary**: `var(--color-bg-primary)` - #0a0a0a (fondo principal)
- **Secondary**: `var(--color-bg-secondary)` - #1a1a1a (paneles)
- **Tertiary**: `var(--color-bg-tertiary)` - #2a2a2a (elementos destacados)

### Texto
- **Primary**: `var(--color-text-primary)` - #ffffff
- **Secondary**: `var(--color-text-secondary)` - #cccccc
- **Muted**: `var(--color-text-muted)` - #888888

### Recursos
- **Metal**: `var(--color-metal)` - #b0b0b0
- **Crystal**: `var(--color-crystal)` - #6ab7ff
- **Deuterium**: `var(--color-deuterium)` - #51d151
- **Energy**: `var(--color-energy)` - #ffc107

### Estados
- **Success**: `var(--color-success)` - #4caf50
- **Warning**: `var(--color-warning)` - #ff9800
- **Danger**: `var(--color-danger)` - #f44336
- **Info**: `var(--color-info)` - #2196f3

## 📐 Estructura de Página

### Contenedor Principal
```html
<div class="ogame-container">
    <!-- Todo el contenido de la página -->
</div>
```

Variantes:
- `.ogame-container-sm` - 800px max
- `.ogame-container` - 1200px max (por defecto)
- `.ogame-container-xl` - 1400px max

### Banner
```html
<div class="ogame-banner" style="background-image: url('assets/banners/tech-banner.jpg');">
    <h2>Título de la Página</h2>
    <p>Descripción opcional</p>
</div>
```

## 📦 Componentes

### Cards (Tarjetas)
```html
<div class="ogame-grid">
    <div class="ogame-card">
        <div class="ogame-badge">Nivel 5</div>
        <img src="path/to/image.jpg" alt="Item">
        <h3>Nombre del Item</h3>
        <p>Descripción</p>

        <div class="ogame-cost-section">
            <div class="ogame-cost-item">Metal: 1,000</div>
            <div class="ogame-cost-item">Crystal: 500</div>
        </div>

        <button class="ogame-btn ogame-btn-primary ogame-btn-block">Construir</button>
    </div>
</div>
```

### Panel
```html
<div class="ogame-panel">
    <h2>Título del Panel</h2>
    <p>Contenido del panel</p>
</div>
```

### Cola de Construcción
```html
<div class="ogame-queue-container">
    <h3>En Construcción</h3>
    <div class="ogame-queue-item">
        <span>Metal Mine (Level 6)</span>
        <span class="ogame-queue-timer">00:15:32</span>
    </div>
</div>
```

## 🔘 Botones

### Tipos
```html
<!-- Primary (azul) - Acción principal -->
<button class="ogame-btn ogame-btn-primary">Construir</button>

<!-- Secondary (gris) - Acción secundaria -->
<button class="ogame-btn ogame-btn-secondary">Ver Detalles</button>

<!-- Success (verde) - Acción positiva -->
<button class="ogame-btn ogame-btn-success">Confirmar</button>

<!-- Danger (rojo) - Acción destructiva -->
<button class="ogame-btn ogame-btn-danger">Eliminar</button>
```

### Tamaños
```html
<button class="ogame-btn ogame-btn-primary ogame-btn-sm">Pequeño</button>
<button class="ogame-btn ogame-btn-primary">Normal</button>
<button class="ogame-btn ogame-btn-primary ogame-btn-lg">Grande</button>
<button class="ogame-btn ogame-btn-primary ogame-btn-block">Ancho Completo</button>
```

### Estados
```html
<!-- Deshabilitado -->
<button class="ogame-btn ogame-btn-primary" disabled>No Disponible</button>
```

## 📱 Grid System

### Grid Principal
```html
<div class="ogame-grid">
    <!-- Items se ajustan automáticamente -->
    <!-- Mínimo 300px, máximo 1fr -->
</div>
```

### Grid Pequeño
```html
<div class="ogame-grid-sm">
    <!-- Mínimo 200px -->
</div>
```

### Grid Grande
```html
<div class="ogame-grid-lg">
    <!-- Mínimo 350px -->
</div>
```

## 🎯 Clases de Utilidad

### Texto
```html
<p class="text-center">Texto centrado</p>
<p class="text-muted">Texto atenuado</p>
<p class="text-success">Texto verde</p>
<p class="text-warning">Texto naranja</p>
<p class="text-danger">Texto rojo</p>
```

### Espaciado
```html
<div class="mb-sm">Margen inferior pequeño</div>
<div class="mb-md">Margen inferior mediano</div>
<div class="mb-lg">Margen inferior grande</div>
<div class="mt-lg">Margen superior grande</div>
```

## 📋 Guía de Migración

### Paso 1: Estructura de Página

**ANTES:**
```html
<style>
    .home-container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 20px;
    }
</style>

<div class="home-container">
    <!-- contenido -->
</div>
```

**DESPUÉS:**
```html
<div class="ogame-container">
    <!-- contenido -->
</div>
```

### Paso 2: Banners

**ANTES:**
```html
<style>
    .ogame-banner {
        background-image: url('assets/banners/tech-banner.jpg');
        background-size: cover;
        background-position: center;
        color: white;
        text-align: center;
        padding: 100px 20px;
        margin-bottom: 20px;
    }
</style>

<div class="ogame-banner">
    <h2>Technologies</h2>
</div>
```

**DESPUÉS:**
```html
<!-- Sin estilos inline necesarios -->
<div class="ogame-banner" style="background-image: url('assets/banners/tech-banner.jpg');">
    <h2>Technologies</h2>
    <p>Research to unlock new capabilities</p>
</div>
```

### Paso 3: Botones

**ANTES:**
```html
<style>
    .ogame-card button {
        padding: 10px;
        background-color: #555;
        color: white;
        border: none;
        cursor: pointer;
        border-radius: 5px;
        width: 100%;
    }
</style>

<button>Construir</button>
```

**DESPUÉS:**
```html
<button class="ogame-btn ogame-btn-primary ogame-btn-block">Construir</button>
```

### Paso 4: Cards

**ANTES:**
```html
<style>
    .ogame-card {
        background: linear-gradient(180deg, #1a1a1a 0%, #000000 100%);
        border-radius: 5px;
        padding: 20px;
        text-align: center;
        border: 1px solid #444;
        color: white;
        /* ... más estilos ... */
    }
</style>
```

**DESPUÉS:**
```html
<!-- Usa la clase del sistema de diseño -->
<div class="ogame-card">
    <!-- contenido -->
</div>
```

## ✅ Checklist de Actualización por Página

Para cada página:

- [ ] Envolver contenido en `.ogame-container`
- [ ] Usar `.ogame-banner` con imagen de fondo
- [ ] Reemplazar estilos custom de cards con `.ogame-card`
- [ ] Usar sistema de botones `.ogame-btn-*`
- [ ] Aplicar `.ogame-grid` para layouts
- [ ] Usar clases de utilidad (text-*, mb-*, etc.)
- [ ] Eliminar estilos duplicados del `<style>` inline
- [ ] Verificar responsive en móvil (< 640px)

## 🎨 Variables CSS Disponibles

Puedes usar estas variables en cualquier estilo:

```css
/* Ejemplo de uso personalizado */
.mi-elemento-custom {
    background-color: var(--color-bg-secondary);
    border: 1px solid var(--color-border-medium);
    padding: var(--spacing-md);
    border-radius: var(--radius-md);
    box-shadow: var(--shadow-md);
}
```

## 📱 Responsive Breakpoints

- **Mobile**: < 640px (1 columna)
- **Tablet**: 641px - 1024px (grid auto-fit)
- **Desktop**: > 1024px (grid auto-fit con max container)

El sistema es **mobile-first** - diseña primero para móvil y se adaptará a pantallas más grandes.

## 🚀 Ventajas del Sistema

1. **Consistencia**: Mismo look & feel en todas las páginas
2. **Mantenibilidad**: Cambios globales en un solo archivo
3. **Responsive**: Funciona en todos los tamaños de pantalla
4. **Performance**: Menos CSS duplicado
5. **DX**: Clases claras y predecibles
6. **Accesibilidad**: Colores con buen contraste
