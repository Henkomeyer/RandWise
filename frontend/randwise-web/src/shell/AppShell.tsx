import { NavLink, Outlet } from "react-router";
import { useAuth } from "../auth/AuthContext";
import { PrivacyModeProvider } from "../privacy/PrivacyModeContext";
import { usePrivacyMode } from "../privacy/privacyModeState";
import { useThemeMode } from "../theme/themeState";
import { Button } from "../ui/Button";
import { SkipLink } from "../ui/SkipLink";
import { classNames } from "../ui/classNames";

const navItems = [
  { label: "Dashboard", to: "/dashboard" },
  { label: "Transactions", to: "/transactions" },
  { label: "Add", to: "/add" },
  { label: "Budget", to: "/budget" },
  { label: "More", to: "/more" }
];

export function AppShell() {
  return (
    <PrivacyModeProvider>
      <AppShellLayout />
    </PrivacyModeProvider>
  );
}

function AppShellLayout() {
  const { isPrivacyMode, togglePrivacyMode } = usePrivacyMode();
  const { isDarkMode, toggleThemeMode } = useThemeMode();
  const auth = useAuth();

  return (
    <div className="min-h-screen bg-slate-50 text-slate-950 dark:bg-[#07111f] dark:text-slate-100">
      <SkipLink targetId="main-content">Skip to main content</SkipLink>
      <div className="mx-auto flex min-h-screen w-full max-w-7xl flex-col md:flex-row">
        <aside className="hidden w-64 shrink-0 border-r border-slate-200 bg-white px-5 py-6 dark:border-slate-800 dark:bg-slate-950/70 md:block">
          <NavLink
            to="/dashboard"
            className="mb-7 inline-flex min-h-11 items-center rounded-md text-xl font-bold tracking-normal text-emerald-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-emerald-700 dark:text-emerald-200"
          >
            RandWise
          </NavLink>
          <div className="mb-6 rounded-lg border border-emerald-100 bg-emerald-50 p-3 dark:border-emerald-400/20 dark:bg-emerald-400/10">
            <p className="text-xs font-semibold uppercase tracking-normal text-emerald-800 dark:text-emerald-200">
              Level 4 Saver
            </p>
            <div className="mt-2 h-2 rounded-full bg-emerald-100 dark:bg-slate-800">
              <div className="h-2 w-3/4 rounded-full bg-emerald-600 dark:bg-emerald-300" />
            </div>
          </div>
          <nav aria-label="Primary navigation" className="space-y-1">
            {navItems.map((item) => (
              <NavItem key={item.to} {...item} />
            ))}
          </nav>
        </aside>

        <div className="flex min-w-0 flex-1 flex-col pb-20 md:pb-0">
          <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/90 px-4 py-3 backdrop-blur dark:border-slate-800 dark:bg-[#07111f]/90 md:px-8">
            <div className="flex min-h-11 items-center justify-between gap-4">
              <NavLink
                to="/dashboard"
                className="text-lg font-bold text-emerald-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-emerald-700 dark:text-emerald-200 md:hidden"
              >
                RandWise
              </NavLink>
              <div className="hidden md:block">
                <p className="text-sm font-medium text-slate-500 dark:text-slate-400">
                  Good afternoon
                </p>
                <p className="text-base font-semibold text-slate-900 dark:text-slate-100">
                  Your budget control centre
                </p>
              </div>
              <div className="flex flex-wrap items-center justify-end gap-2">
                <Button
                  aria-pressed={isDarkMode}
                  onClick={toggleThemeMode}
                  variant={isDarkMode ? "primary" : "secondary"}
                >
                  {isDarkMode ? "Dark" : "Light"}
                </Button>
                <Button
                  aria-pressed={isPrivacyMode}
                  onClick={togglePrivacyMode}
                  variant={isPrivacyMode ? "primary" : "secondary"}
                >
                  Privacy {isPrivacyMode ? "on" : "off"}
                </Button>
                <Button onClick={() => void auth.logout()} variant="ghost">
                  Sign out
                </Button>
              </div>
            </div>
          </header>

          <main id="main-content" className="flex-1 px-4 py-5 md:px-8 md:py-8">
            <Outlet />
          </main>
        </div>
      </div>

      <nav
        aria-label="Mobile navigation"
        className="fixed inset-x-0 bottom-0 z-20 grid grid-cols-5 border-t border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-950 md:hidden"
      >
        {navItems.map((item) => (
          <NavItem key={item.to} {...item} mobile />
        ))}
      </nav>
    </div>
  );
}

type NavItemProps = {
  label: string;
  to: string;
  mobile?: boolean;
};

function NavItem({ label, to, mobile = false }: NavItemProps) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        classNames(
          "flex min-h-11 items-center rounded-md text-sm font-semibold transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700",
          mobile ? "justify-center px-2 py-3" : "px-3 py-2.5",
          isActive
            ? "bg-emerald-950 text-white"
            : "text-slate-700 hover:bg-slate-100 hover:text-slate-950 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white"
        )
      }
      end={to === "/dashboard"}
    >
      {label}
    </NavLink>
  );
}
