# CarePoint Frontend

The CarePoint web application provides responsive portals for patients, approved doctors, and administrators. It is built with React 19, TypeScript, Vite, Redux Toolkit, React Router, Axios, and React Hook Form.

## Local development

From the repository root:

```bash
npm --prefix frontend install
npm --prefix frontend run dev
```

The development server runs at `http://localhost:5173` and proxies `/api` requests to the backend configured by `VITE_API_PROXY_TARGET`.

## Environment

Optional frontend variables:

```text
VITE_API_BASE_URL=/api
VITE_API_PROXY_TARGET=http://127.0.0.1:5005
VITE_CLINIC_TIME_ZONE=Asia/Amman
```

`VITE_API_BASE_URL` defaults to `/api`. The clinic timezone should match the API's `CLINIC_TIME_ZONE` setting.

## Portal routes

- Patients: `/dashboard`, `/find-doctors`, `/my-appointments`, `/medical-history`, `/my-prescriptions`, `/my-documents`, `/my-profile`
- Doctors: `/doctor/dashboard`, `/doctor/appointments`, `/doctor/profile`
- Administrators: `/admin/dashboard`, `/admin/specialties`

Authentication uses short-lived access tokens plus rotating refresh tokens. A 401 response clears invalid local credentials, and logout is synchronized across browser tabs.

## Quality checks

```bash
npm run lint
npm run build
```

The role portal shell supports desktop side navigation and compact mobile navigation, keyboard focus indicators, and reduced-motion preferences.
