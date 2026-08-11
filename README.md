# Asistente Tramix - Plataforma Gubernamental Híbrida

Plataforma de servicios gubernamentales impulsada por Inteligencia Artificial, diseñada con una arquitectura Cloud-Native (Serverless) para la gestión de citas y asesoría ciudadana.

## Arquitectura y Tecnologías
Este proyecto demuestra la implementación de un sistema moderno, escalable y seguro:

*   **Frontend (UI/UX):** HTML5, CSS3 (Flexbox/Grid, Responsive Design) y Vanilla JavaScript.
*   **Backend (Serverless):** C# .NET 8 (Isolated Worker Model) sobre **Azure Functions**.
*   **Base de Datos (NoSQL):** Azure Table Storage para persistencia de datos ultrarrápida y escalable.
*   **Inteligencia Artificial (Híbrida):** 
    *   *Producción:* Integración con Groq API (Llama 3) para latencia ultrabaja.
    *   *Desarrollo:* Entorno preparado para agentes locales.
    *   *RAG UI-Aware:* Ingeniería de prompts contextual que permite a la IA interactuar con el DOM de la página.

## 🛡️ Características de Seguridad y Rendimiento
*   **Shift-Left Security:** Validaciones estrictas de formato (CURP/RFC) implementadas en el cliente (JS) antes de interactuar con la API, minimizando costos de cómputo en la nube.
*   **Manejo de Estados:** Implementación de asincronismo (`async/await`) y *Loading States* visuales (spinners) para bloquear peticiones duplicadas.
*   **Protección de Secretos:** Configuración estricta de variables de entorno (`local.settings.json`) excluida del control de versiones.

## ⚙️ Instalación y Ejecución Local
1. Clonar el repositorio: `git clone https://github.com/tu-usuario/asistente-tramix.git`
2. Abrir la solución en Visual Studio.
3. Asegurar tener el emulador de Azure Storage (Azurite) activo.
4. Crear el archivo `local.settings.json` en la raíz del backend con tu API Key.
5. Ejecutar el proyecto (F5) y abrir `index.html` en tu navegador.

---
**Desarrollador:** Ramón Sosa Vázquez  
*Proyecto de portafolio - Licenciatura en Ingeniería de la Tecnología de la Información e Innovación Digital | Universidad Politécnica de Pénjamo (UPPE)*
