import type { AxiosAdapter } from "axios";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { apiClient } from "./client";

const original = window.location;
const originalAdapter = apiClient.defaults.adapter;

function respondWith(status: number) {
  const adapter: AxiosAdapter = (config) =>
    Promise.reject({ config, response: { status, data: {} } });
  apiClient.defaults.adapter = adapter;
}

describe("apiClient 401 handling", () => {
  beforeEach(() => {
    localStorage.clear();
    Object.defineProperty(window, "location", {
      configurable: true,
      value: { href: "/current" },
    });
  });

  afterEach(() => {
    Object.defineProperty(window, "location", {
      configurable: true,
      value: original,
    });
    apiClient.defaults.adapter = originalAdapter;
  });

  it("given no session, when a request gets 401, then the caller receives the error and no redirect happens", async () => {
    respondWith(401);

    await expect(apiClient.post("/auth/sign-in", {})).rejects.toMatchObject({
      response: { status: 401 },
    });
    expect(window.location.href).toBe("/current");
  });

  it("given a stored session, when a request gets 401, then the session is cleared and the user is sent to login", async () => {
    localStorage.setItem("token", "expired");
    respondWith(401);

    await expect(apiClient.get("/anything")).rejects.toBeDefined();

    expect(localStorage.getItem("token")).toBeNull();
    expect(window.location.href).toBe("/login");
  });
});
