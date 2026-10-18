
"use client";

import {
  FormEvent,
  useCallback,
  useEffect,
  useState,
} from "react";

type Product = {
  id: string;
  name: string;
  description: string;
  price: number;
  isAvailable: boolean;
  createdAtUtc?: string;
};

type ProductForm = {
  name: string;
  price: string;
  description: string;
  isAvailable: boolean;
};

type ProductsResponse = {
  items: Product[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

const API_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

const PAGE_SIZE = 5;
const STATS_PAGE_SIZE = 100;

const emptyForm: ProductForm = {
  name: "",
  price: "",
  description: "",
  isAvailable: true,
};

function formatPrice(price: number) {
  return `LKR ${price.toLocaleString("en-LK", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}


async function fetchProducts(
  page: number,
  pageSize: number,
  search = ""
): Promise<ProductsResponse> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });

  if (search.trim()) {
    params.set("search", search.trim());
  }

  const response = await fetch(
    `${API_URL}/api/products?${params.toString()}`,
    { cache: "no-store" }
  );

  if (!response.ok) {
    throw new Error(
      `Could not load products. API returned ${response.status}.`
    );
  }

  const data: unknown = await response.json();

  if (
    typeof data !== "object" ||
    data === null ||
    !("items" in data) ||
    !Array.isArray(data.items)
  ) {
    throw new Error(
      "Unexpected API response. Make sure the backend returns a paginated products object with an items array."
    );
  }

  const payload = data as Record<string, unknown>;

  return {
    items: payload.items as Product[],
    page:
      typeof payload.page === "number" ? payload.page : page,
    pageSize:
      typeof payload.pageSize === "number" ? payload.pageSize : pageSize,
    totalCount:
      typeof payload.totalCount === "number"
        ? payload.totalCount
        : (payload.items as unknown[]).length,
    totalPages:
      typeof payload.totalPages === "number"
        ? payload.totalPages
        : Math.ceil((payload.items as unknown[]).length / pageSize),
  };
}




export default function Home() {
  const [products, setProducts] = useState<Product[]>([]);
  const [statsProducts, setStatsProducts] = useState<Product[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  const [form, setForm] = useState<ProductForm>(emptyForm);
  const [editingId, setEditingId] = useState<string | null>(null);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [page, setPage] = useState(1);
  const [refreshKey, setRefreshKey] = useState(0);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  // Debounce search to avoid sending a request for every keystroke.
  useEffect(() => {
    const timeout = window.setTimeout(() => {
      setDebouncedSearch(search.trim());
    }, 300);

    return () => window.clearTimeout(timeout);
  }, [search]);

  // Load the current page from the API.
  const loadProducts = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      const data = await fetchProducts(
        page,
        PAGE_SIZE,
        debouncedSearch
      );

      // If deleting the last item on a page makes that page invalid,
      // move to the last available page.
      const lastValidPage = Math.max(data.totalPages, 1);

      if (page > lastValidPage) {
        setPage(lastValidPage);
        return;
      }

      setProducts(Array.isArray(data.items) ? data.items : []);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Unable to load products."
      );
    } finally {
      setLoading(false);
    }
  }, [page, debouncedSearch, refreshKey]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadProducts();
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [loadProducts]);

  // Load every product in batches for accurate dashboard statistics.
  // This is separate from the paginated list.
  const loadDashboardStats = useCallback(async () => {
    try {
      const firstPage = await fetchProducts(1, STATS_PAGE_SIZE);
      const allProducts = [...firstPage.items];

      if (firstPage.totalPages > 1) {
        const remainingPages = await Promise.all(
          Array.from(
            { length: firstPage.totalPages - 1 },
            (_, index) =>
              fetchProducts(index + 2, STATS_PAGE_SIZE)
          )
        );

        for (const result of remainingPages) {
          allProducts.push(...result.items);
        }
      }

      setStatsProducts(allProducts);
    } catch {
      // The main product list displays its own loading error.
    }
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadDashboardStats();
    }, 0);

    return () => window.clearTimeout(timeoutId);
  }, [loadDashboardStats, refreshKey]);

  function updateField<K extends keyof ProductForm>(
    field: K,
    value: ProductForm[K]
  ) {
    setForm((current) => ({
      ...current,
      [field]: value,
    }));
  }

  function resetForm() {
    setForm(emptyForm);
    setEditingId(null);
  }

  function startEditing(product: Product) {
    setEditingId(product.id);

    setForm({
      name: product.name,
      price: String(product.price),
      description: product.description ?? "",
      isAvailable: product.isAvailable,
    });

    setError("");
    setSuccess("");

    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setSaving(true);
    setError("");
    setSuccess("");

    try {
      const isEditing = editingId !== null;

      const response = await fetch(
        isEditing
          ? `${API_URL}/api/products/${editingId}`
          : `${API_URL}/api/products`,
        {
          method: isEditing ? "PUT" : "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            name: form.name.trim(),
            price: Number(form.price),
            description: form.description.trim(),
            ...(isEditing && {
              isAvailable: form.isAvailable,
            }),
          }),
        }
      );

      if (!response.ok) {
        let message = `Request failed (${response.status}).`;

        try {
          const body = await response.json();
          message = body.message ?? body.title ?? message;
        } catch {
          // Keep the fallback message.
        }

        throw new Error(message);
      }

      setSuccess(
        isEditing
          ? "Product updated successfully."
          : "Product added successfully."
      );

      resetForm();

      // Refresh both the list and the dashboard statistics.
      setPage(1);
      setRefreshKey((current) => current + 1);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Something went wrong."
      );
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(product: Product) {
    const confirmed = window.confirm(
      `Are you sure you want to delete "${product.name}"?`
    );

    if (!confirmed) return;

    setDeletingId(product.id);
    setError("");
    setSuccess("");

    try {
      const response = await fetch(
        `${API_URL}/api/products/${product.id}`,
        { method: "DELETE" }
      );

      if (!response.ok) {
        let message = `Could not delete product (${response.status}).`;

        try {
          const body = await response.json();
          message = body.message ?? body.title ?? message;
        } catch {
          // Keep the fallback message.
        }

        throw new Error(message);
      }

      if (editingId === product.id) {
        resetForm();
      }

      setSuccess("Product deleted successfully.");
      setRefreshKey((current) => current + 1);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Could not delete product."
      );
    } finally {
      setDeletingId(null);
    }
  }

  async function toggleAvailability(product: Product) {
    setError("");
    setSuccess("");

    try {
      const response = await fetch(
        `${API_URL}/api/products/${product.id}`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            name: product.name,
            price: product.price,
            description: product.description,
            isAvailable: !product.isAvailable,
          }),
        }
      );

      if (!response.ok) {
        let message = "Could not update product availability.";

        try {
          const body = await response.json();
          message = body.message ?? body.title ?? message;
        } catch {
          // Keep the fallback message.
        }

        throw new Error(message);
      }

      setSuccess("Product availability updated.");
      setRefreshKey((current) => current + 1);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Something went wrong."
      );
    }
  }

  const availableCount = statsProducts.filter(
    (product) => product.isAvailable
  ).length;

  const listedValue = statsProducts.reduce(
    (total, product) => total + product.price,
    0
  );

  const firstResult =
    totalCount === 0 ? 0 : (page - 1) * PAGE_SIZE + 1;

  const lastResult = Math.min(page * PAGE_SIZE, totalCount);

  return (
    <main className="min-h-screen bg-slate-950 px-4 py-8 text-white sm:px-6 lg:px-10">
      <div className="mx-auto max-w-7xl">
        <header className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.25em] text-emerald-400">
              Restaurant management
            </p>

            <h1 className="mt-3 text-3xl font-bold tracking-tight sm:text-4xl">
              ServeFlow
            </h1>

            <p className="mt-2 text-slate-400">
              Manage your restaurant menu in one place.
            </p>
          </div>

          <button
            type="button"
            onClick={() => setRefreshKey((current) => current + 1)}
            disabled={loading}
            className="rounded-lg border border-slate-700 px-4 py-2.5 text-sm font-medium transition hover:border-emerald-400 hover:text-emerald-400 disabled:opacity-50"
          >
            {loading ? "Refreshing..." : "↻ Refresh"}
          </button>
        </header>

        {error && (
          <div
            role="alert"
            className="mb-5 rounded-xl border border-red-900 bg-red-950/60 p-4 text-sm text-red-300"
          >
            {error}
          </div>
        )}

        {success && (
          <div
            role="status"
            className="mb-5 rounded-xl border border-emerald-900 bg-emerald-950/50 p-4 text-sm text-emerald-300"
          >
            {success}
          </div>
        )}

        <section className="mb-8 grid gap-4 sm:grid-cols-3">
          <StatCard
            label="Total products"
            value={String(totalCount)}
            detail="Products matching the current search"
          />

          <StatCard
            label="Available"
            value={String(availableCount)}
            detail="Available across all products"
          />

          <StatCard
            label="Combined listed prices"
            value={formatPrice(listedValue)}
            detail="Sum of all product prices"
          />
        </section>

        <div className="grid items-start gap-6 lg:grid-cols-[0.85fr_1.5fr]">
          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-5 sm:p-6">
            <div className="mb-6">
              <p className="text-xs font-semibold uppercase tracking-widest text-emerald-400">
                Product management
              </p>

              <h2 className="mt-2 text-xl font-semibold">
                {editingId ? "Edit product" : "Add a menu item"}
              </h2>

              <p className="mt-1 text-sm text-slate-400">
                {editingId
                  ? "Update the selected product's details."
                  : "Create a new product for your menu."}
              </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5">
              <div>
                <label
                  htmlFor="name"
                  className="mb-2 block text-sm font-medium text-slate-300"
                >
                  Product name
                </label>

                <input
                  id="name"
                  required
                  maxLength={150}
                  value={form.name}
                  onChange={(event) =>
                    updateField("name", event.target.value)
                  }
                  placeholder="Chicken Burger"
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none transition placeholder:text-slate-600 focus:border-emerald-400 focus:ring-1 focus:ring-emerald-400"
                />
              </div>

              <div>
                <label
                  htmlFor="price"
                  className="mb-2 block text-sm font-medium text-slate-300"
                >
                  Price (LKR)
                </label>

                <input
                  id="price"
                  required
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={form.price}
                  onChange={(event) =>
                    updateField("price", event.target.value)
                  }
                  placeholder="1250.00"
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none transition placeholder:text-slate-600 focus:border-emerald-400 focus:ring-1 focus:ring-emerald-400"
                />
              </div>

              <div>
                <label
                  htmlFor="description"
                  className="mb-2 block text-sm font-medium text-slate-300"
                >
                  Description
                </label>

                <textarea
                  id="description"
                  maxLength={1000}
                  rows={4}
                  value={form.description}
                  onChange={(event) =>
                    updateField("description", event.target.value)
                  }
                  placeholder="Describe the menu item..."
                  className="w-full resize-y rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none transition placeholder:text-slate-600 focus:border-emerald-400 focus:ring-1 focus:ring-emerald-400"
                />
              </div>

              {editingId && (
                <label className="flex cursor-pointer items-center gap-3 rounded-lg border border-slate-800 bg-slate-950 p-3">
                  <input
                    type="checkbox"
                    checked={form.isAvailable}
                    onChange={(event) =>
                      updateField("isAvailable", event.target.checked)
                    }
                    className="h-4 w-4 accent-emerald-400"
                  />

                  <span className="text-sm text-slate-300">
                    Product is available
                  </span>
                </label>
              )}

              <button
                type="submit"
                disabled={saving}
                className="w-full rounded-lg bg-emerald-500 px-4 py-3 font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {saving
                  ? "Saving..."
                  : editingId
                    ? "Save changes"
                    : "Add product"}
              </button>

              {editingId && (
                <button
                  type="button"
                  onClick={resetForm}
                  disabled={saving}
                  className="w-full rounded-lg border border-slate-700 px-4 py-3 font-medium text-slate-300 transition hover:bg-slate-800 disabled:opacity-50"
                >
                  Cancel editing
                </button>
              )}
            </form>
          </section>

          <section className="min-w-0 rounded-2xl border border-slate-800 bg-slate-900 p-5 sm:p-6">
            <div className="mb-5 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="text-xl font-semibold">Menu items</h2>

                <p className="mt-1 text-sm text-slate-400">
                  Search, edit, or manage your products.
                </p>
              </div>

              <span className="w-fit rounded-full bg-slate-800 px-3 py-1 text-sm text-slate-300">
                {totalCount}{" "}
                {totalCount === 1 ? "product" : "products"}
              </span>
            </div>

            <div className="mb-5">
              <label htmlFor="search" className="sr-only">
                Search products
              </label>

              <input
                id="search"
                type="search"
                value={search}
                onChange={(event) => {
                  setSearch(event.target.value);
                  setPage(1);
                }}
                placeholder="Search products..."
                className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none placeholder:text-slate-600 focus:border-emerald-400"
              />
            </div>

            {loading ? (
              <div className="space-y-3">
                {[1, 2, 3].map((item) => (
                  <div
                    key={item}
                    className="h-20 animate-pulse rounded-lg bg-slate-800"
                  />
                ))}

                <p className="text-sm text-slate-400">
                  Loading products...
                </p>
              </div>
            ) : products.length === 0 ? (
              <div className="rounded-xl border border-dashed border-slate-700 px-4 py-12 text-center">
                <p className="font-medium text-slate-300">
                  {debouncedSearch
                    ? "No matching products found."
                    : "No menu items yet."}
                </p>

                <p className="mt-2 text-sm text-slate-500">
                  {debouncedSearch
                    ? "Try a different search term."
                    : "Add your first product using the form."}
                </p>
              </div>
            ) : (
              <>
                <ul className="divide-y divide-slate-800">
                  {products.map((product) => (
                    <li key={product.id} className="py-5">
                      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
                        <div className="min-w-0 flex-1">
                          <div className="flex flex-wrap items-center gap-2">
                            <h3 className="break-words font-semibold text-white">
                              {product.name}
                            </h3>

                            <span
                              className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                                product.isAvailable
                                  ? "bg-emerald-950 text-emerald-400"
                                  : "bg-slate-800 text-slate-400"
                              }`}
                            >
                              {product.isAvailable
                                ? "Available"
                                : "Unavailable"}
                            </span>
                          </div>

                          <p className="mt-2 whitespace-pre-wrap break-words text-sm text-slate-400">
                            {product.description || "No description"}
                          </p>

                          <p className="mt-3 font-semibold text-emerald-400">
                            {formatPrice(product.price)}
                          </p>
                        </div>

                        <div className="flex shrink-0 flex-wrap gap-2">
                          <button
                            type="button"
                            onClick={() =>
                              void toggleAvailability(product)
                            }
                            className="rounded-lg border border-slate-700 px-3 py-2 text-sm text-slate-300 transition hover:border-emerald-400 hover:text-emerald-400"
                          >
                            {product.isAvailable ? "Disable" : "Enable"}
                          </button>

                          <button
                            type="button"
                            onClick={() => startEditing(product)}
                            className="rounded-lg border border-slate-700 px-3 py-2 text-sm text-slate-300 transition hover:border-sky-400 hover:text-sky-400"
                          >
                            Edit
                          </button>

                          <button
                            type="button"
                            disabled={deletingId === product.id}
                            onClick={() => void handleDelete(product)}
                            className="rounded-lg border border-red-900/70 px-3 py-2 text-sm text-red-400 transition hover:bg-red-950 disabled:opacity-50"
                          >
                            {deletingId === product.id
                              ? "Deleting..."
                              : "Delete"}
                          </button>
                        </div>
                      </div>
                    </li>
                  ))}
                </ul>

                <div className="mt-5 flex flex-col gap-4 border-t border-slate-800 pt-5 sm:flex-row sm:items-center sm:justify-between">
                  <p className="text-sm text-slate-400">
                    Showing {firstResult}–{lastResult} of {totalCount}
                  </p>

                  <div className="flex items-center gap-3">
                    <button
                      type="button"
                      onClick={() =>
                        setPage((current) => Math.max(1, current - 1))
                      }
                      disabled={page <= 1 || loading}
                      className="rounded-lg border border-slate-700 px-3 py-2 text-sm transition hover:border-emerald-400 hover:text-emerald-400 disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      Previous
                    </button>

                    <span className="whitespace-nowrap text-sm text-slate-300">
                      Page {page} of {Math.max(totalPages, 1)}
                    </span>

                    <button
                      type="button"
                      onClick={() =>
                        setPage((current) =>
                          Math.min(totalPages, current + 1)
                        )
                      }
                      disabled={
                        page >= totalPages || loading || totalPages === 0
                      }
                      className="rounded-lg border border-slate-700 px-3 py-2 text-sm transition hover:border-emerald-400 hover:text-emerald-400 disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      Next
                    </button>
                  </div>
                </div>
              </>
            )}
          </section>
        </div>

        <footer className="mt-10 border-t border-slate-800 pt-5 text-center text-xs text-slate-500">
          ServeFlow · Restaurant management system
        </footer>
      </div>
    </main>
  );
}

function StatCard({
  label,
  value,
  detail,
}: {
  label: string;
  value: string;
  detail: string;
}) {
  return (
    <div className="rounded-2xl border border-slate-800 bg-slate-900 p-5">
      <p className="text-sm text-slate-400">{label}</p>

      <p className="mt-3 break-words text-2xl font-bold tracking-tight sm:text-3xl">
        {value}
      </p>

      <p className="mt-2 text-xs text-slate-500">{detail}</p>
    </div>
  );
}
