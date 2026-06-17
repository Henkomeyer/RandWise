import { fireEvent, render, screen } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Transaction } from "./api/client";
import { AuthProvider } from "./auth/AuthContext";
import { DashboardPage } from "./features/dashboard/DashboardPage";
import { BudgetPage } from "./features/budget/BudgetPage";
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
          { path: "budget", element: <BudgetPage /> },
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
    expect(screen.getAllByText("R2,840").length).toBeGreaterThan(0);
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

  it("renders the budget workspace with targets and category groups", async () => {
    mockAuthenticatedSession();
    mockBudgetWorkspace();

    renderShell("/budget");

    expect(await screen.findByRole("heading", { name: "Budget" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /category groups/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /recurring commitments/i })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /saving targets/i })).toBeInTheDocument();
    expect(screen.getByText(/weekly buffer/i)).toBeInTheDocument();
  });

  it("renders live dashboard breakdown and hides merchant details in privacy mode", async () => {
    mockAuthenticatedSession();
    mockDashboardWorkspace();

    renderShell("/dashboard");

    expect(await screen.findByText("Live plan")).toBeInTheDocument();
    expect(screen.getByText("How this is calculated")).toBeInTheDocument();
    expect(screen.getByText("Available cash")).toBeInTheDocument();
    expect(screen.getByText(/Shoprite/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /turn privacy mode on/i }));

    expect(screen.getAllByLabelText("Amount hidden").length).toBeGreaterThan(0);
    expect(screen.queryByText(/Shoprite/)).not.toBeInTheDocument();
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

    expect(screen.getAllByText("R2,840").length).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole("button", { name: /turn privacy mode on/i }));

    expect(screen.queryByText("R2,840")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: /turn privacy mode off/i })).toHaveAttribute(
      "aria-pressed",
      "true"
    );
    expect(screen.getAllByLabelText("Amount hidden").length).toBeGreaterThan(0);
  });

  it("toggles the persisted dark theme", () => {
    renderShell();

    fireEvent.click(screen.getByRole("button", { name: /switch to dark mode/i }));

    expect(document.documentElement).toHaveClass("dark");
    expect(screen.getByRole("button", { name: /switch to light mode/i })).toHaveAttribute(
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

function mockBudgetWorkspace() {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (input) => {
    const url = input.toString();

    if (url.endsWith("/categories")) {
      return jsonResponse([
        {
          id: "category-1",
          name: "Groceries",
          slug: "groceries",
          categoryType: "expense",
          icon: "basket",
          sortOrder: 10,
          isSystem: false,
          isActive: true,
          createdUtc: "2026-06-17T10:00:00Z",
          updatedUtc: "2026-06-17T10:00:00Z"
        }
      ]);
    }

    if (url.endsWith("/budget-periods")) {
      return jsonResponse([
        {
          id: "period-1",
          startDate: "2026-06-01",
          endDate: "2026-06-30",
          expectedIncomeCents: 2500000,
          actualIncomeCents: 0,
          openingBalanceCents: 100000,
          status: "open",
          daysRemaining: 14,
          createdUtc: "2026-06-17T10:00:00Z",
          updatedUtc: "2026-06-17T10:00:00Z"
        }
      ]);
    }

    if (url.endsWith("/recurring-transactions")) {
      return jsonResponse([]);
    }

    if (url.endsWith("/budget-periods/period-1/category-budgets")) {
      return jsonResponse([
        {
          id: "budget-1",
          budgetPeriodId: "period-1",
          categoryId: "category-1",
          categoryName: "Groceries",
          allocatedAmountCents: 500000,
          rolloverAmountCents: 0,
          warningThresholdPercent: 80,
          spentAmountCents: 125000,
          createdUtc: "2026-06-17T10:00:00Z",
          updatedUtc: "2026-06-17T10:00:00Z"
        }
      ]);
    }

    if (url.endsWith("/dashboard/safe-to-spend")) {
      return jsonResponse({
        budgetPeriodId: "period-1",
        availableCashInCents: 2600000,
        protectedAmountInCents: 350000,
        safetyBufferInCents: 50000,
        savingsCommitmentInCents: 250000,
        upcomingCommitmentsInCents: 50000,
        remainingCategoryBudgetInCents: 375000,
        amountInCents: 375000,
        dailyAmountInCents: 26785,
        daysRemaining: 14
      });
    }

    return jsonResponse({ title: "Unexpected request" }, 500);
  });
}

function mockDashboardWorkspace() {
  vi.spyOn(globalThis, "fetch").mockImplementation(async (input) => {
    const url = input.toString();

    if (url.endsWith("/dashboard")) {
      return jsonResponse({
        generatedUtc: "2026-06-17T10:00:00Z",
        budgetPeriod: {
          id: "period-1",
          startDate: "2026-06-01",
          endDate: "2026-06-30",
          daysRemaining: 14,
          periodProgressPercent: 48
        },
        financialStatus: {
          status: "onTrack",
          message: "You are currently on track.",
          moneyPulse: 76
        },
        safeToSpend: {
          amountInCents: 375000,
          dailyAmountInCents: 26785,
          availableCashInCents: 2600000,
          protectedAmountInCents: 350000,
          safetyBufferInCents: 50000,
          savingsCommitmentInCents: 250000,
          upcomingCommitmentsInCents: 50000,
          remainingCategoryBudgetInCents: 375000
        },
        spending: {
          spentThisPeriodInCents: 125000,
          spendingPercent: 25,
          expectedSpendingPercent: 48
        },
        recommendedAction: {
          type: "onTrack",
          title: "Keep the current pace",
          message: "Safe-to-spend is positive and spending is aligned."
        },
        categories: [
          {
            categoryId: "category-1",
            name: "Groceries",
            allocatedInCents: 500000,
            spentInCents: 125000,
            remainingInCents: 375000,
            spendingPercent: 25,
            status: "onTrack",
            latestTransaction: "Weekly groceries"
          }
        ],
        upcomingCommitments: [
          {
            id: "commitment-1",
            description: "Internet",
            dueDate: "2026-06-21",
            amountInCents: 50000,
            isProtected: true,
            status: "protected"
          }
        ],
        recentTransactions: [
          {
            id: "transaction-1",
            description: "Groceries",
            merchant: "Shoprite",
            categoryName: "Groceries",
            transactionDate: "2026-06-17",
            amountInCents: 125000,
            transactionType: "expense",
            source: "web",
            status: "confirmed"
          }
        ],
        cashFlowForecast: [
          {
            date: "2026-06-17",
            projectedBalanceInCents: 2600000,
            commitmentAmountInCents: 0,
            isPayday: false
          }
        ],
        insights: [
          {
            type: "positive",
            title: "Budget is stable",
            message: "Safe-to-spend and category pacing are workable."
          }
        ]
      });
    }

    return jsonResponse({ title: "Unexpected request" }, 500);
  });
}
