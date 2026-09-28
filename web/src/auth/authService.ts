import { apiClient } from "../shared/http/client";

export interface Session {
  token: string;
  expiresAt: string;
}

export interface RegisterOrganization {
  organizationName: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  password: string;
}

export interface ActivateAccount {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  /** The link token, read from the activation page's URL. */
  linkToken: string;
  /** The code from the email body, typed in by the person. */
  code: string;
}

export const authService = {
  /**
   * Registers an organization together with its admin account.
   *
   * @throws The Axios error; 400 carries per-field messages, 409 means the email has an account.
   */
  async register(dto: RegisterOrganization): Promise<void> {
    await apiClient.post("/organizations", dto);
  },

  /**
   * Signs in with email and password.
   *
   * @returns The session issued by the API.
   * @throws The Axios error; a 401 means the credentials were not accepted.
   */
  async signIn(email: string, password: string): Promise<Session> {
    const { data } = await apiClient.post<Session>("/auth/sign-in", {
      email,
      password,
    });
    return data;
  },

  /**
   * Checks an email and invitation code without activating anything.
   *
   * @throws The Axios error; 403 means the email or code is not valid, 409 the account is already active.
   */
  async verifyInvitation(
    email: string,
    linkToken: string,
    code: string,
  ): Promise<void> {
    await apiClient.post("/accounts/activation/verify", {
      email,
      linkToken,
      code,
    });
  },

  /**
   * Activates an invited member account.
   *
   * @throws The Axios error; 403 means the email or code is not valid, 409 the account is already active or the plan is full.
   */
  async activate(dto: ActivateAccount): Promise<void> {
    await apiClient.post("/accounts/activation", dto);
  },

  /**
   * Asks for the invitation email to be re-sent with a fresh code. Always resolves; the API never reveals
   * whether the email had a pending invitation.
   */
  async resendInvitation(email: string): Promise<void> {
    await apiClient.post("/accounts/activation/resend", { email });
  },
};
