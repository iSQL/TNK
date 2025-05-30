export interface UserDetailsAdminDTO {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  emailConfirmed: boolean;
  roles: string[];
}