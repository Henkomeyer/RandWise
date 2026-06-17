const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5241/api/v1";
const IS_DEMO_MODE = import.meta.env.VITE_DEMO_MODE === "true";
const demoTransactionsKey = "randwise.demo.transactions";
const demoCategoriesKey = "randwise.demo.categories";
const demoBudgetPeriodsKey = "randwise.demo.budget-periods";
const demoCategoryBudgetsKey = "randwise.demo.category-budgets";
const demoRecurringKey = "randwise.demo.recurring";

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

export type Category = {
  id: string;
  name: string;
  slug: string;
  categoryType: "expense" | "income" | "savings";
  icon: string | null;
  sortOrder: number;
  isSystem: boolean;
  isActive: boolean;
  createdUtc: string;
  updatedUtc: string;
};

export type BudgetPeriod = {
  id: string;
  startDate: string;
  endDate: string;
  expectedIncomeCents: number;
  actualIncomeCents: number;
  openingBalanceCents: number;
  status: "open" | "closed";
  daysRemaining: number;
  createdUtc: string;
  updatedUtc: string;
};

export type CategoryBudget = {
  id: string;
  budgetPeriodId: string;
  categoryId: string;
  categoryName: string;
  allocatedAmountCents: number;
  rolloverAmountCents: number;
  warningThresholdPercent: number;
  spentAmountCents: number;
  createdUtc: string;
  updatedUtc: string;
};

export type RecurringTransaction = {
  id: string;
  categoryId: string;
  description: string;
  merchant: string | null;
  amountInCents: number;
  transactionType: "expense" | "income";
  frequency: "weekly" | "monthly";
  dayOfMonth: number | null;
  dayOfWeek: string | null;
  nextOccurrenceDate: string;
  endDate: string | null;
  autoCreate: boolean;
  isActive: boolean;
  createdUtc: string;
  updatedUtc: string;
};

export type SafeToSpend = {
  budgetPeriodId: string;
  availableCashInCents: number;
  protectedAmountInCents: number;
  safetyBufferInCents: number;
  savingsCommitmentInCents: number;
  upcomingCommitmentsInCents: number;
  remainingCategoryBudgetInCents: number;
  amountInCents: number;
  dailyAmountInCents: number;
  daysRemaining: number;
};

export type DashboardSummary = {
  generatedUtc: string;
  budgetPeriod: {
    id: string;
    startDate: string;
    endDate: string;
    daysRemaining: number;
    periodProgressPercent: number;
  };
  financialStatus: {
    status: string;
    message: string;
    moneyPulse: number;
  };
  safeToSpend: {
    amountInCents: number;
    dailyAmountInCents: number;
    availableCashInCents: number;
    protectedAmountInCents: number;
    safetyBufferInCents: number;
  };
  spending: {
    spentThisPeriodInCents: number;
    spendingPercent: number;
    expectedSpendingPercent: number;
  };
  recommendedAction: {
    type: string;
    title: string;
    message: string;
  };
  categories: DashboardCategoryProgress[];
  upcomingCommitments: DashboardUpcomingCommitment[];
  recentTransactions: DashboardRecentTransaction[];
  cashFlowForecast: DashboardCashFlowPoint[];
  insights: DashboardInsight[];
};

export type DashboardCategoryProgress = {
  categoryId: string;
  name: string;
  allocatedInCents: number;
  spentInCents: number;
  remainingInCents: number;
  spendingPercent: number;
  status: string;
  latestTransaction: string | null;
};

export type DashboardUpcomingCommitment = {
  id: string;
  description: string;
  dueDate: string;
  amountInCents: number;
  isProtected: boolean;
  status: string;
};

export type DashboardRecentTransaction = {
  id: string;
  description: string;
  merchant: string | null;
  categoryName: string;
  transactionDate: string;
  amountInCents: number;
  transactionType: "expense" | "income";
  source: string;
  status: string;
};

export type DashboardCashFlowPoint = {
  date: string;
  projectedBalanceInCents: number;
  commitmentAmountInCents: number;
  isPayday: boolean;
};

