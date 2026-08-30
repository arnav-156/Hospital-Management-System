import { StrictMode, useCallback, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';

const activePageStorageKey = 'activePage';
const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5141';
async function api(path, options = {}) {
  const response = await fetch(`${apiUrl}${path}`, { ...options, headers: { Authorization: `Bearer ${sessionStorage.getItem('accessToken')}`, 'Content-Type': 'application/json', ...(options.headers ?? {}) } });
  const data = response.status === 204 ? null : await response.json();
  if (response.status === 401) {
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('currentUser');
    sessionStorage.removeItem(activePageStorageKey);
    window.dispatchEvent(new Event('hospital:unauthorized'));
    throw new Error('Your session has expired. Please sign in again.');
  }
  if (!response.ok) throw new Error(data.detail ?? 'Request failed.');
  return data;
}

const pages = {
  Patient: ['Dashboard', 'Profile', 'Departments', 'Doctors', 'Book appointment', 'My appointments', 'Treatment history', 'Bills', 'Notifications', 'Feedback'],
  Doctor: ['Dashboard', 'Profile', 'Pending appointments', 'Today', 'Patient history', 'Treatment & prescription', 'Billing'],
  Administrator: ['Dashboard', 'Patients', 'Doctors', 'Staff', 'Departments'],
};
const pageForRole = (role, candidate) => pages[role]?.includes(candidate) ? candidate : 'Dashboard';
const savedPageForRole = (role) => pageForRole(role, sessionStorage.getItem(activePageStorageKey));
const fetchPagedRecords = async (path, page, pageSize) => {
  const separator = path.includes('?') ? '&' : '?';
  const records = await api(`${path}${separator}page=${page}&pageSize=${pageSize}`);
  const hasNext = records.length === pageSize && (await api(`${path}${separator}page=${page + 1}&pageSize=${pageSize}`)).length > 0;
  return { records, hasNext };
};

function App() {
  const savedUser = JSON.parse(sessionStorage.getItem('currentUser') ?? 'null');
  const [role, setRole] = useState(savedUser?.role ?? 'Patient');
  const [page, setPage] = useState(() => savedPageForRole(savedUser?.role ?? 'Patient'));
  const [signedIn, setSignedIn] = useState(() => Boolean(sessionStorage.getItem('accessToken')));
  const [notice, setNotice] = useState('');
  const menu = pages[role];
  const persistPage = (next) => { sessionStorage.setItem(activePageStorageKey, next); setPage(next); };
  const navigate = (next) => { persistPage(pageForRole(role, next)); setNotice(''); };
  const resetSession = useCallback(() => {
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('currentUser');
    sessionStorage.removeItem(activePageStorageKey);
    setPage('Dashboard');
    setNotice('');
    setSignedIn(false);
  }, []);
  useEffect(() => {
    if (!sessionStorage.getItem('accessToken')) return;
    api('/api/auth/me').then((user) => {
      sessionStorage.setItem('currentUser', JSON.stringify(user));
      setRole(user.role);
      setPage((current) => {
        const next = pageForRole(user.role, current);
        sessionStorage.setItem(activePageStorageKey, next);
        return next;
      });
    }).catch(resetSession);
  }, [resetSession]);
  useEffect(() => {
    window.addEventListener('hospital:unauthorized', resetSession);
    return () => window.removeEventListener('hospital:unauthorized', resetSession);
  }, [resetSession]);
  if (!signedIn) return <Auth onSignIn={async (user, isNewPatient) => { setNotice(''); setRole(user.role); let next = 'Dashboard'; if (isNewPatient && user.role === 'Patient') { next = 'Profile'; } else if (user.role === 'Patient') { try { await api('/api/profile/me'); } catch (error) { next = error.message.includes('Patient profile not found') ? 'Profile' : 'Dashboard'; } } persistPage(next); setSignedIn(true); }} />;
  return <div className="shell"><aside><div className="brand">Medi<span>Core</span></div><p className="role">{role} portal</p>{menu.map((item) => <button key={item} className={page === item ? 'nav active' : 'nav'} onClick={() => navigate(item)}>{item}</button>)}<button className="signout" onClick={resetSession}>Sign out</button></aside><main className="content"><header><div><p className="eyebrow">Hospital management system</p><h1>{page}</h1></div><p className="role">{role}</p></header>{notice && <p className="notice">{notice}</p>}<Page role={role} page={page} onNotice={setNotice} onUnauthorized={resetSession} /></main></div>;
}

function Auth({ onSignIn }) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [mode, setMode] = useState('login');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const isRegistration = mode === 'register';
  const submit = async (event) => {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      const response = await fetch(`${apiUrl}/api/auth/${isRegistration ? 'register' : 'login'}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.detail ?? `${isRegistration ? 'Registration' : 'Sign-in'} failed.`);
      sessionStorage.setItem('accessToken', data.accessToken);
      sessionStorage.setItem('currentUser', JSON.stringify(data.user));
      onSignIn(data.user, isRegistration);
    } catch (reason) {
      setError(reason.message);
    } finally {
      setBusy(false);
    }
  };
  return <main className="auth"><section><div className="brand">Medi<span>Core</span></div><h1>{isRegistration ? 'Start your care journey.' : 'Care, connected.'}</h1><p>Manage appointments, treatment, bills and communication from one secure place.</p><form onSubmit={submit}><label>Email<input value={email} onChange={(event) => setEmail(event.target.value)} type="email" required placeholder="you@example.com" /></label><label>Password<input value={password} onChange={(event) => setPassword(event.target.value)} type="password" required minLength="12" placeholder="••••••••••••" /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Please wait…' : isRegistration ? 'Create patient account' : 'Sign in'}</button></form><p className="muted">{isRegistration ? 'Already registered? ' : 'New here? '}<button type="button" className="link-button" onClick={() => { setMode(isRegistration ? 'login' : 'register'); setError(''); }}>{isRegistration ? 'Sign in' : 'Create a patient account'}</button></p></section><div className="auth-art"><p>YOUR HEALTHCARE, IN ONE VIEW</p><strong>Simple workflows.<br/>Better care.</strong></div></main>;
}

function Page({ role, page, onNotice, onUnauthorized }) {
  const pageSize = 25;
  const [state, setState] = useState({ loading: false, error: '', records: null, isList: false, hasNext: false });
  const [reloadVersion, setReloadVersion] = useState(0);
  const [listPage, setListPage] = useState(1);
  const routes = { ...(role === 'Patient' ? {} : { Profile: '/api/profile/me' }), Departments: role === 'Administrator' ? undefined : '/api/departments', Doctors: '/api/doctors', 'My appointments': role === 'Patient' ? undefined : '/api/appointments/my/summaries', Bills: '/api/bills/my', Notifications: '/api/notifications', 'Pending appointments': '/api/doctor/appointments/pending', Today: '/api/doctor/appointments/today', Patients: '/api/admin/patients', Staff: '/api/admin/staff' };
  useEffect(() => { setListPage(1); }, [page]);
  useEffect(() => { const route = routes[page]; if (!route && page !== 'Treatment history') { setState({ loading: false, error: '', records: null, isList: false, hasNext: false }); return; } let active = true; setState({ loading: true, error: '', records: null, isList: false, hasNext: false }); const load = async () => { try { let target = route; if (page === 'Treatment history') { const profile = await api('/api/profile/me'); target = `/api/patients/${profile.patientId}/history`; } const result = await fetchPagedRecords(target, listPage, pageSize); const isList = Array.isArray(result.records); if (active) setState({ loading: false, error: '', records: isList ? result.records : [result.records], isList, hasNext: isList && result.hasNext }); } catch (error) { if (error.message.includes('401')) onUnauthorized(); if (active) setState({ loading: false, error: error.message, records: null, isList: false, hasNext: false }); } }; load(); return () => { active = false; }; }, [page, listPage, onUnauthorized, reloadVersion]);
  if (page === 'Dashboard') return <LiveDashboard role={role} />;
  if (role === 'Patient' && page === 'Profile') return <PatientProfileForm onNotice={onNotice} />;
  if (role === 'Doctor' && page === 'Profile') return <DoctorProfileForm onNotice={onNotice} />;
  if (page === 'Book appointment') return <BookingForm onNotice={onNotice} />;
  if (role === 'Patient' && page === 'My appointments') return <PatientAppointments onNotice={onNotice} />;
  if (role === 'Patient' && page === 'Bills') return <PatientBills onNotice={onNotice} />;
  if (page === 'Notifications') return <NotificationList />;
  if (page === 'Feedback') return <FeedbackForm onNotice={onNotice} />;
  if (page === 'Pending appointments') return <DoctorAppointments onNotice={onNotice} onUnauthorized={onUnauthorized} />;
  if (page === 'Today') return <DoctorToday />;
  if (page === 'Patient history') return <DoctorHistoryForm onNotice={onNotice} />;
  if (page === 'Treatment & prescription') return <TreatmentForm onNotice={onNotice} />;
  if (page === 'Billing') return <BillingForm onNotice={onNotice} />;
  if (role === 'Administrator' && page === 'Patients') return <AdministrationProfileManagement key="patient" type="patient" onNotice={onNotice} />;
  if (role === 'Administrator' && page === 'Doctors') return <AdministrationProfileManagement key="doctor" type="doctor" onNotice={onNotice} />;
  if (role === 'Administrator' && page === 'Staff') return <AdministrationProfileManagement key="staff" type="staff" onNotice={onNotice} />;
  if (role === 'Administrator' && page === 'Departments') return <DepartmentManagement onNotice={onNotice} />;
  return <section className="panel"><div className="panel-title"><h2>{page}</h2><button className="secondary" onClick={() => setReloadVersion((current) => current + 1)} disabled={state.loading}>Refresh</button></div>{state.loading && <p>Loading live data…</p>}{state.error && <p className="error">{state.error}</p>}{state.records && state.records.length === 0 && <p className="muted">No records found.</p>}{state.records?.length > 0 && (page === 'Treatment history' ? <PatientTreatmentHistory records={state.records} /> : <div className="table"><div><b>Record</b><b>Status</b><b>Last updated</b></div>{state.records.map((record, index) => <div key={record.id ?? record.appointmentId ?? record.treatmentId ?? record.billId ?? record.feedbackId ?? record.notificationId ?? record.departmentId ?? record.doctorId ?? record.userId ?? index}><span>{record.doctorName ? `${record.doctorName}${record.departmentName ? ` — ${record.departmentName}` : ''}${record.reason ? ` · ${record.reason}` : record.diagnosis ? ` · ${record.diagnosis}` : ''}` : record.amount !== undefined ? `₹ ${Number(record.amount).toFixed(2)}${record.description ? ` — ${record.description}` : ''}${record.dueDate ? ` · Due ${record.dueDate}` : ''}` : record.name ?? (record.firstName ? `${record.firstName} ${record.lastName ?? ''}${record.specialization ? ` — ${record.specialization}` : ''}${record.departmentName && record.departmentName !== record.specialization ? ` · ${record.departmentName}` : ''}` : record.email ?? record.diagnosis ?? record.reason ?? record.description ?? record.message ?? `Record #${record.appointmentId ?? record.treatmentId ?? record.billId ?? index + 1}`)}</span><span className="pill">{record.status ?? record.role ?? (record.isRead ? 'Read' : record.diagnosis ? 'Recorded' : record.rating ? `${record.rating}/5` : 'Active')}</span><span>{record.createdAt?.slice(0, 10) ?? record.appointmentDateTime?.slice(0, 10) ?? record.treatmentDateTime?.slice(0, 10) ?? record.generatedAt?.slice(0, 10) ?? '—'}</span></div>)}</div>)}{!state.loading && !state.error && state.isList && <PaginationControls page={listPage} hasNext={state.hasNext} onPrevious={() => setListPage((current) => current - 1)} onNext={() => setListPage((current) => current + 1)} />}{!state.loading && !state.error && !state.records && <p className="muted">This workflow is ready for its live API form connection.</p>}</section>;
}

function PatientTreatmentHistory({ records }) {
  return <div className="treatment-history">{records.map((record) => <article className="treatment-record" key={record.treatmentId}><div className="treatment-record-heading"><div><h3>{record.doctorName || 'Care team'}</h3><p>{record.treatmentDateTime?.slice(0, 10) ?? 'Date not recorded'}</p></div><span className="pill">Recorded</span></div><dl><div><dt>Diagnosis</dt><dd>{record.diagnosis || 'Not recorded'}</dd></div><div><dt>Prescription</dt><dd>{record.prescription || 'Not recorded'}</dd></div><div><dt>Progress notes</dt><dd>{record.progressNotes || 'Not recorded'}</dd></div><div><dt>Treatment notes</dt><dd>{record.treatmentNotes || 'Not recorded'}</dd></div></dl></article>)}</div>;
}

function PatientProfileForm({ onNotice }) {
  const blank = { firstName: '', lastName: '', dateOfBirth: '', gender: '', phoneNumber: '', address: '', emergencyContactName: '', emergencyContactPhone: '' };
  const [form, setForm] = useState(blank);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => { let active = true; api('/api/profile/me').then((profile) => { if (active) setForm((current) => ({ ...current, ...profile, dateOfBirth: profile.dateOfBirth ?? '' })); }).catch((failure) => { if (active && !failure.message.includes('Patient profile not found')) setError(failure.message); }).finally(() => { if (active) setLoading(false); }); return () => { active = false; }; }, []);
  const update = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }));
  const submit = async (event) => { event.preventDefault(); setBusy(true); setError(''); try { const profile = await api('/api/profile/me', { method: 'PUT', body: JSON.stringify(form) }); setForm((current) => ({ ...current, ...profile, dateOfBirth: profile.dateOfBirth ?? current.dateOfBirth })); onNotice('Profile saved successfully.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } };
  if (loading) return <section className="panel"><p>Loading profile…</p></section>;
  return <section className="panel"><h2>Patient profile</h2><form className="grid" onSubmit={submit}><label>First name<input value={form.firstName} onChange={update('firstName')} required maxLength="100" /></label><label>Last name<input value={form.lastName} onChange={update('lastName')} required maxLength="100" /></label><label>Date of birth<input type="date" value={form.dateOfBirth} onChange={update('dateOfBirth')} required /></label><label>Gender<select value={form.gender ?? ''} onChange={update('gender')}><option value="">Prefer not to say</option><option value="Female">Female</option><option value="Male">Male</option><option value="NonBinary">Non-binary</option><option value="Undisclosed">Undisclosed</option></select></label><label>Phone number<input value={form.phoneNumber ?? ''} onChange={update('phoneNumber')} maxLength="30" /></label><label>Emergency contact<input value={form.emergencyContactName ?? ''} onChange={update('emergencyContactName')} maxLength="200" /></label><label className="wide">Address<textarea value={form.address ?? ''} onChange={update('address')} maxLength="500" /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Saving…' : 'Save profile'}</button></form></section>;
}
function DoctorProfileForm({ onNotice }) {
  const blank = { firstName: '', lastName: '', specialization: '', phoneNumber: '', consultationFee: '', departmentName: '', licenseNumber: '', email: '' };
  const [form, setForm] = useState(blank); const [loading, setLoading] = useState(true); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  useEffect(() => { let active = true; api('/api/profile/me').then((profile) => { if (active) setForm({ firstName: profile.firstName ?? '', lastName: profile.lastName ?? '', specialization: profile.specialization ?? '', phoneNumber: profile.phoneNumber ?? '', consultationFee: String(profile.consultationFee ?? ''), departmentName: profile.departmentName ?? '', licenseNumber: profile.licenseNumber ?? '', email: profile.email ?? '' }); }).catch((failure) => { if (active) setError(failure.message); }).finally(() => { if (active) setLoading(false); }); return () => { active = false; }; }, []);
  const update = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }));
  const submit = async (event) => { event.preventDefault(); setBusy(true); setError(''); try { const updated = await api('/api/profile/me/doctor', { method: 'PUT', body: JSON.stringify({ firstName: form.firstName, lastName: form.lastName, specialization: form.specialization, phoneNumber: form.phoneNumber.trim() || null, consultationFee: Number(form.consultationFee) }) }); setForm((current) => ({ ...current, firstName: updated.firstName, lastName: updated.lastName, specialization: updated.specialization, phoneNumber: updated.phoneNumber ?? '', consultationFee: String(updated.consultationFee), departmentName: updated.departmentName, licenseNumber: updated.licenseNumber, email: updated.email })); onNotice('Professional profile saved.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } };
  if (loading) return <section className="panel"><p>Loading profile…</p></section>;
  return <section className="panel"><h2>Professional profile</h2><p className="muted">Update your visible professional details. Department assignment, licence information, availability, and account access are managed by an administrator.</p><form className="grid" onSubmit={submit}><label>First name<input value={form.firstName} onChange={update('firstName')} required maxLength="100" /></label><label>Last name<input value={form.lastName} onChange={update('lastName')} required maxLength="100" /></label><label>Specialization<input value={form.specialization} onChange={update('specialization')} required maxLength="150" /></label><label>Phone number<input value={form.phoneNumber} onChange={update('phoneNumber')} maxLength="30" /></label><label>Consultation fee<input type="number" min="0" max="1000000" step="0.01" value={form.consultationFee} onChange={update('consultationFee')} required /></label><label>Email<input value={form.email} disabled /></label><label>Department<input value={form.departmentName} disabled /></label><label>Licence number<input value={form.licenseNumber} disabled /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Saving…' : 'Save professional profile'}</button></form></section>;
}
function LiveDashboard({ role }) {
  const [state, setState] = useState({ loading: true, error: '', cards: [] });
  useEffect(() => {
    let active = true;
    const load = async () => {
      try {
        const summary = await api('/api/dashboard');
        const cards = role === 'Patient'
          ? [{ label: 'Upcoming appointments', value: String(summary.upcomingAppointments) }, { label: 'Unread notifications', value: String(summary.unreadNotifications) }, { label: 'Outstanding bills', value: `₹ ${Number(summary.outstandingBills).toFixed(2)}` }]
          : role === 'Doctor'
            ? [{ label: 'Pending reviews', value: String(summary.pendingReviews) }, { label: 'Unread notifications', value: String(summary.unreadNotifications) }, { label: 'Patients this month', value: String(summary.patientsThisMonth) }]
            : [{ label: 'Staff accounts', value: String(summary.activeStaffAccounts) }, { label: 'Active doctors', value: String(summary.activeDoctors) }, { label: 'Patient records', value: String(summary.patientRecords) }];
        if (active) setState({ loading: false, error: '', cards });
      } catch (error) { if (active) setState({ loading: false, error: error.message, cards: [] }); }
    };
    load(); return () => { active = false; };
  }, [role]);
  return <>{state.loading && <section className="panel"><p>Loading live dashboard data…</p></section>}{state.error && <section className="panel"><p className="error">{state.error}</p></section>}{!state.loading && !state.error && <><section className="stats">{state.cards.map((card) => <Card key={card.label} {...card} />)}</section><section className="panel"><h2>Today at a glance</h2><p>These figures are calculated from all records available to your account.</p></section></>}</>;
}
function NotificationList() {
  const pageSize = 25;
  const [state, setState] = useState({ loading: true, error: '', records: [] }); const [page, setPage] = useState(1); const [hasNext, setHasNext] = useState(false);
  const load = async (requestedPage = page) => { setState({ loading: true, error: '', records: [] }); try { const result = await fetchPagedRecords('/api/notifications', requestedPage, pageSize); setPage(requestedPage); setHasNext(result.hasNext); setState({ loading: false, error: '', records: result.records }); } catch (error) { setState({ loading: false, error: error.message, records: [] }); } };
  useEffect(() => { load(1); }, []);
  const markRead = async (notificationId) => { try { const updated = await api(`/api/notifications/${notificationId}/read`, { method: 'PUT' }); setState((current) => ({ ...current, records: current.records.map((notification) => notification.notificationId === notificationId ? updated : notification) })); } catch (error) { setState((current) => ({ ...current, error: error.message })); } };
  return <section className="panel"><div className="panel-title"><h2>Notifications</h2><button className="secondary" onClick={() => load(page)} disabled={state.loading}>Refresh</button></div>{state.loading && <p>Loading live data…</p>}{state.error && <p className="error">{state.error}</p>}{!state.loading && !state.error && state.records.length === 0 && <p className="muted">No notifications.</p>}{state.records.map((notification) => <div className="table" key={notification.notificationId}><div><b>{notification.notificationType}</b><span>{notification.message}</span><span>{notification.createdAt?.slice(0, 16)} UTC</span></div><div>{notification.isRead ? <span className="pill">Read</span> : <button className="secondary" onClick={() => markRead(notification.notificationId)}>Mark as read</button>}</div></div>)}{!state.loading && !state.error && <PaginationControls page={page} hasNext={hasNext} onPrevious={() => load(page - 1)} onNext={() => load(page + 1)} />}</section>;
}
function useDoctorWorkItems(scope = 'all', itemsPerPage = 25) {
  const [state, setState] = useState({ loading: true, error: '', records: [] }); const [page, setPage] = useState(1); const [hasNext, setHasNext] = useState(false);
  const load = async (requestedPage = page) => {
    setState({ loading: true, error: '', records: [] });
    try {
      const route = scope === 'pending' ? 'pending-work-items' : scope === 'today' ? 'today-work-items' : 'work-items';
      const result = await fetchPagedRecords(`/api/doctor/appointments/${route}`, requestedPage, itemsPerPage);
      setPage(requestedPage); setHasNext(result.hasNext);
      setState({ loading: false, error: '', records: result.records });
    } catch (error) {
      setState({ loading: false, error: error.message, records: [] });
    }
  };
  useEffect(() => { load(1); }, [scope]);
  return { ...state, page, hasNext, load };
}

function workItemLabel(item) {
  return `${item.patientName} (${item.medicalRecordNumber}) — ${item.appointmentDateTime?.slice(0, 16)} UTC${item.reason ? ` · ${item.reason}` : ''}`;
}

function DoctorAppointments({ onNotice, onUnauthorized }) {
  const { loading, error, records, page, hasNext, load } = useDoctorWorkItems('pending');
  const [decisionError, setDecisionError] = useState(''); const [notes, setNotes] = useState({});
  const reviewableAppointments = records.filter((record) => Date.parse(record.appointmentDateTime) > Date.now());
  const elapsedReviewCount = records.length - reviewableAppointments.length;
  const decide = async (appointmentId, decision) => {
    setDecisionError('');
    try {
      await api(`/api/appointments/${appointmentId}/${decision}`, { method: 'PUT', body: JSON.stringify({ note: notes[appointmentId] ?? '' }) });
      await load();
      onNotice(`Appointment ${decision}ed successfully.`);
    } catch (failure) {
      if (failure.message.includes('session has expired')) onUnauthorized();
      setDecisionError(failure.message);
    }
  };
  return <section className="panel"><div className="panel-title"><h2>Pending appointments</h2><button className="secondary" onClick={() => load(page)}>Refresh</button></div><p className="muted">Appointments must be accepted or rejected before their scheduled time.</p>{loading && <p>Loading live data…</p>}{(error || decisionError) && <p className="error">{error || decisionError}</p>}{!loading && !error && reviewableAppointments.length === 0 && <p className="muted">No appointments await a timely review.</p>}{!loading && !error && elapsedReviewCount > 0 && <p className="muted">{elapsedReviewCount} pending appointment{elapsedReviewCount === 1 ? ' has' : 's have'} passed its review window and cannot be decided here.</p>}{reviewableAppointments.map((record) => <div className="table" key={record.appointmentId}><div><b>{record.patientName} · {record.medicalRecordNumber}</b><span>{record.departmentName} · {record.appointmentDateTime?.slice(0, 16)} UTC</span><span>{record.reason || 'No reason provided'}</span></div><div><label htmlFor={`decision-note-${record.appointmentId}`}>Message to patient (optional)<textarea id={`decision-note-${record.appointmentId}`} value={notes[record.appointmentId] ?? ''} onChange={(event) => setNotes((current) => ({ ...current, [record.appointmentId]: event.target.value }))} maxLength="1000" placeholder="Explain the decision or next steps." /></label><button className="secondary" onClick={() => decide(record.appointmentId, 'accept')}>Accept</button><button className="secondary" onClick={() => decide(record.appointmentId, 'reject')}>Reject</button></div></div>)}{!loading && !error && <PaginationControls page={page} hasNext={hasNext} onPrevious={() => load(page - 1)} onNext={() => load(page + 1)} />}</section>;
}

function DoctorToday() {
  const { loading, error, records, page, hasNext, load } = useDoctorWorkItems('today');
  return <section className="panel"><div className="panel-title"><h2>Today&apos;s appointments</h2><button className="secondary" onClick={() => load(page)}>Refresh</button></div>{loading && <p>Loading live data…</p>}{error && <p className="error">{error}</p>}{!loading && !error && records.length === 0 && <p className="muted">No appointments are scheduled for today.</p>}{records.map((record) => <div className="table" key={record.appointmentId}><div><b>{record.patientName} · {record.medicalRecordNumber}</b><span>{record.appointmentDateTime?.slice(11, 16)} UTC · {record.status}</span><span>{record.reason || 'No reason provided'}</span></div></div>)}{!loading && !error && <PaginationControls page={page} hasNext={hasNext} onPrevious={() => load(page - 1)} onNext={() => load(page + 1)} />}</section>;
}

function DoctorHistoryForm({ onNotice }) {
  const { loading, error: loadError, records } = useDoctorWorkItems('all', 100);
  const [patientId, setPatientId] = useState(''); const [result, setResult] = useState(null); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const patients = [...new Map(records.filter((record) => ['Accepted', 'Completed'].includes(record.status)).map((record) => [record.patientId, record])).values()];
  const submit = async (event) => { event.preventDefault(); setBusy(true); setError(''); try { const summary = await api(`/api/patients/${Number(patientId)}/history-summary`, { method: 'POST' }); setResult(summary); onNotice(summary.aiAvailable ? 'AI summary generated for doctor review.' : 'AI is unavailable; normal history remains available.'); } catch (failure) { setError(failure.message); setResult(null); } finally { setBusy(false); } };
  return <section className="panel"><h2>Patient history and AI summary</h2><p className="muted">Only patients assigned to you are available.</p><form className="grid" onSubmit={submit}><label htmlFor="history-patient">Patient<select id="history-patient" value={patientId} onChange={(event) => setPatientId(event.target.value)} disabled={loading || !patients.length} required><option value="">{loading ? 'Loading patients…' : patients.length ? 'Choose a patient' : 'No assigned patients'}</option>{patients.map((patient) => <option key={patient.patientId} value={patient.patientId}>{patient.patientName} · {patient.medicalRecordNumber}</option>)}</select></label>{(loadError || error) && <p className="error">{loadError || error}</p>}<button className="primary" disabled={busy || loading || !patients.length}>{busy ? 'Generating…' : 'Generate AI summary'}</button></form>{result && <section className="panel"><h3>{result.isAiGenerated ? 'AI-generated summary' : 'Normal patient history'}</h3>{result.summary && <p>{result.summary}</p>}<p className="muted">{result.disclaimer}</p>{result.history.length === 0 ? <p className="muted">No treatment records found.</p> : <div className="table"><div><b>Date</b><b>Diagnosis</b><b>Prescription</b></div>{result.history.map((item) => <div key={item.treatmentId}><span>{item.treatmentDateTime?.slice(0, 10)}</span><span>{item.diagnosis || 'Not recorded'}</span><span>{item.prescription || 'Not recorded'}</span></div>)}</div>}</section>}</section>;
}
function BookingForm({ onNotice }) {
  const [departments, setDepartments] = useState([]);
  const [doctors, setDoctors] = useState([]);
  const [slots, setSlots] = useState([]);
  const [departmentId, setDepartmentId] = useState('');
  const [doctorId, setDoctorId] = useState('');
  const [appointmentDate, setAppointmentDate] = useState('');
  const [appointmentDateTime, setAppointmentDateTime] = useState('');
  const [reason, setReason] = useState('');
  const [loadingCatalog, setLoadingCatalog] = useState(true);
  const [loadingDoctors, setLoadingDoctors] = useState(false);
  const [loadingSlots, setLoadingSlots] = useState(false);
  const [departmentPage, setDepartmentPage] = useState(1);
  const [doctorPage, setDoctorPage] = useState(1);
  const [hasNextDepartmentPage, setHasNextDepartmentPage] = useState(false);
  const [hasNextDoctorPage, setHasNextDoctorPage] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let active = true;
    fetchPagedRecords('/api/departments', departmentPage, 25)
      .then((result) => { if (active) { setDepartments(result.records); setHasNextDepartmentPage(result.hasNext); } })
      .catch((failure) => { if (active) setError(failure.message); })
      .finally(() => { if (active) setLoadingCatalog(false); });
    return () => { active = false; };
  }, [departmentPage]);

  useEffect(() => {
    if (!departmentId) {
      setDoctors([]);
      setHasNextDoctorPage(false);
      return undefined;
    }
    let active = true;
    setLoadingDoctors(true);
    fetchPagedRecords(`/api/departments/${departmentId}/doctors`, doctorPage, 25)
      .then((result) => { if (active) { setDoctors(result.records); setHasNextDoctorPage(result.hasNext); } })
      .catch((failure) => { if (active) setError(failure.message); })
      .finally(() => { if (active) setLoadingDoctors(false); });
    return () => { active = false; };
  }, [departmentId, doctorPage]);

  useEffect(() => {
    if (!doctorId || !appointmentDate) {
      setSlots([]);
      return undefined;
    }
    let active = true;
    setLoadingSlots(true);
    api(`/api/doctors/${doctorId}/slots?date=${appointmentDate}`)
      .then((records) => { if (active) setSlots(records); })
      .catch((failure) => { if (active) setError(failure.message); })
      .finally(() => { if (active) setLoadingSlots(false); });
    return () => { active = false; };
  }, [doctorId, appointmentDate]);

  const chooseDepartment = (event) => {
    setDepartmentId(event.target.value);
    setDoctorPage(1);
    setDoctorId('');
    setAppointmentDate('');
    setAppointmentDateTime('');
    setSlots([]);
    setError('');
  };

  const chooseDoctor = (event) => {
    setDoctorId(event.target.value);
    setAppointmentDate('');
    setAppointmentDateTime('');
    setSlots([]);
    setError('');
  };

  const chooseDate = (event) => {
    setAppointmentDate(event.target.value);
    setAppointmentDateTime('');
    setError('');
  };

  const submit = async (event) => {
    event.preventDefault();
    if (!departmentId || !doctorId || !appointmentDateTime) {
      setError('Choose a department, doctor, date, and available appointment time.');
      return;
    }
    setBusy(true);
    try {
      setError('');
      await api('/api/appointments', { method: 'POST', body: JSON.stringify({ doctorId: Number(doctorId), departmentId: Number(departmentId), appointmentDateTime, reason }) });
      onNotice('Appointment requested successfully.');
      setAppointmentDateTime('');
      setSlots((current) => current.filter((slot) => slot !== appointmentDateTime));
    } catch (failure) {
      setError(failure.message);
    } finally {
      setBusy(false);
    }
  };

  const minimumDate = new Date().toISOString().slice(0, 10);
  return <section className="panel">
    <h2>Request an appointment</h2>
    <p className="muted">Choose the care team and a currently available time. Internal record IDs are never required.</p>
    <form className="grid" onSubmit={submit}>
      <label htmlFor="booking-department">Department
        <select id="booking-department" value={departmentId} onChange={chooseDepartment} disabled={loadingCatalog} required>
          <option value="">{loadingCatalog ? 'Loading departments…' : 'Choose a department'}</option>
          {departments.map((department) => <option key={department.departmentId} value={department.departmentId}>{department.name}{department.description ? ` — ${department.description}` : ''}</option>)}
        </select>
      </label>
      <PaginationControls page={departmentPage} hasNext={hasNextDepartmentPage} onPrevious={() => setDepartmentPage((current) => current - 1)} onNext={() => setDepartmentPage((current) => current + 1)} />
      <label htmlFor="booking-doctor">Doctor
        <select id="booking-doctor" value={doctorId} onChange={chooseDoctor} disabled={!departmentId || loadingDoctors} required>
          <option value="">{loadingDoctors ? 'Loading doctors…' : departmentId ? 'Choose a doctor' : 'Choose a department first'}</option>
          {doctors.map((doctor) => <option key={doctor.doctorId} value={doctor.doctorId}>Dr. {doctor.firstName} {doctor.lastName} — {doctor.specialization} · ₹{doctor.consultationFee}</option>)}
        </select>
      </label>
      {departmentId && <PaginationControls page={doctorPage} hasNext={hasNextDoctorPage} onPrevious={() => setDoctorPage((current) => current - 1)} onNext={() => setDoctorPage((current) => current + 1)} />}
      <label htmlFor="booking-date">Appointment date
        <input id="booking-date" type="date" value={appointmentDate} onChange={chooseDate} onInput={chooseDate} min={minimumDate} disabled={!doctorId} required />
      </label>
      <label htmlFor="booking-slot">Available time
        <select id="booking-slot" value={appointmentDateTime} onChange={(event) => setAppointmentDateTime(event.target.value)} disabled={!appointmentDate || loadingSlots} required>
          <option value="">{loadingSlots ? 'Loading available times…' : appointmentDate ? (slots.length ? 'Choose a time' : 'No times available') : 'Choose a date first'}</option>
          {slots.map((slot) => <option key={slot} value={slot}>{slot.slice(11, 16)} UTC</option>)}
        </select>
      </label>
      <label className="wide" htmlFor="booking-reason">Reason
        <textarea id="booking-reason" value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Briefly describe what you need help with." />
      </label>
      {error && <p className="error">{error}</p>}
      <button className="primary" disabled={busy || loadingCatalog || !departments.length}>{busy ? 'Requesting…' : 'Request appointment'}</button>
    </form>
  </section>;
}
function PatientAppointments({ onNotice }) {
  const pageSize = 25;
  const [state, setState] = useState({ loading: true, error: '', records: [] }); const [cancellingId, setCancellingId] = useState(null); const [page, setPage] = useState(1); const [hasNext, setHasNext] = useState(false);
  const load = async (requestedPage = page) => { setState({ loading: true, error: '', records: [] }); try { const result = await fetchPagedRecords('/api/appointments/my/summaries', requestedPage, pageSize); setPage(requestedPage); setHasNext(result.hasNext); setState({ loading: false, error: '', records: result.records }); } catch (error) { setState({ loading: false, error: error.message, records: [] }); } };
  useEffect(() => { load(1); }, []);
  const cancel = async (appointmentId) => { setCancellingId(appointmentId); try { const updated = await api(`/api/appointments/${appointmentId}/cancel`, { method: 'PUT' }); setState((current) => ({ ...current, records: current.records.map((appointment) => appointment.appointmentId === updated.appointmentId ? { ...appointment, status: updated.status } : appointment) })); onNotice('Appointment cancelled. The time is available for another booking.'); } catch (error) { setState((current) => ({ ...current, error: error.message })); } finally { setCancellingId(null); } };
  const now = new Date().toISOString();
  return <section className="panel"><div className="panel-title"><h2>My appointments</h2><button className="secondary" onClick={() => load(page)} disabled={state.loading || cancellingId !== null}>Refresh</button></div><p className="muted">You can cancel a future appointment while it is pending or accepted.</p>{state.loading && <p>Loading live appointments…</p>}{state.error && <p className="error">{state.error}</p>}{!state.loading && !state.error && state.records.length === 0 && <p className="muted">You do not have any appointments yet.</p>}{state.records.map((appointment) => { const canCancel = appointment.appointmentDateTime > now && ['Pending', 'Accepted'].includes(appointment.status); return <div className="table" key={appointment.appointmentId}><div><b>{appointment.doctorName} — {appointment.departmentName}</b><span>{appointment.appointmentDateTime?.slice(0, 16)} UTC</span><span>{appointment.reason || 'No reason provided'}</span>{appointment.doctorResponseNote && <span>Doctor&apos;s message: {appointment.doctorResponseNote}</span>}</div><div><span className="pill">{appointment.status}</span>{canCancel && <button className="secondary" onClick={() => cancel(appointment.appointmentId)} disabled={cancellingId !== null}>{cancellingId === appointment.appointmentId ? 'Cancelling…' : 'Cancel appointment'}</button>}</div></div>; })}{!state.loading && !state.error && <PaginationControls page={page} hasNext={hasNext} onPrevious={() => load(page - 1)} onNext={() => load(page + 1)} />}</section>;
}
function FeedbackForm({ onNotice }) {
  const pageSize = 25; const [appointments, setAppointments] = useState([]); const [appointmentId, setAppointmentId] = useState(''); const [rating, setRating] = useState('5'); const [comments, setComments] = useState(''); const [error, setError] = useState(''); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [page, setPage] = useState(1); const [hasNext, setHasNext] = useState(false);
  const load = async (requestedPage = page) => { setLoading(true); try { const result = await fetchPagedRecords('/api/appointments/my/feedback-eligible', requestedPage, pageSize); setPage(requestedPage); setHasNext(result.hasNext); setAppointments(result.records); } catch (failure) { setError(failure.message); } finally { setLoading(false); } };
  useEffect(() => { load(1); }, []);
  const submit = async (event) => { event.preventDefault(); setBusy(true); try { setError(''); await api('/api/feedback', { method: 'POST', body: JSON.stringify({ appointmentId: Number(appointmentId), rating: Number(rating), comments }) }); setAppointmentId(''); setComments(''); await load(page); onNotice('Feedback saved. Thank you.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } };
  return <section className="panel"><h2>Share feedback</h2><p className="muted">You can provide feedback only once for each completed appointment.</p><form className="grid" onSubmit={submit}><label className="wide" htmlFor="feedback-appointment">Completed appointment<select id="feedback-appointment" value={appointmentId} onChange={(event) => setAppointmentId(event.target.value)} disabled={loading || !appointments.length} required><option value="">{loading ? 'Loading completed appointments…' : appointments.length ? 'Choose a completed appointment' : 'No completed appointments need feedback'}</option>{appointments.map((appointment) => <option key={appointment.appointmentId} value={appointment.appointmentId}>{appointment.appointmentDateTime?.slice(0, 16)} UTC{appointment.reason ? ` — ${appointment.reason}` : ''}</option>)}</select></label><label>Rating<select value={rating} onChange={(event) => setRating(event.target.value)}>{[5, 4, 3, 2, 1].map((value) => <option key={value} value={value}>{value}</option>)}</select></label><label className="wide">Comments<textarea value={comments} onChange={(event) => setComments(event.target.value)} /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy || loading || !appointments.length}>{busy ? 'Submitting…' : 'Submit feedback'}</button></form>{!loading && <PaginationControls page={page} hasNext={hasNext} onPrevious={() => load(page - 1)} onNext={() => load(page + 1)} />}</section>;
}
function TreatmentForm({ onNotice }) {
  const { loading, error: loadError, records, load } = useDoctorWorkItems('all', 100);
  const [appointmentId, setAppointmentId] = useState(''); const [diagnosis, setDiagnosis] = useState(''); const [prescription, setPrescription] = useState(''); const [progressNotes, setProgressNotes] = useState(''); const [treatmentNotes, setTreatmentNotes] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const acceptedAppointments = records.filter((record) => record.status === 'Accepted' && Date.parse(record.appointmentDateTime) <= Date.now());
  const submit = async (event) => { event.preventDefault(); setBusy(true); try { setError(''); await api(`/api/appointments/${appointmentId}/treatment`, { method: 'POST', body: JSON.stringify({ diagnosis, prescription, progressNotes, treatmentNotes }) }); setAppointmentId(''); setDiagnosis(''); setPrescription(''); setProgressNotes(''); setTreatmentNotes(''); await load(); onNotice('Treatment recorded. The appointment is ready for billing.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } };
  return <section className="panel"><h2>Record treatment</h2><p className="muted">Treatment is available only after an accepted appointment&apos;s scheduled time. A diagnosis is required before the appointment can be completed.</p><form className="grid" onSubmit={submit}><label className="wide" htmlFor="treatment-appointment">Eligible appointment<select id="treatment-appointment" value={appointmentId} onChange={(event) => setAppointmentId(event.target.value)} disabled={loading || !acceptedAppointments.length} required><option value="">{loading ? 'Loading appointments…' : acceptedAppointments.length ? 'Choose an eligible appointment' : 'No eligible appointments'}</option>{acceptedAppointments.map((record) => <option key={record.appointmentId} value={record.appointmentId}>{workItemLabel(record)}</option>)}</select></label><label>Diagnosis<input value={diagnosis} onChange={(event) => setDiagnosis(event.target.value)} required maxLength="1000" /></label><label className="wide">Prescription<textarea value={prescription} onChange={(event) => setPrescription(event.target.value)} /></label><label className="wide">Progress notes<textarea value={progressNotes} onChange={(event) => setProgressNotes(event.target.value)} maxLength="1000" /></label><label className="wide">Treatment notes<textarea value={treatmentNotes} onChange={(event) => setTreatmentNotes(event.target.value)} maxLength="1000" /></label>{(loadError || error) && <p className="error">{loadError || error}</p>}<button className="primary" disabled={busy || loading || !acceptedAppointments.length}>{busy ? 'Saving…' : 'Save treatment'}</button></form></section>;
}

function PatientBills({ onNotice }) {
  const [state, setState] = useState({ loading: true, error: '', records: [] }); const [method, setMethod] = useState('UPI'); const [reference, setReference] = useState(''); const [payingId, setPayingId] = useState(null); const [history, setHistory] = useState({ billId: null, loading: false, records: [], error: '' });
  const load = async () => { setState((current) => ({ ...current, loading: true, error: '' })); try { setState({ loading: false, error: '', records: await api('/api/bills/my?pageSize=100') }); } catch (failure) { setState({ loading: false, error: failure.message, records: [] }); } };
  useEffect(() => { load(); }, []);
  const recordPayment = async (billId) => { setPayingId(billId); try { const updated = await api(`/api/bills/${billId}/payments`, { method: 'POST', body: JSON.stringify({ paymentMethod: method, referenceNumber: reference.trim() || null }) }); setState((current) => ({ ...current, records: current.records.map((bill) => bill.billId === updated.billId ? updated : bill) })); setReference(''); onNotice('Payment recorded in the hospital ledger.'); } catch (failure) { setState((current) => ({ ...current, error: failure.message })); } finally { setPayingId(null); } };
  const loadHistory = async (billId) => { setHistory({ billId, loading: true, records: [], error: '' }); try { setHistory({ billId, loading: false, records: await api(`/api/bills/${billId}/payments`), error: '' }); } catch (failure) { setHistory({ billId, loading: false, records: [], error: failure.message }); } };
  return <section className="panel"><div className="panel-title"><h2>My bills</h2><button className="secondary" onClick={load} disabled={state.loading || payingId !== null}>Refresh</button></div><p className="muted">Recording a payment updates this local hospital ledger; it does not charge a card or bank account.</p>{state.loading && <p>Loading bills…</p>}{state.error && <p className="error">{state.error}</p>}{!state.loading && !state.error && state.records.length === 0 && <p className="muted">You have no bills.</p>}{state.records.map((bill) => <article className="panel" key={bill.billId}><div className="panel-title"><div><h3>Bill #{bill.billId} · ₹ {Number(bill.amount).toFixed(2)}</h3><p className="muted">{bill.description || 'No description'} · Generated {bill.generatedAt?.slice(0, 10)}{bill.dueDate ? ` · Due ${bill.dueDate}` : ''}</p></div><span className="pill">{bill.status}</span></div>{bill.status === 'Pending' && <div className="grid"><label>Payment method<select value={method} onChange={(event) => setMethod(event.target.value)} disabled={payingId !== null}><option value="UPI">UPI</option><option value="Card">Card</option><option value="Cash">Cash</option><option value="Insurance">Insurance</option><option value="Other">Other</option></select></label><label>Reference (optional)<input value={reference} onChange={(event) => setReference(event.target.value)} maxLength="100" placeholder="Transaction or receipt number" /></label><button className="primary" onClick={() => recordPayment(bill.billId)} disabled={payingId !== null}>{payingId === bill.billId ? 'Recording…' : 'Record payment'}</button></div>}{bill.status === 'Paid' && <p className="muted">Paid {bill.paidAt?.slice(0, 16)} UTC.</p>}{bill.status === 'Void' && <p className="muted">Voided {bill.voidedAt?.slice(0, 16)} UTC: {bill.voidReason}</p>}<button className="secondary" onClick={() => loadHistory(bill.billId)} disabled={history.loading}>View payment history</button>{history.billId === bill.billId && <PaymentHistory history={history} />}</article>)}</section>;
}

function PaymentHistory({ history }) {
  if (history.loading) return <p className="muted">Loading payment history…</p>;
  if (history.error) return <p className="error">{history.error}</p>;
  return history.records.length === 0 ? <p className="muted">No payment has been recorded for this bill.</p> : <div className="table"><div><b>Amount</b><b>Method</b><b>Recorded</b></div>{history.records.map((payment) => <div key={payment.paymentId}><span>₹ {Number(payment.amount).toFixed(2)}</span><span>{payment.paymentMethod}{payment.referenceNumber ? ` · ${payment.referenceNumber}` : ''}</span><span>{payment.recordedAt?.slice(0, 16)} UTC</span></div>)}</div>;
}

function BillingForm({ onNotice }) {
  const { loading, error: loadError, records, load } = useDoctorWorkItems('all', 100);
  const [appointmentId, setAppointmentId] = useState(''); const [amount, setAmount] = useState(''); const [description, setDescription] = useState(''); const [dueDate, setDueDate] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const [ledgerVersion, setLedgerVersion] = useState(0);
  const completedAppointments = records.filter((record) => record.status === 'Completed' && !record.hasBill);
  const submit = async (event) => { event.preventDefault(); setBusy(true); try { setError(''); await api(`/api/appointments/${appointmentId}/bill`, { method: 'POST', body: JSON.stringify({ amount: Number(amount), description, dueDate: dueDate || null }) }); setAppointmentId(''); setAmount(''); setDescription(''); setDueDate(''); await load(); setLedgerVersion((current) => current + 1); onNotice('Bill generated.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } };
  return <><section className="panel"><h2>Generate bill</h2><p className="muted">Only completed appointments can be billed.</p><form className="grid" onSubmit={submit}><label className="wide" htmlFor="billing-appointment">Completed appointment<select id="billing-appointment" value={appointmentId} onChange={(event) => setAppointmentId(event.target.value)} disabled={loading || !completedAppointments.length} required><option value="">{loading ? 'Loading completed appointments…' : completedAppointments.length ? 'Choose a completed appointment' : 'No completed appointments'}</option>{completedAppointments.map((record) => <option key={record.appointmentId} value={record.appointmentId}>{workItemLabel(record)}</option>)}</select></label><label>Amount<input type="number" min="0.01" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} required /></label><label>Due date<input type="date" value={dueDate} onChange={(event) => setDueDate(event.target.value)} onInput={(event) => setDueDate(event.target.value)} min={new Date().toISOString().slice(0, 10)} /></label><label className="wide">Description<textarea value={description} onChange={(event) => setDescription(event.target.value)} /></label>{(loadError || error) && <p className="error">{loadError || error}</p>}<button className="primary" disabled={busy || loading || !completedAppointments.length}>{busy ? 'Generating…' : 'Generate bill'}</button></form></section><DoctorBillLedger onNotice={onNotice} refreshKey={ledgerVersion} /></>;
}

function DoctorBillLedger({ onNotice, refreshKey }) {
  const [state, setState] = useState({ loading: true, error: '', records: [] }); const [voidReasons, setVoidReasons] = useState({}); const [voidingId, setVoidingId] = useState(null); const [history, setHistory] = useState({ billId: null, loading: false, records: [], error: '' });
  const load = async () => { setState((current) => ({ ...current, loading: true, error: '' })); try { setState({ loading: false, error: '', records: await api('/api/doctor/bills?pageSize=100') }); } catch (failure) { setState({ loading: false, error: failure.message, records: [] }); } };
  useEffect(() => { load(); }, [refreshKey]);
  const voidBill = async (billId) => { setVoidingId(billId); try { const updated = await api(`/api/bills/${billId}/void`, { method: 'PUT', body: JSON.stringify({ reason: voidReasons[billId] ?? '' }) }); setState((current) => ({ ...current, records: current.records.map((bill) => bill.billId === updated.billId ? updated : bill) })); onNotice('Bill voided and the patient notified.'); } catch (failure) { setState((current) => ({ ...current, error: failure.message })); } finally { setVoidingId(null); } };
  const loadHistory = async (billId) => { setHistory({ billId, loading: true, records: [], error: '' }); try { setHistory({ billId, loading: false, records: await api(`/api/doctor/bills/${billId}/payments`), error: '' }); } catch (failure) { setHistory({ billId, loading: false, records: [], error: failure.message }); } };
  return <section className="panel"><div className="panel-title"><h2>My generated bills</h2><button className="secondary" onClick={load} disabled={state.loading || voidingId !== null}>Refresh</button></div>{state.loading && <p>Loading bills…</p>}{state.error && <p className="error">{state.error}</p>}{!state.loading && !state.error && state.records.length === 0 && <p className="muted">You have not generated any bills.</p>}{state.records.map((bill) => <article className="panel" key={bill.billId}><div className="panel-title"><div><h3>Bill #{bill.billId} · ₹ {Number(bill.amount).toFixed(2)}</h3><p className="muted">{bill.description || 'No description'} · Generated {bill.generatedAt?.slice(0, 10)}</p></div><span className="pill">{bill.status}</span></div>{bill.status === 'Pending' && <div className="grid"><label className="wide">Void reason<textarea value={voidReasons[bill.billId] ?? ''} onChange={(event) => setVoidReasons((current) => ({ ...current, [bill.billId]: event.target.value }))} maxLength="500" required placeholder="Explain why this bill should be voided." /></label><button className="secondary" onClick={() => voidBill(bill.billId)} disabled={voidingId !== null}>{voidingId === bill.billId ? 'Voiding…' : 'Void bill'}</button></div>}{bill.status === 'Paid' && <p className="muted">Paid {bill.paidAt?.slice(0, 16)} UTC.</p>}{bill.status === 'Void' && <p className="muted">Voided {bill.voidedAt?.slice(0, 16)} UTC: {bill.voidReason}</p>}<button className="secondary" onClick={() => loadHistory(bill.billId)} disabled={history.loading}>View payment history</button>{history.billId === bill.billId && <PaymentHistory history={history} />}</article>)}</section>;
}
function AdministrationProfileManagement({ type, onNotice }) {
  const labels = { patient: 'patient', doctor: 'doctor', staff: 'staff member' };
  const endpoint = type === 'staff' ? 'staff' : `${type}s`;
  const pageSize = 25;
  const blank = type === 'patient'
    ? { firstName: '', lastName: '', dateOfBirth: '', gender: '', phoneNumber: '', address: '', emergencyContactName: '', emergencyContactPhone: '', isAccountActive: true }
    : type === 'doctor'
      ? { firstName: '', lastName: '', licenseNumber: '', specialization: '', departmentId: '', phoneNumber: '', consultationFee: '', isActive: true, isAccountActive: true }
      : { firstName: '', lastName: '', employeeNumber: '', jobTitle: '', departmentId: '', phoneNumber: '', isActive: true, isAccountActive: true };
  const [records, setRecords] = useState([]); const [departments, setDepartments] = useState([]); const [selectedId, setSelectedId] = useState(''); const [form, setForm] = useState(blank); const [search, setSearch] = useState(''); const [page, setPage] = useState(1); const [hasNext, setHasNext] = useState(false); const [error, setError] = useState(''); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const load = async (requestedPage = page) => { setLoading(true); setError(''); const query = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : ''; try { const [result, catalog] = await Promise.all([fetchPagedRecords(`/api/admin/${endpoint}${query}`, requestedPage, pageSize), type === 'patient' ? Promise.resolve([]) : api('/api/admin/departments?pageSize=100')]); setRecords(result.records); setDepartments(catalog.filter((department) => department.isActive)); setHasNext(result.hasNext); setPage(requestedPage); setSelectedId(''); setForm(blank); } catch (failure) { setError(failure.message); } finally { setLoading(false); } };
  useEffect(() => { load(1); }, [type]);
  const selectRecord = (event) => { const record = records.find((candidate) => String(candidate[`${type}Id`]) === event.target.value); setSelectedId(event.target.value); setError(''); if (!record) { setForm(blank); return; } setForm(type === 'patient' ? { firstName: record.firstName, lastName: record.lastName, dateOfBirth: record.dateOfBirth ?? '', gender: record.gender ?? '', phoneNumber: record.phoneNumber ?? '', address: record.address ?? '', emergencyContactName: record.emergencyContactName ?? '', emergencyContactPhone: record.emergencyContactPhone ?? '', isAccountActive: record.isAccountActive } : type === 'doctor' ? { firstName: record.firstName, lastName: record.lastName, licenseNumber: record.licenseNumber, specialization: record.specialization, departmentId: String(record.departmentId), phoneNumber: record.phoneNumber ?? '', consultationFee: String(record.consultationFee), isActive: record.isActive, isAccountActive: record.isAccountActive } : { firstName: record.firstName, lastName: record.lastName, employeeNumber: record.employeeNumber, jobTitle: record.jobTitle, departmentId: record.departmentId ? String(record.departmentId) : '', phoneNumber: record.phoneNumber ?? '', isActive: record.isActive, isAccountActive: record.isAccountActive }); };
  const update = (field) => (event) => setForm((current) => ({ ...current, [field]: field === 'isActive' || field === 'isAccountActive' ? event.target.value === 'true' : event.target.value }));
  const submit = async (event) => { event.preventDefault(); const selected = records.find((record) => String(record[`${type}Id`]) === selectedId); if (!selected) return; setBusy(true); setError(''); try { if (form.isAccountActive !== selected.isAccountActive) await api(`/api/admin/accounts/${selected.userId}/status`, { method: 'PATCH', body: JSON.stringify({ isActive: form.isAccountActive }) }); const request = type === 'patient' ? form : type === 'doctor' ? { ...form, departmentId: Number(form.departmentId), consultationFee: Number(form.consultationFee) } : { ...form, departmentId: form.departmentId ? Number(form.departmentId) : null }; delete request.isAccountActive; const updated = await api(`/api/admin/${endpoint}/${selectedId}`, { method: 'PUT', body: JSON.stringify(request) }); const combined = { ...updated, isAccountActive: form.isAccountActive }; setRecords((current) => current.map((record) => record[`${type}Id`] === combined[`${type}Id`] ? combined : record)); onNotice(`${labels[type][0].toUpperCase()}${labels[type].slice(1)} record updated.`); } catch (failure) { setError(failure.message); } finally { setBusy(false); } };
  const selected = Boolean(selectedId);
  return <section className="panel"><div className="panel-title"><h2>Manage {type}s</h2><button className="secondary" onClick={() => load(page)} disabled={loading || busy}>Refresh</button></div><p className="muted">Select a {labels[type]} to update their profile. Deactivating an account blocks sign-in while preserving the hospital record and its clinical history.</p><div className="grid"><label className="wide" htmlFor={`${type}-search`}>Find a {labels[type]}<input id={`${type}-search`} value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Name, email, or identifier" maxLength="256" /></label><button type="button" className="secondary" onClick={() => load(1)} disabled={loading || busy}>Search</button></div>{loading && <p>Loading records…</p>}{!loading && <><label htmlFor={`${type}-record`}>Record<select id={`${type}-record`} value={selectedId} onChange={selectRecord} disabled={!records.length}><option value="">{records.length ? `Choose a ${labels[type]}` : `No ${type}s found`}</option>{records.map((record) => <option key={record[`${type}Id`]} value={record[`${type}Id`]}>{record.firstName} {record.lastName} — {record.email}{record.medicalRecordNumber ? ` · ${record.medicalRecordNumber}` : record.licenseNumber ? ` · ${record.licenseNumber}` : ` · ${record.employeeNumber}`}</option>)}</select></label><PaginationControls page={page} hasNext={hasNext} onPrevious={() => load(page - 1)} onNext={() => load(page + 1)} /></>}{selected && <form className="grid" onSubmit={submit}><label>First name<input value={form.firstName} onChange={update('firstName')} required maxLength="100" /></label><label>Last name<input value={form.lastName} onChange={update('lastName')} required maxLength="100" /></label>{type === 'patient' && <><label>Date of birth<input type="date" value={form.dateOfBirth} onChange={update('dateOfBirth')} required /></label><label>Gender<select value={form.gender} onChange={update('gender')}><option value="">Prefer not to say</option><option value="Female">Female</option><option value="Male">Male</option><option value="NonBinary">Non-binary</option><option value="Undisclosed">Undisclosed</option></select></label><label>Phone number<input value={form.phoneNumber} onChange={update('phoneNumber')} maxLength="30" /></label><label>Emergency contact<input value={form.emergencyContactName} onChange={update('emergencyContactName')} maxLength="200" /></label><label className="wide">Address<textarea value={form.address} onChange={update('address')} maxLength="500" /></label></>}{type === 'doctor' && <><label>License number<input value={form.licenseNumber} onChange={update('licenseNumber')} required maxLength="100" /></label><label>Specialization<input value={form.specialization} onChange={update('specialization')} required maxLength="150" /></label></>}{type === 'staff' && <><label>Employee number<input value={form.employeeNumber} onChange={update('employeeNumber')} required maxLength="50" /></label><label>Job title<input value={form.jobTitle} onChange={update('jobTitle')} required maxLength="150" /></label></>}{type !== 'patient' && <><label>Department<select value={form.departmentId} onChange={update('departmentId')} required={type === 'doctor'}><option value="">{type === 'doctor' ? 'Choose a department' : 'No department'}</option>{departments.map((department) => <option key={department.departmentId} value={department.departmentId}>{department.departmentCode} — {department.name}</option>)}</select></label><label>Phone number<input value={form.phoneNumber} onChange={update('phoneNumber')} maxLength="30" /></label>{type === 'doctor' && <label>Consultation fee<input type="number" min="0" max="1000000" step="0.01" value={form.consultationFee} onChange={update('consultationFee')} required /></label>}<label>{type === 'doctor' ? 'Booking availability' : 'Employment status'}<select value={String(form.isActive)} onChange={update('isActive')}><option value="true">Active</option><option value="false">Inactive</option></select></label></>}<label>Account status<select value={String(form.isAccountActive)} onChange={update('isAccountActive')}><option value="true">Active — can sign in</option><option value="false">Inactive — sign-in blocked</option></select></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Saving…' : `Save ${labels[type]} changes`}</button></form>}{error && !selected && <p className="error">{error}</p>}</section>;
}
function DepartmentManagement({ onNotice }) {
  const blank = { departmentCode: '', name: '', description: '', isActive: true };
  const [departments, setDepartments] = useState([]); const [departmentId, setDepartmentId] = useState(''); const [form, setForm] = useState(blank); const [loading, setLoading] = useState(true); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const load = async () => { setLoading(true); setError(''); try { setDepartments(await api('/api/admin/departments?pageSize=100')); } catch (failure) { setError(failure.message); } finally { setLoading(false); } };
  useEffect(() => { load(); }, []);
  const chooseDepartment = (event) => { const selected = departments.find((department) => String(department.departmentId) === event.target.value); setDepartmentId(event.target.value); setForm(selected ? { departmentCode: selected.departmentCode, name: selected.name, description: selected.description ?? '', isActive: selected.isActive } : blank); setError(''); };
  const update = (field) => (event) => setForm((current) => ({ ...current, [field]: field === 'isActive' ? event.target.value === 'true' : event.target.value }));
  const submit = async (event) => { event.preventDefault(); setBusy(true); setError(''); try { const request = { ...form, description: form.description.trim() || null }; const updated = await api(departmentId ? `/api/departments/${departmentId}` : '/api/departments', { method: departmentId ? 'PUT' : 'POST', body: JSON.stringify(request) }); setDepartments((current) => departmentId ? current.map((department) => department.departmentId === updated.departmentId ? updated : department).sort((left, right) => left.name.localeCompare(right.name)) : [...current, updated].sort((left, right) => left.name.localeCompare(right.name))); setDepartmentId(String(updated.departmentId)); setForm({ departmentCode: updated.departmentCode, name: updated.name, description: updated.description ?? '', isActive: updated.isActive }); onNotice(departmentId ? 'Department updated.' : 'Department created.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } };
  return <section className="panel"><div className="panel-title"><h2>Manage departments</h2><button className="secondary" onClick={load} disabled={loading || busy}>Refresh</button></div><p className="muted">Choose an existing department to edit it, including inactive departments, or create a new department. Inactive departments are not visible to patients.</p><form className="grid" onSubmit={submit}><label className="wide" htmlFor="department-record">Department<select id="department-record" value={departmentId} onChange={chooseDepartment} disabled={loading}><option value="">Create a new department</option>{departments.map((department) => <option key={department.departmentId} value={department.departmentId}>{department.departmentCode} — {department.name}{department.isActive ? '' : ' (Inactive)'}</option>)}</select></label><label htmlFor="department-code">Department code<input id="department-code" value={form.departmentCode} onChange={update('departmentCode')} required maxLength="20" placeholder="e.g. CARD" /></label><label htmlFor="department-name">Department name<input id="department-name" value={form.name} onChange={update('name')} required maxLength="100" placeholder="e.g. Cardiology" /></label><label className="wide" htmlFor="department-description">Description<textarea id="department-description" value={form.description} onChange={update('description')} maxLength="500" placeholder="Brief service description for patients." /></label><label htmlFor="department-status">Availability<select id="department-status" value={String(form.isActive)} onChange={update('isActive')}><option value="true">Active — visible to patients</option><option value="false">Inactive — hidden from patients</option></select></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy || loading}>{busy ? 'Saving…' : departmentId ? 'Update department' : 'Create department'}</button></form></section>;
}
function PaginationControls({ page, hasNext, onPrevious, onNext }) { return <div className="panel-title"><p className="muted">Page {page}</p><div><button className="secondary" onClick={onPrevious} disabled={page === 1}>Previous</button><button className="secondary" onClick={onNext} disabled={!hasNext}>Next</button></div></div>; }
function Card({ label, value }) { return <article className="card"><p>{label}</p><strong>{value}</strong></article>; }
const appContainer = document.getElementById('root');
const appRoot = appContainer.__mediCoreRoot ?? createRoot(appContainer);
appContainer.__mediCoreRoot = appRoot;
appRoot.render(<StrictMode><App /></StrictMode>);
