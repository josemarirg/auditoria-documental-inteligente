import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  // url base de la api 
  private baseUrl = 'https://api-auditoria-facturas-g3hwfqd9a7h6bgb8.spaincentral-01.azurewebsites.net/api/Documentos';

  // envia el pdf al servidor
  subirDocumento(archivo: File) {
    const formData = new FormData();
    formData.append('archivo', archivo);
    return this.http.post(`${this.baseUrl}/upload`, formData);
  }

  // pide las ultimas facturas forzando al navegador a no usar la cache
  obtenerHistorial() {
    const timestamp = new Date().getTime();
    return this.http.get(`${this.baseUrl}/historial?t=${timestamp}`);
  }

  // borra todos los datos de prueba
  limpiarHistorial() {
    return this.http.delete(`${this.baseUrl}/limpiar`);
  }
}