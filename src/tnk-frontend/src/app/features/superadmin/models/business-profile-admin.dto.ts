export interface BusinessProfileAdminDTO {
  id: number;
  name: string;
  address?: string | null;
  phoneNumber?: string | null;
  description?: string | null;
  vendorId: string;
  // Add any other properties returned by your GET endpoints for business profiles
}

// Interface for the PagedResult wrapper
export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
