export interface AdminUserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: string[];
  isDisabled: boolean;
  isLockedOut: boolean;
  createdAt: string;
}
