import { useEffect, useState } from "react";
import { dashboardFixture, type CategoryGroup, type DashboardSummary as DashboardViewModel, type SavingsTarget } from "./dashboardFixture";
import { api, type DashboardSummary as LiveDashboardSummary } from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { Button } from "../../ui/Button";
import { MoneyText } from "../../ui/MoneyText";
import { Panel } from "../../ui/Panel";
import { StatusBadge } from "../../ui/StatusBadge";
import { classNames } from "../../ui/classNames";

const accentClasses: Record<CategoryGroup["accent"], string> = {
  emerald: "bg-emerald-500 dark:bg-emerald-300",
  cyan: "bg-cyan-500 dark:bg-cyan-300",
  amber: "bg-amber-500 dark:bg-amber-300",
  rose: "bg-rose-500 dark:bg-rose-300"
};

export function DashboardPage() {
  const auth = useAuth();
  const [liveDashboard, setLiveDashboard] = useState<LiveDashboardSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const dashboard = liveDashboard ? toViewModel(liveDashboard) : dashboardFixture;
  const pointsPercent = getPercent(
    dashboard.gamePlan.points,
    dashboard.gamePlan.nextLevelPoints
  );

  useEffect(() => {
    if (!auth.tokens) {
      return;
    }

    let isActive = true;
    async function loadDashboard(accessToken: string) {
      try {
        const nextDashboard = await api.getDashboard(accessToken);
        if (isActive) {
          setLiveDashboard(nextDashboard);
          setError(null);
        }
      } catch (cause) {
        if (isActive) {
          setError(cause instanceof Error ? cause.message : "Could not load dashboard.");
        }
      }
    }

    void loadDashboard(auth.tokens.accessToken);

    return () => {
      isActive = false;
    };
  }, [auth.tokens]);

  return (
    <div className="space-y-5">
      <section className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-950 dark:text-slate-100">
            Dashboard
          </h1>
          <p className="mt-1 max-w-3xl text-sm leading-6 text-slate-600 dark:text-slate-400">
            Your financial control centre for safe spending, payday pressure and the next required action.
          </p>
        </div>
        <StatusBadge tone={error ? "warning" : "success"}>
          {error ? "Using fallback" : "Live plan"}
        </StatusBadge>
      </section>

      {error ? (
        <div className="rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm font-semibold text-amber-950 dark:border-amber-300/30 dark:bg-amber-300/10 dark:text-amber-100">
          {error}
        </div>
      ) : null}

      <section aria-labelledby="safe-to-spend-heading" className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_360px]">
        <Panel className="bg-white dark:bg-slate-900">
          <div>
            <div className="flex flex-wrap items-center gap-3">
              <h1
                id="safe-to-spend-heading"
                className="text-base font-semibold text-slate-600 dark:text-slate-300"
              >
                Safe to spend
              </h1>
              <StatusBadge tone="success">
                {dashboard.financialStatus.status === "onTrack"
                  ? "On track"
                  : dashboard.financialStatus.status}
              </StatusBadge>
            </div>
            <p className="mt-3 text-4xl font-bold tracking-normal text-slate-950 dark:text-slate-100 md:text-5xl">
              <MoneyText amountInCents={dashboard.safeToSpend.amountInCents} />
            </p>
            <p className="mt-3 max-w-2xl text-base leading-7 text-slate-600 dark:text-slate-300">
              <MoneyText amountInCents={dashboard.safeToSpend.dailyAmountInCents} /> per
              day for {dashboard.budgetPeriod.daysRemaining} days. Protected cash is{" "}
              <MoneyText amountInCents={dashboard.safeToSpend.protectedAmountInCents} />.
            </p>
          </div>
        </Panel>

        <Panel>
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-sm font-medium text-slate-500 dark:text-slate-400">Money Pulse</p>
                <p className="mt-1 text-3xl font-bold text-slate-950 dark:text-slate-100">{dashboard.financialStatus.moneyPulse}</p>
              </div>
              <div className="text-right">
                <p className="text-sm font-medium text-slate-500 dark:text-slate-400">
                  Level {dashboard.gamePlan.level}
                </p>
                <p className="mt-1 text-lg font-bold text-emerald-900 dark:text-emerald-200">
                  {dashboard.gamePlan.title}
                </p>
              </div>
            </div>
            <ProgressBar
              label="Level progress"
              value={pointsPercent}
              className="mt-4 bg-white/15"
              fillClassName="bg-emerald-300"
            />
            <p className="mt-3 text-sm text-slate-600 dark:text-slate-400">
              {dashboard.gamePlan.points} / {dashboard.gamePlan.nextLevelPoints} XP
            </p>
        </Panel>
      </section>

      <section aria-label="Budget status summary" className="grid gap-4 md:grid-cols-3">
        <StatusPanel
          title="Payday"
          value={`${dashboard.budgetPeriod.daysRemaining} days`}
          detail={`Current period ends ${dashboard.budgetPeriod.endDate}.`}
        />
        <StatusPanel
          title="Spending pace"
          value={`${dashboard.spending.spendingPercent}% used`}
          detail={`${dashboard.spending.expectedSpendingPercent}% of this period has passed.`}
        />
        <StatusPanel
          title="Streak"
          value={`${dashboard.gamePlan.streakDays} days`}
          detail="Daily check-ins and target progress are active."
        />
      </section>

      <section
        aria-labelledby="recommendation-heading"
        className="rounded-lg border border-amber-300 bg-amber-50 p-5 text-amber-950 dark:border-amber-300/30 dark:bg-amber-300/10 dark:text-amber-100"
      >
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <div>
            <h2 id="recommendation-heading" className="text-lg font-bold">
              {dashboard.recommendedAction.title}
            </h2>
            <p className="mt-2 text-sm leading-6">{dashboard.recommendedAction.message}</p>
          </div>
          <Button variant="secondary">Set weekly cap</Button>
        </div>
      </section>

      <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_420px]">
        <Panel aria-labelledby="cash-flow-heading">
          <h2 id="cash-flow-heading" className="text-xl font-bold text-slate-950 dark:text-slate-100">
            Cash-flow forecast
          </h2>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Projected balance after protected commitments.
          </p>
          <div className="mt-5 grid gap-2">
            {liveDashboard?.cashFlowForecast.slice(0, 7).map((point) => (
              <div key={point.date} className="grid grid-cols-[96px_minmax(0,1fr)_auto] items-center gap-3 text-sm">
                <span className="font-medium text-slate-600 dark:text-slate-400">{point.date.slice(5)}</span>
                <div className="h-2 rounded-full bg-slate-100 dark:bg-slate-800">
                  <div
                    className="h-2 rounded-full bg-emerald-600 dark:bg-emerald-300"
                    style={{ width: `${Math.min(100, Math.max(8, getPercent(point.projectedBalanceInCents, dashboard.safeToSpend.availableCashInCents)))}%` }}
                  />
                </div>
                <MoneyText amountInCents={point.projectedBalanceInCents} className="font-semibold text-slate-950 dark:text-slate-100" />
              </div>
            )) ?? (
              <p className="text-sm text-slate-600 dark:text-slate-400">
                Forecast appears after the live dashboard loads.
              </p>
            )}
          </div>
        </Panel>

        <Panel aria-labelledby="commitments-heading">
          <h2 id="commitments-heading" className="text-xl font-bold text-slate-950 dark:text-slate-100">
            Upcoming commitments
          </h2>
          <div className="mt-4 divide-y divide-slate-200 dark:divide-slate-800">
            {liveDashboard?.upcomingCommitments.length ? (
              liveDashboard.upcomingCommitments.map((commitment) => (
                <div key={commitment.id} className="flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0">
                  <div>
                    <p className="font-semibold text-slate-950 dark:text-slate-100">{commitment.description}</p>
                    <p className="text-sm text-slate-600 dark:text-slate-400">Due {commitment.dueDate}</p>
                  </div>
                  <MoneyText amountInCents={commitment.amountInCents} className="font-bold text-slate-950 dark:text-slate-100" />
                </div>
              ))
            ) : (
              <p className="text-sm text-slate-600 dark:text-slate-400">No protected commitments before payday.</p>
            )}
          </div>
        </Panel>
      </section>

      <section aria-labelledby="category-groups-heading">
        <div className="flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
          <div>
            <h2
              id="category-groups-heading"
              className="text-xl font-bold text-slate-950 dark:text-slate-100"
            >
              Category groups
            </h2>
            <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
              Planned categories grouped by how they affect this period.
            </p>
          </div>
          <Button variant="ghost">Manage groups</Button>
        </div>
        <div className="mt-4 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {dashboard.categoryGroups.map((group) => (
            <CategoryGroupPanel key={group.id} group={group} />
          ))}
        </div>
      </section>

      <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_360px]">
        <Panel aria-labelledby="targets-heading">
          <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
            <div>
              <h2
                id="targets-heading"
                className="text-xl font-bold text-slate-950 dark:text-slate-100"
              >
                Savings targets
              </h2>
              <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
                Weekly and monthly goals with visible progress.
              </p>
            </div>
            <StatusBadge tone="success">3 active</StatusBadge>
          </div>
          <div className="mt-5 space-y-5">
            {dashboard.savingsTargets.map((target) => (
              <SavingsTargetRow key={target.id} target={target} />
            ))}
          </div>
        </Panel>

        <Panel aria-labelledby="badges-heading">
          <h2 id="badges-heading" className="text-xl font-bold text-slate-950 dark:text-slate-100">
            Achievements
          </h2>
          <div className="mt-4 grid gap-3">
            {dashboard.gamePlan.badges.map((badge) => (
              <div
                key={badge}
                className="flex min-h-11 items-center justify-between rounded-lg border border-slate-200 px-3 text-sm font-semibold text-slate-800 dark:border-slate-700 dark:text-slate-100"
              >
                <span>{badge}</span>
                <StatusBadge tone="neutral">Earned</StatusBadge>
              </div>
            ))}
          </div>
          <Button className="mt-5 w-full" variant="primary">
            Add target
          </Button>
        </Panel>
      </section>

      <section className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_420px]">
        <Panel aria-labelledby="recent-heading">
          <h2 id="recent-heading" className="text-xl font-bold text-slate-950 dark:text-slate-100">
            Recent transactions
          </h2>
          <div className="mt-4 divide-y divide-slate-200 dark:divide-slate-800">
            {liveDashboard?.recentTransactions.length ? (
              liveDashboard.recentTransactions.map((transaction) => (
                <div key={transaction.id} className="flex items-center justify-between gap-3 py-3 first:pt-0 last:pb-0">
                  <div>
                    <p className="font-semibold text-slate-950 dark:text-slate-100">{transaction.description}</p>
                    <p className="text-sm text-slate-600 dark:text-slate-400">
                      {transaction.categoryName} - {transaction.transactionDate}
                    </p>
                  </div>
                  <MoneyText
                    amountInCents={transaction.transactionType === "expense" ? -transaction.amountInCents : transaction.amountInCents}
                    className="font-bold text-slate-950 dark:text-slate-100"
                  />
                </div>
              ))
            ) : (
              <p className="text-sm text-slate-600 dark:text-slate-400">No recent transactions yet.</p>
            )}
          </div>
        </Panel>

        <Panel aria-labelledby="insights-heading">
          <h2 id="insights-heading" className="text-xl font-bold text-slate-950 dark:text-slate-100">
            Insights
          </h2>
          <div className="mt-4 space-y-4">
            {(liveDashboard?.insights.length ? liveDashboard.insights : [
              { type: "fixture", title: "Budget is ready", message: "Add more live transactions to unlock stronger dashboard insights." }
            ]).map((insight) => (
              <article key={`${insight.type}-${insight.title}`} className="rounded-md border border-slate-200 p-3 dark:border-slate-800">
                <h3 className="font-semibold text-slate-950 dark:text-slate-100">{insight.title}</h3>
                <p className="mt-1 text-sm leading-6 text-slate-600 dark:text-slate-400">{insight.message}</p>
              </article>
            ))}
          </div>
        </Panel>
      </section>
    </div>
  );
}

