const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5241/api/v1";
const IS_DEMO_MODE = import.meta.env.VITE_DEMO_MODE === "true";
const demoTransactionsKey = "randwise.demo.transactions";

export type ApiTokens = {
  accessToken: string;
  accessTokenExpiresUtc: string;
  refreshToken: string;
  refreshTokenExpiresUtc: string;
  tokenType: string;
};

export type MeResponse = {
  email: string;
  displayName: string;
  preferredCurrency: string;
  timeZone: string;
  preferredLanguage: string;
};

export type FinancialProfile = {
  id: string;
  defaultMonthlyIncomeCents: number;
  paydayDay: number | null;
  budgetCycleType: string;
  startingBalanceCents: number;
  safetyBufferCents: number;
  savingsCommitmentCents: number;
  notificationMode: string;
  firstDayOfWeek: string;
  createdUtc: string;
  updatedUtc: string;
};

export type Transaction = {
  id: string;
  amountInCents: number;
  transactionType: "expense" | "income";
  categoryId: string;
  description: string;
  merchant: string | null;
  transactionDate: string;
  source: string;
  status: string;
  notes: string | null;
  createdUtc: string;
  updatedUtc: string;
  deletedUtc: string | null;
};

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type CreateTransactionInput = {
  amountInCents: number;
  transactionType: "expense" | "income";
  categoryId: string | null;
  description: string;
  merchant: string | null;
  transactionDate: string;
  source: "web";
};

export type UpdateTransactionInput = {
  amountInCents: number;
  transactionType: "expense" | "income";
  categoryId: string;
  description: string;
  merchant: string | null;
  transactionDate: string;
  notes: string | null;
};

