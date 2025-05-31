import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { ApiService } from '@core/services/api.service'; // Using path alias
import {
  BusinessProfileAdminDTO,
  PagedResult
} from '../models/business-profile-admin.dto';
import { CreateBusinessProfileAdminRequest } from '../models/create-business-profile-admin.request';
import { UpdateBusinessProfileAdminRequest } from '../models/update-business-profile-admin.request';
import { UserDetailsAdminDTO } from '../models/user-details-admin.dto';

interface ApiWrappedResponse<T> {
  value: T;
  status: number;
  isSuccess: boolean;
  successMessage?: string;
  correlationId?: string;
  location?: string;
  errors?: string[];
  validationErrors?: any[];
}

@Injectable({
  providedIn: 'root' // Or 'providedIn: SuperadminModule' if specific to this module
})
export class SuperadminVendorService {
  private readonly baseUrl = '/admin/businessprofiles'; // Relative to API base URL from ApiService

  constructor(private apiService: ApiService, private http: HttpClient) {} // Inject HttpClient if ApiService doesn't expose all methods

  getUsers(
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm?: string
  ): Observable<PagedResult<UserDetailsAdminDTO>> { // Return type is correct
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    if (searchTerm && searchTerm.trim() !== '') {
      params = params.set('searchTerm', searchTerm.trim());
    }

    // 1. Expect the wrapped response from the apiService.get call
    return this.apiService.get<ApiWrappedResponse<PagedResult<UserDetailsAdminDTO>>>(`/admin/users`, params)
      .pipe(
        // 2. Use the map operator to extract and return only the 'value' property
        map(apiResponse => {
          if (apiResponse && apiResponse.isSuccess) {
            return apiResponse.value; // This is the PagedResult<UserDetailsAdminDTO>
          }
          // Handle cases where isSuccess might be false but still returns a value, or throw an error
          // For simplicity, assuming isSuccess true means value is present.
          // You might want more sophisticated error handling here based on your API contract.
          if (apiResponse && apiResponse.value) {
            return apiResponse.value;
          }
          throw new Error('API response did not indicate success or value was missing.');
        })
      );
  }

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
