import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { AuthProvider } from "./AuthContext";
import { LoginCallbackPage } from "./LoginCallbackPage";

function renderCallback() {
  return render(
    <MemoryRouter initialEntries={["/login/callback"]}>
      <AuthProvider>
        <Routes>
          <Route path="/login/callback" element={<LoginCallbackPage />} />
          <Route path="/login" element={<p>Login page</p>} />
          <Route path="/app" element={<p>Home</p>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe("LoginCallbackPage", () => {
  beforeEach(() => {
    localStorage.clear();
    window.history.replaceState(null, "", "/");
  });

  it("given a token in the URL fragment, when the page opens, then it signs in and removes the token from the address bar", () => {
    window.history.replaceState(null, "", "/login/callback#token=jwt-2");

    renderCallback();

    expect(localStorage.getItem("token")).toBe("jwt-2");
    expect(window.location.hash).toBe("");
    expect(screen.getByText("Home")).toBeInTheDocument();
  });

  it("given no token in the URL, when the page opens, then it goes back to the login page", () => {
    renderCallback();

    expect(localStorage.getItem("token")).toBeNull();
    expect(screen.getByText("Login page")).toBeInTheDocument();
  });
});
