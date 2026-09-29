import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Notice } from "./Notice";

describe("Notice", () => {
  it("given an error, when rendered, then it is announced as an alert with a decorative icon", () => {
    const { container } = render(
      <Notice tone="error">Wrong email or password.</Notice>,
    );

    expect(screen.getByRole("alert")).toHaveTextContent(
      "Wrong email or password.",
    );
    expect(container.querySelector("svg")).toHaveAttribute(
      "aria-hidden",
      "true",
    );
  });

  it("given a success, when rendered, then it is announced politely as a status", () => {
    render(<Notice tone="success">Email confirmed.</Notice>);

    expect(screen.getByRole("status")).toHaveTextContent("Email confirmed.");
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
