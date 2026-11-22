import { api } from "./client";

function urlBase64ToUint8Array(base64: string) {
  const padding = "=".repeat((4 - (base64.length % 4)) % 4);
  const raw = atob((base64 + padding).replace(/-/g, "+").replace(/_/g, "/"));
  return Uint8Array.from([...raw].map((c) => c.charCodeAt(0)));
}

export async function enableVisitAlerts(): Promise<string> {
  const { data } = await api.get<{ publicKey?: string | null }>("/me/push/vapid");
  if (!data.publicKey) return "Visit alerts need VAPID keys on the API.";
  if (!("Notification" in window) || !navigator.serviceWorker) return "This browser cannot take alerts.";
  const perm = await Notification.requestPermission();
  if (perm !== "granted") return "Alerts were not allowed.";
  const reg = await navigator.serviceWorker.register("/sw.js");
  const sub = await reg.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: urlBase64ToUint8Array(data.publicKey)
  });
  const json = sub.toJSON();
  await api.post("/me/push", { endpoint: json.endpoint, p256dh: json.keys?.p256dh, auth: json.keys?.auth });
  return "This browser will get visit alerts.";
}
