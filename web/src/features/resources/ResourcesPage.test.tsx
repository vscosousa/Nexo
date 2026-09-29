import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAuth } from "../../auth/AuthContext";
import { ResourcesPage } from "./ResourcesPage";
import { resourcesService } from "./resourcesService";

vi.mock("./resourcesService");
vi.mock("../../auth/AuthContext");

const UTENSIL = { id: "utensil-id", name: "Utensil" };
const KAYAK = { id: "kayak-id", name: "Kayak" };
const OTHER = { id: "other-id", name: "Other" };

function renderAs(role: string, entry = "/app/resources") {
  vi.mocked(useAuth).mockReturnValue({
    status: "signedIn",
    account: {
      id: "a",
      organizationId: "o",
      role,
      email: "ana@example.com",
      firstName: "Ana",
      lastName: "Ribeiro",
    },
    login: vi.fn(),
    logout: vi.fn(),
  });
  return render(
    <MemoryRouter initialEntries={[entry]}>
      <ResourcesPage />
    </MemoryRouter>,
  );
}

async function openDialog() {
  const user = userEvent.setup();
  await user.click(screen.getByRole("button", { name: "Register resource" }));
  const dialog = await screen.findByRole("dialog", {
    name: "Register resource",
  });
  await waitFor(() =>
    expect(
      within(dialog).getByRole("button", { name: "Register" }),
    ).toBeEnabled(),
  );
  return { user, dialog };
}

async function pickType(
  user: ReturnType<typeof userEvent.setup>,
  dialog: HTMLElement,
  type: string,
) {
  await user.click(within(dialog).getByRole("combobox", { name: "Type" }));
  await user.click(within(dialog).getByRole("option", { name: type }));
}

async function fillAndSubmit(
  user: ReturnType<typeof userEvent.setup>,
  dialog: HTMLElement,
  type = "Utensil",
) {
  await user.type(within(dialog).getByLabelText("Name"), "Van");
  await pickType(user, dialog, type);
  await user.click(within(dialog).getByRole("button", { name: "Register" }));
}

describe("ResourcesPage", () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(resourcesService.listTypes).mockResolvedValue([
      OTHER,
      KAYAK,
      UTENSIL,
    ]);
  });

  it("given a staff account and valid details, when registering, then it sends them, closes the dialog and confirms", async () => {
    vi.mocked(resourcesService.register).mockResolvedValue({
      id: "r",
      name: "Paella pan",
      typeId: UTENSIL.id,
      typeName: "Utensil",
      description: "90 cm",
      status: "Available",
      organizationId: "o",
    });
    renderAs("Staff");
    const { user, dialog } = await openDialog();

    await user.type(within(dialog).getByLabelText("Name"), " Paella pan ");
    await pickType(user, dialog, "Utensil");
    await user.type(
      within(dialog).getByLabelText("Description (optional)"),
      "90 cm",
    );
    await user.click(within(dialog).getByRole("button", { name: "Register" }));

    expect(resourcesService.register).toHaveBeenCalledWith({
      name: "Paella pan",
      typeId: UTENSIL.id,
      description: "90 cm",
    });
    expect(await screen.findByRole("status")).toHaveTextContent(
      '"Paella pan" was registered and is available.',
    );
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("given the organization's types, when the type list opens, then they are sorted by name with Other last", async () => {
    renderAs("Admin");
    const { user, dialog } = await openDialog();

    const combobox = within(dialog).getByRole("combobox", { name: "Type" });
    expect(combobox).toHaveTextContent("Choose a type");
    await user.click(combobox);

    expect(
      within(dialog)
        .getAllByRole("option")
        .map((o) => o.textContent),
    ).toEqual(["Kayak", "Utensil", "Other"]);
  });

  it("given a member account, when opening the page, then there is no way to register and it says who can", () => {
    renderAs("Member");

    expect(
      screen.queryByRole("button", { name: "Register resource" }),
    ).not.toBeInTheDocument();
    expect(
      screen.getByText("Only the admin and staff can register resources."),
    ).toBeInTheDocument();
  });

  it("given missing details, when submitting, then it marks the fields and sends nothing", async () => {
    renderAs("Admin");
    const { user, dialog } = await openDialog();

    await user.click(within(dialog).getByRole("button", { name: "Register" }));

    expect(within(dialog).getByLabelText("Name")).toHaveAccessibleDescription(
      "Enter the resource's name.",
    );
    expect(within(dialog).getByLabelText("Type")).toHaveAccessibleDescription(
      "Choose a type.",
    );
    expect(resourcesService.register).not.toHaveBeenCalled();
  });

  it("given the API rejects a field, when submitting, then its message shows under that field", async () => {
    vi.mocked(resourcesService.register).mockRejectedValue({
      response: {
        status: 400,
        data: { errors: { TypeId: ["A known resource type is required."] } },
      },
    });
    renderAs("Admin");
    const { user, dialog } = await openDialog();

    await fillAndSubmit(user, dialog, "Kayak");

    expect(
      await within(dialog).findByText("A known resource type is required."),
    ).toBeInTheDocument();
    expect(within(dialog).getByLabelText("Type")).toHaveAccessibleDescription(
      "A known resource type is required.",
    );
  });

  it.each([
    [
      409,
      "Your organization has reached its plan's resource limit. Change plan to register more.",
    ],
    [403, "Your account can't register resources."],
  ])(
    "given the API answers %i, when submitting, then the dialog explains it",
    async (status, message) => {
      vi.mocked(resourcesService.register).mockRejectedValue({
        response: { status },
      });
      renderAs("Admin");
      const { user, dialog } = await openDialog();

      await fillAndSubmit(user, dialog);

      expect(await within(dialog).findByRole("alert")).toHaveTextContent(
        message,
      );
    },
  );

  it("given the types cannot load, when the dialog opens, then it says so", async () => {
    vi.mocked(resourcesService.listTypes).mockRejectedValue({
      response: { status: 500 },
    });
    renderAs("Admin");

    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: "Register resource" }));

    expect(
      await within(screen.getByRole("dialog")).findByRole("alert"),
    ).toHaveTextContent("Couldn't load the types. Close and try again.");
  });

  it("given the dashboard shortcut, when the page opens with ?new=1, then the dialog is already open", async () => {
    renderAs("Admin", "/app/resources?new=1");

    expect(
      await screen.findByRole("dialog", { name: "Register resource" }),
    ).toBeInTheDocument();
  });

  it("given the dialog is open, when cancelling, then it closes without registering", async () => {
    renderAs("Admin");
    const { user, dialog } = await openDialog();

    await user.click(within(dialog).getByRole("button", { name: "Cancel" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(resourcesService.register).not.toHaveBeenCalled();
  });
});
