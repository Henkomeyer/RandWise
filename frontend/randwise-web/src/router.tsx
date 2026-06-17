import { createBrowserRouter, Navigate } from "react-router";
import { RequireAuth } from "./auth/RequireAuth";
import { AppShell } from "./shell/AppShell";
import { LoginPage, RegisterPage } from "./features/auth/AuthPages";
import { DashboardPage } from "./features/dashboard/DashboardPage";
import { BudgetPage } from "./features/budget/BudgetPage";
import { OnboardingPage } from "./features/onboarding/OnboardingPage";
import { PlaceholderRoute } from "./features/placeholder/PlaceholderRoute";
import { TransactionsPage } from "./features/transactions/TransactionsPage";

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  { path: "/register", element: <RegisterPage /> },
  {
    path: "/",
    element: <RequireAuth />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: <Navigate to="/dashboard" replace /> },
          { path: "dashboard", element: <DashboardPage /> },
          { path: "onboarding", element: <OnboardingPage /> },
          { path: "transactions", element: <TransactionsPage /> },
          { path: "add", element: <TransactionsPage /> },
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
    ]
  }
]);
