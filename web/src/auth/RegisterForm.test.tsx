import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AuthProvider } from "./AuthContext";
import { LoginPage } from "./LoginPage";
import { RegisterPage } from "./RegisterPage";
import { authService } from "./authService";

vi.mock("./authService");

function renderRegister() {
  return render(
    <MemoryRouter initialEntries={["/register"]}>
      <AuthProvider>
        <Routes>
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

async function fill(values: Record<string, string>) {
  const user = userEvent.setup();
  for (const [label, value] of Object.entries(values)) {
    if (value) await user.type(screen.getByLabelText(label), value);
  }
  await user.click(
    screen.getByRole("button", { name: "Register organization" }),
  );
}

const valid = {
  "Organization name": "Associação Horizonte",
  "First name": "Ana",
  "Last name": "Admin",
  Email: "ana@example.com",
  Password: "Secret-123",
};

describe("RegisterForm", () => {
  beforeEach(() => vi.resetAllMocks());

  it("given valid details, when submitting, then it registers and sends the admin to sign in", async () => {
    vi.mocked(authService.register).mockResolvedValue(undefined);
    renderRegister();

    await fill(valid);

    expect(authService.register).toHaveBeenCalledWith({
      organizationName: "Associação Horizonte",
      adminFirstName: "Ana",
      adminLastName: "Admin",
      adminEmail: "ana@example.com",
      password: "Secret-123",
    });
    expect(await screen.findByRole("status")).toHaveTextContent(
      "Organization registered",
    );
  });

  it("given an empty field, when submitting, then it asks for it without calling the API", async () => {
    renderRegister();

    await fill({ ...valid, Password: "" });

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Fill in every field.",
    );
    expect(authService.register).not.toHaveBeenCalled();
  });

  it("given the email already registered, when submitting, then it says so", async () => {
    vi.mocked(authService.register).mockRejectedValue({
      response: { status: 409 },
    });
    renderRegister();

    await fill(valid);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "already registered",
    );
  });

  it("given the API rejects a field, when submitting, then it shows the API's message", async () => {
    vi.mocked(authService.register).mockRejectedValue({
      response: {
        status: 400,
        data: { errors: { Password: ["Password is too short."] } },
      },
    });
    renderRegister();

    await fill(valid);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Password is too short.",
    );
  });
});
