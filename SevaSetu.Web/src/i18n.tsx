import { createContext, useContext, useMemo, useState, type ReactNode } from "react";

type Lang = "en" | "hi";

const copy: Record<Lang, Record<string, string>> = {
  en: {
    help: "Help",
    home: "Home",
    find: "Find",
    bills: "Bills",
    shifts: "Shifts",
    jobs: "Jobs",
    desk: "Desk",
    inbox: "Inbox",
    discharge: "Discharge",
    kyc: "KYC",
    audit: "Audit",
    leave: "Leave",
    gst: "GST",
    logout: "Logout",
    langToggle: "हिन्दी",
    sos: "SOS",
    iamOk: "I am ok",
    enableAlerts: "Enable visit alerts",
    alertsOff: "Visit alerts need VAPID keys on the API.",
    alertsOn: "This browser will get visit alerts.",
    rateCare: "Rate this care",
    replace: "Replace",
    blockLeave: "Block leave",
    woundPhoto: "Wound / ID photo",
    idPhoto: "ID photo",
    branches: "Branches",
    consumables: "Consumables",
    save: "Save",
    elderHome: "I am here"
  },
  hi: {
    help: "मदद",
    home: "होम",
    find: "खोजें",
    bills: "बिल",
    shifts: "शिफ्ट",
    jobs: "काम",
    desk: "डेस्क",
    inbox: "इनबॉक्स",
    discharge: "डिस्चार्ज",
    kyc: "केवाईसी",
    audit: "ऑडिट",
    leave: "छुट्टी",
    gst: "जीएसटी",
    logout: "लॉग आउट",
    langToggle: "English",
    sos: "एसओएस",
    iamOk: "मैं ठीक हूँ",
    enableAlerts: "विज़िट अलर्ट चालू करें",
    alertsOff: "अलर्ट के लिए API पर VAPID कुंजी चाहिए।",
    alertsOn: "यह ब्राउज़र विज़िट अलर्ट पाएगा।",
    rateCare: "देखभाल रेट करें",
    replace: "बदलें",
    blockLeave: "छुट्टी रोकें",
    woundPhoto: "घाव / पहचान फोटो",
    idPhoto: "पहचान फोटो",
    branches: "शाखाएँ",
    consumables: "सामान",
    save: "सेव",
    elderHome: "मैं यहाँ हूँ"
  }
};

const LocaleContext = createContext<{ lang: Lang; t: (k: string) => string; toggle: () => void } | null>(null);

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [lang, setLang] = useState<Lang>(() => (localStorage.getItem("lang") === "hi" ? "hi" : "en"));
  const value = useMemo(
    () => ({
      lang,
      t: (k: string) => copy[lang][k] ?? k,
      toggle: () => {
        const next = lang === "en" ? "hi" : "en";
        localStorage.setItem("lang", next);
        setLang(next);
      }
    }),
    [lang]
  );
  return <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>;
}

export function useT() {
  const ctx = useContext(LocaleContext);
  if (!ctx) throw new Error("LocaleProvider missing");
  return ctx;
}
