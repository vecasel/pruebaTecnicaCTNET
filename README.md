# Guía de Implementación en Ambiente Productivo – Backend .NET

Esta guía describe, paso a paso, cómo desplegar la versión **.NET (ASP.NET Core Web API)** de la API de **SAC Ríos del Desierto** en un ambiente productivo sencillo.

Se plantean dos escenarios típicos:

- Despliegue en **Windows Server + IIS**
- Despliegue en **Linux (Ubuntu) + Kestrel + Nginx**

---

## 1. Preparar el entorno (.NET)

### 1.1. Instalar .NET SDK / Runtime

En el servidor de producción se recomienda instalar el **ASP.NET Core Runtime** (si solo se va a ejecutar) o el SDK (si también se compila ahí).

En Windows:

- Descarga desde el sitio oficial de .NET el **Hosting Bundle** (incluye runtime + integración con IIS).

En Linux (Ubuntu) – ejemplo rápido:

```bash
# Agregar repositorio Microsoft (consultar docs oficiales para versión específica)
sudo apt update
# Instalar runtime (ejemplo genérico, ajustar versión)
# sudo apt install -y aspnetcore-runtime-8.0
```

> En la prueba técnica basta con explicar que en el servidor se instala el runtime adecuado para la versión de .NET utilizada.

---

## 2. Publicar la aplicación (build de producción)

Desde la máquina de desarrollo (o un servidor de build), situarse en la carpeta del proyecto .NET:

```bash
cd SacRiosDesiertoApi
```

Publicar en modo Release:

```bash
dotnet publish -c Release -o ./publish
```

Esto generará una carpeta `publish/` con todos los archivos necesarios para ejecutar la API en producción.

---

## 3. Configuración de la cadena de conexión y appsettings

En producción se suele utilizar un `appsettings.Production.json` o variables de entorno para:

- Cadena de conexión (`DefaultConnection`)
- Configuración de logging
- Cualquier otra configuración sensible

