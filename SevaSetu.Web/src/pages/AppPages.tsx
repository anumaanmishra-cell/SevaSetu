import { FormEvent, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { useT } from "../i18n";
import type {
  AdlDto,
  CareLogDto,
  CareRequestDto,
  DirectoryAgencyDto,
  DirectoryCaregiverDto,
  ElderDto,
  EpisodeDto,
  IncidentDto,
  InvoiceDto,
  MarDto,
  ShiftDto,
  VitalDto
} from "../api/types";
import { caregiverKind, requestStatus } from "../api/types";

function Err({ error }: { error: string }) {
  if (!error) return null;
  return <p className="mb-3 text-sm text-red-600">{error}</p>;
}

function PhotoThumb({ id, kind }: { id: number; kind: string }) {
  const [src, setSrc] = useState("");
  useEffect(() => {
    let url = "";
    let cancelled = false;
    api.get(`/photos/${id}`, { responseType: "blob" }).then((r) => {
      url = URL.createObjectURL(r.data);
      if (cancelled) URL.revokeObjectURL(url);
      else setSrc(url);
    });
    return () => {
      cancelled = true;
      if (url) URL.revokeObjectURL(url);
    };
  }, [id]);
  if (!src) return <span className="text-xs text-slate-500">{kind}</span>;
  return <img src={src} alt={kind} className="mt-1 h-24 w-24 rounded object-cover" />;
}

export function FamilyHome() {
  const [elders, setElders] = useState<ElderDto[]>([]);
  const [episodes, setEpisodes] = useState<EpisodeDto[]>([]);
  const [name, setName] = useState("");
  const [age, setAge] = useState("78");
  const [address, setAddress] = useState("");
  const [error, setError] = useState("");

  async function load() {
    const [e, ep] = await Promise.all([api.get<ElderDto[]>("/family/elders"), api.get<EpisodeDto[]>("/episodes")]);
    setElders(e.data);
    setEpisodes(ep.data);
  }
  useEffect(() => {
    load().catch((err) => setError(err.message));
  }, []);

  async function add(ev: FormEvent) {
    ev.preventDefault();
    await api.post("/family/elders", { fullName: name, age: Number(age), address, city: "Delhi NCR", consentHealth: true, consentPhotos: false });
    setName("");
    await load();
  }

  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Home</h2>
      <Err error={error} />
      <form onSubmit={add} className="mb-4 space-y-2 rounded-xl bg-white p-4 shadow-sm">
        <p className="text-sm font-medium">Add elder</p>
        <input className="w-full rounded-lg border px-3 py-2" placeholder="Name" value={name} onChange={(e) => setName(e.target.value)} required />
        <input className="w-full rounded-lg border px-3 py-2" placeholder="Age" value={age} onChange={(e) => setAge(e.target.value)} />
        <input className="w-full rounded-lg border px-3 py-2" placeholder="Address" value={address} onChange={(e) => setAddress(e.target.value)} required />
        <button className="rounded-lg bg-brand-600 px-4 py-2 text-white">Save</button>
      </form>
      <ul className="space-y-2">
        {elders.map((el) => (
          <li key={el.id} className="rounded-lg bg-white p-3 shadow-sm">
            {el.fullName} · {el.age}
            <button className="ml-3 text-sm text-brand-700" onClick={() => api.post(`/elders/${el.id}/sos`, { elderId: el.id, ok: false }).catch((e) => setError(e.message))}>
              SOS
            </button>
          </li>
        ))}
      </ul>
      <h3 className="mb-2 mt-6 font-medium">Episodes</h3>
      <ul className="space-y-2">
        {episodes.map((ep) => (
          <li key={ep.id} className="rounded-lg bg-white p-3 shadow-sm">
            <Link className="text-brand-700" to={`/family/episodes/${ep.id}`}>
              {ep.elderName} · {ep.planTemplate}
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function FamilySearch() {
  const [elders, setElders] = useState<ElderDto[]>([]);
  const [caregivers, setCaregivers] = useState<DirectoryCaregiverDto[]>([]);
  const [agencies, setAgencies] = useState<DirectoryAgencyDto[]>([]);
  const [elderId, setElderId] = useState("");
  const [error, setError] = useState("");
  useEffect(() => {
    Promise.all([api.get<ElderDto[]>("/family/elders"), api.get<DirectoryCaregiverDto[]>("/directory/caregivers"), api.get<DirectoryAgencyDto[]>("/directory/agencies")])
      .then(([e, c, a]) => {
        setElders(e.data);
        setCaregivers(c.data);
        setAgencies(a.data);
        if (e.data[0]) setElderId(String(e.data[0].id));
      })
      .catch((err) => setError(err.message));
  }, []);

  async function book(caregiverId: number | null, agencyId: number | null, instant: boolean) {
    setError("");
    const start = new Date();
    start.setHours(start.getHours() + 3);
    const end = new Date(start.getTime() + 8 * 3600000);
    try {
      await api.post("/family/requests", {
        elderId: Number(elderId),
        caregiverId,
        agencyId,
        mode: 0,
        startAt: start.toISOString(),
        endAt: end.toISOString(),
        instantBook: instant
      });
      alert("Request sent.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    }
  }

  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Find care</h2>
      <Err error={error} />
      <select className="mb-3 w-full rounded-lg border px-3 py-2" value={elderId} onChange={(e) => setElderId(e.target.value)}>
        {elders.map((e) => (
          <option key={e.id} value={e.id}>
            {e.fullName}
          </option>
        ))}
      </select>
      <h3 className="mb-2 font-medium">Independent caregivers (KYC passed)</h3>
      <ul className="space-y-2">
        {caregivers.map((c) => (
          <li key={c.id} className="rounded-lg bg-white p-3 shadow-sm">
            <p className="font-medium">{c.fullName}</p>
            <p className="text-sm text-slate-500">
              {caregiverKind[c.kind]} · ₹{c.hourlyRate}/hr · {c.city}
              {c.rating ? ` · ${c.rating}★` : ""}
            </p>
            <button className="mr-2 text-sm text-brand-700" onClick={() => book(c.id, null, false)}>
              Request
            </button>
            <button className="text-sm text-brand-700" onClick={() => book(c.id, null, true)}>
              Instant book
            </button>
          </li>
        ))}
      </ul>
      <h3 className="mb-2 mt-4 font-medium">Agencies</h3>
      <ul className="space-y-2">
        {agencies.map((a) => (
          <li key={a.id} className="rounded-lg bg-white p-3 shadow-sm">
            {a.name} · ₹{a.hourlyRate}/hr
            <button className="ml-3 text-sm text-brand-700" onClick={() => book(null, a.id, false)}>
              Request agency
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function EpisodeDesk({ prefix }: { prefix: string }) {
  const { id } = useParams();
  const episodeId = Number(id);
  const [ep, setEp] = useState<EpisodeDto | null>(null);
  const [logs, setLogs] = useState<CareLogDto[]>([]);
  const [vitals, setVitals] = useState<VitalDto[]>([]);
  const [adl, setAdl] = useState<AdlDto[]>([]);
  const [mar, setMar] = useState<MarDto[]>([]);
  const [incidents, setIncidents] = useState<IncidentDto[]>([]);
  const [shifts, setShifts] = useState<ShiftDto[]>([]);
  const [invoices, setInvoices] = useState<InvoiceDto[]>([]);
  const [photos, setPhotos] = useState<{ id: number; kind: string; url: string }[]>([]);
  const [ratings, setRatings] = useState<{ stars: number; comment?: string | null }[]>([]);
  const [score, setScore] = useState("6");
  const [stars, setStars] = useState("5");
  const [error, setError] = useState("");
  const { role } = useAuth();
  const { t } = useT();

  async function load() {
    const [e, l, v, a, m, i, s, inv, p, r] = await Promise.all([
      api.get<EpisodeDto>(`/episodes/${episodeId}`),
      api.get<CareLogDto[]>(`/episodes/${episodeId}/logs`),
      api.get<VitalDto[]>(`/episodes/${episodeId}/vitals`),
      api.get<AdlDto[]>(`/episodes/${episodeId}/adl`),
      api.get<MarDto[]>(`/episodes/${episodeId}/mar`),
      api.get<IncidentDto[]>(`/episodes/${episodeId}/incidents`),
      api.get<ShiftDto[]>("/shifts", { params: { episodeId } }),
      api.get<InvoiceDto[]>("/invoices", { params: { episodeId } }),
      api.get<{ id: number; kind: string; url: string }[]>(`/episodes/${episodeId}/photos`),
      api.get<{ stars: number; comment?: string | null }[]>(`/episodes/${episodeId}/ratings`)
    ]);
    setEp(e.data);
    setLogs(l.data);
    setVitals(v.data);
    setAdl(a.data);
    setMar(m.data);
    setIncidents(i.data);
    setShifts(s.data);
    setInvoices(inv.data);
    setPhotos(p.data);
    setRatings(r.data);
  }
  useEffect(() => {
    load().catch((err) => setError(err.message));
  }, [episodeId]);

  return (
    <div>
      <Link to={prefix} className="text-sm text-brand-700">
        ← Back
      </Link>
      <Err error={error} />
      {ep && (
        <h2 className="my-3 text-xl font-semibold">
          {ep.elderName} · {ep.planTemplate}
        </h2>
      )}
      <div className="mb-4 rounded-xl bg-white p-4 shadow-sm">
        <p className="mb-2 font-medium">Today’s comfort (1–10)</p>
        <form
          className="flex gap-2"
          onSubmit={async (e) => {
            e.preventDefault();
            await api.post(`/episodes/${episodeId}/log`, { comfortScore: Number(score) });
            await load();
          }}
        >
          <input className="w-20 rounded-lg border px-2 py-1" value={score} onChange={(e) => setScore(e.target.value)} />
          <button className="rounded-lg bg-brand-600 px-3 py-1 text-white">Save log</button>
        </form>
        <div className="mt-2 flex gap-2">
          <button className="text-sm text-brand-700" onClick={() => api.post(`/episodes/${episodeId}/adl`, { activity: "Bathing" }).then(load)}>
            ADL bathing
          </button>
          <button
            className="text-sm text-brand-700"
            onClick={() =>
              api.post(`/episodes/${episodeId}/mar`, { drug: "Amlodipine", dose: "5mg", scheduledAt: new Date().toISOString(), status: 0, witnessName: "Nurse" }).then(load)
            }
          >
            MAR given
          </button>
          <button className="text-sm text-brand-700" onClick={() => api.post(`/episodes/${episodeId}/incidents`, { kind: 0, note: "Near fall in corridor" }).then(load)}>
            Incident
          </button>
        </div>
      </div>
      <p className="text-sm font-medium">Last 14 days</p>
      <ul className="mb-4 text-sm">
        {logs.map((l) => (
          <li key={l.id}>
            {l.logDate.slice(0, 10)} · comfort {l.comfortScore} {l.note ?? ""}
          </li>
        ))}
      </ul>
      <p className="text-sm font-medium">Vitals</p>
      <ul className="mb-4 text-sm">
        {vitals.map((v) => (
          <li key={v.id}>
            {v.logDate.slice(0, 10)} · {v.systolic}/{v.diastolic}
          </li>
        ))}
      </ul>
      <p className="text-sm font-medium">Shifts</p>
      <ul className="mb-4 space-y-1 text-sm">
        {shifts.map((s) => (
          <li key={s.id} className="rounded bg-white p-2">
            {s.caregiverName} · {new Date(s.slotStart).toLocaleString()}
            <button className="ml-2 text-brand-700" onClick={() => api.post(`/shifts/${s.id}/check-in`, {}).then(load)}>
              Check in
            </button>
            <button className="ml-2 text-brand-700" onClick={() => api.post(`/shifts/${s.id}/check-out`, {}).then(load)}>
              Out
            </button>
            <button className="ml-2 text-brand-700" onClick={() => api.post(`/shifts/${s.id}/no-show`).then(load)}>
              No-show
            </button>
          </li>
        ))}
      </ul>
      <p className="text-sm font-medium">MAR / ADL / incidents</p>
      <ul className="mb-4 text-sm">
        {mar.map((m) => (
          <li key={m.id}>
            {m.drug} {m.dose}
          </li>
        ))}
        {adl.map((a) => (
          <li key={a.id}>{a.activity}</li>
        ))}
        {incidents.map((i) => (
          <li key={i.id}>{i.note}</li>
        ))}
      </ul>
      <p className="text-sm font-medium">{t("woundPhoto")}</p>
      <form
        className="mb-3 flex gap-2"
        onSubmit={async (e) => {
          e.preventDefault();
          const input = (e.target as HTMLFormElement).elements.namedItem("photo") as HTMLInputElement;
          if (!input.files?.[0]) return;
          const body = new FormData();
          body.append("file", input.files[0]);
          body.append("kind", "Wound");
          await api.post(`/episodes/${episodeId}/photos`, body);
          await load();
        }}
      >
        <input name="photo" type="file" accept="image/jpeg,image/png,image/webp" />
        <button className="text-sm text-brand-700">{t("save")}</button>
      </form>
      <ul className="mb-4 flex flex-wrap gap-3 text-sm">
        {photos.map((ph) => (
          <li key={ph.id}>
            <p>{ph.kind}</p>
            <PhotoThumb id={ph.id} kind={ph.kind} />
          </li>
        ))}
      </ul>
      {role === "Family" && ep && new Date(ep.startDate).getTime() <= Date.now() - 7 * 86400000 && ratings.length === 0 && (
        <form
          className="mb-4 rounded-xl bg-white p-4 shadow-sm"
          onSubmit={async (e) => {
            e.preventDefault();
            await api.post(`/episodes/${episodeId}/rate`, { stars: Number(stars) });
            await load();
          }}
        >
          <p className="mb-2 font-medium">{t("rateCare")}</p>
          <input className="w-16 rounded-lg border px-2 py-1" value={stars} onChange={(e) => setStars(e.target.value)} />
          <button className="ml-2 rounded-lg bg-brand-600 px-3 py-1 text-white">{t("save")}</button>
        </form>
      )}
      {ratings.length > 0 && <p className="mb-4 text-sm">Rated {ratings[0].stars}★</p>}
      <p className="text-sm font-medium">Bills</p>
      <button
        className="mb-2 text-sm text-brand-700"
        onClick={() =>
          api
            .post(`/episodes/${episodeId}/invoices`, {
              periodStart: new Date().toISOString(),
              periodEnd: new Date().toISOString(),
              hoursOrDays: 8,
              liveIn: false
            })
            .then(load)
        }
      >
        Issue invoice
      </button>
      <ul className="text-sm">
        {invoices.map((inv) => (
          <li key={inv.id}>
            {inv.number} · ₹{inv.total} · {inv.paymentStatus === 1 ? "Paid" : "Unpaid"} · {inv.gstEnabledSnapshot ? "GST" : "simple bill"}
            <button className="ml-2 text-brand-700" onClick={() => api.put(`/invoices/${inv.id}/pay`, { mode: 1 }).then(load)}>
              Mark UPI
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function InboxPage() {
  const [rows, setRows] = useState<CareRequestDto[]>([]);
  const [staff, setStaff] = useState<{ id: number; fullName: string }[]>([]);
  const [staffId, setStaffId] = useState("");
  const [error, setError] = useState("");
  const { role } = useAuth();
  useEffect(() => {
    api
      .get<CareRequestDto[]>("/requests")
      .then((r) => setRows(r.data))
      .catch((e) => setError(e.message));
    if (role === "AgencyAdmin") {
      api.get<{ id: number; fullName: string }[]>("/agency/staff").then((s) => {
        setStaff(s.data);
        if (s.data[0]) setStaffId(String(s.data[0].id));
      });
    }
  }, [role]);
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Requests</h2>
      <Err error={error} />
      {role === "AgencyAdmin" && (
        <select className="mb-2 w-full rounded-lg border px-3 py-2" value={staffId} onChange={(e) => setStaffId(e.target.value)}>
          {staff.map((s) => (
            <option key={s.id} value={s.id}>
              {s.fullName}
            </option>
          ))}
        </select>
      )}
      <ul className="space-y-2">
        {rows.map((r) => (
          <li key={r.id} className="rounded-lg bg-white p-3 shadow-sm">
            {r.elderName} · {requestStatus[r.status]}
            <button className="ml-2 text-sm text-brand-700" onClick={() => api.post(`/requests/${r.id}/respond`, null, { params: { accept: true, staffCaregiverId: staffId || undefined } }).then(() => location.reload())}>
              Accept
            </button>
            <button className="ml-2 text-sm text-red-600" onClick={() => api.post(`/requests/${r.id}/respond`, null, { params: { accept: false } }).then(() => location.reload())}>
              Decline
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function CaregiverHome() {
  const [shifts, setShifts] = useState<ShiftDto[]>([]);
  const [episodes, setEpisodes] = useState<EpisodeDto[]>([]);
  const [blocks, setBlocks] = useState<{ id: number; startAt: string; endAt: string }[]>([]);
  const [startAt, setStartAt] = useState("");
  const [endAt, setEndAt] = useState("");
  const { t } = useT();
  async function load() {
    const [s, e, b] = await Promise.all([api.get<ShiftDto[]>("/shifts"), api.get<EpisodeDto[]>("/episodes"), api.get<{ id: number; startAt: string; endAt: string }[]>("/calendar/blocks")]);
    setShifts(s.data);
    setEpisodes(e.data);
    setBlocks(b.data);
  }
  useEffect(() => {
    load().catch(() => undefined);
  }, []);
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">My shifts</h2>
      <ul className="space-y-2">
        {shifts.map((s) => (
          <li key={s.id} className="rounded-lg bg-white p-3 shadow-sm">
            {new Date(s.slotStart).toLocaleString()}
            <Link className="ml-3 text-brand-700" to={`/caregiver/episodes/${s.episodeId}`}>
              Open episode
            </Link>
          </li>
        ))}
      </ul>
      <form
        className="mt-4 space-y-2 rounded-xl bg-white p-4 shadow-sm"
        onSubmit={async (e) => {
          e.preventDefault();
          await api.post("/calendar/block", { startAt: new Date(startAt).toISOString(), endAt: new Date(endAt).toISOString() });
          await load();
        }}
      >
        <p className="font-medium">{t("blockLeave")}</p>
        <input className="w-full rounded-lg border px-3 py-2" type="datetime-local" value={startAt} onChange={(e) => setStartAt(e.target.value)} required />
        <input className="w-full rounded-lg border px-3 py-2" type="datetime-local" value={endAt} onChange={(e) => setEndAt(e.target.value)} required />
        <button className="rounded-lg bg-brand-600 px-4 py-2 text-white">{t("save")}</button>
      </form>
      <ul className="mt-2 text-sm">
        {blocks.map((b) => (
          <li key={b.id}>
            {new Date(b.startAt).toLocaleString()} → {new Date(b.endAt).toLocaleString()}
          </li>
        ))}
      </ul>
      <form
        className="mt-4 space-y-2 rounded-xl bg-white p-4 shadow-sm"
        onSubmit={async (e) => {
          e.preventDefault();
          const input = (e.target as HTMLFormElement).elements.namedItem("idphoto") as HTMLInputElement;
          if (!input.files?.[0]) return;
          const body = new FormData();
          body.append("file", input.files[0]);
          await api.post("/me/id-photo", body);
          alert("ID photo stored outside SQL.");
        }}
      >
        <p className="font-medium">{t("idPhoto")}</p>
        <input name="idphoto" type="file" accept="image/jpeg,image/png,image/webp" />
        <button className="rounded-lg bg-brand-600 px-4 py-2 text-white">{t("save")}</button>
      </form>
      <h3 className="mb-2 mt-4 font-medium">Episodes</h3>
      {episodes.map((e) => (
        <p key={e.id}>
          <Link className="text-brand-700" to={`/caregiver/episodes/${e.id}`}>
            {e.elderName}
          </Link>
        </p>
      ))}
    </div>
  );
}

export function AgencyHome() {
  const [occ, setOcc] = useState<{ hoursRostered: number; hoursBilled: number } | null>(null);
  const [staff, setStaff] = useState<{ id: number; fullName: string; kind: number; kycStatus: number }[]>([]);
  const [shifts, setShifts] = useState<ShiftDto[]>([]);
  const [staffId, setStaffId] = useState("");
  const [branches, setBranches] = useState<{ id: number; name: string; city: string }[]>([]);
  const [consumables, setConsumables] = useState<{ id: number; elderId: number; item: string; quantity: number }[]>([]);
  const [episodes, setEpisodes] = useState<EpisodeDto[]>([]);
  const [branchName, setBranchName] = useState("");
  const [branchCity, setBranchCity] = useState("Delhi NCR");
  const [item, setItem] = useState("Gloves");
  const [qty, setQty] = useState("1");
  const [elderId, setElderId] = useState("");
  const [error, setError] = useState("");
  const { t } = useT();

  async function load() {
    const [o, s, sh, b, c, e] = await Promise.all([
      api.get("/agency/occupancy"),
      api.get<{ id: number; fullName: string; kind: number; kycStatus: number }[]>("/agency/staff"),
      api.get<ShiftDto[]>("/shifts"),
      api.get<{ id: number; name: string; city: string }[]>("/agency/branches"),
      api.get<{ id: number; elderId: number; item: string; quantity: number }[]>("/agency/consumables"),
      api.get<EpisodeDto[]>("/episodes")
    ]);
    setOcc(o.data);
    setStaff(s.data);
    setShifts(sh.data);
    setBranches(b.data);
    setConsumables(c.data);
    setEpisodes(e.data);
    if (s.data[0] && !staffId) setStaffId(String(s.data[0].id));
    if (e.data[0] && !elderId) setElderId(String(e.data[0].elderId));
  }
  useEffect(() => {
    load().catch((err) => setError(err.message));
  }, []);

  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Agency</h2>
      <Err error={error} />
      {occ && (
        <p className="mb-3 text-sm text-slate-600">
          Hours rostered {occ.hoursRostered} · billed {occ.hoursBilled}
        </p>
      )}
      <ul className="space-y-2">
        {staff.map((s) => (
          <li key={s.id} className="rounded-lg bg-white p-3 shadow-sm">
            {s.fullName} · KYC {s.kycStatus}
          </li>
        ))}
      </ul>
      <h3 className="mb-2 mt-6 font-medium">{t("replace")}</h3>
      <select className="mb-2 w-full rounded-lg border px-3 py-2" value={staffId} onChange={(e) => setStaffId(e.target.value)}>
        {staff.map((s) => (
          <option key={s.id} value={s.id}>
            {s.fullName}
          </option>
        ))}
      </select>
      <ul className="space-y-2">
        {shifts.map((s) => (
          <li key={s.id} className="rounded-lg bg-white p-3 text-sm shadow-sm">
            {s.caregiverName} · {new Date(s.slotStart).toLocaleString()}
            <button
              className="ml-2 text-brand-700"
              onClick={() => api.post(`/shifts/${s.id}/replace`, { newCaregiverId: Number(staffId) }).then(load).catch((err) => setError(err.message))}
            >
              {t("replace")}
            </button>
          </li>
        ))}
      </ul>
      <form
        className="mt-6 space-y-2 rounded-xl bg-white p-4 shadow-sm"
        onSubmit={async (e) => {
          e.preventDefault();
          await api.post("/agency/branches", { name: branchName, city: branchCity });
          setBranchName("");
          await load();
        }}
      >
        <p className="font-medium">{t("branches")}</p>
        <input className="w-full rounded-lg border px-3 py-2" placeholder="Name" value={branchName} onChange={(e) => setBranchName(e.target.value)} required />
        <input className="w-full rounded-lg border px-3 py-2" placeholder="City" value={branchCity} onChange={(e) => setBranchCity(e.target.value)} required />
        <button className="rounded-lg bg-brand-600 px-4 py-2 text-white">{t("save")}</button>
      </form>
      <ul className="mt-2 text-sm">
        {branches.map((b) => (
          <li key={b.id}>
            {b.name} · {b.city}
          </li>
        ))}
      </ul>
      <form
        className="mt-4 space-y-2 rounded-xl bg-white p-4 shadow-sm"
        onSubmit={async (e) => {
          e.preventDefault();
          await api.post("/agency/consumables", { elderId: Number(elderId), item, quantity: Number(qty) });
          await load();
        }}
      >
        <p className="font-medium">{t("consumables")}</p>
        <select className="w-full rounded-lg border px-3 py-2" value={elderId} onChange={(e) => setElderId(e.target.value)}>
          {episodes.map((ep) => (
            <option key={ep.id} value={ep.elderId}>
              {ep.elderName}
            </option>
          ))}
        </select>
        <input className="w-full rounded-lg border px-3 py-2" value={item} onChange={(e) => setItem(e.target.value)} required />
        <input className="w-full rounded-lg border px-3 py-2" value={qty} onChange={(e) => setQty(e.target.value)} />
        <button className="rounded-lg bg-brand-600 px-4 py-2 text-white">{t("save")}</button>
      </form>
      <ul className="mt-2 text-sm">
        {consumables.map((c) => (
          <li key={c.id}>
            {c.item} × {c.quantity}
          </li>
        ))}
      </ul>
    </div>
  );
}

export function GstForm() {
  const { t } = useT();
  const [gstEnabled, setGstEnabled] = useState(false);
  const [gstin, setGstin] = useState("");
  const [legalName, setLegalName] = useState("");
  const [address, setAddress] = useState("");
  const [cgst, setCgst] = useState("9");
  const [sgst, setSgst] = useState("9");
  const [igst, setIgst] = useState("0");
  const [error, setError] = useState("");
  const [saved, setSaved] = useState("");
  useEffect(() => {
    api
      .get<{ gstEnabled: boolean; gstin?: string | null; legalName?: string | null; address?: string | null; cgstPercent: number; sgstPercent: number; igstPercent: number }>("/me/gst")
      .then((r) => {
        setGstEnabled(r.data.gstEnabled);
        setGstin(r.data.gstin ?? "");
        setLegalName(r.data.legalName ?? "");
        setAddress(r.data.address ?? "");
        setCgst(String(r.data.cgstPercent));
        setSgst(String(r.data.sgstPercent));
        setIgst(String(r.data.igstPercent));
      })
      .catch((err) => setError(err.message));
  }, []);
  return (
    <form
      className="space-y-2 rounded-xl bg-white p-4 shadow-sm"
      onSubmit={async (e) => {
        e.preventDefault();
        setError("");
        setSaved("");
        try {
          await api.put("/me/gst", {
            gstEnabled,
            gstin,
            legalName,
            address,
            cgstPercent: Number(cgst),
            sgstPercent: Number(sgst),
            igstPercent: Number(igst)
          });
          setSaved("Saved. New invoices will snapshot this GST setting.");
        } catch (err) {
          setError(err instanceof Error ? err.message : "Failed");
        }
      }}
    >
      <h2 className="text-xl font-semibold">{t("gst")}</h2>
      <Err error={error} />
      {saved && <p className="text-sm text-brand-700">{saved}</p>}
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" checked={gstEnabled} onChange={(e) => setGstEnabled(e.target.checked)} />
        Charge GST on new tax invoices
      </label>
      <input className="w-full rounded-lg border px-3 py-2" placeholder="GSTIN" value={gstin} onChange={(e) => setGstin(e.target.value)} />
      <input className="w-full rounded-lg border px-3 py-2" placeholder="Legal name" value={legalName} onChange={(e) => setLegalName(e.target.value)} />
      <input className="w-full rounded-lg border px-3 py-2" placeholder="Address" value={address} onChange={(e) => setAddress(e.target.value)} />
      <div className="grid grid-cols-3 gap-2">
        <input className="rounded-lg border px-3 py-2" placeholder="CGST %" value={cgst} onChange={(e) => setCgst(e.target.value)} />
        <input className="rounded-lg border px-3 py-2" placeholder="SGST %" value={sgst} onChange={(e) => setSgst(e.target.value)} />
        <input className="rounded-lg border px-3 py-2" placeholder="IGST %" value={igst} onChange={(e) => setIgst(e.target.value)} />
      </div>
      <button className="rounded-lg bg-brand-600 px-4 py-2 text-white">{t("save")}</button>
    </form>
  );
}

export function ElderHome() {
  const { t } = useT();
  const [elder, setElder] = useState<ElderDto | null>(null);
  const [msg, setMsg] = useState("");
  const [error, setError] = useState("");
  useEffect(() => {
    api
      .get<ElderDto>("/me/elder")
      .then((r) => setElder(r.data))
      .catch((err) => setError(err.message));
  }, []);
  async function ping(ok: boolean) {
    if (!elder) return;
    setError("");
    await api.post(`/elders/${elder.id}/sos`, { elderId: elder.id, ok });
    setMsg(ok ? t("iamOk") : t("sos"));
  }
  return (
    <div className="mx-auto max-w-sm pt-8 text-center">
      <h2 className="mb-2 text-2xl font-semibold">{elder?.fullName ?? t("elderHome")}</h2>
      <Err error={error} />
      {msg && <p className="mb-4 text-sm text-brand-700">{msg}</p>}
      <button className="mb-3 w-full rounded-2xl bg-red-600 py-6 text-xl font-semibold text-white" onClick={() => ping(false).catch((e) => setError(e.message))}>
        {t("sos")}
      </button>
      <button className="w-full rounded-2xl bg-brand-600 py-6 text-xl font-semibold text-white" onClick={() => ping(true).catch((e) => setError(e.message))}>
        {t("iamOk")}
      </button>
    </div>
  );
}

export function HospitalHome() {
  const [elders, setElders] = useState<ElderDto[]>([]);
  const [elderId, setElderId] = useState("");
  const [visits, setVisits] = useState<{ id: number; status: number; note?: string }[]>([]);
  useEffect(() => {
    api.get("/hospital/visits").then((v) => setVisits(v.data));
    api.get<EpisodeDto[]>("/episodes").then((e) => {
      /* coordinators see episodes; elder list via packet form uses id */
      if (e.data[0]) setElderId(String(e.data[0].elderId));
    });
  }, []);
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Hospital discharge</h2>
      <form
        className="mb-4 space-y-2 rounded-xl bg-white p-4"
        onSubmit={async (e) => {
          e.preventDefault();
          await api.post("/hospital/packet", { elderId: Number(elderId), diagnosis: "Post-op", meds: "As charted", followUpOn: new Date().toISOString() });
          alert("Packet saved");
        }}
      >
        <input className="w-full rounded-lg border px-3 py-2" placeholder="Elder id" value={elderId} onChange={(e) => setElderId(e.target.value)} />
        <button className="rounded-lg bg-brand-600 px-4 py-2 text-white">Save packet</button>
      </form>
      <ul className="text-sm">
        {visits.map((v) => (
          <li key={v.id}>Visit {v.id} · status {v.status}</li>
        ))}
      </ul>
    </div>
  );
}

export function AdminHome() {
  const [queue, setQueue] = useState<{ id: number; fullName: string; phone: string; city: string }[]>([]);
  useEffect(() => {
    api.get("/admin/kyc").then((r) => setQueue(r.data));
  }, []);
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">KYC</h2>
      <ul className="space-y-2">
        {queue.map((q) => (
          <li key={q.id} className="rounded-lg bg-white p-3 shadow-sm">
            {q.fullName} · {q.phone}
            <button className="ml-2 text-brand-700" onClick={() => api.post(`/admin/kyc/${q.id}`, { status: 1 }).then(() => location.reload())}>
              Pass
            </button>
            <button className="ml-2 text-red-600" onClick={() => api.post(`/admin/kyc/${q.id}`, { status: 2, reason: "Documents unclear" }).then(() => location.reload())}>
              Fail
            </button>
          </li>
        ))}
      </ul>
      <Link className="mt-4 inline-block text-sm text-brand-700" to="/admin/audit">
        Audit log
      </Link>
    </div>
  );
}

export function AdminAudit() {
  const [rows, setRows] = useState<{ id: number; action: string; at: string; elderId?: number }[]>([]);
  useEffect(() => {
    api.get("/admin/audit").then((r) => setRows(r.data));
  }, []);
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Audit</h2>
      <ul className="text-sm">
        {rows.map((r) => (
          <li key={r.id}>
            {r.at} · {r.action} · elder {r.elderId}
          </li>
        ))}
      </ul>
    </div>
  );
}

export function FamilyBills() {
  const [rows, setRows] = useState<InvoiceDto[]>([]);
  useEffect(() => {
    api.get<InvoiceDto[]>("/invoices").then((r) => setRows(r.data));
  }, []);
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Bills</h2>
      <button className="mb-3 text-sm text-brand-700" onClick={() => api.put("/me/whatsapp-opt-out", null, { params: { optOut: true } })}>
        Opt out of WhatsApp
      </button>
      <button className="mb-3 ml-3 text-sm text-brand-700" onClick={() => api.get("/family/export").then((r) => alert(JSON.stringify(r.data).slice(0, 200)))}>
        Export my data
      </button>
      <ul className="space-y-2">
        {rows.map((r) => (
          <li key={r.id} className="rounded-lg bg-white p-3 shadow-sm">
            {r.number} · ₹{r.total} {r.isCommission ? "(commission)" : ""}
          </li>
        ))}
      </ul>
    </div>
  );
}
