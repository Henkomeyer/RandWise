import { fireEvent, render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { describe, expect, it } from "vitest";
import { AuthProvider } from "./auth/AuthContext";
import { DashboardPage } from "./features/dashboard/DashboardPage";
import { PlaceholderRoute } from "./features/placeholder/PlaceholderRoute";
import { TransactionsPage } from "./features/transactions/TransactionsPage";
import { AppShell } from "./shell/AppShell";
import { ThemeModeProvider } from "./theme/ThemeModeContext";
import { formatRandCents } from "./ui/moneyFormat";

function renderShell(initialPath = "/dashboard") {
  const router = createMemoryRouter(
    [
      {
        path: "/",
        element: <AppShell />,
        children: [
          { path: "dashboard", element: <DashboardPage /> },
          {
            path: "transactions",
            element: <TransactionsPage />
          },
          {
            path: "add",
            element: (
              <PlaceholderRoute
                title="Add"
                intent="Capture a new expense or income entry quickly."
              />
            )
          },
          {
            path: "budget",
            element: (
              <PlaceholderRoute
                title="Budget"
                intent="Set payday, buffers, category budgets and recurring commitments."
              />
            )
          },
          {
            path: "more",
            element: (
              <PlaceholderRoute
                title="More"
                intent="Manage account, WhatsApp linking, privacy, exports and support."
              />
            )
          }
        ]
      }
    ],
    { initialEntries: [initialPath] }
  );

  render(
    <ThemeModeProvider>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </ThemeModeProvider>
  );
}

describe("RandWise app shell", () => {
  it("renders the dashboard route with core budget answers", () => {
    renderShell();

    expect(
      screen.getByRole("heading", { name: /safe to spend/i })
    ).toBeInTheDocument();
    expect(screen.getByText("R2,840")).toBeInTheDocument();
    expect(screen.getAllByText(/10 days/i).length).toBeGreaterThan(0);
    expect(screen.getByText(/slow down on takeaways/i)).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /category groups/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /savings targets/i })).toBeInTheDocument();
    expect(screen.getByText(/june savings/i)).toBeInTheDocument();
  });

  it("provides accessible navigation and a skip link", () => {
    renderShell();

    expect(screen.getByText(/skip to main content/i)).toHaveAttribute(
      "href",
      "#main-content"
    );
    expect(
      screen.getByRole("navigation", { name: /primary navigation/i })
    ).toBeInTheDocument();
    expect(
      screen.getByRole("navigation", { name: /mobile navigation/i })
    ).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: /add/i })).toHaveLength(2);
  });

  it("navigates to the transaction workspace", async () => {
    renderShell();

    fireEvent.click(screen.getAllByRole("link", { name: /transactions/i })[0]);

    expect(
      await screen.findByRole("heading", { name: "Transactions" })
    ).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /quick add/i })).toBeInTheDocument();
  });

  it("formats negative money with the sign before the currency", () => {
    expect(formatRandCents(-4500)).toBe("-R45");
  });

  it("redacts visible dashboard money when privacy mode is enabled", () => {
    renderShell();

    expect(screen.getByText("R2,840")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /privacy off/i }));

    expect(screen.queryByText("R2,840")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /privacy on/i })).toHaveAttribute(
      "aria-pressed",
      "true"
    );
    expect(screen.getAllByLabelText("Amount hidden").length).toBeGreaterThan(0);
  });

  it("toggles the persisted dark theme", () => {
    renderShell();

    fireEvent.click(screen.getByRole("button", { name: /light/i }));

    expect(document.documentElement).toHaveClass("dark");
    expect(screen.getByRole("button", { name: /dark/i })).toHaveAttribute(
      "aria-pressed",
      "true"
    );
  });
});
