import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { AuthProvider } from "./AuthContext";
import { LoginPage } from "./LoginPage";

function renderLogin() {
  return render(
    <MemoryRouter initialEntries={["/login"]}>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<p>Landing</p>} />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe("AuthLayout", () => {
  it("when the Back button is pressed with no earlier page, then it goes to the landing page", async () => {
    renderLogin();

    await userEvent.click(screen.getByRole("button", { name: "Back" }));

    expect(screen.getByText("Landing")).toBeInTheDocument();
  });

  it("when the page opens, then the logo is artwork and not a link home", () => {
    renderLogin();

    expect(
      screen.queryByRole("link", { name: "Nexo" }),
    ).not.toBeInTheDocument();
    expect(screen.getAllByRole("img", { name: "Nexo" }).length).toBeGreaterThan(
      0,
    );
  });
});
