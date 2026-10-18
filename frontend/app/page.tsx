
"use client";

import { FormEvent, useEffect, useState } from "react";

type Product = {
  id: string;
  name: string;
  description: string;
  price: number;
  isAvailable: boolean;
};

const API_URL =
  process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

export default function Home() {
  const [products, setProducts] = useState<Product[]>([]);
  const [name, setName] = useState("");
  const [price, setPrice] = useState("");
  const [description, setDescription] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function loadProducts() {
    setLoading(true);
    setError("");

    try {
      const response = await fetch(`${API_URL}/api/products`);

      if (!response.ok) {
        throw new Error("Could not load products.");
      }

      const data: Product[] = await response.json();
      setProducts(data);
    } catch {
      setError(
        "Unable to connect to ServeFlow. Check that the API and database are running."
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadProducts();
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError("");

    try {
      const response = await fetch(`${API_URL}/api/products`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          name,
          price: Number(price),
          description,
        }),
      });

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || "Could not create product.");
      }

      const created: Product = await response.json();

      setProducts((current) =>
        [...current, created].sort((a, b) =>
          a.name.localeCompare(b.name)
        )
      );

      setName("");
      setPrice("");
      setDescription("");
    } catch (error) {
      setError(
        error instanceof Error
          ? error.message
          : "Something went wrong."
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <main className="min-h-screen bg-slate-950 px-6 py-10 text-white">
      <div className="mx-auto max-w-5xl">
        <header className="mb-10">
          <p className="text-sm font-semibold uppercase tracking-[0.25em] text-emerald-400">
            Restaurant management
          </p>
          <h1 className="mt-3 text-4xl font-bold tracking-tight">
            ServeFlow
          </h1>
          <p className="mt-2 text-slate-400">
            Manage your restaurant menu in one place.
          </p>
        </header>

        <div className="grid gap-8 md:grid-cols-[1fr_1.2fr]">
          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
            <h2 className="mb-5 text-xl font-semibold">
              Add a menu item
            </h2>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label htmlFor="name" className="mb-2 block text-sm text-slate-300">
                  Product name
                </label>
                <input
                  id="name"
                  required
                  maxLength={150}
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                  placeholder="Chicken Burger"
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none focus:border-emerald-400"
                />
              </div>

              <div>
                <label htmlFor="price" className="mb-2 block text-sm text-slate-300">
                  Price (LKR)
                </label>
                <input
                  id="price"
                  required
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={price}
                  onChange={(event) => setPrice(event.target.value)}
                  placeholder="1250.00"
                  className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none focus:border-emerald-400"
                />
              </div>

              <div>
                <label htmlFor="description" className="mb-2 block text-sm text-slate-300">
                  Description
                </label>
                <textarea
                  id="description"
                  maxLength={1000}
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                  placeholder="Describe the menu item..."
                  className="min-h-24 w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none focus:border-emerald-400"
                />
              </div>

              <button
                type="submit"
                disabled={saving}
                className="w-full rounded-lg bg-emerald-500 px-4 py-3 font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:opacity-50"
              >
                {saving ? "Saving..." : "Add product"}
              </button>
            </form>
          </section>

          <section className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
            <div className="mb-5 flex items-center justify-between gap-3">
              <h2 className="text-xl font-semibold">Menu items</h2>
              <span className="rounded-full bg-slate-800 px-3 py-1 text-sm text-slate-300">
                {products.length} products
              </span>
            </div>

            {error && (
              <p role="alert" className="mb-4 rounded-lg bg-red-950 p-3 text-sm text-red-300">
                {error}
              </p>
            )}

            {loading ? (
              <p className="text-slate-400">Loading products...</p>
            ) : products.length === 0 ? (
              <p className="text-slate-400">
                No menu items yet. Add your first product.
              </p>
            ) : (
              <ul className="divide-y divide-slate-800">
                {products.map((product) => (
                  <li
                    key={product.id}
                    className="flex items-start justify-between gap-4 py-4"
                  >
                    <div>
                      <h3 className="font-medium">{product.name}</h3>
                      <p className="mt-1 text-sm text-slate-400">
                        {product.description || "No description"}
                      </p>
                    </div>
                    <span className="whitespace-nowrap font-semibold text-emerald-400">
                      LKR {product.price.toLocaleString("en-LK", {
                        minimumFractionDigits: 2,
                      })}
                    </span>
                  </li>
                ))}
              </ul>
            )}

            <button
              type="button"
              onClick={() => void loadProducts()}
              className="mt-5 text-sm text-emerald-400 hover:text-emerald-300"
            >
              Refresh products
            </button>
          </section>
        </div>
      </div>
    </main>
  );
}
