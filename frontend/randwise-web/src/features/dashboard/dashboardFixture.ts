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
    status: "onTrack" | "comfortable" | "watchSpending" | "budgetPressure";
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
  categoryGroups: CategoryGroup[];
  savingsTargets: SavingsTarget[];
  gamePlan: {
    level: number;
    title: string;
    streakDays: number;
    points: number;
    nextLevelPoints: number;
    badges: string[];
  };
};

export type CategoryGroup = {
  id: string;
  name: string;
  allocatedInCents: number;
  spentInCents: number;
  accent: "emerald" | "cyan" | "amber" | "rose";
  categories: string[];
};

export type SavingsTarget = {
  id: string;
  name: string;
  cadence: "weekly" | "monthly";
  targetInCents: number;
  savedInCents: number;
  dueDate: string;
  reward: string;
};

export const dashboardFixture: DashboardSummary = {
  generatedUtc: "2026-06-14T11:30:00Z",
  budgetPeriod: {
    id: "period-id",
    startDate: "2026-05-25",
    endDate: "2026-06-24",
    daysRemaining: 10,
    periodProgressPercent: 67
  },
  financialStatus: {
    status: "onTrack",
    message: "You are currently on track.",
    moneyPulse: 74
  },
  safeToSpend: {
    amountInCents: 284000,
    dailyAmountInCents: 28400,
    availableCashInCents: 610000,
    protectedAmountInCents: 326000,
    safetyBufferInCents: 50000
  },
  spending: {
    spentThisPeriodInCents: 842000,
    spendingPercent: 58,
    expectedSpendingPercent: 67
  },
  recommendedAction: {
    type: "categoryWarning",
    title: "Slow down on takeaways",
    message: "Keep takeaway spending below R120 this week to remain on track."
  },
  categoryGroups: [
    {
      id: "essentials",
      name: "Essentials",
      allocatedInCents: 760000,
      spentInCents: 482000,
      accent: "emerald",
      categories: ["Rent", "Groceries", "Transport"]
    },
    {
      id: "lifestyle",
      name: "Lifestyle",
      allocatedInCents: 240000,
      spentInCents: 178000,
      accent: "amber",
      categories: ["Takeaways", "Entertainment", "Coffee"]
    },
    {
      id: "growth",
      name: "Growth",
      allocatedInCents: 180000,
      spentInCents: 112000,
      accent: "cyan",
      categories: ["Savings", "Courses", "Tools"]
    },
    {
      id: "debt",
      name: "Debt",
      allocatedInCents: 150000,
      spentInCents: 70000,
      accent: "rose",
      categories: ["Credit card", "Store account"]
    }
  ],
  savingsTargets: [
    {
      id: "weekly-buffer",
      name: "Weekly buffer",
      cadence: "weekly",
      targetInCents: 75000,
      savedInCents: 52000,
      dueDate: "2026-06-21",
      reward: "+120 XP"
    },
    {
      id: "month-save",
      name: "June savings",
      cadence: "monthly",
      targetInCents: 220000,
      savedInCents: 154000,
      dueDate: "2026-06-30",
      reward: "Level boost"
    },
    {
      id: "emergency-fund",
      name: "Emergency fund",
      cadence: "monthly",
      targetInCents: 500000,
      savedInCents: 326000,
      dueDate: "2026-08-31",
      reward: "Shield badge"
    }
  ],
  gamePlan: {
    level: 4,
    title: "Saver",
    streakDays: 12,
    points: 740,
    nextLevelPoints: 1000,
    badges: ["No-spend streak", "Buffer builder", "Pace keeper"]
  }
};
