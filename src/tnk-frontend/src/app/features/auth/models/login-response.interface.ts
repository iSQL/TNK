export interface LoginResponse {
  token: string;
  userId: string;
  email: string;
  roles: string[]; // List<string> in C# translates to string[] in TS
}
