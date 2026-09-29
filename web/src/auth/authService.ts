import { apiClient } from "../shared/http/client";

/** What Google reported for a registration or activation still to be confirmed on the form. */
export interface PendingExternal {
  intent: "register" | "activate";
  email: string;
  firstName: string | null;
  lastName: string | null;
  organizationName: string | null;
}

export interface RegisterOrganizationExternal {
  organizationName: string;
  adminFirstName: string;
  adminLastName: string;
  planId: string;
}

/** The signed-in account, as `GET /auth/me` reports it. */
export interface CurrentAccount {
  id: string;
  organizationId: string;
  role: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
}

export interface RegisterOrganization {
  organizationName: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  password: string;
  /** The plan picked on the plans page, from `GET /plans`. */
  planId: string;
}

export interface PlanDto {
  id: string;
  name: string;
  memberLimit: number;
  resourceLimit: number;
  /** Monthly price in euros, or null for a custom/"contact us" plan. */
  monthlyPrice: number | null;
  hasIncidentTracking: boolean;
  hasExpenseTracking: boolean;
  hasDecisionHistory: boolean;
  hasAiInsights: boolean;
  hasPrioritySupport: boolean;
}

/** A `memberLimit` or `resourceLimit` value of this size means the plan applies no cap. */
export const UNLIMITED = 2147483647;

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
   * Registers an organization together with its admin account, which must confirm its email (the API emails a link)
   * before signing in. Resolves the same way when the email already has an account, so the form cannot reveal that.
   *
   * @throws The Axios error; 400 carries per-field messages, 409 means the same email was registered at the same moment.
   */
  async register(dto: RegisterOrganization): Promise<void> {
    await apiClient.post("/organizations", dto);
  },

  /**
   * Signs in with email and password; the API sets the httpOnly session cookie.
   *
   * @throws The Axios error; a 401 means the credentials were not accepted.
   */
  async signIn(email: string, password: string): Promise<void> {
    await apiClient.post("/auth/sign-in", { email, password });
  },

  /**
   * Reads who the session cookie belongs to.
   *
   * @returns The account, or `null` when there is no valid session.
   * @throws The Axios error for anything other than a 401.
   */
  async currentAccount(): Promise<CurrentAccount | null> {
    try {
      const { data } = await apiClient.get<CurrentAccount>("/auth/me");
      return data;
    } catch (e) {
      if ((e as { response?: { status?: number } }).response?.status === 401)
        return null;
      throw e;
    }
  },

  /** Ends the session; the API clears the cookie. */
  async signOut(): Promise<void> {
    await apiClient.post("/auth/sign-out");
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

  /**
   * Confirms a newly registered admin's email with the token from the emailed link, so the admin can sign in.
   *
   * @throws The Axios error; 403 means the link is not valid or has expired.
   */
  async confirmEmail(email: string, token: string): Promise<void> {
    await apiClient.post("/accounts/activation/confirm-email", {
      email,
      token,
    });
  },

  /**
   * Unlocks an account locked by too many wrong passwords, with the token from the emailed unlock link.
   *
   * @throws The Axios error; 403 means the link is not valid (for example, an older email's link).
   */
  async unlockAccount(email: string, token: string): Promise<void> {
    await apiClient.post("/auth/unlock", { email, token });
  },

  /**
   * Reads the Google details waiting to be confirmed after returning from Google.
   *
   * @throws The Axios error; a 401 means nothing is pending or it expired.
   */
  async pendingExternal(): Promise<PendingExternal> {
    const { data } = await apiClient.get<PendingExternal>(
      "/auth/external/pending",
    );
    return data;
  },

  /**
   * Registers an organization whose admin signs in with the pending Google account; the API sets the session cookie.
   *
   * @throws The Axios error; 400 carries per-field messages, 401 means the Google details expired, 409 the email has an account.
   */
  async registerExternal(dto: RegisterOrganizationExternal): Promise<void> {
    await apiClient.post("/auth/external/register", dto);
  },

  /**
   * Activates the invited account with the pending Google account; the API sets the session cookie.
   *
   * @throws The Axios error; 401 means the Google details expired, 403 the invitation or Google email does not match, 409 a conflict.
   */
  async activateExternal(dto: {
    firstName: string;
    lastName: string;
  }): Promise<void> {
    await apiClient.post("/auth/external/activate", dto);
  },

  /** Lists the plans an organization can register on. */
  async listPlans(): Promise<PlanDto[]> {
    const { data } = await apiClient.get<PlanDto[]>("/plans");
    return data;
  },
};
