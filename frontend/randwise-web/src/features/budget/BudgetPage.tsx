import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  api,
  type BudgetPeriod,
  type Category,
  type CategoryBudget,
  type RecurringTransaction,
  type SafeToSpend
} from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { Button } from "../../ui/Button";
import { MoneyText } from "../../ui/MoneyText";
import { Panel } from "../../ui/Panel";
import { StatusBadge } from "../../ui/StatusBadge";
import { classNames } from "../../ui/classNames";

type Target = {
  id: string;
  name: string;
  cadence: "weekly" | "monthly";
  targetInCents: number;
  savedInCents: number;
};

const targetStorageKey = "randwise.targets";

export function BudgetPage() {
  const auth = useAuth();
  const [categories, setCategories] = useState<Category[]>([]);
  const [periods, setPeriods] = useState<BudgetPeriod[]>([]);
  const [categoryBudgets, setCategoryBudgets] = useState<CategoryBudget[]>([]);
  const [recurring, setRecurring] = useState<RecurringTransaction[]>([]);
  const [safeToSpend, setSafeToSpend] = useState<SafeToSpend | null>(null);
  const [targets, setTargets] = useState<Target[]>(() => readTargets());
  const [periodForm, setPeriodForm] = useState(() => createPeriodForm());
  const [categoryForm, setCategoryForm] = useState({ name: "", categoryType: "expense", icon: "tag" });
  const [budgetForm, setBudgetForm] = useState({ categoryId: "", allocatedAmountCents: "", warningThresholdPercent: "80" });
  const [recurringForm, setRecurringForm] = useState(() => createRecurringForm());
  const [targetForm, setTargetForm] = useState({ name: "", cadence: "weekly", targetInCents: "", savedInCents: "" });
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const selectedPeriod = periods.find((period) => period.status === "open") ?? periods[0] ?? null;
  const expenseCategories = categories.filter((category) => category.categoryType !== "income");
  const totalAllocated = categoryBudgets.reduce((sum, budget) => sum + budget.allocatedAmountCents + budget.rolloverAmountCents, 0);
  const totalSpent = categoryBudgets.reduce((sum, budget) => sum + budget.spentAmountCents, 0);
  const targetPoints = targets.reduce((sum, target) => sum + Math.min(100, getPercent(target.savedInCents, target.targetInCents)), 0);
  const level = Math.max(1, Math.floor(targetPoints / 120) + 1);

  const groupedBudgets = useMemo(() => {
    const essentials = categoryBudgets.filter((budget) =>
      /groceries|transport|rent|utilities|home/i.test(budget.categoryName)
    );
    const essentialIds = new Set(essentials.map((budget) => budget.id));
    const lifestyle = categoryBudgets.filter(
      (budget) =>
        !essentialIds.has(budget.id)
        && /food|coffee|entertainment|shopping|takeaway/i.test(budget.categoryName)
    );
    const assignedIds = new Set([...essentials, ...lifestyle].map((budget) => budget.id));
    const saving = categoryBudgets.filter((budget) => !assignedIds.has(budget.id));

    return [
      { title: "Essentials", tone: "emerald", budgets: essentials },
      { title: "Lifestyle", tone: "cyan", budgets: lifestyle },
      { title: "Saving", tone: "amber", budgets: saving }
    ];
  }, [categoryBudgets]);

  useEffect(() => {
    let isActive = true;

    async function load(accessToken: string) {
      setIsLoading(true);
      setError(null);
      try {
        const [nextCategories, nextPeriods, nextRecurring] = await Promise.all([
          api.listCategories(accessToken),
          api.listBudgetPeriods(accessToken),
          api.listRecurringTransactions(accessToken)
        ]);

        if (!isActive) {
          return;
        }

        setCategories(nextCategories);
        setPeriods(nextPeriods);
        setRecurring(nextRecurring);
        setBudgetForm((current) => ({ ...current, categoryId: nextCategories[0]?.id ?? "" }));
        setRecurringForm((current) => ({ ...current, categoryId: nextCategories[0]?.id ?? "" }));
      } catch (cause) {
        if (isActive) {
          setError(cause instanceof Error ? cause.message : "Could not load budget workspace.");
        }
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    if (auth.tokens) {
      void load(auth.tokens.accessToken);
    }

    return () => {
      isActive = false;
    };
  }, [auth.tokens]);

  useEffect(() => {
    if (!auth.tokens || !selectedPeriod) {
      return;
    }

    let isActive = true;
    async function loadPeriod(accessToken: string, period: BudgetPeriod) {
      try {
        const [nextBudgets, nextSafe] = await Promise.all([
          api.listCategoryBudgets(accessToken, period.id),
          api.getSafeToSpend(accessToken).catch(() => null)
        ]);

        if (isActive) {
          setCategoryBudgets(nextBudgets);
          setSafeToSpend(nextSafe);
        }
      } catch (cause) {
        if (isActive) {
          setError(cause instanceof Error ? cause.message : "Could not load period budgets.");
        }
      }
    }

    void loadPeriod(auth.tokens.accessToken, selectedPeriod);

    return () => {
      isActive = false;
    };
  }, [auth.tokens, selectedPeriod]);

  useEffect(() => {
    window.localStorage.setItem(targetStorageKey, JSON.stringify(targets));
  }, [targets]);

  async function createPeriod(event: FormEvent) {
    event.preventDefault();
    if (!auth.tokens) {
      return;
    }

    const accessToken = auth.tokens.accessToken;
    await submit(async () => {
      const period = await api.createBudgetPeriod(accessToken, {
        startDate: periodForm.startDate,
        endDate: periodForm.endDate,
        expectedIncomeCents: Number(periodForm.expectedIncomeCents),
        openingBalanceCents: Number(periodForm.openingBalanceCents)
      });
      setPeriods((current) => [period, ...current]);
    }, "Could not create budget period.");
  }

  async function createCategory(event: FormEvent) {
    event.preventDefault();
    if (!auth.tokens) {
      return;
    }

    const accessToken = auth.tokens.accessToken;
    await submit(async () => {
      const category = await api.createCategory(accessToken, {
        name: categoryForm.name,
        categoryType: categoryForm.categoryType as Category["categoryType"],
        icon: categoryForm.icon || null,
        sortOrder: categories.length + 10
      });
      setCategories((current) => [...current, category]);
      setBudgetForm((current) => ({ ...current, categoryId: category.id }));
      setCategoryForm({ name: "", categoryType: "expense", icon: "tag" });
    }, "Could not create category.");
  }

  async function createCategoryBudget(event: FormEvent) {
    event.preventDefault();
    if (!auth.tokens || !selectedPeriod) {
      return;
    }

    const accessToken = auth.tokens.accessToken;
    const periodId = selectedPeriod.id;
    await submit(async () => {
      const budget = await api.createCategoryBudget(accessToken, periodId, {
        categoryId: budgetForm.categoryId,
        allocatedAmountCents: Number(budgetForm.allocatedAmountCents),
        rolloverAmountCents: 0,
        warningThresholdPercent: Number(budgetForm.warningThresholdPercent)
      });
      setCategoryBudgets((current) => [...current, budget]);
      setBudgetForm((current) => ({ ...current, allocatedAmountCents: "" }));
    }, "Could not set category budget.");
  }

  async function createRecurring(event: FormEvent) {
    event.preventDefault();
    if (!auth.tokens) {
      return;
    }

    const accessToken = auth.tokens.accessToken;
    await submit(async () => {
      const item = await api.createRecurringTransaction(accessToken, {
        categoryId: recurringForm.categoryId,
        description: recurringForm.description,
        merchant: recurringForm.merchant || null,
        amountInCents: Number(recurringForm.amountInCents),
        transactionType: "expense",
        frequency: recurringForm.frequency as "weekly" | "monthly",
        dayOfMonth: recurringForm.frequency === "monthly" ? Number(recurringForm.dayOfMonth) : null,
        dayOfWeek: recurringForm.frequency === "weekly" ? recurringForm.dayOfWeek : null,
        nextOccurrenceDate: recurringForm.nextOccurrenceDate,
        endDate: null,
        autoCreate: true
      });
      setRecurring((current) => [...current, item]);
      setRecurringForm(createRecurringForm(item.categoryId));
    }, "Could not add recurring commitment.");
  }

  function createTarget(event: FormEvent) {
    event.preventDefault();
    const target: Target = {
      id: createId(),
      name: targetForm.name,
      cadence: targetForm.cadence as Target["cadence"],
      targetInCents: Number(targetForm.targetInCents),
      savedInCents: Number(targetForm.savedInCents || 0)
    };
    setTargets((current) => [target, ...current]);
    setTargetForm({ name: "", cadence: "weekly", targetInCents: "", savedInCents: "" });
  }

  async function pauseRecurring(item: RecurringTransaction) {
    if (!auth.tokens) {
      return;
    }

    const accessToken = auth.tokens.accessToken;
    await submit(async () => {
      const paused = await api.pauseRecurringTransaction(accessToken, item.id);
      setRecurring((current) => current.map((value) => (value.id === paused.id ? paused : value)));
    }, "Could not pause recurring commitment.");
  }

  async function submit(operation: () => Promise<void>, message: string) {
    setIsSubmitting(true);
    setError(null);
    try {
      await operation();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="space-y-5">
      <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_360px]">
        <div className="overflow-hidden rounded-lg border border-emerald-900/10 bg-slate-950 text-white shadow-sm dark:border-emerald-300/20">
          <div className="p-5 md:p-7">
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-2xl font-bold">Budget command center</h1>
              <StatusBadge tone="success">Level {level}</StatusBadge>
            </div>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-emerald-50">
              Group spending categories, protect fixed commitments and pace weekly or monthly saving targets.
            </p>
            <div className="mt-6 grid gap-4 md:grid-cols-3">
              <Metric label="Safe to spend" value={<MoneyText amountInCents={safeToSpend?.amountInCents ?? 0} />} />
              <Metric label="Allocated" value={<MoneyText amountInCents={totalAllocated} />} />
              <Metric label="Spent" value={<MoneyText amountInCents={totalSpent} />} />
            </div>
          </div>
        </div>

        <Panel aria-labelledby="period-heading">
          <h2 id="period-heading" className="text-lg font-bold text-slate-950 dark:text-slate-100">
            Current period
          </h2>
          {selectedPeriod ? (
            <div className="mt-4 space-y-3 text-sm text-slate-600 dark:text-slate-400">
              <p className="font-semibold text-slate-950 dark:text-slate-100">
                {selectedPeriod.startDate} to {selectedPeriod.endDate}
              </p>
              <p>{selectedPeriod.daysRemaining} days remaining</p>
              <p>
                Expected income <MoneyText amountInCents={selectedPeriod.expectedIncomeCents} />
              </p>
            </div>
          ) : null}
          <form className="mt-5 grid gap-3" onSubmit={createPeriod}>
            <Field label="Start date" type="date" value={periodForm.startDate} onChange={(value) => setPeriodForm((current) => ({ ...current, startDate: value }))} />
            <Field label="End date" type="date" value={periodForm.endDate} onChange={(value) => setPeriodForm((current) => ({ ...current, endDate: value }))} />
            <Field label="Expected income cents" type="number" value={periodForm.expectedIncomeCents} onChange={(value) => setPeriodForm((current) => ({ ...current, expectedIncomeCents: value }))} />
            <Field label="Opening balance cents" type="number" value={periodForm.openingBalanceCents} onChange={(value) => setPeriodForm((current) => ({ ...current, openingBalanceCents: value }))} />
            <Button type="submit" variant="primary" disabled={isSubmitting}>
              Start period
            </Button>
          </form>
        </Panel>
      </section>

      {error ? (
        <div className="rounded-lg border border-rose-300 bg-rose-50 p-4 text-sm font-semibold text-rose-800 dark:border-rose-300/30 dark:bg-rose-300/10 dark:text-rose-100">
          {error}
        </div>
      ) : null}

      <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_380px]">
        <div>
          <div className="flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
            <div>
              <h2 className="text-xl font-bold text-slate-950 dark:text-slate-100">
                Category groups
              </h2>
              <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
                Budgets are grouped by essentials, lifestyle and saving intent.
              </p>
            </div>
            <StatusBadge tone="neutral">{categoryBudgets.length} budgets</StatusBadge>
          </div>
          <div className="mt-4 grid gap-4 xl:grid-cols-3">
            {groupedBudgets.map((group) => (
              <Panel key={group.title} as="article">
                <h3 className="text-base font-bold text-slate-950 dark:text-slate-100">{group.title}</h3>
                <div className="mt-4 space-y-4">
                  {group.budgets.length === 0 ? (
                    <p className="text-sm text-slate-600 dark:text-slate-400">No budgets yet.</p>
                  ) : (
                    group.budgets.map((budget) => <BudgetRow key={budget.id} budget={budget} tone={group.tone} />)
                  )}
                </div>
              </Panel>
            ))}
          </div>
        </div>

        <Panel aria-labelledby="budget-form-heading">
          <h2 id="budget-form-heading" className="text-lg font-bold text-slate-950 dark:text-slate-100">
            Add budget
          </h2>
          <form className="mt-5 space-y-4" onSubmit={createCategoryBudget}>
            <Select label="Category" value={budgetForm.categoryId} onChange={(value) => setBudgetForm((current) => ({ ...current, categoryId: value }))}>
              {expenseCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </Select>
            <Field label="Allocated amount cents" type="number" value={budgetForm.allocatedAmountCents} onChange={(value) => setBudgetForm((current) => ({ ...current, allocatedAmountCents: value }))} />
            <Field label="Warning threshold %" type="number" value={budgetForm.warningThresholdPercent} onChange={(value) => setBudgetForm((current) => ({ ...current, warningThresholdPercent: value }))} />
            <Button type="submit" variant="primary" disabled={isSubmitting || !selectedPeriod}>
              Save budget
            </Button>
          </form>
        </Panel>
      </section>

      <section className="grid gap-4 lg:grid-cols-3">
        <Panel aria-labelledby="category-form-heading">
          <h2 id="category-form-heading" className="text-lg font-bold text-slate-950 dark:text-slate-100">
            Categories
          </h2>
          <form className="mt-5 space-y-4" onSubmit={createCategory}>
            <Field label="Category name" value={categoryForm.name} onChange={(value) => setCategoryForm((current) => ({ ...current, name: value }))} />
            <Select label="Type" value={categoryForm.categoryType} onChange={(value) => setCategoryForm((current) => ({ ...current, categoryType: value }))}>
              <option value="expense">Expense</option>
              <option value="income">Income</option>
              <option value="savings">Savings</option>
            </Select>
            <Field label="Icon keyword" value={categoryForm.icon} onChange={(value) => setCategoryForm((current) => ({ ...current, icon: value }))} required={false} />
            <Button type="submit" variant="primary" disabled={isSubmitting}>
              Add category
            </Button>
          </form>
        </Panel>

        <Panel aria-labelledby="recurring-heading">
          <h2 id="recurring-heading" className="text-lg font-bold text-slate-950 dark:text-slate-100">
            Recurring commitments
          </h2>
          <form className="mt-5 space-y-4" onSubmit={createRecurring}>
            <Select label="Category" value={recurringForm.categoryId} onChange={(value) => setRecurringForm((current) => ({ ...current, categoryId: value }))}>
              {expenseCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </Select>
            <Field label="Description" value={recurringForm.description} onChange={(value) => setRecurringForm((current) => ({ ...current, description: value }))} />
            <Field label="Amount cents" type="number" value={recurringForm.amountInCents} onChange={(value) => setRecurringForm((current) => ({ ...current, amountInCents: value }))} />
            <Select label="Frequency" value={recurringForm.frequency} onChange={(value) => setRecurringForm((current) => ({ ...current, frequency: value }))}>
              <option value="weekly">Weekly</option>
              <option value="monthly">Monthly</option>
            </Select>
            <Field label="Next date" type="date" value={recurringForm.nextOccurrenceDate} onChange={(value) => setRecurringForm((current) => ({ ...current, nextOccurrenceDate: value }))} />
            <Button type="submit" variant="primary" disabled={isSubmitting}>
              Add commitment
            </Button>
          </form>
          <div className="mt-5 space-y-3">
            {recurring.map((item) => (
              <div key={item.id} className="flex items-center justify-between gap-3 border-t border-slate-200 pt-3 first:border-t-0 first:pt-0 dark:border-slate-700">
                <div>
                  <p className="font-semibold text-slate-950 dark:text-slate-100">{item.description}</p>
                  <p className="text-sm text-slate-600 dark:text-slate-400">
                    <MoneyText amountInCents={item.amountInCents} /> {item.frequency}
                  </p>
                </div>
                {item.isActive ? (
                  <Button variant="ghost" onClick={() => void pauseRecurring(item)}>
                    Pause
                  </Button>
                ) : (
                  <StatusBadge tone="neutral">Paused</StatusBadge>
                )}
              </div>
            ))}
          </div>
        </Panel>

        <Panel aria-labelledby="targets-heading">
          <div className="flex items-start justify-between gap-3">
            <h2 id="targets-heading" className="text-lg font-bold text-slate-950 dark:text-slate-100">
              Saving targets
            </h2>
            <StatusBadge tone="success">{targetPoints} XP</StatusBadge>
          </div>
          <form className="mt-5 space-y-4" onSubmit={createTarget}>
            <Field label="Target name" value={targetForm.name} onChange={(value) => setTargetForm((current) => ({ ...current, name: value }))} />
            <Select label="Cadence" value={targetForm.cadence} onChange={(value) => setTargetForm((current) => ({ ...current, cadence: value }))}>
              <option value="weekly">Weekly</option>
              <option value="monthly">Monthly</option>
            </Select>
            <Field label="Target cents" type="number" value={targetForm.targetInCents} onChange={(value) => setTargetForm((current) => ({ ...current, targetInCents: value }))} />
            <Field label="Saved cents" type="number" value={targetForm.savedInCents} onChange={(value) => setTargetForm((current) => ({ ...current, savedInCents: value }))} required={false} />
            <Button type="submit" variant="primary">
              Add target
            </Button>
          </form>
          <div className="mt-5 space-y-4">
            {targets.map((target) => (
              <TargetRow key={target.id} target={target} />
            ))}
          </div>
        </Panel>
      </section>

      {isLoading ? (
        <p className="text-sm text-slate-600 dark:text-slate-400">Loading budget workspace...</p>
      ) : null}
    </div>
  );
}

function BudgetRow({ budget, tone }: { budget: CategoryBudget; tone: string }) {
  const total = budget.allocatedAmountCents + budget.rolloverAmountCents;
  const percent = getPercent(budget.spentAmountCents, total);
  return (
    <article>
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="font-semibold text-slate-950 dark:text-slate-100">{budget.categoryName}</p>
          <p className="text-sm text-slate-600 dark:text-slate-400">
            <MoneyText amountInCents={budget.spentAmountCents} /> of <MoneyText amountInCents={total} />
          </p>
        </div>
        <StatusBadge tone={percent >= budget.warningThresholdPercent ? "warning" : "success"}>
          {percent}%
        </StatusBadge>
      </div>
      <Progress value={percent} tone={tone} />
    </article>
  );
}

function TargetRow({ target }: { target: Target }) {
  const percent = getPercent(target.savedInCents, target.targetInCents);
  return (
    <article className="border-t border-slate-200 pt-4 first:border-t-0 first:pt-0 dark:border-slate-700">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="font-semibold text-slate-950 dark:text-slate-100">{target.name}</p>
          <p className="text-sm text-slate-600 dark:text-slate-400">
            {target.cadence} goal
          </p>
        </div>
        <p className="text-sm font-bold text-slate-950 dark:text-slate-100">{percent}%</p>
      </div>
      <Progress value={percent} tone={target.cadence === "weekly" ? "cyan" : "amber"} />
    </article>
  );
}

function Metric({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="rounded-lg border border-white/10 bg-white/10 p-4">
      <p className="text-sm font-medium text-slate-300">{label}</p>
      <p className="mt-2 text-2xl font-bold">{value}</p>
    </div>
  );
}

function Progress({ value, tone }: { value: number; tone: string }) {
  const color = tone === "cyan" ? "bg-cyan-400" : tone === "amber" ? "bg-amber-300" : "bg-emerald-300";
  return (
    <div className="mt-3 h-2 rounded-full bg-slate-100 dark:bg-slate-800">
      <div className={classNames("h-2 rounded-full", color)} style={{ width: `${Math.min(100, Math.max(0, value))}%` }} />
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
  required = true
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
}) {
  return (
    <label className="block text-sm font-semibold text-slate-800 dark:text-slate-200">
      {label}
      <input
        className="mt-2 min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 text-base text-slate-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700 dark:border-slate-600 dark:bg-slate-950 dark:text-slate-100"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required={required}
        type={type}
      />
    </label>
  );
}

function Select({
  label,
  value,
  onChange,
  children
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  children: React.ReactNode;
}) {
  return (
    <label className="block text-sm font-semibold text-slate-800 dark:text-slate-200">
      {label}
      <select
        className="mt-2 min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 text-base text-slate-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700 dark:border-slate-600 dark:bg-slate-950 dark:text-slate-100"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      >
        {children}
      </select>
    </label>
  );
}

function createPeriodForm() {
  const start = new Date();
  const end = new Date();
  end.setDate(end.getDate() + 30);
  return {
    startDate: start.toISOString().slice(0, 10),
    endDate: end.toISOString().slice(0, 10),
    expectedIncomeCents: "",
    openingBalanceCents: "0"
  };
}

function createRecurringForm(categoryId = "") {
  return {
    categoryId,
    description: "",
    merchant: "",
    amountInCents: "",
    frequency: "monthly",
    dayOfMonth: "25",
    dayOfWeek: "Monday",
    nextOccurrenceDate: new Date().toISOString().slice(0, 10)
  };
}

function readTargets(): Target[] {
  try {
    const raw = window.localStorage.getItem(targetStorageKey);
    return raw
      ? (JSON.parse(raw) as Target[])
      : [
          { id: "weekly-buffer", name: "Weekly buffer", cadence: "weekly", targetInCents: 100000, savedInCents: 45000 },
          { id: "monthly-savings", name: "Monthly savings", cadence: "monthly", targetInCents: 350000, savedInCents: 210000 }
        ];
  } catch {
    return [];
  }
}

function getPercent(value: number, total: number) {
  if (total <= 0) {
    return 0;
  }

  return Math.round((value / total) * 100);
}

function createId() {
  return typeof crypto !== "undefined" && "randomUUID" in crypto
    ? crypto.randomUUID()
    : `target-${Date.now()}`;
}
