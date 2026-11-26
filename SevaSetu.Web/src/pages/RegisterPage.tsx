import { FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { homeFor, useAuth } from "../auth/AuthContext";

export default function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [role, setRole] = useState("family");
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");
  const [org, setOrg] = useState("");
  const [error, setError] = useState("");

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError("");
    try {
      const path =
        role === "caregiver" ? "/auth/register-caregiver" : role === "agency" ? "/auth/register-agency" : role === "hospital" ? "/auth/register-hospital" : "/auth/register-family";
      const payload: Record<string, unknown> = { fullName, phone, password, city: "Delhi NCR" };
      if (role === "caregiver") Object.assign(payload, { kind: 1, years: 1, hourlyRate: 250, liveInOk: true });
      if (role === "agency") payload.agencyName = org || "My agency";
      if (role === "hospital") payload.hospitalName = org || "My hospital";
      const data = await register(path, payload);
      navigate(homeFor(data.role));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    }
  }

  return (
    <div className="mx-auto flex min-h-dvh max-w-md flex-col justify-center p-6">
      <h1 className="text-2xl font-bold text-brand-700">Join AaoSaharaBane</h1>
      <form onSubmit={onSubmit} className="mt-4 space-y-3 rounded-xl bg-white p-5 shadow-sm">
        {error && <p className="text-sm text-red-600">{error}</p>}
        <select className="w-full rounded-lg border px-3 py-2" value={role} onChange={(e) => setRole(e.target.value)}>
          <option value="family">Family</option>
          <option value="caregiver">Independent caregiver</option>
          <option value="agency">Agency</option>
          <option value="hospital">Hospital coordinator</option>
        </select>
        <input className="w-full rounded-lg border px-3 py-2" placeholder="Full name" value={fullName} onChange={(e) => setFullName(e.target.value)} required />
        <input className="w-full rounded-lg border px-3 py-2" placeholder="Mobile" value={phone} onChange={(e) => setPhone(e.target.value)} required />
        {(role === "agency" || role === "hospital") && (
          <input className="w-full rounded-lg border px-3 py-2" placeholder={role === "agency" ? "Agency name" : "Hospital name"} value={org} onChange={(e) => setOrg(e.target.value)} />
        )}
        <input type="password" className="w-full rounded-lg border px-3 py-2" placeholder="Password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={6} />
        <button className="w-full rounded-lg bg-brand-600 py-2 text-white">Register</button>
      </form>
      <Link className="mt-4 text-sm text-brand-700" to="/login">
        Back to login
      </Link>
    </div>
  );
}
