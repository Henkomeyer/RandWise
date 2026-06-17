import type { ButtonHTMLAttributes, ReactNode } from "react";
import { classNames } from "./classNames";

type ButtonVariant = "primary" | "secondary" | "ghost";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  children: ReactNode;
  variant?: ButtonVariant;
};

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    "border-emerald-900 bg-emerald-900 text-white hover:bg-emerald-800 dark:border-emerald-300 dark:bg-emerald-300 dark:text-slate-950 dark:hover:bg-emerald-200",
  secondary:
    "border-slate-300 bg-white text-slate-800 shadow-sm hover:border-emerald-700 hover:text-emerald-950 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:hover:border-emerald-300 dark:hover:text-emerald-100",
  ghost:
    "border-transparent bg-transparent text-slate-700 hover:bg-slate-100 hover:text-slate-950 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white"
};

export function Button({
  children,
  className,
  type = "button",
  variant = "secondary",
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      className={classNames(
        "inline-flex min-h-11 items-center justify-center gap-2 rounded-md border px-4 text-sm font-semibold transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700 disabled:cursor-not-allowed disabled:opacity-60",
        variantClasses[variant],
        className
      )}
      {...props}
    >
      {children}
    </button>
  );
}
