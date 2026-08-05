import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private http = inject(HttpClient);
  
  // revisa que el puerto 5133 es el correcto de la api
  private apiUrl = 'http://localhost:5133/api/Documentos'; 

  subirDocumento(archivo: File) {
    // preparamos el archivo para enviarlo
    const formData = new FormData();
    formData.append('archivo', archivo);

    // enviamos el pdf al backend
    return this.http.post(`${this.apiUrl}/upload`, formData);
  }
}