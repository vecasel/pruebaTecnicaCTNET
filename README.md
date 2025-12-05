# SAC Ríos del Desierto – API .NET (Prueba Técnica)

Este proyecto implementa la versión **.NET** de la API para el sistema de fidelización de clientes **“Ríos del Desierto S.A.S.”**, equivalente a la versión desarrollada en **Python/Django**.

El objetivo de la API es:

1. Consultar un cliente por tipo y número de documento, incluyendo sus compras.
2. Exportar los datos del cliente y sus compras a **CSV**.
3. Generar un reporte de **clientes fidelizados** (los que superan un monto mínimo de compras en el último mes) en **Excel (.xlsx)**.

---

## 1. Tecnologías utilizadas

- **.NET**: ASP.NET Core Web API
- **ORM**: Entity Framework Core
- **Base de datos**: SQL Server
- **Librería Excel**: ClosedXML
- **Formato de exportación**: CSV y Excel
- **Documentación interactiva**: Swagger (OpenAPI)
- **Frontend**: HTML + JavaScript (fetch) consumiendo esta API

---

## 2. Arquitectura general

La solución .NET está organizada en varias piezas clave:

- **Program.cs**  
  Configuración de servicios (DbContext, CORS, Controllers, Swagger) y pipeline HTTP.

- **Data/AppDbContext.cs**  
  DbContext de Entity Framework Core. Expone las tablas principales y define relaciones e índices.

