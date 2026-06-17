import type { HTMLAttributes, ReactNode } from "react";
import { classNames } from "./classNames";

type VisuallyHiddenProps = HTMLAttributes<HTMLSpanElement> & {
  children: ReactNode;
};

export function VisuallyHidden({
  children,
  className,
  ...props
}: VisuallyHiddenProps) {
  return (
    <span className={classNames("sr-only", className)} {...props}>
      {children}
    </span>
  );
}
