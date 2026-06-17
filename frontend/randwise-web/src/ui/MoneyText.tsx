import type { HTMLAttributes } from "react";
import { usePrivacyMode } from "../privacy/privacyModeState";
import { classNames } from "./classNames";
import { formatRandCents } from "./moneyFormat";
import { VisuallyHidden } from "./VisuallyHidden";

type MoneyTextProps = HTMLAttributes<HTMLSpanElement> & {
  amountInCents: number;
  redact?: boolean;
};

export function MoneyText({
  amountInCents,
  className,
  redact = true,
  ...props
}: MoneyTextProps) {
  const { isPrivacyMode } = usePrivacyMode();
  const shouldRedact = redact && isPrivacyMode;

  if (shouldRedact) {
    return (
      <span
        aria-label="Amount hidden"
        className={classNames("font-mono tabular-nums", className)}
        {...props}
      >
        R****
        <VisuallyHidden> hidden</VisuallyHidden>
      </span>
    );
  }

  return (
    <span className={classNames("tabular-nums", className)} {...props}>
      {formatRandCents(amountInCents)}
    </span>
  );
}
