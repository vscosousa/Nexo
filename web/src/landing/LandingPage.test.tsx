import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { LandingPage } from "./LandingPage";

describe("LandingPage", () => {
  it("when the page opens, then it links to sign-in and registration", () => {
    render(
      <MemoryRouter>
        <LandingPage />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Sign in" })).toHaveAttribute(
      "href",
      "/login",
    );
    for (const link of screen.getAllByRole("link", { name: "Get started" })) {
      expect(link).toHaveAttribute("href", "/register");
    }
    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent(
      "Bookings, equipment and volunteers, without the chaos",
    );
  });
});
