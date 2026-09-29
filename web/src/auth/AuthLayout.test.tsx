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
  it("when the logo is clicked, then it goes to the landing page", async () => {
    renderLogin();

    await userEvent.click(screen.getAllByRole("link", { name: "Nexo" })[0]);

    expect(screen.getByText("Landing")).toBeInTheDocument();
  });
});
