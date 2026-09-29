import { describe, expect, it } from "vitest";
import { messages } from "../preferences/messages";
import { describeApiError } from "./apiError";

const m = messages.en;

describe("describeApiError", () => {
  it("given no response at all, when describing it, then it says the server cannot be reached", () => {
    expect(describeApiError(new Error("Network Error"), m).message).toBe(
      m.common.networkError,
    );
  });

  it("given a 429 with Retry-After, when describing it, then it says how long to wait", () => {
    const failure = describeApiError(
      { response: { status: 429, headers: { "retry-after": "42" } } },
      m,
    );

    expect(failure.message).toBe(m.common.tooManyRequests(42));
  });

  it("given a 429 without Retry-After, when describing it, then it asks to wait a minute", () => {
    expect(
      describeApiError({ response: { status: 429, headers: {} } }, m).message,
    ).toBe(m.common.tooManyRequests(60));
  });

  it("given a server error, when describing it, then it blames the server, not the user", () => {
    expect(describeApiError({ response: { status: 503 } }, m).message).toBe(
      m.common.serverError,
    );
  });

  it("given a status the caller explains, when describing it, then the caller's message wins", () => {
    expect(
      describeApiError({ response: { status: 409 } }, m, { 409: "Taken." })
        .message,
    ).toBe("Taken.");
  });

  it("given field errors from the API, when describing them, then each is keyed by the form's field name", () => {
    const failure = describeApiError(
      {
        response: {
          status: 400,
          data: {
            errors: {
              AdminEmail: ["A valid email is required."],
              Password: ["Too short.", "Needs a digit."],
            },
          },
        },
      },
      m,
    );

    expect(failure.fieldErrors).toEqual({
      adminEmail: "A valid email is required.",
      password: "Too short.",
    });
    expect(failure.message).toBe(m.common.checkFields);
  });

  it("given anything else, when describing it, then it falls back to the generic message", () => {
    expect(describeApiError({ response: { status: 418 } }, m).message).toBe(
      m.common.genericError,
    );
  });
});
