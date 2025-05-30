import { UserRole } from './user-role.enum'; 

export interface CurrentUserModel {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole | null;
}
