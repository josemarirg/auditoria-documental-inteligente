import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  // url base de la api 
  private baseUrl = 'http://localhost:5133/api/Documentos';

  // envia el pdf al servidor
  subirDocumento(archivo: File) {
    const formData = new FormData();
    formData.append('archivo', archivo);
    return this.http.post(`${this.baseUrl}/upload`, formData);
  }

  // pide las ultimas facturas procesadas
  obtenerHistorial() {
    return this.http.get(`${this.baseUrl}/historial`);
  }

  // borra todos los datos de prueba
  limpiarHistorial() {
    return this.http.delete(`${this.baseUrl}/limpiar`);
  }
}