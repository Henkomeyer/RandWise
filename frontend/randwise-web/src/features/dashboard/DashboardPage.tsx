import { dashboardFixture, type CategoryGroup, type SavingsTarget } from "./dashboardFixture";
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
  const dashboard = dashboardFixture;
  const pointsPercent = getPercent(
    dashboard.gamePlan.points,
    dashboard.gamePlan.nextLevelPoints
  );

  return (
    <div className="space-y-5">
      <section
        aria-labelledby="safe-to-spend-heading"
        className="overflow-hidden rounded-lg border border-emerald-900/10 bg-slate-950 text-white shadow-sm dark:border-emerald-300/20 dark:bg-slate-950"
      >
        <div className="grid gap-5 p-5 md:p-7 lg:grid-cols-[minmax(0,1fr)_340px] lg:items-end">
          <div>
            <div className="flex flex-wrap items-center gap-3">
              <h1
                id="safe-to-spend-heading"
                className="text-base font-semibold text-emerald-100"
              >
                Safe to spend
              </h1>
              <StatusBadge tone="success">
                {dashboard.financialStatus.status === "onTrack"
                  ? "On track"
                  : dashboard.financialStatus.status}
              </StatusBadge>
            </div>
            <p className="mt-3 text-4xl font-bold tracking-normal md:text-5xl">
              <MoneyText amountInCents={dashboard.safeToSpend.amountInCents} />
            </p>
            <p className="mt-3 max-w-2xl text-base leading-7 text-emerald-50">
              <MoneyText amountInCents={dashboard.safeToSpend.dailyAmountInCents} /> per
              day for {dashboard.budgetPeriod.daysRemaining} days. Protected cash is{" "}
              <MoneyText amountInCents={dashboard.safeToSpend.protectedAmountInCents} />.
            </p>
          </div>

          <div className="rounded-lg border border-white/10 bg-white/10 p-4">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-sm font-medium text-slate-300">Money Pulse</p>
                <p className="mt-1 text-3xl font-bold">{dashboard.financialStatus.moneyPulse}</p>
              </div>
              <div className="text-right">
                <p className="text-sm font-medium text-slate-300">
                  Level {dashboard.gamePlan.level}
                </p>
                <p className="mt-1 text-lg font-bold text-emerald-100">
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
            <p className="mt-3 text-sm text-slate-300">
              {dashboard.gamePlan.points} / {dashboard.gamePlan.nextLevelPoints} XP
            </p>
          </div>
        </div>
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
    </div>
  );
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
