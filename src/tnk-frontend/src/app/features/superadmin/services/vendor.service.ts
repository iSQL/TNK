import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '@core/services/api.service'; // Using path alias
import {
  BusinessProfileAdminDTO,
  PagedResult
} from '../models/business-profile-admin.dto';
import { CreateBusinessProfileAdminRequest } from '../models/create-business-profile-admin.request';
import { UpdateBusinessProfileAdminRequest } from '../models/update-business-profile-admin.request';

@Injectable({
  providedIn: 'root' // Or 'providedIn: SuperadminModule' if specific to this module
})
export class SuperadminVendorService {
  private readonly baseUrl = '/admin/businessprofiles'; // Relative to API base URL from ApiService

  constructor(private apiService: ApiService, private http: HttpClient) {} // Inject HttpClient if ApiService doesn't expose all methods

  getVendors(
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm?: string
  ): Observable<PagedResult<BusinessProfileAdminDTO>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }
    return this.apiService.get<PagedResult<BusinessProfileAdminDTO>>(`${this.baseUrl}`, params);
  }

  getVendorById(id: number): Observable<BusinessProfileAdminDTO> {
    return this.apiService.get<BusinessProfileAdminDTO>(`${this.baseUrl}/${id}`);
  }

  createVendor(payload: CreateBusinessProfileAdminRequest): Observable<BusinessProfileAdminDTO> {
    return this.apiService.post<BusinessProfileAdminDTO>(`${this.baseUrl}`, payload);
  }

  updateVendor(id: number, payload: UpdateBusinessProfileAdminRequest): Observable<BusinessProfileAdminDTO> {
    return this.apiService.put<BusinessProfileAdminDTO>(`${this.baseUrl}/${id}`, payload);
  }

  deleteVendor(id: number): Observable<void> { // Assuming DELETE returns 204 No Content
    return this.apiService.delete<void>(`${this.baseUrl}/${id}`);
  }
}