function toViewModel(liveDashboard: LiveDashboardSummary): DashboardViewModel {
  const categoryGroups = liveDashboard.categories.map((category, index) => ({
    id: category.categoryId,
    name: category.name,
    allocatedInCents: category.allocatedInCents,
    spentInCents: category.spentInCents,
    accent: (["emerald", "cyan", "amber", "rose"][index % 4] ?? "emerald") as CategoryGroup["accent"],
    categories: [category.latestTransaction ?? category.status]
  }));

  return {
    generatedUtc: liveDashboard.generatedUtc,
    budgetPeriod: liveDashboard.budgetPeriod,
    financialStatus: {
      status: liveDashboard.financialStatus.status as DashboardViewModel["financialStatus"]["status"],
      message: liveDashboard.financialStatus.message,
      moneyPulse: liveDashboard.financialStatus.moneyPulse
    },
    safeToSpend: liveDashboard.safeToSpend,
    spending: liveDashboard.spending,
    recommendedAction: liveDashboard.recommendedAction,
    categoryGroups,
    savingsTargets: dashboardFixture.savingsTargets,
    gamePlan: {
      level: Math.max(1, Math.floor(liveDashboard.financialStatus.moneyPulse / 20)),
      title: liveDashboard.financialStatus.status === "comfortable" ? "Comfortable" : "Saver",
      streakDays: dashboardFixture.gamePlan.streakDays,
      points: Math.min(1000, liveDashboard.financialStatus.moneyPulse * 10),
      nextLevelPoints: 1000,
      badges: liveDashboard.insights.map((insight) => insight.title).slice(0, 3)
    }
  };
}