export type DashboardInsight = {
  type: string;
  title: string;
  message: string;
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

export type CategoryInput = {
  name: string;
  categoryType: "expense" | "income" | "savings";
  icon: string | null;
  sortOrder: number;
};

export type BudgetPeriodInput = {
  startDate: string;
  endDate: string;
  expectedIncomeCents: number;
  openingBalanceCents: number;
};

export type CategoryBudgetInput = {
  categoryId: string;
  allocatedAmountCents: number;
  rolloverAmountCents: number;
  warningThresholdPercent: number;
};

export type RecurringTransactionInput = {
  categoryId: string;
  description: string;
  merchant: string | null;
  amountInCents: number;
  transactionType: "expense" | "income";
  frequency: "weekly" | "monthly";
  dayOfMonth: number | null;
  dayOfWeek: string | null;
  nextOccurrenceDate: string;
  endDate: string | null;
  autoCreate: boolean;
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
        }),
  listCategories: (accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.listCategories(accessToken)
      : apiRequest<Category[]>("/categories", { accessToken }),
  createCategory: (accessToken: string, body: CategoryInput) =>
    IS_DEMO_MODE
      ? demoApi.createCategory(accessToken, body)
      : apiRequest<Category>("/categories", {
          method: "POST",
          accessToken,
          body: JSON.stringify(body)
        }),
  updateCategory: (accessToken: string, id: string, body: CategoryInput) =>
    IS_DEMO_MODE
      ? demoApi.updateCategory(accessToken, id, body)
      : apiRequest<Category>(`/categories/${id}`, {
          method: "PUT",
          accessToken,
          body: JSON.stringify(body)
        }),
  deleteCategory: (accessToken: string, id: string) =>
    IS_DEMO_MODE
      ? demoApi.deleteCategory(accessToken, id)
      : apiRequest<void>(`/categories/${id}`, {
          method: "DELETE",
          accessToken
        }),
  listBudgetPeriods: (accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.listBudgetPeriods(accessToken)
      : apiRequest<BudgetPeriod[]>("/budget-periods", { accessToken }),
  getCurrentBudgetPeriod: (accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.getCurrentBudgetPeriod(accessToken)
      : apiRequest<BudgetPeriod>("/budget-periods/current", { accessToken }),
  createBudgetPeriod: (accessToken: string, body: BudgetPeriodInput) =>
    IS_DEMO_MODE
      ? demoApi.createBudgetPeriod(accessToken, body)
      : apiRequest<BudgetPeriod>("/budget-periods", {
          method: "POST",
          accessToken,
          body: JSON.stringify(body)
        }),
  listCategoryBudgets: (accessToken: string, periodId: string) =>
    IS_DEMO_MODE
      ? demoApi.listCategoryBudgets(accessToken, periodId)
      : apiRequest<CategoryBudget[]>(`/budget-periods/${periodId}/category-budgets`, {
          accessToken
        }),
  createCategoryBudget: (accessToken: string, periodId: string, body: CategoryBudgetInput) =>
    IS_DEMO_MODE
      ? demoApi.createCategoryBudget(accessToken, periodId, body)
      : apiRequest<CategoryBudget>(`/budget-periods/${periodId}/category-budgets`, {
          method: "POST",
          accessToken,
          body: JSON.stringify(body)
        }),
  updateCategoryBudget: (accessToken: string, id: string, body: CategoryBudgetInput) =>
    IS_DEMO_MODE
      ? demoApi.updateCategoryBudget(accessToken, id, body)
      : apiRequest<CategoryBudget>(`/category-budgets/${id}`, {
          method: "PUT",
          accessToken,
          body: JSON.stringify(body)
        }),
  deleteCategoryBudget: (accessToken: string, id: string) =>
    IS_DEMO_MODE
      ? demoApi.deleteCategoryBudget(accessToken, id)
      : apiRequest<void>(`/category-budgets/${id}`, {
          method: "DELETE",
          accessToken
        }),
  listRecurringTransactions: (accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.listRecurringTransactions(accessToken)
      : apiRequest<RecurringTransaction[]>("/recurring-transactions", { accessToken }),
  createRecurringTransaction: (accessToken: string, body: RecurringTransactionInput) =>
    IS_DEMO_MODE
      ? demoApi.createRecurringTransaction(accessToken, body)
      : apiRequest<RecurringTransaction>("/recurring-transactions", {
          method: "POST",
          accessToken,
          body: JSON.stringify(body)
        }),
  pauseRecurringTransaction: (accessToken: string, id: string) =>
    IS_DEMO_MODE
      ? demoApi.pauseRecurringTransaction(accessToken, id)
      : apiRequest<RecurringTransaction>(`/recurring-transactions/${id}/pause`, {
          method: "POST",
          accessToken
        }),
  getSafeToSpend: (accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.getSafeToSpend(accessToken)
      : apiRequest<SafeToSpend>("/dashboard/safe-to-spend", {
          accessToken
        }),
  getDashboard: (accessToken: string) =>
    IS_DEMO_MODE
      ? demoApi.getDashboard(accessToken)
      : apiRequest<DashboardSummary>("/dashboard", { accessToken })
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
  },
  async listCategories(accessToken: string): Promise<Category[]> {
    void accessToken;
    return readDemoCategories();
  },
  async createCategory(accessToken: string, body: CategoryInput): Promise<Category> {
    void accessToken;
    const now = new Date().toISOString();
    const category: Category = {
      ...body,
      id: createId(),
      slug: slugify(body.name),
      isSystem: false,
      isActive: true,
      createdUtc: now,
      updatedUtc: now
    };
    writeDemoCategories([...readDemoCategories(), category]);
    return category;
  },
  async updateCategory(accessToken: string, id: string, body: CategoryInput): Promise<Category> {
    void accessToken;
    const categories = readDemoCategories();
    const existing = categories.find((category) => category.id === id);

    if (!existing || existing.isSystem) {
      throw new ApiError(404, "Category was not found.");
    }

    const updated: Category = {
      ...existing,
      ...body,
      slug: slugify(body.name),
      updatedUtc: new Date().toISOString()
    };
    writeDemoCategories(categories.map((category) => (category.id === id ? updated : category)));
    return updated;
  },
  async deleteCategory(accessToken: string, id: string): Promise<void> {
    void accessToken;
    writeDemoCategories(readDemoCategories().filter((category) => category.id !== id));
  },
  async listBudgetPeriods(accessToken: string): Promise<BudgetPeriod[]> {
    void accessToken;
    return readDemoBudgetPeriods();
  },
  async getCurrentBudgetPeriod(accessToken: string): Promise<BudgetPeriod> {
    void accessToken;
    return readDemoBudgetPeriods()[0];
  },
  async createBudgetPeriod(accessToken: string, body: BudgetPeriodInput): Promise<BudgetPeriod> {
    void accessToken;
    const now = new Date().toISOString();
    const period: BudgetPeriod = {
      ...body,
      id: createId(),
      actualIncomeCents: 0,
      status: "open",
      daysRemaining: Math.max(0, dayDiff(new Date().toISOString().slice(0, 10), body.endDate) + 1),
      createdUtc: now,
      updatedUtc: now
    };
    writeDemoBudgetPeriods([period, ...readDemoBudgetPeriods()]);
    return period;
  },
  async listCategoryBudgets(accessToken: string, periodId: string): Promise<CategoryBudget[]> {
    void accessToken;
    return readDemoCategoryBudgets().filter((budget) => budget.budgetPeriodId === periodId);
  },
  async createCategoryBudget(
    accessToken: string,
    periodId: string,
    body: CategoryBudgetInput
  ): Promise<CategoryBudget> {
    void accessToken;
    const category = readDemoCategories().find((item) => item.id === body.categoryId);
    const now = new Date().toISOString();
    const budget: CategoryBudget = {
      ...body,
      id: createId(),
      budgetPeriodId: periodId,
      categoryName: category?.name ?? "Category",
      spentAmountCents: 0,
      createdUtc: now,
      updatedUtc: now
    };
    writeDemoCategoryBudgets([...readDemoCategoryBudgets(), budget]);
    return budget;
  },
  async updateCategoryBudget(
    accessToken: string,
    id: string,
    body: CategoryBudgetInput
  ): Promise<CategoryBudget> {
    void accessToken;
    const budgets = readDemoCategoryBudgets();
    const existing = budgets.find((budget) => budget.id === id);
    const category = readDemoCategories().find((item) => item.id === body.categoryId);

    if (!existing) {
      throw new ApiError(404, "Category budget was not found.");
    }

    const updated: CategoryBudget = {
      ...existing,
      ...body,
      categoryName: category?.name ?? existing.categoryName,
      updatedUtc: new Date().toISOString()
    };
    writeDemoCategoryBudgets(budgets.map((budget) => (budget.id === id ? updated : budget)));
    return updated;
  },
  async deleteCategoryBudget(accessToken: string, id: string): Promise<void> {
    void accessToken;
    writeDemoCategoryBudgets(readDemoCategoryBudgets().filter((budget) => budget.id !== id));
  },
  async listRecurringTransactions(accessToken: string): Promise<RecurringTransaction[]> {
    void accessToken;
    return readDemoRecurringTransactions();
  },
  async createRecurringTransaction(
    accessToken: string,
    body: RecurringTransactionInput
  ): Promise<RecurringTransaction> {
    void accessToken;
    const now = new Date().toISOString();
    const recurring: RecurringTransaction = {
      ...body,
      id: createId(),
      isActive: true,
      createdUtc: now,
      updatedUtc: now
    };
    writeDemoRecurringTransactions([...readDemoRecurringTransactions(), recurring]);
    return recurring;
  },
  async pauseRecurringTransaction(accessToken: string, id: string): Promise<RecurringTransaction> {
    void accessToken;
    const recurringItems = readDemoRecurringTransactions();
    const existing = recurringItems.find((item) => item.id === id);

    if (!existing) {
      throw new ApiError(404, "Recurring transaction was not found.");
    }

    const paused: RecurringTransaction = {
      ...existing,
      isActive: false,
      updatedUtc: new Date().toISOString()
    };
    writeDemoRecurringTransactions(
      recurringItems.map((item) => (item.id === id ? paused : item))
    );
    return paused;
  },
  async getSafeToSpend(accessToken: string): Promise<SafeToSpend> {
    void accessToken;
    const period = readDemoBudgetPeriods()[0];
    const budgets = readDemoCategoryBudgets().filter((budget) => budget.budgetPeriodId === period.id);
    const remainingBudget = budgets.reduce(
      (sum, budget) =>
        sum + Math.max(0, budget.allocatedAmountCents + budget.rolloverAmountCents - budget.spentAmountCents),
      0
    );
    const upcomingCommitments = readDemoRecurringTransactions()
      .filter((item) => item.isActive && item.transactionType === "expense")
      .reduce((sum, item) => sum + item.amountInCents, 0);
    const safetyBuffer = 50000;
    const savingsCommitment = 220000;
    const availableCash = period.openingBalanceCents + period.expectedIncomeCents;
    const protectedAmount = safetyBuffer + savingsCommitment + upcomingCommitments;
    const amount = Math.max(0, Math.min(availableCash - protectedAmount, remainingBudget || availableCash));

    return {
      budgetPeriodId: period.id,
      availableCashInCents: availableCash,
      protectedAmountInCents: protectedAmount,
      safetyBufferInCents: safetyBuffer,
      savingsCommitmentInCents: savingsCommitment,
      upcomingCommitmentsInCents: upcomingCommitments,
      remainingCategoryBudgetInCents: remainingBudget,
      amountInCents: amount,
      dailyAmountInCents: period.daysRemaining > 0 ? Math.floor(amount / period.daysRemaining) : 0,
      daysRemaining: period.daysRemaining
    };
  },
  async getDashboard(accessToken: string): Promise<DashboardSummary> {
    void accessToken;
    const period = readDemoBudgetPeriods()[0];
    const budgets = readDemoCategoryBudgets().filter((budget) => budget.budgetPeriodId === period.id);
    const recurring = readDemoRecurringTransactions().filter((item) => item.isActive);
    const safe = await this.getSafeToSpend("demo");
    const spent = budgets.reduce((sum, budget) => sum + budget.spentAmountCents, 0);
    const allocated = budgets.reduce(
      (sum, budget) => sum + budget.allocatedAmountCents + budget.rolloverAmountCents,
      0
    );

    return {
      generatedUtc: new Date().toISOString(),
      budgetPeriod: {
        id: period.id,
        startDate: period.startDate,
        endDate: period.endDate,
        daysRemaining: period.daysRemaining,
        periodProgressPercent: 48
      },
      financialStatus: {
        status: "onTrack",
        message: "You are currently on track.",
        moneyPulse: 76
      },
      safeToSpend: {
        amountInCents: safe.amountInCents,
        dailyAmountInCents: safe.dailyAmountInCents,
        availableCashInCents: safe.availableCashInCents,
        protectedAmountInCents: safe.protectedAmountInCents,
        safetyBufferInCents: safe.safetyBufferInCents
      },
      spending: {
        spentThisPeriodInCents: spent,
        spendingPercent: allocated > 0 ? Math.round((spent / allocated) * 100) : 0,
        expectedSpendingPercent: 48
      },
      recommendedAction: {
        type: "onTrack",
        title: "Keep the current pace",
        message: "Safe-to-spend is positive and your category usage is aligned with the month."
      },
      categories: budgets.map((budget) => ({
        categoryId: budget.categoryId,
        name: budget.categoryName,
        allocatedInCents: budget.allocatedAmountCents + budget.rolloverAmountCents,
        spentInCents: budget.spentAmountCents,
        remainingInCents: Math.max(0, budget.allocatedAmountCents + budget.rolloverAmountCents - budget.spentAmountCents),
        spendingPercent:
          budget.allocatedAmountCents > 0
            ? Math.round((budget.spentAmountCents / (budget.allocatedAmountCents + budget.rolloverAmountCents)) * 100)
            : 0,
        status: "onTrack",
        latestTransaction: null
      })),
      upcomingCommitments: recurring.map((item) => ({
        id: item.id,
        description: item.description,
        dueDate: item.nextOccurrenceDate,
        amountInCents: item.amountInCents,
        isProtected: true,
        status: "protected"
      })),
      recentTransactions: readDemoTransactions().slice(0, 6).map((transaction) => ({
        id: transaction.id,
        description: transaction.description,
        merchant: transaction.merchant,
        categoryName: readDemoCategories().find((category) => category.id === transaction.categoryId)?.name ?? "Uncategorised",
        transactionDate: transaction.transactionDate,
        amountInCents: transaction.amountInCents,
        transactionType: transaction.transactionType,
        source: transaction.source,
        status: transaction.status
      })),
      cashFlowForecast: Array.from({ length: Math.min(10, period.daysRemaining) }, (_, index) => ({
        date: addDays(new Date().toISOString().slice(0, 10), index),
        projectedBalanceInCents: safe.availableCashInCents - recurring.reduce((sum, item) => sum + (index === 3 ? item.amountInCents : 0), 0),
        commitmentAmountInCents: index === 3 ? recurring.reduce((sum, item) => sum + item.amountInCents, 0) : 0,
        isPayday: index === period.daysRemaining - 1
      })),
      insights: [
        {
          type: "commitments",
          title: "Upcoming commitments are protected",
          message: `${recurring.length} active commitment entries are reserved before flexible spending.`
        }
      ]
    };
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

function readDemoCategories(): Category[] {
  const fallback: Category[] = [
    createDemoCategory("demo-groceries", "Groceries", "expense", "basket", 10),
    createDemoCategory("demo-transport", "Transport", "expense", "car", 20),
    createDemoCategory("demo-savings", "Savings", "savings", "target", 30)
  ];

  return readStoredArray(demoCategoriesKey, fallback);
}

function writeDemoCategories(categories: Category[]) {
  window.localStorage.setItem(demoCategoriesKey, JSON.stringify(categories));
}

function readDemoBudgetPeriods(): BudgetPeriod[] {
  const today = new Date().toISOString().slice(0, 10);
  const fallback: BudgetPeriod[] = [
    {
      id: "demo-period",
      startDate: today,
      endDate: "2026-06-30",
      expectedIncomeCents: 2875000,
      actualIncomeCents: 0,
      openingBalanceCents: 610000,
      status: "open",
      daysRemaining: Math.max(1, dayDiff(today, "2026-06-30") + 1),
      createdUtc: "2026-06-17T10:00:00Z",
      updatedUtc: "2026-06-17T10:00:00Z"
    }
  ];

  return readStoredArray(demoBudgetPeriodsKey, fallback);
}

function writeDemoBudgetPeriods(periods: BudgetPeriod[]) {
  window.localStorage.setItem(demoBudgetPeriodsKey, JSON.stringify(periods));
}

function readDemoCategoryBudgets(): CategoryBudget[] {
  const fallback: CategoryBudget[] = [
    createDemoCategoryBudget("demo-grocery-budget", "demo-period", "demo-groceries", "Groceries", 520000, 120000),
    createDemoCategoryBudget("demo-transport-budget", "demo-period", "demo-transport", "Transport", 280000, 90000),
    createDemoCategoryBudget("demo-saving-budget", "demo-period", "demo-savings", "Savings", 350000, 140000)
  ];

  return readStoredArray(demoCategoryBudgetsKey, fallback);
}

function writeDemoCategoryBudgets(budgets: CategoryBudget[]) {
  window.localStorage.setItem(demoCategoryBudgetsKey, JSON.stringify(budgets));
}

function readDemoRecurringTransactions(): RecurringTransaction[] {
  const fallback: RecurringTransaction[] = [
    {
      id: "demo-rent",
      categoryId: "demo-groceries",
      description: "Rent",
      merchant: "Landlord",
      amountInCents: 820000,
      transactionType: "expense",
      frequency: "monthly",
      dayOfMonth: 25,
      dayOfWeek: null,
      nextOccurrenceDate: "2026-06-25",
      endDate: null,
      autoCreate: true,
      isActive: true,
      createdUtc: "2026-06-17T10:00:00Z",
      updatedUtc: "2026-06-17T10:00:00Z"
    }
  ];

  return readStoredArray(demoRecurringKey, fallback);
}

function writeDemoRecurringTransactions(items: RecurringTransaction[]) {
  window.localStorage.setItem(demoRecurringKey, JSON.stringify(items));
}

function readStoredArray<T>(key: string, fallback: T[]): T[] {
  try {
    const raw = window.localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T[]) : fallback;
  } catch {
    return fallback;
  }
}

function createDemoCategory(
  id: string,
  name: string,
  categoryType: Category["categoryType"],
  icon: string,
  sortOrder: number
): Category {
  return {
    id,
    name,
    slug: slugify(name),
    categoryType,
    icon,
    sortOrder,
    isSystem: true,
    isActive: true,
    createdUtc: "2026-06-17T10:00:00Z",
    updatedUtc: "2026-06-17T10:00:00Z"
  };
}

function createDemoCategoryBudget(
  id: string,
  budgetPeriodId: string,
  categoryId: string,
  categoryName: string,
  allocatedAmountCents: number,
  spentAmountCents: number
): CategoryBudget {
  return {
    id,
    budgetPeriodId,
    categoryId,
    categoryName,
    allocatedAmountCents,
    rolloverAmountCents: 0,
    warningThresholdPercent: 80,
    spentAmountCents,
    createdUtc: "2026-06-17T10:00:00Z",
    updatedUtc: "2026-06-17T10:00:00Z"
  };
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

function slugify(value: string) {
  return value.trim().toLowerCase().split(/\s+/).join("-");
}

function dayDiff(from: string, to: string) {
  const fromDate = new Date(`${from}T00:00:00Z`);
  const toDate = new Date(`${to}T00:00:00Z`);
  return Math.floor((toDate.getTime() - fromDate.getTime()) / 86400000);
}

function addDays(from: string, days: number) {
  const date = new Date(`${from}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}
