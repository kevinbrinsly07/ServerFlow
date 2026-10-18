
"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { API_URL } from "@/lib/auth";

function getPasswordErrors(password: string): string[] {
  const errors: string[] = [];

  if (password.length < 8) {
    errors.push("At least 8 characters are required.");
  }

  if (!/[A-Z]/.test(password)) {
    errors.push("At least one uppercase letter (A–Z) is required.");
  }

  if (!/[a-z]/.test(password)) {
    errors.push("At least one lowercase letter (a–z) is required.");
  }

  if (!/[0-9]/.test(password)) {
    errors.push("At least one number (0–9) is required.");
  }

  if (!/[^a-zA-Z0-9]/.test(password)) {
    errors.push("At least one special character (e.g. ! @ # $) is required.");
  }

  return errors;
}

export default function RegisterPage() {
  const router = useRouter();

  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    setSuccess("");

    const passwordErrors = getPasswordErrors(password);

    if (passwordErrors.length > 0) {
      setError(passwordErrors.join(" "));
      return;
    }

    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    setLoading(true);

    try {
      const response = await fetch(`${API_URL}/api/auth/register`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          fullName: fullName.trim(),
          email: email.trim(),
          password,
        }),
      });

      const data = await response.json().catch(() => null);

      if (!response.ok) {
        const details = data?.errors;

        let message = data?.message ?? data?.title;

        if (details && typeof details === "object") {
          message = Object.values(details)
            .flatMap((value) =>
              Array.isArray(value) ? value : [String(value)],
            )
            .join(" ");
        }

        if (!message && typeof data === "string") {
          message = data;
        }

        throw new Error(
          message || `Registration failed (HTTP ${response.status}).`,
        );
      }

      setSuccess("Account created successfully! Redirecting to login...");

      setTimeout(() => {
        router.push("/login");
      }, 1200);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Unable to create your account. Please try again.",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-950 px-4 py-10 text-white">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-md space-y-5 rounded-2xl border border-slate-800 bg-slate-900 p-8 shadow-xl"
      >
        <div>
          <h1 className="text-3xl font-bold">Create account</h1>
          <p className="mt-2 text-sm text-slate-400">
            Join ServeFlow to manage restaurant products.
          </p>
        </div>

        {error && (
          <div
            role="alert"
            className="rounded-lg border border-red-900 bg-red-950/50 p-3 text-sm text-red-300"
          >
            {error}
          </div>
        )}

        {success && (
          <div
            role="status"
            className="rounded-lg border border-emerald-900 bg-emerald-950/50 p-3 text-sm text-emerald-300"
          >
            {success}
          </div>
        )}

        <div className="space-y-2">
          <label htmlFor="fullName" className="block text-sm text-slate-300">
            Full name
          </label>
          <input
            id="fullName"
            type="text"
            autoComplete="name"
            required
            maxLength={100}
            value={fullName}
            onChange={(event) => setFullName(event.target.value)}
            placeholder="Your full name"
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none focus:border-emerald-400"
          />
        </div>

        <div className="space-y-2">
          <label htmlFor="email" className="block text-sm text-slate-300">
            Email address
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="you@example.com"
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none focus:border-emerald-400"
          />
        </div>

        <div className="space-y-2">
          <label htmlFor="password" className="block text-sm text-slate-300">
            Password
          </label>
          <input
            id="password"
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder="Create a password"
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none focus:border-emerald-400"
          />

          <div className="mt-2 space-y-1">
            {getPasswordErrors(password).map((message) => (
              <p key={message} className="text-xs text-amber-400">
                • {message}
              </p>
            ))}

            {password.length > 0 &&
              getPasswordErrors(password).length === 0 && (
                <p className="text-xs text-emerald-400">
                  ✓ Password meets the displayed requirements.
                </p>
              )}
          </div>
        </div>

        <div className="space-y-2">
          <label
            htmlFor="confirmPassword"
            className="block text-sm text-slate-300"
          >
            Confirm password
          </label>
          <input
            id="confirmPassword"
            type="password"
            autoComplete="new-password"
            required
            minLength={8}
            value={confirmPassword}
            onChange={(event) => setConfirmPassword(event.target.value)}
            placeholder="Enter your password again"
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-4 py-3 outline-none focus:border-emerald-400"
          />

          {confirmPassword.length > 0 && (
            <p
              className={`mt-2 text-xs ${
                password === confirmPassword
                  ? "text-emerald-400"
                  : "text-red-400"
              }`}
            >
              {password === confirmPassword
                ? "✓ Passwords match."
                : "Passwords do not match."}
            </p>
          )}
        </div>

        <button
          type="submit"
          disabled={loading || Boolean(success)}
          className="w-full rounded-lg bg-emerald-500 px-4 py-3 font-semibold text-slate-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {loading ? "Creating account..." : "Create account"}
        </button>

        <p className="text-center text-sm text-slate-400">
          Already have an account?{" "}
          <Link
            href="/login"
            className="font-medium text-emerald-400 hover:text-emerald-300"
          >
            Sign in
          </Link>
        </p>
      </form>
    </main>
  );
}