Ejemplo de `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SERVIDOR_SQL;Database=SacRiosDesiertoDb;User Id=usuario;Password=clave;TrustServerCertificate=True"
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

En `Program.cs` la aplicación usará la configuración correspondiente al entorno (`ASPNETCORE_ENVIRONMENT=Production`).

---

## 4. Despliegue en Windows Server + IIS

### 4.1. Requisitos

- Windows Server
- IIS instalado
- ASP.NET Core Hosting Bundle instalado

### 4.2. Copiar los archivos publicados

Copiar el contenido de la carpeta `publish/` hacia una ruta en el servidor, por ejemplo:

```text
C:\inetpub\wwwroot\SacRiosDesiertoApi\
```

### 4.3. Crear un sitio en IIS

1. Abrir **IIS Manager**.
2. Click derecho en **Sites → Add Website…**
3. Configurar:
   - **Site name**: `SacRiosDesiertoApi`
   - **Physical path**: `C:\inetpub\wwwroot\SacRiosDesiertoApi`
   - **Binding**: puerto (ej. 443 si hay certificado, o 8080/80).
4. Aceptar.

IIS utilizará el módulo de ASP.NET Core para arrancar la aplicación (ejecutando `dotnet SacRiosDesiertoApi.dll` internamente).

### 4.4. Probar el sitio

En el navegador del servidor o desde tu máquina:

```text
https://mi-servidor/api/client/search?document_type=CC&document_number=1022422328
```

Debe devolver la respuesta JSON esperada.

---

## 5. Despliegue en Linux + Nginx + Kestrel

### 5.1. Copiar archivos al servidor

En el servidor Linux, copiar el contenido de `publish/` a una ruta, por ejemplo:

```bash
sudo mkdir -p /opt/sac-rios-desierto-net
sudo cp -r publish/* /opt/sac-rios-desierto-net/
```

### 5.2. Crear servicio systemd para la API

1. Crear archivo de servicio:

   ```bash
   sudo nano /etc/systemd/system/sac-rios-desierto-net.service
   ```

2. Contenido de ejemplo:

   ```ini
   [Unit]
   Description=SAC Rios del Desierto .NET API
   After=network.target

   [Service]
   WorkingDirectory=/opt/sac-rios-desierto-net
   ExecStart=/usr/bin/dotnet /opt/sac-rios-desierto-net/SacRiosDesiertoApi.dll
   Restart=always
   RestartSec=10
   SyslogIdentifier=sac-rios-desierto-net
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production

   [Install]
   WantedBy=multi-user.target
   ```

3. Recargar daemon y habilitar servicio:

   ```bash
   sudo systemctl daemon-reload
   sudo systemctl start sac-rios-desierto-net
   sudo systemctl enable sac-rios-desierto-net
   ```

4. Verificar estado:

   ```bash
   sudo systemctl status sac-rios-desierto-net
   ```

La aplicación escuchará por defecto en `http://localhost:5000` o el puerto configurado en `appsettings` o variables de entorno.

### 5.3. Configurar Nginx como reverse proxy

1. Crear archivo de configuración:

   ```bash
   sudo nano /etc/nginx/sites-available/rios-desierto-net
   ```

2. Ejemplo de configuración:

   ```nginx
   server {
       listen 80;
       server_name mi-dominio.com;

       # Proxy a la API .NET (Kestrel)
       location /api/ {
           proxy_pass         http://127.0.0.1:5000;
           proxy_http_version 1.1;
           proxy_set_header   Upgrade $http_upgrade;
           proxy_set_header   Connection keep-alive;
           proxy_set_header   Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header   X-Forwarded-Proto $scheme;
       }

       # Frontend estático (si se desea servir desde el mismo servidor)
       location / {
           root /opt/sac-rios-desierto-frontend;
           try_files $uri /index.html;
       }
   }
   ```

3. Habilitar sitio y recargar Nginx:

   ```bash
   sudo ln -s /etc/nginx/sites-available/rios-desierto-net /etc/nginx/sites-enabled/
   sudo nginx -t
   sudo systemctl restart nginx
   ```

Con esto:

- `http://mi-dominio.com` servirá el frontend.
- `http://mi-dominio.com/api/...` apuntará a la API .NET.

---

## 6. Consideraciones de CORS en producción

En `Program.cs` se definió una política CORS, por ejemplo:

```csharp
const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "https://mi-frontend.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

Y en el pipeline:

```csharp
app.UseCors(FrontendCorsPolicy);
```

En producción se debe ajustar el origen (`WithOrigins`) al dominio real donde vive el frontend.

---

## 7. Base de datos en producción

La API .NET utiliza **Entity Framework Core** y una cadena de conexión configurada en `ConnectionStrings:DefaultConnection`.

Pasos típicos:

1. Crear la base de datos `SacRiosDesiertoDb` en el servidor SQL Server.
2. Ejecutar migraciones (si se ejecutan desde el servidor de apps):

   ```bash
   dotnet ef database update --project SacRiosDesiertoApi
   ```

3. Poblar datos iniciales (tipos de documento, cliente de prueba, compras de ejemplo) usando:
   - Scripts SQL (como los ejemplos del README).
   - O un seeder en .NET si se desea automatizar.

---

## 8. Resumen de arquitectura productiva (.NET)

- **Nginx / IIS** recibe las peticiones HTTP/HTTPS.
- Reenvía las rutas `/api/` a la aplicación .NET (Kestrel o módulo de ASP.NET Core en IIS).
- La aplicación **ASP.NET Core Web API**:
  - Expone endpoints `/api/client/search`, `/api/client/export`, `/api/reports/loyal-customers`.
  - Utiliza **EF Core** para consultar/actualizar la base de datos.
  - Usa **ClosedXML** para generar archivos Excel en memoria.
- El **frontend** (HTML + JS) consume la API mediante `fetch`, usando la URL pública del backend.

Con esta guía puedes explicar cómo pasar de la versión de desarrollo de la API .NET a un despliegue productivo básico, tanto en Windows como en Linux.
