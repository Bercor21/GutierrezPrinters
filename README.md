# Gutierrez Printers

Sistema web para llevar el control de las impresoras que entran a reparación en un taller:
ingreso del equipo, etiqueta con código QR, boleta de ingreso imprimible, seguimiento por
estados y buscador. Funciona desde la computadora y desde el celular.

## Qué hace

- **Ingreso en una sola pantalla:** cliente, impresora y falla reportada. Si el teléfono ya
  existe, los datos del cliente y sus impresoras anteriores se cargan solos.
- **Número de orden corto** (`#0042`) para decirlo por teléfono o escribirlo a mano.
- **Etiqueta adhesiva 4 × 6 con QR** para pegar en la impresora. Al escanearla con el celular
  se abre la ficha completa de la orden.
- **Boleta de ingreso** en papel tamaño carta: la copia del cliente, con las condiciones del
  taller y las firmas. Siempre sale en una sola hoja.
- **Estados:** recibida, en diagnóstico, esperando aprobación, esperando repuesto,
  en reparación, lista, no se pudo reparar, entregada, entregada sin reparar y enviada a
  reciclaje.
- **Reglas del taller:** garantía de 6 meses que se pone sola al entregar, diagnóstico de
  ₡7 000 que se cobra siempre, se repare o no; la reparación se cobra aparte.
- **Avisos por WhatsApp** con el mensaje ya escrito según el estado, usando enlaces `wa.me`.
- **Buscador único** por número de orden, cliente, teléfono (con o sin guion), marca,
  modelo o número de serie.
- **Historial** de cada cliente y de cada impresora.
- **Alertas** de órdenes atrasadas y de impresoras que llevan 30 días sin que las retiren,
  con un recordatorio por WhatsApp y la opción de enviarlas a reciclaje.
- **Respaldo en Excel** de órdenes, equipos y clientes.
- **Eliminar** una impresora (con sus órdenes) o un cliente (con sus impresoras y órdenes),
  desde el final de su ficha. Solo lo pueden hacer los administradores y pide confirmación.
- **Inicio de sesión** obligatorio. No hay registro público: los administradores crean las
  cuentas en **Usuarios** (menú de arriba a la derecha), cambian contraseñas y dan o quitan
  permisos de administrador.

## Tecnologías

- C# con ASP.NET Core MVC sobre .NET 10
- Entity Framework Core con PostgreSQL en Supabase
- ASP.NET Core Identity para el login
- Bootstrap 5 y JavaScript sin frameworks; el QR se genera con `qrcode-generator` (MIT)

## Cómo correrlo en tu compu

### 1. Secretos (la conexión a Supabase y el primer usuario)

En Visual Studio, dale clic derecho al proyecto **GutierrezPrinters.Web** y escogé
**Administrar secretos de usuario**. Pegá esto y reemplazá los datos:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=AQUI_EL_HOST.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.AQUI_LA_REFERENCIA;Password=AQUI_LA_CONTRASEÑA;SSL Mode=Require"
  },
  "AdminUser": {
    "Email": "tu-correo@ejemplo.com",
    "Password": "UnaClave2026"
  }
}
```

- El **host** y el **usuario** se copian en Supabase → botón **Connect** → **Session pooler**.
- La **contraseña** es la de la base de datos que pusiste al crear el proyecto en Supabase.
  Si no la recordás: **Project Settings → Database → Reset database password**.
- La contraseña del usuario administrador necesita al menos 8 caracteres, un número y una
  minúscula.

Estos secretos se guardan en tu compu, fuera del proyecto, y **nunca se suben a GitHub**.

### 2. Crear la migración (solo la primera vez)

**Herramientas → Administrador de paquetes NuGet → Consola del Administrador de paquetes**, y escribí:

```
Add-Migration Inicial
```

Esto genera la carpeta `Migrations` con el código que crea las tablas.

### 3. Correr

Dale al botón verde **https**. Al arrancar, la app crea las tablas en Supabase, dentro del
esquema `taller`, y el usuario administrador. Entrá con el correo y la contraseña de
`AdminUser`.

## Configuración del taller

En `appsettings.json`, sección `Shop`, se cambian sin tocar código:

| Dato | Para qué sirve |
| --- | --- |
| `Name`, `Phone`, `Address` | Datos del taller en la boleta, la etiqueta y los mensajes. |
| `PublicUrl` | La dirección del sistema en internet (cuando esté en Azure). El QR de la etiqueta la usa para abrir la orden desde el celular. |
| `LabelWidthMm`, `LabelHeightMm` | Tamaño de la etiqueta. Por defecto 101.6 × 152.4 mm (4 × 6 pulgadas). |
| `WarrantyDays` | Garantía que se pone sola al entregar (180 = 6 meses). |
| `DiagnosisFee` | Lo que cuesta el diagnóstico; se cobra siempre, se repare o no (7000). |
| `AbandonedAfterDays` | Días sin retirar antes de la alerta de reciclaje (30). |
| `Terms` | Condiciones de la boleta. `{diagnostico}`, `{garantia}` y `{dias}` se cambian solos por los valores de arriba. |

El logo está en `wwwroot/img`: `logo.jpg` (completo), `wordmark-white.png` (barra de arriba) y
`wordmark-black.png` (etiqueta). La letra es Archivo (licencia OFL), en `wwwroot/lib/archivo`.

## En Azure: que abra rápido

El plan gratis de Azure (F1) apaga el sistema si nadie entra en 20 minutos, y el siguiente que
lo abre espera a que vuelva a arrancar. Para evitarlo:

- **Despertador interno** (`Services/KeepAwakeService.cs`): en Azure, el sistema visita su
  propia página `/Home/Ping` cada 4 minutos, así nunca llega a apagarse y la conexión con
  Supabase se mantiene abierta. No hay que configurar nada: usa la dirección que Azure le da.
- **Despertador de respaldo en GitHub** (`.github/workflows/mantener-activo.yml`): una vez al
  día visita el sistema por si Azure lo hubiera apagado, y le da actividad a Supabase para que
  no pause la base de datos. Necesita el secreto `APP_URL` en GitHub → Settings → Secrets and
  variables → Actions, con la dirección del sistema (por ejemplo `https://tu-app.azurewebsites.net`).
- **Arranque rápido**: al publicar para `win-x86` el código se precompila (ReadyToRun) y al
  arrancar solo se tocan las tablas si hay migraciones nuevas.

## Estructura

```
GutierrezPrinters.Web/
├── Controllers/    Home (panel), Orders, Customers, Printers, Account
├── Data/           AppDbContext (tablas) y SeedData (primer usuario)
├── Models/         Customer, Printer, RepairOrder y los enums
├── Services/       Formatos, WhatsApp, Excel, datos del taller y despertador
├── ViewModels/     Formularios
├── Views/          Pantallas (Razor)
└── wwwroot/        CSS, JS, íconos y librerías
```
