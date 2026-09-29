import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Link, MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ScrollToTop } from "./ScrollToTop";

function renderRoutes() {
  return render(
    <MemoryRouter initialEntries={["/register"]}>
      <ScrollToTop />
      <Routes>
        <Route
          path="/register"
          element={<Link to="/register/organization">Continue</Link>}
        />
        <Route path="/register/organization" element={<p>Form</p>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("ScrollToTop", () => {
  afterEach(() => vi.restoreAllMocks());

  it("given a scrolled page, when navigating to another page, then the new page starts at the top", async () => {
    const scrollTo = vi.spyOn(window, "scrollTo").mockImplementation(() => {});
    renderRoutes();
    scrollTo.mockClear();

    await userEvent.setup().click(screen.getByText("Continue"));

    expect(await screen.findByText("Form")).toBeInTheDocument();
    expect(scrollTo).toHaveBeenCalledWith(0, 0);
  });
});
