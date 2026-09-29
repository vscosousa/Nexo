import type { AxiosAdapter, InternalAxiosRequestConfig } from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { apiClient, resetCsrfToken, setUnauthorizedHandler } from "./client";

const originalAdapter = apiClient.defaults.adapter;
let sent: InternalAxiosRequestConfig[];

/** Answers `/auth/csrf` with a numbered token and every other request with `status`. */
function respondWith(status: number) {
  let issued = 0;
  const adapter: AxiosAdapter = (config) => {
    sent.push(config);
    if (config.url === "/auth/csrf") {
      issued += 1;
      return Promise.resolve({
        config,
        status: 200,
        statusText: "OK",
        headers: {},
        data: { token: `csrf-${issued}` },
      });
    }
    return status < 400
      ? Promise.resolve({
          config,
          status,
          statusText: "",
          headers: {},
          data: {},
        })
      : Promise.reject({ config, response: { status, data: {} } });
  };
  apiClient.defaults.adapter = adapter;
}

const changes = () => sent.filter((c) => c.url !== "/auth/csrf");

describe("apiClient", () => {
  beforeEach(() => {
    sent = [];
    resetCsrfToken();
  });

  afterEach(() => {
    apiClient.defaults.adapter = originalAdapter;
    setUnauthorizedHandler(() => {});
  });

  it("sends the session cookie with every request", () => {
    expect(apiClient.defaults.withCredentials).toBe(true);
  });

  it("given a change, when it is sent, then it carries the anti-forgery token fetched once", async () => {
    respondWith(204);

    await apiClient.post("/one");
    await apiClient.delete("/two");

    expect(changes().map((c) => c.headers["X-XSRF-TOKEN"])).toEqual([
      "csrf-1",
      "csrf-1",
    ]);
    expect(sent.filter((c) => c.url === "/auth/csrf")).toHaveLength(1);
  });

  it("given a read, when it is sent, then no anti-forgery token is fetched", async () => {
    respondWith(200);

    await apiClient.get("/plans");

    expect(sent.map((c) => c.url)).toEqual(["/plans"]);
  });

  it("given the token was reset, when the next change is sent, then it fetches a new one", async () => {
    respondWith(204);
    await apiClient.post("/one");

    resetCsrfToken();
    await apiClient.post("/two");

    expect(changes()[1].headers["X-XSRF-TOKEN"]).toBe("csrf-2");
  });

  it("given a 401, when the error reaches the caller, then the unauthorized handler has run", async () => {
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);
    respondWith(401);

    await expect(apiClient.get("/anything")).rejects.toMatchObject({
      response: { status: 401 },
    });
    expect(onUnauthorized).toHaveBeenCalledOnce();
  });
});
