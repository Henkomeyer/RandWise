import { Link } from "react-router";
import { Panel } from "../../ui/Panel";

type PlaceholderRouteProps = {
  title: string;
  intent: string;
};

export function PlaceholderRoute({ title, intent }: PlaceholderRouteProps) {
  return (
    <Panel aria-labelledby={`${title.toLowerCase()}-route-heading`}>
      <div className="max-w-2xl">
        <h1
          id={`${title.toLowerCase()}-route-heading`}
          className="text-2xl font-bold text-slate-950"
        >
          {title}
        </h1>
        <p className="mt-3 text-sm leading-6 text-slate-600">{intent}</p>
        <Link
          to="/dashboard"
          className="mt-5 inline-flex min-h-11 items-center rounded-md border border-slate-300 bg-white px-4 text-sm font-semibold text-slate-800 shadow-sm transition hover:border-emerald-700 hover:text-emerald-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700"
        >
          Back to dashboard
        </Link>
      </div>
    </Panel>
  );
}
