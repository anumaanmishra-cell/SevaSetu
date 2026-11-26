import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import axios from "axios";
import { api } from "../api/client";
import { ROLES, type AuthResponse, type Role } from "../api/types";

const apiBase = `${import.meta.env.VITE_API_URL ?? "http://localhost:5300"}/api/v1`;

interface AuthState {
  role: Role | null;
  fullName: string;
  email: string;
  login: (emailOrPhone: string, password: string) => Promise<AuthResponse>;
  register: (path: string, payload: Record<string, unknown>) => Promise<AuthResponse>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

export function isValidRole(value: string | null | undefined): value is Role {
  return !!value && (ROLES as string[]).includes(value);
}

function persist(data: AuthResponse) {
  localStorage.setItem("accessToken", data.accessToken);
  localStorage.setItem("refreshToken", data.refreshToken);
  localStorage.setItem("role", data.role);
  localStorage.setItem("fullName", data.fullName);
  localStorage.setItem("email", data.email);
}

function clearSession() {
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
  localStorage.removeItem("role");
  localStorage.removeItem("fullName");
  localStorage.removeItem("email");
}

function readBootSession(): { role: Role | null; fullName: string; email: string } {
  const storedRole = localStorage.getItem("role");
  const accessToken = localStorage.getItem("accessToken");
  if (!isValidRole(storedRole) || !accessToken) {
    clearSession();
    return { role: null, fullName: "", email: "" };
  }
  return { role: storedRole, fullName: localStorage.getItem("fullName") ?? "", email: localStorage.getItem("email") ?? "" };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const boot = readBootSession();
  const [role, setRole] = useState<Role | null>(boot.role);
  const [fullName, setFullName] = useState(boot.fullName);
  const [email, setEmail] = useState(boot.email);

  const value = useMemo<AuthState>(
    () => ({
      role,
      fullName,
      email,
      login: async (emailOrPhone, password) => {
        const { data } = await api.post<AuthResponse>("/auth/login", { email: emailOrPhone, password });
        if (!isValidRole(data.role)) {
          clearSession();
          throw new Error("Unsupported account role for AaoSaharaBane.");
        }
        persist(data);
        setRole(data.role);
        setFullName(data.fullName);
        setEmail(data.email);
        if ("Notification" in window && navigator.serviceWorker) {
          navigator.serviceWorker.register("/sw.js").catch(() => undefined);
        }
        return data;
      },
      register: async (path, payload) => {
        const { data } = await api.post<AuthResponse>(path, payload);
        persist(data);
        setRole(data.role);
        setFullName(data.fullName);
        setEmail(data.email);
        return data;
      },
      logout: async () => {
        const accessToken = localStorage.getItem("accessToken");
        const refreshToken = localStorage.getItem("refreshToken");
        try {
          await axios.post(`${apiBase}/auth/logout`, { refreshToken }, accessToken ? { headers: { Authorization: `Bearer ${accessToken}` } } : undefined);
        } catch {
          /* ignore */
        }
        clearSession();
        setRole(null);
        setFullName("");
        setEmail("");
      }
    }),
    [role, fullName, email]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("AuthProvider missing");
  return ctx;
}

export function homeFor(role: Role | string | null | undefined) {
  if (role === "Family") return "/family";
  if (role === "Caregiver") return "/caregiver";
  if (role === "AgencyAdmin") return "/agency";
  if (role === "HospitalCoordinator") return "/hospital";
  if (role === "PlatformAdmin") return "/admin";
  if (role === "Elder") return "/elder";
  return "/login";
}