type StatusPanelProps = {
  title: string;
  value: string;
  detail: string;
};

function StatusPanel({ title, value, detail }: StatusPanelProps) {
  return (
    <Panel as="article">
      <h2 className="text-sm font-semibold text-slate-500 dark:text-slate-400">{title}</h2>
      <p className="mt-2 text-2xl font-bold text-slate-950 dark:text-slate-100">{value}</p>
      <p className="mt-2 text-sm leading-6 text-slate-600 dark:text-slate-400">{detail}</p>
    </Panel>
  );
}

function CategoryGroupPanel({ group }: { group: CategoryGroup }) {
  const spentPercent = getPercent(group.spentInCents, group.allocatedInCents);
  const remainingInCents = group.allocatedInCents - group.spentInCents;
  const tone = spentPercent > 85 ? "warning" : "success";

  return (
    <Panel as="article">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h3 className="text-base font-bold text-slate-950 dark:text-slate-100">{group.name}</h3>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            {group.categories.join(", ")}
          </p>
        </div>
        <StatusBadge tone={tone}>{spentPercent}%</StatusBadge>
      </div>
      <ProgressBar
        label={`${group.name} budget used`}
        value={spentPercent}
        className="mt-4"
        fillClassName={accentClasses[group.accent]}
      />
      <div className="mt-4 grid grid-cols-2 gap-3 text-sm">
        <div>
          <p className="font-medium text-slate-500 dark:text-slate-400">Spent</p>
          <p className="mt-1 font-bold text-slate-950 dark:text-slate-100">
            <MoneyText amountInCents={group.spentInCents} />
          </p>
        </div>
        <div>
          <p className="font-medium text-slate-500 dark:text-slate-400">Left</p>
          <p className="mt-1 font-bold text-slate-950 dark:text-slate-100">
            <MoneyText amountInCents={remainingInCents} />
          </p>
        </div>
      </div>
    </Panel>
  );
}

