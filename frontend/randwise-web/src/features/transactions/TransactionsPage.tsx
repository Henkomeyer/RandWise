import { FormEvent, useEffect, useMemo, useState } from "react";
import { api, type Transaction } from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { Button } from "../../ui/Button";
import { MoneyText } from "../../ui/MoneyText";
import { Panel } from "../../ui/Panel";
import { StatusBadge } from "../../ui/StatusBadge";

type TransactionFormState = {
  description: string;
  amountInCents: string;
  merchant: string;
  transactionDate: string;
  notes: string;
};

const emptyForm = (): TransactionFormState => ({
  description: "",
  amountInCents: "",
  merchant: "",
  transactionDate: new Date().toISOString().slice(0, 10),
  notes: ""
});

export function TransactionsPage() {
  const auth = useAuth();
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [form, setForm] = useState<TransactionFormState>(() => emptyForm());
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deletedTransaction, setDeletedTransaction] = useState<Transaction | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const editingTransaction = useMemo(
    () => transactions.find((transaction) => transaction.id === editingId) ?? null,
    [editingId, transactions]
  );

  useEffect(() => {
    let isActive = true;

    async function loadTransactions(accessToken: string) {
      setIsLoading(true);
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
    setIsSubmitting(true);
    try {
      if (editingTransaction) {
        const updated = await api.updateTransaction(auth.tokens.accessToken, editingTransaction.id, {
          amountInCents: Number(form.amountInCents),
          transactionType: editingTransaction.transactionType,
          categoryId: editingTransaction.categoryId,
          description: form.description,
          merchant: form.merchant || null,
          transactionDate: form.transactionDate,
          notes: form.notes || null
        });
        setTransactions((current) =>
          current.map((transaction) => (transaction.id === updated.id ? updated : transaction))
        );
      } else {
        const created = await api.createTransaction(auth.tokens.accessToken, {
          amountInCents: Number(form.amountInCents),
          transactionType: "expense",
          categoryId: null,
          description: form.description,
          merchant: form.merchant || null,
          transactionDate: form.transactionDate,
          source: "web"
        });
        setTransactions((current) => [created, ...current]);
      }

      clearForm();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not save transaction.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function deleteTransaction(transaction: Transaction) {
    if (!auth.tokens) {
      return;
    }

    setError(null);
    try {
      await api.deleteTransaction(auth.tokens.accessToken, transaction.id);
      setTransactions((current) => current.filter((item) => item.id !== transaction.id));
      setDeletedTransaction(transaction);
      if (editingId === transaction.id) {
        clearForm();
      }
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not delete transaction.");
    }
  }

  async function restoreDeletedTransaction() {
    if (!auth.tokens || !deletedTransaction) {
      return;
    }

    setError(null);
    try {
      const restored = await api.restoreTransaction(auth.tokens.accessToken, deletedTransaction.id);
      setTransactions((current) => [restored, ...current]);
      setDeletedTransaction(null);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not restore transaction.");
    }
  }

  function startEditing(transaction: Transaction) {
    setEditingId(transaction.id);
    setForm({
      description: transaction.description,
      amountInCents: transaction.amountInCents.toString(),
      merchant: transaction.merchant ?? "",
      transactionDate: transaction.transactionDate,
      notes: transaction.notes ?? ""
    });
  }

  function clearForm() {
    setEditingId(null);
    setForm(emptyForm());
  }

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_380px]">
      <section>
        <div className="flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-950 dark:text-slate-100">
              Transactions
            </h1>
            <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
              Add, edit, delete and restore web transactions.
            </p>
          </div>
          <StatusBadge tone="neutral">{transactions.length} active</StatusBadge>
        </div>

        {deletedTransaction ? (
          <div className="mt-4 flex flex-col gap-3 rounded-lg border border-amber-300 bg-amber-50 p-4 text-amber-950 dark:border-amber-300/30 dark:bg-amber-300/10 dark:text-amber-100 md:flex-row md:items-center md:justify-between">
            <p className="text-sm font-semibold">
              Deleted {deletedTransaction.description}.
            </p>
            <Button onClick={() => void restoreDeletedTransaction()} variant="secondary">
              Restore
            </Button>
          </div>
        ) : null}

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
                  className="flex flex-col gap-4 py-4 md:flex-row md:items-center md:justify-between"
                >
                  <div>
                    <p className="font-semibold text-slate-950 dark:text-slate-100">
                      {transaction.description}
                    </p>
                    <p className="text-sm text-slate-600 dark:text-slate-400">
                      {transaction.merchant ?? transaction.source} -{" "}
                      {transaction.transactionDate}
                    </p>
                    {transaction.notes ? (
                      <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
                        {transaction.notes}
                      </p>
                    ) : null}
                  </div>
                  <div className="flex flex-wrap items-center gap-2 md:justify-end">
                    <MoneyText
                      amountInCents={
                        transaction.transactionType === "expense"
                          ? -transaction.amountInCents
                          : transaction.amountInCents
                      }
                      className="min-w-20 font-bold text-slate-950 dark:text-slate-100 md:text-right"
                    />
                    <Button onClick={() => startEditing(transaction)} variant="secondary">
                      Edit
                    </Button>
                    <Button onClick={() => void deleteTransaction(transaction)} variant="ghost">
                      Delete
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </section>

      <Panel aria-labelledby="transaction-form-heading">
        <div className="flex items-start justify-between gap-3">
          <div>
            <h2
              id="transaction-form-heading"
              className="text-lg font-bold text-slate-950 dark:text-slate-100"
            >
              {editingTransaction ? "Edit transaction" : "Quick add"}
            </h2>
            <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
              {editingTransaction ? "Update the selected entry." : "Capture a new expense."}
            </p>
          </div>
          {editingTransaction ? <StatusBadge tone="warning">Editing</StatusBadge> : null}
        </div>
        <form className="mt-5 space-y-4" onSubmit={submit}>
          <Field
            label="Description"
            value={form.description}
            onChange={(value) => setForm((current) => ({ ...current, description: value }))}
          />
          <Field
            label="Amount in cents"
            value={form.amountInCents}
            onChange={(value) => setForm((current) => ({ ...current, amountInCents: value }))}
            type="number"
          />
          <Field
            label="Merchant"
            value={form.merchant}
            onChange={(value) => setForm((current) => ({ ...current, merchant: value }))}
            required={false}
          />
          <Field
            label="Transaction date"
            value={form.transactionDate}
            onChange={(value) => setForm((current) => ({ ...current, transactionDate: value }))}
            type="date"
          />
          {editingTransaction ? (
            <Field
              label="Notes"
              value={form.notes}
              onChange={(value) => setForm((current) => ({ ...current, notes: value }))}
              required={false}
            />
          ) : null}
          {error ? (
            <p className="text-sm font-medium text-red-700 dark:text-rose-300">{error}</p>
          ) : null}
          <div className="flex flex-wrap gap-2">
            <Button type="submit" variant="primary" disabled={isSubmitting}>
              {editingTransaction ? "Save changes" : "Add expense"}
            </Button>
            {editingTransaction ? (
              <Button onClick={clearForm} variant="ghost">
                Cancel
              </Button>
            ) : null}
          </div>
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
