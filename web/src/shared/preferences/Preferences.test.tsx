import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { LandingPage } from "../../landing/LandingPage";
import { PreferencesProvider } from "./Preferences";

function renderLanding() {
  render(
    <MemoryRouter>
      <PreferencesProvider>
        <LandingPage />
      </PreferencesProvider>
    </MemoryRouter>,
  );
}

describe("Preferences", () => {
  beforeEach(() => localStorage.clear());

  it("when the language is switched to Portuguese, then the page copy and <html lang> change and the choice is saved", async () => {
    renderLanding();

    await userEvent.click(screen.getByRole("button", { name: "PT" }));

    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent(
      "Reservas, equipamento e voluntários, sem confusão",
    );
    expect(document.documentElement.lang).toBe("pt");
    expect(localStorage.getItem("lang")).toBe("pt");
  });
});
