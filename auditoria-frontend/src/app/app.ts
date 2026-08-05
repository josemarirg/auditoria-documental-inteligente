import { Component, inject, ChangeDetectorRef, OnInit } from '@angular/core'; 
import { CommonModule } from '@angular/common'; 
import { ApiService } from './servicios/api'; 

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit {
  private api = inject(ApiService);
  private cdr = inject(ChangeDetectorRef);

  archivoSeleccionado: File | null = null;
  cargando = false;
  resultado: any = null;
  error = '';
  historial: any[] = []; // aqui guardamos las facturas para la tabla

  // se ejecuta al arrancar la pagina para cargar los datos previos
  ngOnInit() {
    this.cargarHistorial();
  }

  // pide los datos al backend de forma segura y actualiza la vista
  cargarHistorial() {
    this.api.obtenerHistorial().subscribe({
      next: (datos: any) => {
        this.historial = datos;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('error al cargar historial', err)
    });
  }

  limpiarDatos() {
    if (confirm('¿seguro que quieres borrar todos los datos de prueba?')) {
      this.api.limpiarHistorial().subscribe({
        next: () => {
          this.historial = []; // vaciamos la tabla en la pantalla
          this.resultado = null; // quitamos el ultimo resultado visual
          this.cdr.detectChanges();
        },
        error: (err) => console.error('error al limpiar', err)
      });
    }
  }

  seleccionarArchivo(evento: any) {
    const archivo = evento.target.files[0];
    if (archivo) {
      this.archivoSeleccionado = archivo;
      this.error = '';
      this.resultado = null;
    }
  }

  subir() {
    if (!this.archivoSeleccionado) return;

    // comprobamos el peso antes de llamar al backend
    if (this.archivoSeleccionado.size > 2097152) {
      this.error = 'el archivo es demasiado grande. el límite máximo es de 2mb.';
      return; 
    }

    this.cargando = true;
    this.error = '';
    this.resultado = null;
    this.cdr.detectChanges(); 

    this.api.subirDocumento(this.archivoSeleccionado).subscribe({
      next: (respuesta) => {
        this.resultado = respuesta; 
        this.cargando = false;
        this.cargarHistorial(); 
        this.cdr.detectChanges(); 
      },
      error: (err) => {
        // capturamos los bloqueos de seguridad del backend (rate limiting, etc)
        if (err.status === 429) {
          this.error = 'has alcanzado el límite de seguridad anti-bots. por favor, espera un minuto.';
        } else if (err.status === 500) {
          this.error = 'la inteligencia artificial no ha podido analizar este documento. asegúrate de que es una factura válida y legible.';
        } else {
          this.error = 'error de conexión. revisa si el servidor está encendido.';
        }
        
        console.error(err);
        this.cargando = false;
        this.cdr.detectChanges(); 
      }
    });
  }
}