import { FormEvent, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api/client";
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
  const [score, setScore] = useState("6");
  const [error, setError] = useState("");

  async function load() {
    const [e, l, v, a, m, i, s, inv] = await Promise.all([
      api.get<EpisodeDto>(`/episodes/${episodeId}`),
      api.get<CareLogDto[]>(`/episodes/${episodeId}/logs`),
      api.get<VitalDto[]>(`/episodes/${episodeId}/vitals`),
      api.get<AdlDto[]>(`/episodes/${episodeId}/adl`),
      api.get<MarDto[]>(`/episodes/${episodeId}/mar`),
      api.get<IncidentDto[]>(`/episodes/${episodeId}/incidents`),
      api.get<ShiftDto[]>("/shifts", { params: { episodeId } }),
      api.get<InvoiceDto[]>("/invoices", { params: { episodeId } })
    ]);
    setEp(e.data);
    setLogs(l.data);
    setVitals(v.data);
    setAdl(a.data);
    setMar(m.data);
    setIncidents(i.data);
    setShifts(s.data);
    setInvoices(inv.data);
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
  const [staffId, setStaffId] = useState("");
  const [error, setError] = useState("");
  useEffect(() => {
    api
      .get<CareRequestDto[]>("/requests")
      .then((r) => setRows(r.data))
      .catch((e) => setError(e.message));
  }, []);
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Requests</h2>
      <Err error={error} />
      <input className="mb-2 w-full rounded-lg border px-3 py-2" placeholder="Staff caregiver id (agency)" value={staffId} onChange={(e) => setStaffId(e.target.value)} />
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
  useEffect(() => {
    Promise.all([api.get<ShiftDto[]>("/shifts"), api.get<EpisodeDto[]>("/episodes")]).then(([s, e]) => {
      setShifts(s.data);
      setEpisodes(e.data);
    });
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
  useEffect(() => {
    Promise.all([api.get("/agency/occupancy"), api.get("/agency/staff")]).then(([o, s]) => {
      setOcc(o.data);
      setStaff(s.data);
    });
  }, []);
  return (
    <div>
      <h2 className="mb-4 text-xl font-semibold">Agency</h2>
      {occ && (
        <p className="mb-3 text-sm text-slate-600">
          Hours rostered {occ.hoursRostered} · billed {occ.hoursBilled}
        </p>
      )}
      <ul className="space-y-2">
        {staff.map((s) => (
          <li key={s.id} className="rounded-lg bg-white p-3 shadow-sm">
            {s.fullName} · id {s.id} · KYC {s.kycStatus}
          </li>
        ))}
      </ul>
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
