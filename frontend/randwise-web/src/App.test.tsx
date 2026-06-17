import { fireEvent, render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Transaction } from "./api/client";
import { AuthProvider } from "./auth/AuthContext";
import { DashboardPage } from "./features/dashboard/DashboardPage";
import { PlaceholderRoute } from "./features/placeholder/PlaceholderRoute";
import { TransactionsPage } from "./features/transactions/TransactionsPage";
import { AppShell } from "./shell/AppShell";
import { ThemeModeProvider } from "./theme/ThemeModeContext";
import { formatRandCents } from "./ui/moneyFormat";

beforeEach(() => {
  window.localStorage.clear();
  document.documentElement.className = "";
  vi.restoreAllMocks();
});

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

  it("supports the transaction create edit delete and restore flow", async () => {
    const transactions: Transaction[] = [];
    mockAuthenticatedSession();
    vi.spyOn(globalThis, "fetch").mockImplementation(async (input, init) => {
      const url = input.toString();
      const method = init?.method ?? "GET";

      if (url.includes("/transactions?page=") && method === "GET") {
        return jsonResponse({
          items: transactions.filter((transaction) => transaction.deletedUtc === null),
          page: 1,
          pageSize: 20,
          totalCount: transactions.length,
          totalPages: 1
        });
      }

      if (url.endsWith("/transactions") && method === "POST") {
        const body = JSON.parse(init?.body?.toString() ?? "{}") as Partial<Transaction>;
        const created: Transaction = {
          id: "transaction-1",
          amountInCents: Number(body.amountInCents),
          transactionType: "expense",
          categoryId: "category-1",
          description: body.description ?? "",
          merchant: body.merchant ?? null,
          transactionDate: body.transactionDate ?? "2026-06-17",
          source: "web",
          status: "confirmed",
          notes: null,
          createdUtc: "2026-06-17T10:00:00Z",
          updatedUtc: "2026-06-17T10:00:00Z",
          deletedUtc: null
        };
        transactions.unshift(created);
        return jsonResponse(created, 201);
      }

      if (url.endsWith("/transactions/transaction-1") && method === "PUT") {
        const body = JSON.parse(init?.body?.toString() ?? "{}") as Partial<Transaction>;
        const updated: Transaction = {
          ...transactions[0],
          amountInCents: Number(body.amountInCents),
          description: body.description ?? transactions[0].description,
          merchant: body.merchant ?? null,
          transactionDate: body.transactionDate ?? transactions[0].transactionDate,
          notes: body.notes ?? null,
          updatedUtc: "2026-06-17T11:00:00Z"
        };
        transactions[0] = updated;
        return jsonResponse(updated);
      }

      if (url.endsWith("/transactions/transaction-1") && method === "DELETE") {
        transactions[0] = {
          ...transactions[0],
          deletedUtc: "2026-06-17T12:00:00Z"
        };
        return new Response(null, { status: 204 });
      }

      if (url.endsWith("/transactions/transaction-1/restore") && method === "POST") {
        transactions[0] = {
          ...transactions[0],
          deletedUtc: null
        };
        return jsonResponse(transactions[0]);
      }

      return jsonResponse({ title: "Unexpected request" }, 500);
    });

    renderShell("/transactions");

    fireEvent.change(screen.getByLabelText("Description"), {
      target: { value: "Coffee" }
    });
    fireEvent.change(screen.getByLabelText("Amount in cents"), {
      target: { value: "4500" }
    });
    fireEvent.change(screen.getByLabelText("Merchant"), {
      target: { value: "Cafe" }
    });
    fireEvent.click(screen.getByRole("button", { name: /add expense/i }));

    expect(await screen.findByText("Coffee")).toBeInTheDocument();
    expect(screen.getByText("-R45")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /edit/i }));
    fireEvent.change(screen.getByLabelText("Description"), {
      target: { value: "Coffee edited" }
    });
    fireEvent.change(screen.getByLabelText("Notes"), {
      target: { value: "Receipt checked" }
    });
    fireEvent.click(screen.getByRole("button", { name: /save changes/i }));

    expect(await screen.findByText("Coffee edited")).toBeInTheDocument();
    expect(screen.getByText("Receipt checked")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /delete/i }));

    expect(await screen.findByText(/deleted coffee edited/i)).toBeInTheDocument();
    expect(screen.queryByText("Coffee edited")).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /restore/i }));

    expect(await screen.findByText("Coffee edited")).toBeInTheDocument();
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

function mockAuthenticatedSession() {
  window.localStorage.setItem(
    "randwise.auth",
    JSON.stringify({
      tokens: {
        accessToken: "test-access-token",
        accessTokenExpiresUtc: "2026-06-17T12:00:00Z",
        refreshToken: "test-refresh-token",
        refreshTokenExpiresUtc: "2026-06-24T12:00:00Z",
        tokenType: "Bearer"
      },
      user: {
        email: "test@example.com",
        displayName: "Test User",
        preferredCurrency: "ZAR",
        timeZone: "Africa/Johannesburg",
        preferredLanguage: "en-ZA"
      }
    })
  );
}

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status,
      headers: { "Content-Type": "application/json" }
    })
  );
}
