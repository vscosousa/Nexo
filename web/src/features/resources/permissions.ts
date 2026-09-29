/** Whether an account with this role may register resources: `Admin` or `Staff` (US-004). */
export const canManageResources = (role?: string) =>
  role === "Admin" || role === "Staff";
