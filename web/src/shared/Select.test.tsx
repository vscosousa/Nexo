import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Select } from "./Select";

const OPTIONS = [
  { value: "e", label: "Equipment" },
  { value: "u", label: "Utensil" },
  { value: "v", label: "Vehicle" },
];

function renderSelect(props: Partial<Parameters<typeof Select>[0]> = {}) {
  const onFormKeyDown = vi.fn();
  render(
    <form aria-label="form" onKeyDown={onFormKeyDown}>
      <Select
        label="Type"
        name="typeId"
        placeholder="Choose a type"
        options={OPTIONS}
        {...props}
      />
      <button type="button">After</button>
    </form>,
  );
  return {
    onFormKeyDown,
    combobox: screen.getByRole("combobox", { name: "Type" }),
  };
}

const value = () =>
  (document.querySelector('input[name="typeId"]') as HTMLInputElement).value;

describe("Select", () => {
  it("given the placeholder, when clicking the box and an option, then it shows and submits that option", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    expect(combobox).toHaveTextContent("Choose a type");
    expect(combobox).toHaveAttribute("aria-expanded", "false");

    await user.click(combobox);
    expect(combobox).toHaveAttribute("aria-expanded", "true");
    await user.click(screen.getByRole("option", { name: "Utensil" }));

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(combobox).toHaveTextContent("Utensil");
    expect(value()).toBe("u");
    expect(combobox).toHaveFocus();
  });

  it("given the keyboard, when using the arrows and Enter, then it moves the active option and picks it", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    combobox.focus();

    await user.keyboard("{ArrowDown}");
    const listbox = screen.getByRole("listbox");
    expect(combobox).toHaveAttribute(
      "aria-activedescendant",
      screen.getByRole("option", { name: "Equipment" }).id,
    );
    await user.keyboard("{ArrowDown}{ArrowDown}{ArrowDown}{Enter}");

    expect(listbox).not.toBeInTheDocument();
    expect(value()).toBe("v");
  });

  it("given an open list, when pressing Escape, then it closes without choosing and the key does not reach the dialog", async () => {
    const user = userEvent.setup();
    const { combobox, onFormKeyDown } = renderSelect();
    await user.click(combobox);

    await user.keyboard("{ArrowDown}{Escape}");

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(value()).toBe("");
    expect(onFormKeyDown).not.toHaveBeenCalledWith(
      expect.objectContaining({ key: "Escape" }),
    );
  });

  it("given a closed list, when typing a letter, then it picks the first option starting with it", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    combobox.focus();

    await user.keyboard("v");

    expect(value()).toBe("v");
  });

  it("given an open list, when tabbing away, then it picks the active option and moves on", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    combobox.focus();

    await user.keyboard("{ArrowDown}{ArrowDown}{Tab}");

    expect(value()).toBe("u");
    expect(screen.getByRole("button", { name: "After" })).toHaveFocus();
  });

  it("given an open list, when clicking elsewhere, then it closes without choosing", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    await user.click(combobox);

    fireEvent.pointerDown(document.body);

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(value()).toBe("");
  });

  it("given an open list, when the list itself scrolls, then it stays open", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    await user.click(combobox);

    fireEvent.scroll(screen.getByRole("listbox"));

    expect(screen.getByRole("listbox")).toBeInTheDocument();
  });

  it("given an open list, when the page scrolls, then it closes", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    await user.click(combobox);

    fireEvent.scroll(document);

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });

  it("given an open list, when the window resizes, then it closes", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    await user.click(combobox);

    fireEvent(window, new Event("resize"));

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });

  it("given an error, when rendered, then the box is invalid and described by it", () => {
    const { combobox } = renderSelect({ error: "Choose a type." });

    expect(combobox).toHaveAttribute("aria-invalid", "true");
    expect(combobox).toHaveAccessibleDescription("Choose a type.");
  });

  it("given a selected option, when the list opens, then that option is marked selected", async () => {
    const user = userEvent.setup();
    const { combobox } = renderSelect();
    await user.click(combobox);
    await user.click(screen.getByRole("option", { name: "Vehicle" }));

    await user.click(combobox);

    expect(screen.getByRole("option", { name: "Vehicle" })).toHaveAttribute(
      "aria-selected",
      "true",
    );
  });
});