function SavingsTargetRow({ target }: { target: SavingsTarget }) {
  const progress = getPercent(target.savedInCents, target.targetInCents);

  return (
    <article className="border-t border-slate-200 pt-5 first:border-t-0 first:pt-0 dark:border-slate-700">
      <div className="flex flex-col gap-2 md:flex-row md:items-start md:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-bold text-slate-950 dark:text-slate-100">{target.name}</h3>
            <StatusBadge tone={target.cadence === "weekly" ? "warning" : "neutral"}>
              {target.cadence}
            </StatusBadge>
          </div>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Due {target.dueDate} - {target.reward}
          </p>
        </div>
        <p className="text-sm font-bold text-slate-950 dark:text-slate-100">
          <MoneyText amountInCents={target.savedInCents} /> /{" "}
          <MoneyText amountInCents={target.targetInCents} />
        </p>
      </div>
      <ProgressBar label={`${target.name} progress`} value={progress} className="mt-3" />
    </article>
  );
}

function ProgressBar({
  label,
  value,
  className,
  fillClassName = "bg-emerald-600 dark:bg-emerald-300"
}: {
  label: string;
  value: number;
  className?: string;
  fillClassName?: string;
}) {
  const width = `${Math.min(Math.max(value, 0), 100)}%`;

  return (
    <div
      aria-label={label}
      aria-valuemax={100}
      aria-valuemin={0}
      aria-valuenow={value}
      className={classNames("h-2 rounded-full bg-slate-100 dark:bg-slate-800", className)}
      role="progressbar"
    >
      <div className={classNames("h-2 rounded-full", fillClassName)} style={{ width }} />
    </div>
  );
}

function getPercent(value: number, total: number) {
  if (total <= 0) {
    return 0;
  }

  return Math.round((value / total) * 100);
}
