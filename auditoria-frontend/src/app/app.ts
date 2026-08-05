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

    this.cargando = true;
    this.error = '';
    this.resultado = null;
    this.cdr.detectChanges(); 

    this.api.subirDocumento(this.archivoSeleccionado).subscribe({
      next: (respuesta) => {
        this.resultado = respuesta; 
        this.cargando = false;
        this.cargarHistorial(); // actualiza la tabla con el nuevo documento automaticamente
        this.cdr.detectChanges(); 
      },
      error: (err) => {
        this.error = 'error de conexion con el servidor.';
        console.error(err);
        this.cargando = false;
        this.cdr.detectChanges(); 
      }
    });
  }
}