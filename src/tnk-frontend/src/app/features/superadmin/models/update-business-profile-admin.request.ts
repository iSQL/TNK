export interface UpdateBusinessProfileAdminRequest {
  // Note: The ID is typically passed as a route parameter, not in the body for PUT.
  // The body contains the fields to be updated.
  name: string;
  address?: string | null;
  phoneNumber?: string | null;
  description?: string | null;
}
