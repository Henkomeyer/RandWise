import type { ReactNode } from "react";

type SkipLinkProps = {
  children: ReactNode;
  targetId: string;
};

export function SkipLink({ children, targetId }: SkipLinkProps) {
  return (
    <a
      href={`#${targetId}`}
      className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-50 focus:rounded-md focus:bg-emerald-950 focus:px-4 focus:py-3 focus:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white"
    >
      {children}
    </a>
  );
}
