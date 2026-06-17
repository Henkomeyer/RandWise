import { FormEvent, useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { api, type FinancialProfile } from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { Button } from "../../ui/Button";
import { Panel } from "../../ui/Panel";

type FormState = {
  defaultMonthlyIncomeCents: string;
  paydayDay: string;
  startingBalanceCents: string;
  safetyBufferCents: string;
  savingsCommitmentCents: string;
};

const initialState: FormState = {
  defaultMonthlyIncomeCents: "2500000",
  paydayDay: "25",
  startingBalanceCents: "0",
  safetyBufferCents: "50000",
  savingsCommitmentCents: "0"
};

export function OnboardingPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState<FormState>(initialState);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (!auth.tokens) {
      return;
    }

    api
      .getFinancialProfile(auth.tokens.accessToken)
      .then((profile) => setForm(toForm(profile)))
      .catch(() => undefined);
  }, [auth.tokens]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!auth.tokens) {
      return;
    }

    setError(null);
    setIsSubmitting(true);
    try {
      await api.updateFinancialProfile(auth.tokens.accessToken, {
        defaultMonthlyIncomeCents: Number(form.defaultMonthlyIncomeCents),
        paydayDay: form.paydayDay ? Number(form.paydayDay) : null,
        budgetCycleType: "paydayToPayday",
        startingBalanceCents: Number(form.startingBalanceCents),
        safetyBufferCents: Number(form.safetyBufferCents),
        savingsCommitmentCents: Number(form.savingsCommitmentCents),
        notificationMode: "confirm",
        firstDayOfWeek: "Monday"
      });
      navigate("/transactions", { replace: true });
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not save onboarding.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <h1 className="text-2xl font-bold text-slate-950">Financial profile</h1>
      <Panel className="mt-5">
        <form className="grid gap-4 sm:grid-cols-2" onSubmit={submit}>
          <NumberField
            label="Monthly income in cents"
            value={form.defaultMonthlyIncomeCents}
            onChange={(value) => setForm({ ...form, defaultMonthlyIncomeCents: value })}
          />
          <NumberField
            label="Payday"
            value={form.paydayDay}
            onChange={(value) => setForm({ ...form, paydayDay: value })}
          />
          <NumberField
            label="Starting balance in cents"
            value={form.startingBalanceCents}
            onChange={(value) => setForm({ ...form, startingBalanceCents: value })}
          />
          <NumberField
            label="Safety buffer in cents"
            value={form.safetyBufferCents}
            onChange={(value) => setForm({ ...form, safetyBufferCents: value })}
          />
          <NumberField
            label="Savings commitment in cents"
            value={form.savingsCommitmentCents}
            onChange={(value) => setForm({ ...form, savingsCommitmentCents: value })}
          />
          <div className="sm:col-span-2">
            {error ? <p className="mb-3 text-sm font-medium text-red-700">{error}</p> : null}
            <Button type="submit" variant="primary" disabled={isSubmitting}>
              Save profile
            </Button>
          </div>
        </form>
      </Panel>
    </div>
  );
}

function NumberField({
  label,
  value,
  onChange
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <label className="block text-sm font-semibold text-slate-800">
      {label}
      <input
        className="mt-2 min-h-11 w-full rounded-md border border-slate-300 px-3 text-base focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700"
        inputMode="numeric"
        min="0"
        required
        type="number"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}

function toForm(profile: FinancialProfile): FormState {
  return {
    defaultMonthlyIncomeCents: String(profile.defaultMonthlyIncomeCents),
    paydayDay: profile.paydayDay ? String(profile.paydayDay) : "",
    startingBalanceCents: String(profile.startingBalanceCents),
    safetyBufferCents: String(profile.safetyBufferCents),
    savingsCommitmentCents: String(profile.savingsCommitmentCents)
  };
}
