# SevaSetu

Home nursing and elder care for India. Families, independent caregivers, agencies, hospitals, and platform admin share one care episode.

PhysioCard is a separate product. This folder is its own git repo. Do not add it as a gitlink in the parent HealthTech repository.

## Run locally

API: `http://localhost:5300` (database `SevaSetu` on local SQL Server)

```
dotnet run --project SevaSetu.Api --launch-profile http
```

Web: `http://localhost:5180`

```
cd SevaSetu.Web
$env:VITE_API_URL = "http://localhost:5300"
npm run dev
```

## Demo logins

- Family: family@sevasetu.local / Family@12345
- Nurse: nurse@sevasetu.local / Caregiver@12345
- Agency: agency@sevasetu.local / Agency@12345
- Hospital: hospital@sevasetu.local / Hospital@12345
- Admin: admin@sevasetu.local / Admin@12345

Cash/UPI is recorded, not collected. WhatsApp Cloud API is skipped when tokens are empty.
