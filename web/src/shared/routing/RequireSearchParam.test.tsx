import { render, screen } from "@testing-library/react";
import { MemoryRouter, Routes, Route } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { RequireSearchParam } from "./RequireSearchParam";

function renderGuardedRoute(url: string) {
  return render(
    <MemoryRouter initialEntries={[url]}>
      <Routes>
        <Route path="/" element={<p>Landing</p>} />
        <Route path="/register" element={<p>Plans</p>} />
        <Route
          path="/register/organization"
          element={
            <RequireSearchParam name="planId" redirectTo="/register">
              <p>Organization form</p>
            </RequireSearchParam>
          }
        />
        <Route
          path="/activate"
          element={
            <RequireSearchParam name="token">
              <p>Protected content</p>
            </RequireSearchParam>
          }
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe("RequireSearchParam", () => {
  it("given the param is missing, when rendering a guarded route, then it redirects to the landing page", () => {
    renderGuardedRoute("/activate");

    expect(screen.getByText("Landing")).toBeInTheDocument();
    expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
  });

  it("given a redirect target and the param is missing, when rendering a guarded route, then it redirects there", () => {
    renderGuardedRoute("/register/organization");

    expect(screen.getByText("Plans")).toBeInTheDocument();
  });

  it("given the param is present, when rendering a guarded route, then it renders the content", () => {
    renderGuardedRoute("/activate?token=abc123");

    expect(screen.getByText("Protected content")).toBeInTheDocument();
  });
});
