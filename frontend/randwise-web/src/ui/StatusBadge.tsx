import type { HTMLAttributes, ReactNode } from "react";
import { classNames } from "./classNames";

type StatusTone = "success" | "warning" | "danger" | "neutral";

type StatusBadgeProps = HTMLAttributes<HTMLSpanElement> & {
  children: ReactNode;
  tone?: StatusTone;
};

const toneClasses: Record<StatusTone, string> = {
  success:
    "border-emerald-200 bg-emerald-50 text-emerald-800 dark:border-emerald-400/40 dark:bg-emerald-400/10 dark:text-emerald-200",
  warning:
    "border-amber-200 bg-amber-50 text-amber-900 dark:border-amber-300/40 dark:bg-amber-300/10 dark:text-amber-100",
  danger:
    "border-rose-200 bg-rose-50 text-rose-900 dark:border-rose-300/40 dark:bg-rose-300/10 dark:text-rose-100",
  neutral:
    "border-slate-200 bg-slate-100 text-slate-700 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200"
};

export function StatusBadge({
  children,
  className,
  tone = "neutral",
  ...props
}: StatusBadgeProps) {
  return (
    <span
      className={classNames(
        "inline-flex min-h-6 items-center rounded-md border px-2 text-xs font-semibold",
        toneClasses[tone],
        className
      )}
      {...props}
    >
      {children}
    </span>
  );
}
