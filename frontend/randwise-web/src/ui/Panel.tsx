import type { HTMLAttributes, ReactNode } from "react";
import { classNames } from "./classNames";

type PanelProps = HTMLAttributes<HTMLElement> & {
  as?: "article" | "section" | "div";
  children: ReactNode;
};

export function Panel({
  as: Element = "section",
  children,
  className,
  ...props
}: PanelProps) {
  return (
    <Element
      className={classNames(
        "rounded-lg border border-slate-200 bg-white p-5 shadow-sm shadow-slate-200/60 dark:border-slate-800 dark:bg-slate-900 dark:shadow-none",
        className
      )}
      {...props}
    >
      {children}
    </Element>
  );
}
