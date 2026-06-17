import { FormEvent, useEffect, useState } from "react";
import { api, type Transaction } from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { Button } from "../../ui/Button";
import { MoneyText } from "../../ui/MoneyText";
import { Panel } from "../../ui/Panel";

export function TransactionsPage() {
  const auth = useAuth();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [description, setDescription] = useState("");
  const [amountInCents, setAmountInCents] = useState("");
  const [merchant, setMerchant] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    let isActive = true;

    async function loadTransactions(accessToken: string) {
      try {
        const page = await api.listTransactions(accessToken);
        if (isActive) {
          setTransactions(page.items);
        }
      } catch (cause) {
        if (isActive) {
          setError(cause instanceof Error ? cause.message : "Could not load transactions.");
        }
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    }

    if (!auth.tokens) {
      return;
    }

    void loadTransactions(auth.tokens.accessToken);

    return () => {
      isActive = false;
    };
  }, [auth.tokens]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!auth.tokens) {
      return;
    }

    setError(null);
    try {
      const created = await api.createTransaction(auth.tokens.accessToken, {
        amountInCents: Number(amountInCents),
        transactionType: "expense",
        categoryId: null,
        description,
        merchant: merchant || null,
        transactionDate: new Date().toISOString().slice(0, 10),
        source: "web"
      });
      setTransactions([created, ...transactions]);
      setDescription("");
      setAmountInCents("");
      setMerchant("");
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not add transaction.");
    }
  }

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_360px]">
      <section>
        <h1 className="text-2xl font-bold text-slate-950 dark:text-slate-100">
          Transactions
        </h1>
        <Panel className="mt-5">
          {isLoading ? (
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Loading transactions...
            </p>
          ) : transactions.length === 0 ? (
            <p className="text-sm text-slate-600 dark:text-slate-400">
              No transactions yet.
            </p>
          ) : (
            <ul className="divide-y divide-slate-200 dark:divide-slate-700">
              {transactions.map((transaction) => (
                <li
                  key={transaction.id}
                  className="flex items-center justify-between gap-4 py-4"
                >
                  <div>
                    <p className="font-semibold text-slate-950 dark:text-slate-100">
                      {transaction.description}
                    </p>
                    <p className="text-sm text-slate-600 dark:text-slate-400">
                      {transaction.merchant ?? transaction.source} -{" "}
                      {transaction.transactionDate}
                    </p>
                  </div>
                  <MoneyText
                    amountInCents={
                      transaction.transactionType === "expense"
                        ? -transaction.amountInCents
                        : transaction.amountInCents
                    }
                    className="font-bold text-slate-950 dark:text-slate-100"
                  />
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </section>

      <Panel aria-labelledby="quick-add-heading">
        <h2
          id="quick-add-heading"
          className="text-lg font-bold text-slate-950 dark:text-slate-100"
        >
          Quick add
        </h2>
        <form className="mt-5 space-y-4" onSubmit={submit}>
          <Field label="Description" value={description} onChange={setDescription} />
          <Field
            label="Amount in cents"
            value={amountInCents}
            onChange={setAmountInCents}
            type="number"
          />
          <Field label="Merchant" value={merchant} onChange={setMerchant} required={false} />
          {error ? (
            <p className="text-sm font-medium text-red-700 dark:text-rose-300">{error}</p>
          ) : null}
          <Button type="submit" variant="primary">
            Add expense
          </Button>
        </form>
      </Panel>
    </div>
  );
}

function Field({
  label,
  value,
  onChange,
  type = "text",
  required = true
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  required?: boolean;
}) {
  return (
    <label className="block text-sm font-semibold text-slate-800 dark:text-slate-200">
      {label}
      <input
        className="mt-2 min-h-11 w-full rounded-md border border-slate-300 bg-white px-3 text-base text-slate-950 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-emerald-700 dark:border-slate-600 dark:bg-slate-950 dark:text-slate-100"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        required={required}
        type={type}
      />
    </label>
  );
}
