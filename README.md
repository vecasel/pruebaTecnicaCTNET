# SAC Ríos del Desierto – API .NET  
## Documentación General + Guía de Implementación en Producción

Este documento unifica el **README técnico** del proyecto .NET y la **Guía de Implementación** para entornos productivos. Su objetivo es ofrecer una visión completa del sistema, su arquitectura, uso, despliegue y administración.

---

# 1. Descripción General del Proyecto

La API **SAC Ríos del Desierto – versión .NET** implementa un sistema de fidelización de clientes que permite:

1. Consultar un cliente por tipo y número de documento, incluyendo su historial de compras.
2. Exportar la información completa del cliente en formato **CSV**.
3. Generar un reporte de **clientes fidelizados** en archivo **Excel (.xlsx)** usando ClosedXML.
4. Exponer endpoints REST documentados con Swagger.

Este backend reemplaza o complementa la versión Python/Django, manteniendo la misma lógica de negocio.

---

# 2. Tecnologías Utilizadas

- **.NET 7/8 – ASP.NET Core Web API**  
- **Entity Framework Core** (ORM)  
- **SQL Server**  
- **ClosedXML** (generación de Excel)  
- **Swagger / OpenAPI**  
- **Frontend estático**: HTML + JavaScript usando fetch para el consumo de la API  
- **CORS** habilitado para comunicación con frontends externos  

---

# 3. Arquitectura del Proyecto

## 3.1 Estructura Principal

- **Program.cs**  
  Configuración de servicios, pipeline, Swagger, CORS y DbContext.

- **Data/AppDbContext.cs**  
  Mapea las entidades hacia SQL Server.

- **Models/**  
  - DocumentType  
  - Client  
  - Purchase  

- **Dtos/**  
  Evitan exponer entidades completas a través de la API.

- **Controllers/**  
  - `ClientController`: búsqueda y exportación de datos  
  - `ReportsController`: reporte de clientes fidelizados  

---

# 4. Modelo de Datos

## 4.1 Entidades

### DocumentType
- Id  
- Code  
- Name  

### Client
- Id  
- DocumentTypeId  
- DocumentNumber  
- FirstName  
- LastName  
- Email  
- Phone  
- CreatedAt / UpdatedAt  
- Relación 1:N con Purchase  

Índice único:  
`(DocumentTypeId, DocumentNumber)`

### Purchase
- Id  
- ClientId  
- Amount  
- PurchaseDate  
- Description  
- OrderNumber  
- CreatedAt  

---

# 5. Endpoints Principales

## 5.1 GET `/api/client/search`
Busca y devuelve cliente + compras.

## 5.2 GET `/api/client/export`
Genera un archivo CSV con la información del cliente.

## 5.3 GET `/api/reports/loyal-customers`
Genera un reporte en Excel con clientes cuyo total de compras en últimos 30 días supera **5.000.000**.

---

# 6. Configuración y Ejecución en Entorno Local

## 6.1 appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\SQLEXPRESS;Database=SacRiosDesiertoDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "AllowedHosts": "*"
}
```

## 6.2 Migraciones EF Core

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 6.3 Ejecutar API

```bash
dotnet run
```

Swagger estará disponible en:

```
https://localhost:44304/swagger
```

---

# 7. Guía de Implementación en Ambiente Productivo

Esta sección corresponde a la integración de la **guía de despliegue**, aplicable a:

- **Windows Server + IIS**
- **Linux + Kestrel + Nginx**

---

# 7.1 Preparar el entorno .NET

### Windows
Instalar el **Hosting Bundle** de .NET.

### Linux (Ubuntu)

```bash
sudo apt update
# Instalar el runtime según versión de .NET usada
```

---

# 7.2 Publicar la aplicación

Desde desarrollo:

```bash
dotnet publish -c Release -o ./publish
```

Se generará la carpeta `publish/` lista para despliegue.

---

# 7.3 Configuración de Producción

Se recomienda archivo:

`appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SERVIDOR_SQL;Database=SacRiosDesiertoDb;User Id=usuario;Password=clave;TrustServerCertificate=True"
  }
}
```

---

# 8. Despliegue en **Windows Server + IIS**

## 8.1 Pasos

1. Instalar IIS + Hosting Bundle.
2. Copiar carpeta **publish/** a:

```
C:\inetpub\wwwroot\SacRiosDesiertoApi```

3. Crear sitio en IIS:
   - Nombre: SacRiosDesiertoApi  
   - Path: ruta anterior  
   - Puerto: 80/443  

4. Probar en navegador:

```
http://mi-servidor/api/client/search?document_type=CC&document_number=123
```

---

# 9. Despliegue en **Linux + Kestrel + Nginx**

## 9.1 Copiar archivos

```bash
sudo mkdir -p /opt/sac-rios-desierto-net
sudo cp -r publish/* /opt/sac-rios-desierto-net/
```

## 9.2 Crear servicio systemd

Archivo:

`/etc/systemd/system/sac-rios-desierto-net.service`

```ini
[Unit]
Description=SAC Rios del Desierto API .NET
After=network.target

[Service]
WorkingDirectory=/opt/sac-rios-desierto-net
ExecStart=/usr/bin/dotnet /opt/sac-rios-desierto-net/SacRiosDesiertoApi.dll
Restart=always
Environment=ASPNETCORE_ENVIRONMENT=Production
User=www-data

[Install]
WantedBy=multi-user.target
```

Activar:

```bash
sudo systemctl daemon-reload
sudo systemctl start sac-rios-desierto-net
sudo systemctl enable sac-rios-desierto-net
```

## 9.3 Configurar Nginx

Archivo:

`/etc/nginx/sites-available/rios-desierto-net`

```nginx
server {
    listen 80;
    server_name mi-dominio.com;

    location /api/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location / {
        root /opt/sac-frontend;
        try_files $uri /index.html;
    }
}
```

Habilitar sitio:

```bash
sudo ln -s /etc/nginx/sites-available/rios-desierto-net /etc/nginx/sites-enabled/
sudo systemctl restart nginx
```

---

# 10. Configuración de CORS en Producción

En Program.cs:

```csharp
policy.WithOrigins("https://mi-frontend.com")
      .AllowAnyHeader()
      .AllowAnyMethod();
```

---

# 11. Base de Datos en Producción

1. Crear BD en SQL Server.
2. Ejecutar migraciones o scripts.
3. Poblar datos iniciales (tipos de documento, cliente de prueba, compras).

---

# 12. Resumen Completo

| Sección | Contenido |
|--------|-----------|
| Backend | API .NET Core + EF Core + SQL Server |
| Funcionalidad | Clientes, compras, exportación CSV, reporte Excel |
| Despliegue | IIS o Linux con Kestrel + Nginx |
| Configuración | appsettings, cadenas de conexión, CORS |
| Frontend | HTML + JS con fetch |

El sistema queda totalmente documentado para **explicación en entrevista**, **entrega técnica** y **puesta en producción real**.

---

# 13. Autor
Proyecto realizado como parte de una **Prueba Técnica .NET**.

