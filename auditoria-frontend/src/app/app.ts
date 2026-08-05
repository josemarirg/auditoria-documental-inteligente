import { Component, inject, ChangeDetectorRef } from '@angular/core'; // 1. Importamos el detector
import { CommonModule } from '@angular/common'; 
import { ApiService } from './servicios/api'; 

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App {
  private api = inject(ApiService);
  private cdr = inject(ChangeDetectorRef); // 2. Lo inyectamos en nuestra clase

  archivoSeleccionado: File | null = null;
  cargando = false;
  resultado: any = null;
  error = '';

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
    this.cdr.detectChanges(); // 3. Avisamos para que muestre "Analizando..."

    this.api.subirDocumento(this.archivoSeleccionado).subscribe({
      next: (respuesta) => {
        this.resultado = respuesta; 
        this.cargando = false;
        this.cdr.detectChanges(); // 4. ¡Avisamos para que dibuje las tarjetas!
      },
      error: (err) => {
        this.error = 'hubo un error al conectar con el servidor. ¿está encendido?';
        console.error(err);
        this.cargando = false;
        this.cdr.detectChanges(); // 5. Avisamos si hay error
      }
    });
  }
}