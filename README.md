# 📄 Auditoría Documental Inteligente de Facturas

![Estado](https://img.shields.io/badge/Estado-En_Producción-2EA043?style=for-the-badge)
![Versión](https://img.shields.io/badge/Versión-1.0.0-007ACC?style=for-the-badge)
![Arquitectura](https://img.shields.io/badge/Arquitectura-Desacoplada-D2691E?style=for-the-badge)

Una aplicación web full-stack de nivel empresarial diseñada para automatizar la extracción, análisis y auditoría de datos de facturas físicas mediante el uso de Inteligencia Artificial.

El sistema permite la subida de documentos PDF, procesa su contenido mediante modelos de visión por computador, y almacena tanto el archivo físico como los datos estructurados en un entorno seguro en la nube.

🔗 **Demo en vivo:** [https://auditoria-factura.vercel.app](https://auditoria-factura.vercel.app)

---

## ✨ Características Principales

*   🧠 **Procesamiento Inteligente:** Extracción automatizada de datos clave de facturas (emisor, importes, fechas, conceptos) utilizando Inteligencia Artificial.
*   ☁️ **Almacenamiento en la Nube:** Subida segura y gestión de archivos PDF originales.
*   📊 **Historial de Auditoría:** Registro completo y persistente de todas las facturas procesadas con lectura rápida desde base de datos relacional.
*   🧩 **Arquitectura Desacoplada:** Separación total entre cliente (SPA) y servidor (API REST) para máxima escalabilidad y rendimiento.
*   🌍 **Despliegue Global:** Interfaz de usuario servida a través de CDN (Edge Network) para una carga ultrarrápida.

---

## 🏗️ Arquitectura y Tecnologías

Este proyecto está construido siguiendo los estándares de la industria para aplicaciones modernas nativas de la nube (Cloud-Native), utilizando servicios de Microsoft Azure y Vercel.

### 💻 Frontend (Cliente)
*   **Framework:** ![Angular](https://img.shields.io/badge/Angular-DD0031?style=flat-square&logo=angular&logoColor=white)
*   **Lenguaje:** ![TypeScript](https://img.shields.io/badge/TypeScript-007ACC?style=flat-square&logo=typescript&logoColor=white)
*   **Hosting:** Vercel *(Edge Network, CI/CD automático, HTTPS integrado)*

### ⚙️ Backend (API REST)
*   **Framework:** ![.NET Core](https://img.shields.io/badge/.NET_Core-5C2D91?style=flat-square&logo=.net&logoColor=white) (C#)
*   **Hosting:** Azure App Service
*   **Políticas:** CORS configurado de forma estricta para el dominio de producción.

### 🧠 Datos e Inteligencia Artificial (Microsoft Azure)
*   **Base de Datos:** Azure SQL Database *(Almacenamiento relacional de datos extraídos)*.
*   **Almacenamiento de Archivos:** Azure Blob Storage *(Persistencia segura de documentos PDF)*.
*   **Procesamiento OCR/IA:** Azure Cognitive Services / Document Intelligence.
