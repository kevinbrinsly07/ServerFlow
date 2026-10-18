
"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";

import {
  API_URL,
  apiFetch,
  getToken,
  getUser,
  logout,
  type AuthUser,
} from "@/lib/auth";

type AuditLog = {
  id: string;
  userId: string | null;
  actorEmail: string;
  action: string;
  entityType: string;
  entityId: string;
  description: string;
  createdAtUtc: string;
};

type AuditLogsResponse = {
  items: AuditLog[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

const PAGE_SIZE = 20;

function formatDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString("en-LK", {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

function ActionBadge({ action }: { action: string }) {
  const styles: Record<string, string> = {
    Created: "border-emerald-800 bg-emerald-950 text-emerald-400",
    Updated: "border-sky-800 bg-sky-950 text-sky-400",
    Deleted: "border-red-900 bg-red-950 text-red-400",
  };

  const style =
    styles[action] ?? "border-slate-700 bg-slate-800 text-slate-300";

  return (
    <span
      className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${style}`}
    >
      {action}
    </span>
  );
}

export default function AuditLogsPage() {
  const router = useRouter();

  const [authChecked, setAuthChecked] = useState(false);
  const [currentUser, setCurrentUser] = useState<AuthUser | null>(null);

  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [refreshKey, setRefreshKey] = useState(0);

  const [search, setSearch] = useState("");
  const [actionFilter, setActionFilter] = useState("");
  const [entityFilter, setEntityFilter] = useState("");
  const [page, setPage] = useState(1);

  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  const isAdmin = currentUser?.roles?.includes("Admin") ?? false;

  useEffect(() => {
    const token = getToken();
    const user = getUser();

    if (!token || !user) {
      router.replace("/login");
      setAuthChecked(true);
      setLoading(false);
      return;
    }

    setCurrentUser(user);
    setAuthChecked(true);
    setLoading(false);
  }, [router]);

  const loadAuditLogs = useCallback(
    async (signal: AbortSignal) => {
      setLoading(true);
      setError("");

      try {
        const params = new URLSearchParams({
          page: String(page),
          pageSize: String(PAGE_SIZE),
        });

        if (search.trim()) {
          params.set("search", search.trim());
        }

        if (actionFilter) {
          params.set("action", actionFilter);
        }

        if (entityFilter) {
          params.set("entityType", entityFilter);
        }

        const response = await apiFetch(
          `${API_URL}/api/auditlogs?${params.toString()}`,
          {
            cache: "no-store",
            signal,
          },
        );

        if (!response.ok) {
          if (response.status === 403) {
            throw new Error(
              "Access denied. Only administrators can view audit history.",
            );
          }

          throw new Error(
            `Could not load audit history. API returned ${response.status}.`,
          );
        }

        const data: unknown = await response.json();

        if (
          typeof data !== "object" ||
          data === null ||
          !("items" in data) ||
          !Array.isArray(data.items)
        ) {
          throw new Error("The API returned an unexpected audit history response.");
        }

        const result = data as AuditLogsResponse;

        if (signal.aborted) return;

        setLogs(result.items);
        setTotalCount(result.totalCount);
        setTotalPages(result.totalPages);
      } catch (err) {
        if (signal.aborted) return;

        setError(
          err instanceof Error
            ? err.message
            : "Something went wrong while loading audit history.",
        );

        setLogs([]);
        setTotalCount(0);
        setTotalPages(0);
      } finally {
        if (!signal.aborted) {
          setLoading(false);
        }
      }
    },
    [page, search, actionFilter, entityFilter],
  );

  useEffect(() => {
    if (!authChecked || !isAdmin) return;

    const controller = new AbortController();

    void loadAuditLogs(controller.signal);

    return () => controller.abort();
  }, [authChecked, isAdmin, loadAuditLogs, refreshKey]);

  function resetFilters() {
    setSearch("");
    setActionFilter("");
    setEntityFilter("");
    setPage(1);
  }

  if (!authChecked) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-300">
        Checking your session...
      </main>
    );
  }

  if (!getToken() || !currentUser) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-300">
        Redirecting to sign in...
      </main>
    );
  }

  if (!isAdmin) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-slate-950 px-4 text-white">
        <section className="w-full max-w-md rounded-2xl border border-slate-800 bg-slate-900 p-8 text-center">
          <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-red-950 text-2xl text-red-400">
            !
          </div>

          <h1 className="mt-5 text-2xl font-bold">Access denied</h1>

          <p className="mt-3 text-sm leading-6 text-slate-400">
            Audit history is restricted to administrators. Your current
            account does not have the Admin role.
          </p>

          <Link
            href="/"
            className="mt-6 inline-flex rounded-lg bg-emerald-500 px-5 py-3 font-semibold text-slate-950 transition hover:bg-emerald-400"
          >
            Back to dashboard
          </Link>

          <button
            type="button"
            onClick={logout}
            className="mt-3 block w-full rounded-lg border border-slate-700 px-5 py-3 text-sm text-slate-300 transition hover:bg-slate-800"
          >
            Sign out
          </button>
        </section>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-slate-950 px-4 py-8 text-white sm:px-6 lg:px-10">
      <div className="mx-auto max-w-7xl">
        <header className="mb-8 flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <Link
              href="/"
              className="inline-flex items-center gap-2 text-sm text-slate-400 transition hover:text-emerald-400"
            >
              <span aria-hidden="true">←</span>
              Back to dashboard
            </Link>

            <p className="mt-6 text-xs font-semibold uppercase tracking-[0.25em] text-emerald-400">
              Security and accountability
            </p>

            <h1 className="mt-3 text-3xl font-bold tracking-tight sm:text-4xl">
              Audit history
            </h1>

            <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-400">
              Review product and category changes, identify who performed
              each action, and track when it happened.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="mr-2">
              <p className="text-sm font-medium text-white">
                {currentUser.fullName}
              </p>
              <span className="mt-1 inline-flex rounded-full bg-emerald-500/10 px-2.5 py-1 text-xs font-semibold text-emerald-400">
                Administrator
              </span>
            </div>

            <button
              type="button"
              onClick={() => setRefreshKey((value) => value + 1)}
              disabled={loading}
              className="rounded-lg border border-slate-700 px-4 py-2.5 text-sm font-medium transition hover:border-emerald-400 hover:text-emerald-400 disabled:opacity-50"
            >
              {loading ? "Refreshing..." : "↻ Refresh"}
            </button>

            <button
              type="button"
              onClick={logout}
              className="rounded-lg border border-red-900 px-4 py-2.5 text-sm font-medium text-red-400 transition hover:bg-red-950"
            >
              Sign out
            </button>
          </div>
        </header>

        <section className="mb-6 grid gap-4 sm:grid-cols-2">
          <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-sm text-slate-400">Matching events</p>
                <p className="mt-3 text-3xl font-bold tracking-tight">
                  {totalCount.toLocaleString()}
                </p>
                <p className="mt-2 text-xs text-slate-500">
                  Events matching the current filters
                </p>
              </div>

              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-emerald-500/10 text-xl text-emerald-400">
                ↗
              </div>
            </div>
          </div>

          <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-sm text-slate-400">Current page</p>
                <p className="mt-3 text-3xl font-bold tracking-tight">
                  {page} <span className="text-lg text-slate-500">/ {Math.max(totalPages, 1)}</span>
                </p>
                <p className="mt-2 text-xs text-slate-500">
                  Up to {PAGE_SIZE} events per page
                </p>
              </div>

              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-sky-500/10 text-xl text-sky-400">
                ☷
              </div>
            </div>
          </div>
        </section>

        <section className="mb-6 rounded-2xl border border-slate-800 bg-slate-900 p-5 sm:p-6">
          <div className="mb-5">
            <h2 className="text-lg font-semibold">Filter audit events</h2>
            <p className="mt-1 text-sm text-slate-400">
              Narrow the history by action, record type, or keyword.
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-[1.6fr_1fr_1fr_auto] xl:items-end">
            <div>
              <label
                htmlFor="auditSearch"
                className="mb-2 block text-sm font-medium text-slate-300"
              >
                Search
              </label>
              <input
                id="auditSearch"
                type="search"
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(1);
                }}
                placeholder="Search actor, description, or ID..."
                className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 text-sm outline-none transition placeholder:text-slate-600 focus:border-emerald-400 focus:ring-1 focus:ring-emerald-400"
              />
            </div>

            <div>
              <label
                htmlFor="actionFilter"
                className="mb-2 block text-sm font-medium text-slate-300"
              >
                Action
              </label>
              <select
                id="actionFilter"
                value={actionFilter}
                onChange={(event) => {
                  setActionFilter(event.target.value);
                  setPage(1);
                }}
                className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 text-sm text-white outline-none focus:border-emerald-400"
              >
                <option value="">All actions</option>
                <option value="Created">Created</option>
                <option value="Updated">Updated</option>
                <option value="Deleted">Deleted</option>
              </select>
            </div>

            <div>
              <label
                htmlFor="entityFilter"
                className="mb-2 block text-sm font-medium text-slate-300"
              >
                Record type
              </label>
              <select
                id="entityFilter"
                value={entityFilter}
                onChange={(event) => {
                  setEntityFilter(event.target.value);
                  setPage(1);
                }}
                className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 text-sm text-white outline-none focus:border-emerald-400"
              >
                <option value="">All record types</option>
                <option value="Product">Product</option>
                <option value="Category">Category</option>
              </select>
            </div>

            <button
              type="button"
              onClick={resetFilters}
              className="rounded-lg border border-slate-700 px-4 py-3 text-sm font-medium text-slate-300 transition hover:border-slate-500 hover:bg-slate-800"
            >
              Clear filters
            </button>
          </div>
        </section>

        {error && (
          <div
            role="alert"
            className="mb-6 rounded-xl border border-red-900 bg-red-950/50 p-4 text-sm text-red-300"
          >
            <p className="font-semibold">Unable to load audit history</p>
            <p className="mt-1">{error}</p>

            {error.includes("Access denied") && (
              <Link
                href="/"
                className="mt-3 inline-block underline underline-offset-4"
              >
                Return to dashboard
              </Link>
            )}
          </div>
        )}

        <section className="overflow-hidden rounded-2xl border border-slate-800 bg-slate-900">
          <div className="flex flex-col gap-2 border-b border-slate-800 px-5 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-6">
            <div>
              <h2 className="text-lg font-semibold">Activity log</h2>
              <p className="mt-1 text-sm text-slate-400">
                Latest recorded changes appear first.
              </p>
            </div>

            <span className="w-fit rounded-full bg-slate-800 px-3 py-1.5 text-xs font-medium text-slate-300">
              {totalCount.toLocaleString()} events
            </span>
          </div>

          {loading ? (
            <div className="space-y-4 p-6">
              {[1, 2, 3, 4].map((item) => (
                <div
                  key={item}
                  className="h-20 animate-pulse rounded-xl bg-slate-800"
                />
              ))}
              <p className="text-sm text-slate-400">
                Loading audit events...
              </p>
            </div>
          ) : logs.length === 0 ? (
            <div className="px-6 py-16 text-center">
              <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-slate-800 text-2xl text-slate-400">
                ☷
              </div>

              <h3 className="mt-4 font-semibold text-white">
                {error ? "Audit history unavailable" : "No audit events found"}
              </h3>

              <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-400">
                {error
                  ? "Check that the API is running and try refreshing the page."
                  : "Try changing your filters, or create or update a product or category to generate an audit event."}
              </p>

              {error && (
                <button
                  type="button"
                  onClick={() => setRefreshKey((value) => value + 1)}
                  className="mt-5 rounded-lg border border-slate-700 px-4 py-2.5 text-sm text-slate-300 transition hover:border-emerald-400 hover:text-emerald-400"
                >
                  Try again
                </button>
              )}
            </div>
          ) : (
            <>
              <div className="divide-y divide-slate-800">
                {logs.map((log) => (
                  <article
                    key={log.id}
                    className="px-5 py-5 transition hover:bg-slate-800/30 sm:px-6"
                  >
                    <div className="flex flex-col gap-4 sm:flex-row sm:items-start">
                      <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-slate-700 bg-slate-950 text-lg text-emerald-400">
                        {log.action === "Created"
                          ? "+"
                          : log.action === "Updated"
                            ? "↻"
                            : log.action === "Deleted"
                              ? "−"
                              : "•"}
                      </div>

                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <ActionBadge action={log.action} />

                          <span className="rounded-full border border-slate-700 bg-slate-950 px-2.5 py-1 text-xs font-medium text-slate-300">
                            {log.entityType}
                          </span>
                        </div>

                        <p className="mt-3 break-words text-sm leading-6 text-slate-200">
                          {log.description}
                        </p>

                        <div className="mt-3 flex flex-col gap-2 text-xs text-slate-400 sm:flex-row sm:flex-wrap sm:items-center sm:gap-x-5">
                          <span>
                            <span className="text-slate-500">Actor: </span>
                            <span className="break-all text-slate-300">
                              {log.actorEmail}
                            </span>
                          </span>

                          <span>
                            <span className="text-slate-500">Record ID: </span>
                            <span className="break-all font-mono text-slate-300">
                              {log.entityId}
                            </span>
                          </span>

                          <span>
                            <span className="text-slate-500">Time: </span>
                            {formatDate(log.createdAtUtc)}
                          </span>
                        </div>
                      </div>

                      <div className="shrink-0 text-xs text-slate-600">
                        {log.id.slice(0, 8)}
                      </div>
                    </div>
                  </article>
                ))}
              </div>

              <div className="flex flex-col gap-4 border-t border-slate-800 px-5 py-5 sm:flex-row sm:items-center sm:justify-between sm:px-6">
                <p className="text-sm text-slate-400">
                  Showing{" "}
                  <span className="font-medium text-white">
                    {(page - 1) * PAGE_SIZE + 1}
                    {"–"}
                    {Math.min(page * PAGE_SIZE, totalCount)}
                  </span>{" "}
                  of {totalCount.toLocaleString()} events
                </p>

                <div className="flex items-center gap-3">
                  <button
                    type="button"
                    onClick={() => setPage((value) => Math.max(1, value - 1))}
                    disabled={page <= 1 || loading}
                    className="rounded-lg border border-slate-700 px-4 py-2.5 text-sm transition hover:border-emerald-400 hover:text-emerald-400 disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    Previous
                  </button>

                  <span className="whitespace-nowrap text-sm text-slate-300">
                    {page} / {Math.max(totalPages, 1)}
                  </span>

                  <button
                    type="button"
                    onClick={() =>
                      setPage((value) => Math.min(totalPages, value + 1))
                    }
                    disabled={page >= totalPages || loading || totalPages === 0}
                    className="rounded-lg border border-slate-700 px-4 py-2.5 text-sm transition hover:border-emerald-400 hover:text-emerald-400 disabled:cursor-not-allowed disabled:opacity-40"
                  >
                    Next
                  </button>
                </div>
              </div>
            </>
          )}
        </section>

        <footer className="mt-8 border-t border-slate-800 pt-5 text-center text-xs text-slate-500">
          ServeFlow · Administrative audit history
        </footer>
      </div>
    </main>
  );
}