type RequestOptions = RequestInit & {
  accessToken?: string | null;
};

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string
  ) {
    super(message);
  }
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}) {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (options.accessToken) {
    headers.set("Authorization", `Bearer ${options.accessToken}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => undefined);
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? "Request failed.");
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  register: (body: { email: string; password: string; displayName: string }) =>
    IS_DEMO_MODE
      ? demoApi.register(body)
      : apiRequest<ApiTokens>("/auth/register", { method: "POST", body: JSON.stringify(body) }),
  login: (body: { email: string; password: string }) =>
    IS_DEMO_MODE
      ? demoApi.login(body)
      : apiRequest<ApiTokens>("/auth/login", { method: "POST", body: JSON.stringify(body) }),
  me: (accessToken: string) =>
    IS_DEMO_MODE ? demoApi.me(accessToken) : apiRequest<MeResponse>("/auth/me", { accessToken }),
  logout: (refreshToken: string, accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.logout(refreshToken, accessToken)
      : apiRequest<void>("/auth/logout", {
          method: "POST",
          accessToken,
          body: JSON.stringify({ refreshToken })
        }),
  getFinancialProfile: (accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.getFinancialProfile(accessToken)
      : apiRequest<FinancialProfile>("/financial-profile", { accessToken }),
  updateFinancialProfile: (
    accessToken: string,
    body: Omit<FinancialProfile, "id" | "createdUtc" | "updatedUtc">
  ) =>
    IS_DEMO_MODE
      ? demoApi.updateFinancialProfile(accessToken, body)
      : apiRequest<FinancialProfile>("/financial-profile", {
          method: "PUT",
          accessToken,
          body: JSON.stringify(body)
        }),
  listTransactions: (accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.listTransactions(accessToken)
      : apiRequest<PagedResponse<Transaction>>("/transactions?page=1&pageSize=20", {
          accessToken
        }),
  createTransaction: (
    accessToken: string,
    body: CreateTransactionInput
  ) =>
    IS_DEMO_MODE
      ? demoApi.createTransaction(accessToken, body)
      : apiRequest<Transaction>("/transactions", {
          method: "POST",
          accessToken,
          body: JSON.stringify(body)
        }),
  updateTransaction: (accessToken: string, id: string, body: UpdateTransactionInput) =>
    IS_DEMO_MODE
      ? demoApi.updateTransaction(accessToken, id, body)
      : apiRequest<Transaction>(`/transactions/${id}`, {
          method: "PUT",
          accessToken,
          body: JSON.stringify(body)
        }),
  deleteTransaction: (accessToken: string, id: string) =>
    IS_DEMO_MODE
      ? demoApi.deleteTransaction(accessToken, id)
      : apiRequest<void>(`/transactions/${id}`, {
          method: "DELETE",
          accessToken
        }),
  restoreTransaction: (accessToken: string, id: string) =>
    IS_DEMO_MODE
      ? demoApi.restoreTransaction(accessToken, id)
      : apiRequest<Transaction>(`/transactions/${id}/restore`, {
          method: "POST",
          accessToken
        })
};

const demoApi = {
  async register(body: { email: string; password: string; displayName: string }) {
    storeDemoUser({ email: body.email, displayName: body.displayName });
    return createDemoTokens();
  },
  async login(body: { email: string; password: string }) {
    storeDemoUser({ email: body.email, displayName: body.email.split("@")[0] ?? "Demo User" });
    return createDemoTokens();
  },
  async me(accessToken: string): Promise<MeResponse> {
    void accessToken;
    const user = readDemoUser();
    return {
      email: user.email,
      displayName: user.displayName,
      preferredCurrency: "ZAR",
      timeZone: "Africa/Johannesburg",
      preferredLanguage: "en-ZA"
    };
  },
  async logout(refreshToken: string, accessToken: string) {
    void refreshToken;
    void accessToken;
    return undefined;
  },
  async getFinancialProfile(accessToken: string): Promise<FinancialProfile> {
    void accessToken;
    return createDemoFinancialProfile();
  },
  async updateFinancialProfile(
    accessToken: string,
    body: Omit<FinancialProfile, "id" | "createdUtc" | "updatedUtc">
  ): Promise<FinancialProfile> {
    void accessToken;
    return {
      ...body,
      id: "demo-profile",
      createdUtc: new Date().toISOString(),
      updatedUtc: new Date().toISOString()
    };
  },
  async listTransactions(accessToken: string): Promise<PagedResponse<Transaction>> {
    void accessToken;
    const items = readDemoTransactions().filter((transaction) => transaction.deletedUtc === null);
    return {
      items,
      page: 1,
      pageSize: 20,
      totalCount: items.length,
      totalPages: 1
    };
  },
  async createTransaction(
    accessToken: string,
    body: CreateTransactionInput
  ): Promise<Transaction> {
    void accessToken;
    const now = new Date().toISOString();
    const transaction: Transaction = {
      ...body,
      id: createId(),
      categoryId: body.categoryId ?? "demo-category",
      status: "posted",
      notes: null,
      createdUtc: now,
      updatedUtc: now,
      deletedUtc: null
    };
    const next = [transaction, ...readDemoTransactions()].slice(0, 20);
    window.localStorage.setItem(demoTransactionsKey, JSON.stringify(next));
    return transaction;
  },
  async updateTransaction(
    accessToken: string,
    id: string,
    body: UpdateTransactionInput
  ): Promise<Transaction> {
    void accessToken;
    const transactions = readDemoTransactions();
    const existing = transactions.find((transaction) => transaction.id === id);

    if (!existing) {
      throw new ApiError(404, "Transaction was not found.");
    }

    const updated: Transaction = {
      ...existing,
      ...body,
      updatedUtc: new Date().toISOString()
    };
    writeDemoTransactions(
      transactions.map((transaction) => (transaction.id === id ? updated : transaction))
    );
    return updated;
  },
  async deleteTransaction(accessToken: string, id: string): Promise<void> {
    void accessToken;
    const transactions = readDemoTransactions();
    const deletedUtc = new Date().toISOString();
    writeDemoTransactions(
      transactions.map((transaction) =>
        transaction.id === id
          ? { ...transaction, deletedUtc, updatedUtc: deletedUtc }
          : transaction
      )
    );
  },
  async restoreTransaction(accessToken: string, id: string): Promise<Transaction> {
    void accessToken;
    const transactions = readDemoTransactions();
    const existing = transactions.find((transaction) => transaction.id === id);

    if (!existing) {
      throw new ApiError(404, "Transaction was not found.");
    }

    const restored: Transaction = {
      ...existing,
      deletedUtc: null,
      updatedUtc: new Date().toISOString()
    };
    writeDemoTransactions(
      transactions.map((transaction) => (transaction.id === id ? restored : transaction))
    );
    return restored;
  }
};

function createDemoTokens(): ApiTokens {
  const now = Date.now();
  return {
    accessToken: `demo-access-${now}`,
    accessTokenExpiresUtc: new Date(now + 60 * 60 * 1000).toISOString(),
    refreshToken: `demo-refresh-${now}`,
    refreshTokenExpiresUtc: new Date(now + 7 * 24 * 60 * 60 * 1000).toISOString(),
    tokenType: "Bearer"
  };
}

function createDemoFinancialProfile(): FinancialProfile {
  const now = new Date().toISOString();
  return {
    id: "demo-profile",
    defaultMonthlyIncomeCents: 2875000,
    paydayDay: 25,
    budgetCycleType: "monthly",
    startingBalanceCents: 610000,
    safetyBufferCents: 50000,
    savingsCommitmentCents: 220000,
    notificationMode: "email",
    firstDayOfWeek: "monday",
    createdUtc: now,
    updatedUtc: now
  };
}

function readDemoTransactions(): Transaction[] {
  const fallback: Transaction[] = [
    {
      id: "demo-coffee",
      amountInCents: 4500,
      transactionType: "expense",
      categoryId: "demo-category",
      description: "Coffee QA",
      merchant: "Cafe Test",
      transactionDate: "2026-06-14",
      source: "web",
      status: "posted",
      notes: null,
      createdUtc: "2026-06-14T10:00:00Z",
      updatedUtc: "2026-06-14T10:00:00Z",
      deletedUtc: null
    }
  ];

  try {
    const raw = window.localStorage.getItem(demoTransactionsKey);
    return raw ? (JSON.parse(raw) as Transaction[]) : fallback;
  } catch {
    return fallback;
  }
}

function writeDemoTransactions(transactions: Transaction[]) {
  window.localStorage.setItem(demoTransactionsKey, JSON.stringify(transactions));
}

function readDemoUser() {
  try {
    const raw = window.localStorage.getItem("randwise.demo.user");
    return raw
      ? (JSON.parse(raw) as { email: string; displayName: string })
      : { email: "demo@randwise.local", displayName: "Demo User" };
  } catch {
    return { email: "demo@randwise.local", displayName: "Demo User" };
  }
}

function storeDemoUser(user: { email: string; displayName: string }) {
  window.localStorage.setItem("randwise.demo.user", JSON.stringify(user));
}

function createId() {
  return typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `demo-${Date.now()}`;
}
