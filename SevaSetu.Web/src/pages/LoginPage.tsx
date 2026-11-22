import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { homeFor, useAuth } from "../auth/AuthContext";

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("family@sevasetu.local");
  const [password, setPassword] = useState("Family@12345");
  const [error, setError] = useState("");

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError("");
    try {
      const data = await login(email, password);
      navigate(homeFor(data.role));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
    }
  }

  return (
    <div className="mx-auto flex min-h-dvh max-w-md flex-col justify-center p-6">
      <h1 className="text-2xl font-bold text-brand-700">SevaSetu</h1>
      <p className="mb-6 text-sm text-slate-500">Home nursing and elder care</p>
      <form onSubmit={onSubmit} className="space-y-3 rounded-xl bg-white p-5 shadow-sm">
        {error && <p className="text-sm text-red-600">{error}</p>}
        <input className="w-full rounded-lg border px-3 py-2" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email or mobile" />
        <input type="password" className="w-full rounded-lg border px-3 py-2" value={password} onChange={(e) => setPassword(e.target.value)} />
        <button className="w-full rounded-lg bg-brand-600 py-2 text-white">Login</button>
      </form>
      <p className="mt-4 text-sm">
        New here? <Link className="text-brand-700" to="/register">Register</Link>
      </p>
      <p className="mt-6 text-xs text-slate-400">
        Family: family@sevasetu.local / Family@12345
        <br />
        Nurse: nurse@sevasetu.local / Caregiver@12345
        <br />
        Agency: agency@sevasetu.local / Agency@12345
        <br />
        Hospital: hospital@sevasetu.local / Hospital@12345
        <br />
        Admin: admin@sevasetu.local / Admin@12345
        <br />
        Elder: elder@sevasetu.local / Elder@12345
      </p>
    </div>
  );
}