- **Models/**  
  Entidades de dominio mapeadas a tablas de base de datos:
  - `DocumentType`  
  - `Client`  
  - `Purchase`  

- **Dtos/**  
  Objetos de transferencia de datos utilizados para controlar el formato del JSON que se devuelve al frontend:
  - `DocumentTypeDto`  
  - `ClientDto`  
  - `PurchaseDto`  

- **Controllers/**  
  Controladores Web API que exponen los endpoints REST:
  - `ClientController`  
    - `GET /api/client/search`  
    - `GET /api/client/export`  
  - `ReportsController`  
    - `GET /api/reports/loyal-customers`  

---

## 3. Modelo de datos

### 3.1. Entidades principales

#### DocumentType

Representa el tipo de documento del cliente (`CC`, `NIT`, `PAS`, etc.).

Campos:
- `Id` (int, PK)
- `Code` (string) – Ej: `CC`
- `Name` (string) – Ej: `Cédula de ciudadanía`
- Relación 1:N con `Client`

#### Client

Representa un cliente registrado en el sistema.

Campos principales:
- `Id` (int, PK)
- `DocumentTypeId` (FK a `DocumentType`)
- `DocumentNumber` (string) – Ej: `1022422328`
- `FirstName` (string)
- `LastName` (string)
- `Email` (string)
- `Phone` (string)
- `CreatedAt`, `UpdatedAt` (DateTime)
- Relación 1:N con `Purchase`

Además, se define un **índice único** sobre `(DocumentTypeId, DocumentNumber)` para evitar clientes duplicados con el mismo tipo y número de documento.

#### Purchase

Representa una compra realizada por un cliente.

Campos:
- `Id` (int, PK)
- `ClientId` (FK a `Client`)
- `Amount` (decimal) – Monto de la compra
- `PurchaseDate` (DateTime)
- `Description` (string, opcional)
- `OrderNumber` (string, opcional)
- `CreatedAt` (DateTime)

---

## 4. Configuración del proyecto

### 4.1. Cadena de conexión (SQL Server)

En el archivo `appsettings.json` se configura la cadena de conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=SacRiosDesiertoDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Ajustar según el entorno local (nombre de servidor, base de datos, etc.).

### 4.2. DbContext en Program.cs

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
```

Esto indica que `AppDbContext` usará SQL Server con la cadena `DefaultConnection`.

---

## 5. Configuración de CORS

Para permitir que un frontend en otro puerto (por ejemplo `http://127.0.0.1:5500`) consuma la API, se configura **CORS** en `Program.cs`:

```csharp
const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://127.0.0.1:5500",
                "http://localhost:5500"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

En el pipeline de la aplicación se activa la política:

```csharp
app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();
```

De esta manera, las respuestas incluirán el header `Access-Control-Allow-Origin` y el navegador permitirá las peticiones desde el frontend.

---

## 6. Endpoints principales

### 6.1. `GET /api/client/search`

Busca un cliente por tipo y número de documento y devuelve sus datos junto con la lista de compras.

**Ejemplo de request:**

```http
GET https://localhost:44304/api/client/search?document_type=CC&document_number=1022422328
```

**Parámetros:**

- `document_type` (string, requerido) – Código del tipo de documento (`CC`, `NIT`, etc.).  
- `document_number` (string, requerido) – Número de documento del cliente.

**Respuestas posibles:**

- `200 OK` – Cliente encontrado, devuelve JSON con datos del cliente y compras.
- `400 Bad Request` – Faltan parámetros o tipo de documento inválido.
- `404 Not Found` – Cliente no encontrado.

**Ejemplo de respuesta 200:**

```json
{
  "documentType": {
    "code": "CC",
    "name": "Cédula de ciudadanía"
  },
  "documentNumber": "1022422328",
  "firstName": "Daniel",
  "lastName": "Velasquez",
  "email": "daniel@example.com",
  "phone": "3001234567",
  "purchases": [
    {
      "amount": 2500000,
      "purchaseDate": "2025-12-04T20:01:58.0566667",
      "description": "Compra en Falabella Hogar",
      "orderNumber": "ORD-1001"
    },
    {
      "amount": 3200000,
      "purchaseDate": "2025-11-24T20:01:58.0566667",
      "description": "Electrodomésticos",
      "orderNumber": "ORD-1002"
    },
    {
      "amount": 450000,
      "purchaseDate": "2025-11-14T20:01:58.0566667",
      "description": "Ropa Hombre",
      "orderNumber": "ORD-1003"
    }
  ]
}
```

### 6.2. `GET /api/client/export`

Genera un archivo **CSV** con los datos del cliente y todas sus compras.

**Request:**

```http
GET https://localhost:44304/api/client/export?document_type=CC&document_number=1022422328
```

El servidor:

1. Valida los parámetros.
2. Busca el `DocumentType` (por `Code`).
3. Busca el `Client` con ese tipo y número de documento.
4. Construye un CSV en memoria con la siguiente estructura:

```text
Datos del cliente
Tipo documento;Número de documento;Nombre;Apellido;Correo;Teléfono
CC;1022422328;Daniel;Velasquez;daniel@example.com;3001234567

Compras del cliente
Fecha de compra;Monto;Descripción;Número de orden
2025-12-04;2500000;Compra en Falabella Hogar;ORD-1001
2025-11-24;3200000;Electrodomésticos;ORD-1002
2025-11-14;450000;Ropa Hombre;ORD-1003
```

Devuelve el CSV como un archivo descargable.

### 6.3. `GET /api/reports/loyal-customers`

Genera un archivo **Excel (.xlsx)** con los clientes que superan un monto mínimo de compras en el último mes.

Reglas de negocio:

- Se consideran las compras de los **últimos 30 días**.
- Se agrupan las compras por cliente y se calcula el total.
- Se filtran los clientes cuyo total de compras en ese periodo sea **mayor a 5.000.000**.
- Se genera una hoja de cálculo con columnas como:
  - `Tipo documento`
  - `Nombre tipo documento`
  - `Número de documento`
  - `Nombre`
  - `Apellido`
  - `Correo`
  - `Teléfono`
  - `Total último mes`

El archivo se construye en memoria utilizando **ClosedXML** y se devuelve como un archivo `.xlsx` descargable.

**Ejemplo de request:**

```http
GET https://localhost:44304/api/reports/loyal-customers
```

**Respuestas:**

- `200 OK` – Devuelve el archivo Excel.
- `404 Not Found` – Si no hay compras en el último mes o ningún cliente supera el monto mínimo.

---

## 7. Población de datos de prueba

Ejemplo de inserciones SQL para probar la API con un cliente real.

### 7.1. Insertar tipo de documento (DocumentTypes)

```sql
INSERT INTO DocumentTypes (Code, Name)
VALUES ('CC', 'Cédula de ciudadanía');
```

### 7.2. Insertar cliente (Clients)

```sql
INSERT INTO Clients (
    DocumentTypeId, DocumentNumber, FirstName, LastName,
    Email, Phone, CreatedAt, UpdatedAt
) VALUES (
    1,                         -- Id del tipo de documento CC
    '1022422328',
    'Daniel',
    'Velasquez',
    'daniel@example.com',
    '3001234567',
    GETDATE(),
    GETDATE()
);
```

### 7.3. Insertar compras (Purchases)

```sql
INSERT INTO Purchases (ClientId, Amount, PurchaseDate, Description, OrderNumber, CreatedAt)
VALUES
(1, 2500000, GETDATE(), 'Compra en Falabella Hogar', 'ORD-1001', GETDATE()),
(1, 3200000, DATEADD(DAY, -10, GETDATE()), 'Electrodomésticos', 'ORD-1002', GETDATE()),
(1, 450000, DATEADD(DAY, -20, GETDATE()), 'Ropa Hombre', 'ORD-1003', GETDATE());
```

Con estos datos de prueba, el cliente `CC – 1022422328` puede consultarse y mostrará compras en el frontend.

---

## 8. Ejecución del proyecto

### 8.1. Requisitos previos

- .NET SDK (6, 7 u 8)
- SQL Server (local o remoto)
- Herramienta para ejecutar SQL (por ejemplo, SQL Server Management Studio)
- Navegador para consumir Swagger o Postman para pruebas

### 8.2. Pasos para ejecutar

1. Restaurar paquetes y compilar:
   ```bash
   dotnet restore
   dotnet build
   ```

2. Ejecutar migraciones (si se usan migraciones de EF Core):
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

3. Levantar la API:
   ```bash
   dotnet run
   ```

4. Abrir Swagger en el navegador (URL aproximada):
   ```text
   https://localhost:44304/swagger
   ```

5. Probar los endpoints directamente desde Swagger o con Postman.

---

## 9. Integración con el frontend

El frontend es una página HTML con JavaScript que consume esta API:

- Para **buscar cliente**, realiza un `fetch` a:

  ```js
  const url = `${API_BASE_URL}/api/client/search/?document_type=${encodeURIComponent(documentType)}&document_number=${encodeURIComponent(documentNumber)}`;
  const response = await fetch(url);
  ```

- Para **exportar CSV**, abre una nueva pestaña con:

  ```js
  const url = `${API_BASE_URL}/api/client/export/?document_type=${encodeURIComponent(documentType)}&document_number=${encodeURIComponent(documentNumber)}`;
  window.open(url, '_blank');
  ```

- Para el **reporte de fidelización**, consume el endpoint `/api/reports/loyal-customers` y descarga el Excel generado por la API .NET.

---

## 10. Resumen para la entrevista

- API construida con **ASP.NET Core Web API** + **EF Core**.
- Modelo de datos sencillo, pero preparado para extensión (clientes, tipos de documento, compras).
- Endpoints REST:
  - Búsqueda de cliente + compras.
  - Exportación a CSV de un cliente.
  - Reporte de clientes fieles en Excel.
- Uso de:
  - **DTOs** para controlar el contrato de salida.
  - **CORS** para permitir front en un origen distinto.
  - **ClosedXML** para generación de Excel en memoria.
  - **Swagger** para documentación y pruebas rápidas.

Este README resume el diseño técnico de la solución .NET y sirve como guía de instalación, uso y explicación en la prueba técnica.
