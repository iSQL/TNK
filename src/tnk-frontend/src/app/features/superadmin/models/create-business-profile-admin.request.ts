export interface CreateBusinessProfileAdminRequest {
  vendorId: string;
  name: string;
  address?: string | null;
  phoneNumber?: string | null;
  description?: string | null;
}
