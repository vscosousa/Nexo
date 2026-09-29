import type { messages } from "../preferences/messages";

type Messages = (typeof messages)["pt"];

/** What went wrong with an API call, ready to show: a form-level message and any per-field messages. */
export interface ApiFailure {
  /** The HTTP status, or `undefined` when no response arrived (offline, server down, blocked). */
  status?: number;
  message: string;
  /** Field messages from a 400 validation response, keyed by the form's field name (`adminEmail`, `password`). */
  fieldErrors: Record<string, string>;
}

interface ErrorShape {
  response?: {
    status?: number;
    headers?: Record<string, string | undefined>;
    data?: { errors?: Record<string, string[]> };
  };
}

/**
 * Turns a failed API call into something the person can act on, so no form falls back to "something went
 * wrong" for a problem it could explain. `byStatus` gives the caller's meaning for statuses specific to its
 * endpoint (e.g. 409 "email taken"); the common cases are handled here for every form: no connection, rate
 * limiting (with the wait from `Retry-After`), server errors, and 400 field errors.
 *
 * @param error The rejection from an `apiClient` call.
 * @param m The current language's messages.
 * @param byStatus Messages for statuses this endpoint gives a specific meaning; they win over the defaults,
 *   except for 429 and 5xx, which mean the same everywhere.
 */
export function describeApiError(
  error: unknown,
  m: Messages,
  byStatus: Partial<Record<number, string>> = {},
): ApiFailure {
  const response = (error as ErrorShape | undefined)?.response;
  const status = response?.status;
  if (!response || status === undefined)
    return { message: m.common.networkError, fieldErrors: {} };
  if (status === 429) {
    const seconds = Number(response.headers?.["retry-after"]);
    return {
      status,
      message: m.common.tooManyRequests(seconds > 0 ? seconds : 60),
      fieldErrors: {},
    };
  }
  if (status >= 500)
    return { status, message: m.common.serverError, fieldErrors: {} };
  if (byStatus[status])
    return { status, message: byStatus[status], fieldErrors: {} };

  const fieldErrors = Object.fromEntries(
    Object.entries(response.data?.errors ?? {})
      .filter(([, list]) => list.length > 0)
      .map(([key, list]) => [
        key.charAt(0).toLowerCase() + key.slice(1),
        list[0],
      ]),
  );
  if (status === 400 && Object.keys(fieldErrors).length > 0)
    return { status, message: m.common.checkFields, fieldErrors };
  return { status, message: m.common.genericError, fieldErrors: {} };
}
