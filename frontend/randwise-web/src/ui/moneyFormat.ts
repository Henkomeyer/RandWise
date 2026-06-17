const randFormatter = new Intl.NumberFormat("en-US", {
  maximumFractionDigits: 0
});

export function formatRandCents(amountInCents: number) {
  const sign = amountInCents < 0 ? "-" : "";
  return `${sign}R${randFormatter.format(Math.abs(amountInCents) / 100)}`;
}
