import { StrictMode, useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './styles.css';

const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5141';
async function api(path, options = {}) {
  const response = await fetch(`${apiUrl}${path}`, { ...options, headers: { Authorization: `Bearer ${sessionStorage.getItem('accessToken')}`, 'Content-Type': 'application/json', ...(options.headers ?? {}) } });
  const data = response.status === 204 ? null : await response.json();
  if (response.status === 401) {
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('currentUser');
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

function App() {
  const savedUser = JSON.parse(sessionStorage.getItem('currentUser') ?? 'null');
  const [role, setRole] = useState(savedUser?.role ?? 'Patient');
  const [page, setPage] = useState('Dashboard');
  const [signedIn, setSignedIn] = useState(() => Boolean(sessionStorage.getItem('accessToken')));
  const [notice, setNotice] = useState('');
  const menu = pages[role];
  const navigate = (next) => { setPage(next); setNotice(''); };
  useEffect(() => {
    if (!sessionStorage.getItem('accessToken')) return;
    api('/api/auth/me').then((user) => {
      sessionStorage.setItem('currentUser', JSON.stringify(user));
      setRole(user.role);
    }).catch(() => {
      sessionStorage.removeItem('accessToken');
      sessionStorage.removeItem('currentUser');
      setSignedIn(false);
    });
  }, []);
  useEffect(() => {
    const signOutExpiredSession = () => setSignedIn(false);
    window.addEventListener('hospital:unauthorized', signOutExpiredSession);
    return () => window.removeEventListener('hospital:unauthorized', signOutExpiredSession);
  }, []);
  if (!signedIn) return <Auth onSignIn={(user) => { setRole(user.role); setPage('Dashboard'); setSignedIn(true); }} />;
  return <div className="shell"><aside><div className="brand">Medi<span>Core</span></div><p className="role">{role} portal</p>{menu.map((item) => <button key={item} className={page === item ? 'nav active' : 'nav'} onClick={() => navigate(item)}>{item}</button>)}<button className="signout" onClick={() => { sessionStorage.removeItem('accessToken'); sessionStorage.removeItem('currentUser'); setSignedIn(false); }}>Sign out</button></aside><main className="content"><header><div><p className="eyebrow">Hospital management system</p><h1>{page}</h1></div><p className="role">{role}</p></header>{notice && <p className="notice">{notice}</p>}<Page role={role} page={page} onNotice={setNotice} onUnauthorized={setSignedIn} /></main></div>;
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
      onSignIn(data.user);
    } catch (reason) {
      setError(reason.message);
    } finally {
      setBusy(false);
    }
  };
  return <main className="auth"><section><div className="brand">Medi<span>Core</span></div><h1>{isRegistration ? 'Start your care journey.' : 'Care, connected.'}</h1><p>Manage appointments, treatment, bills and communication from one secure place.</p><form onSubmit={submit}><label>Email<input value={email} onChange={(event) => setEmail(event.target.value)} type="email" required placeholder="you@example.com" /></label><label>Password<input value={password} onChange={(event) => setPassword(event.target.value)} type="password" required minLength="12" placeholder="••••••••••••" /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Please wait…' : isRegistration ? 'Create patient account' : 'Sign in'}</button></form><p className="muted">{isRegistration ? 'Already registered? ' : 'New here? '}<button type="button" className="link-button" onClick={() => { setMode(isRegistration ? 'login' : 'register'); setError(''); }}>{isRegistration ? 'Sign in' : 'Create a patient account'}</button></p></section><div className="auth-art"><p>YOUR HEALTHCARE, IN ONE VIEW</p><strong>Simple workflows.<br/>Better care.</strong></div></main>;
}

function Page({ role, page, onNotice, onUnauthorized }) {
  const [state, setState] = useState({ loading: false, error: '', records: null });
  const routes = { ...(role === 'Patient' ? {} : { Profile: '/api/profile/me' }), Departments: '/api/departments', Doctors: '/api/doctors', 'My appointments': '/api/appointments/my', Bills: '/api/bills/my', Notifications: '/api/notifications', 'Pending appointments': '/api/doctor/appointments/pending', Today: '/api/doctor/appointments/today', Patients: '/api/admin/patients', Staff: '/api/admin/staff' };
  useEffect(() => { const route = routes[page]; if (!route && page !== 'Treatment history') { setState({ loading: false, error: '', records: null }); return; } let active = true; setState({ loading: true, error: '', records: null }); const load = async () => { try { let target = route; if (page === 'Treatment history') { const profile = await api('/api/profile/me'); target = `/api/patients/${profile.patientId}/history`; } const data = await api(target); if (active) setState({ loading: false, error: '', records: Array.isArray(data) ? data : [data] }); } catch (error) { if (error.message.includes('401')) { sessionStorage.removeItem('accessToken'); sessionStorage.removeItem('currentUser'); onUnauthorized(false); } if (active) setState({ loading: false, error: error.message, records: null }); } }; load(); return () => { active = false; }; }, [page, onUnauthorized]);
  if (page === 'Dashboard') return <><section className="stats"><Card label="Upcoming appointments" value={role === 'Patient' ? '2' : '8'} /><Card label={role === 'Administrator' ? 'Active staff' : 'Unread notifications'} value={role === 'Administrator' ? '42' : '3'} /><Card label="This month" value={role === 'Doctor' ? '31 patients' : '₹ 1,200'} /></section><section className="panel"><h2>Today at a glance</h2><p>Use the navigation to continue your workflow and view live records in each connected area.</p></section></>;
  if (role === 'Patient' && page === 'Profile') return <PatientProfileForm onNotice={onNotice} />;
  if (page === 'Book appointment') return <BookingForm onNotice={onNotice} />;
  if (page === 'Feedback') return <FeedbackForm onNotice={onNotice} />;
  if (page === 'Pending appointments') return <DoctorAppointments onNotice={onNotice} onUnauthorized={onUnauthorized} />;
  if (page === 'Patient history') return <DoctorHistoryForm onNotice={onNotice} />;
  if (page === 'Treatment & prescription') return <TreatmentForm onNotice={onNotice} />;
  if (page === 'Billing') return <BillingForm onNotice={onNotice} />;
  if (role === 'Administrator' && page === 'Staff') return <AccountStatusForm onNotice={onNotice} />;
  return <section className="panel"><div className="panel-title"><h2>{page}</h2><button className="secondary" onClick={() => onNotice('Live data refreshes when you open this screen.')}>Refresh</button></div>{state.loading && <p>Loading live data…</p>}{state.error && <p className="error">{state.error}</p>}{state.records && state.records.length === 0 && <p className="muted">No records found.</p>}{state.records?.length > 0 && <div className="table"><div><b>Record</b><b>Status</b><b>Last updated</b></div>{state.records.map((record, index) => <div key={record.id ?? record.appointmentId ?? record.treatmentId ?? record.billId ?? record.feedbackId ?? record.notificationId ?? record.departmentId ?? record.doctorId ?? record.userId ?? index}><span>{record.name ?? record.email ?? record.firstName ?? record.diagnosis ?? record.reason ?? record.description ?? record.message ?? `Record #${record.appointmentId ?? record.treatmentId ?? record.billId ?? index + 1}`}</span><span className="pill">{record.status ?? record.role ?? (record.isRead ? 'Read' : record.diagnosis ? 'Recorded' : record.rating ? `${record.rating}/5` : 'Active')}</span><span>{record.createdAt?.slice(0, 10) ?? record.appointmentDateTime?.slice(0, 10) ?? record.treatmentDateTime?.slice(0, 10) ?? record.generatedAt?.slice(0, 10) ?? '—'}</span></div>)}</div>}{!state.loading && !state.error && !state.records && <p className="muted">This workflow is ready for its live API form connection.</p>}</section>;
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
  return <section className="panel"><h2>Patient profile</h2><form className="grid" onSubmit={submit}><label>First name<input value={form.firstName} onChange={update('firstName')} required maxLength="100" /></label><label>Last name<input value={form.lastName} onChange={update('lastName')} required maxLength="100" /></label><label>Date of birth<input type="date" value={form.dateOfBirth} onChange={update('dateOfBirth')} required /></label><label>Gender<input value={form.gender ?? ''} onChange={update('gender')} maxLength="20" /></label><label>Phone number<input value={form.phoneNumber ?? ''} onChange={update('phoneNumber')} maxLength="30" /></label><label>Emergency contact<input value={form.emergencyContactName ?? ''} onChange={update('emergencyContactName')} maxLength="200" /></label><label className="wide">Address<textarea value={form.address ?? ''} onChange={update('address')} maxLength="500" /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Saving…' : 'Save profile'}</button></form></section>;
}
function DoctorAppointments({ onNotice, onUnauthorized }) { const [state, setState] = useState({ loading: true, error: '', records: [] }); const load = async () => { setState({ loading: true, error: '', records: [] }); try { const records = await api('/api/doctor/appointments/pending'); setState({ loading: false, error: '', records }); } catch (error) { if (error.message.includes('401')) { sessionStorage.removeItem('accessToken'); sessionStorage.removeItem('currentUser'); onUnauthorized(false); } setState({ loading: false, error: error.message, records: [] }); } }; useEffect(() => { load(); }, []); const decide = async (appointmentId, decision) => { try { await api(`/api/appointments/${appointmentId}/${decision}`, { method: 'PUT', body: JSON.stringify({ note: 'Updated from the doctor portal.' }) }); setState((current) => ({ ...current, records: current.records.filter((record) => record.appointmentId !== appointmentId) })); onNotice(`Appointment ${decision}ed successfully.`); } catch (error) { setState((current) => ({ ...current, error: error.message })); } }; return <section className="panel"><div className="panel-title"><h2>Pending appointments</h2><button className="secondary" onClick={load}>Refresh</button></div>{state.loading && <p>Loading live data…</p>}{state.error && <p className="error">{state.error}</p>}{!state.loading && !state.error && state.records.length === 0 && <p className="muted">No records found.</p>}{state.records.map((record) => <div className="table" key={record.appointmentId}><div><b>Appointment #{record.appointmentId} · {record.appointmentDateTime?.slice(0, 16)}</b><span className="pill">{record.status}</span><span>{record.reason || 'No reason provided'}</span></div><div><button className="secondary" onClick={() => decide(record.appointmentId, 'accept')}>Accept</button><button className="secondary" onClick={() => decide(record.appointmentId, 'reject')}>Reject</button></div></div>)}</section>; }
function DoctorHistoryForm({ onNotice }) { const [patientId, setPatientId] = useState(''); const [result, setResult] = useState(null); const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const submit = async (event) => { event.preventDefault(); setBusy(true); setError(''); try { const summary = await api(`/api/patients/${Number(patientId)}/history-summary`, { method: 'POST' }); setResult(summary); onNotice(summary.aiAvailable ? 'AI summary generated for doctor review.' : 'AI is unavailable; normal history remains available.'); } catch (failure) { setError(failure.message); setResult(null); } finally { setBusy(false); } }; return <section className="panel"><h2>Patient history and AI summary</h2><form className="grid" onSubmit={submit}><label>Patient ID<input type="number" min="1" value={patientId} onChange={(event) => setPatientId(event.target.value)} required /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Generating…' : 'Generate AI summary'}</button></form>{result && <section className="panel"><h3>{result.isAiGenerated ? 'AI-generated summary' : 'Normal patient history'}</h3>{result.summary && <p>{result.summary}</p>}<p className="muted">{result.disclaimer}</p>{result.history.length === 0 ? <p className="muted">No treatment records found.</p> : <div className="table"><div><b>Date</b><b>Diagnosis</b><b>Prescription</b></div>{result.history.map((item) => <div key={item.treatmentId}><span>{item.treatmentDateTime?.slice(0, 10)}</span><span>{item.diagnosis || 'Not recorded'}</span><span>{item.prescription || 'Not recorded'}</span></div>)}</div>}</section>}</section>; }
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
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let active = true;
    api('/api/departments?pageSize=100')
      .then((records) => { if (active) setDepartments(records); })
      .catch((failure) => { if (active) setError(failure.message); })
      .finally(() => { if (active) setLoadingCatalog(false); });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (!departmentId) {
      setDoctors([]);
      return undefined;
    }
    let active = true;
    setLoadingDoctors(true);
    api(`/api/departments/${departmentId}/doctors?pageSize=100`)
      .then((records) => { if (active) setDoctors(records); })
      .catch((failure) => { if (active) setError(failure.message); })
      .finally(() => { if (active) setLoadingDoctors(false); });
    return () => { active = false; };
  }, [departmentId]);

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
      <label htmlFor="booking-doctor">Doctor
        <select id="booking-doctor" value={doctorId} onChange={chooseDoctor} disabled={!departmentId || loadingDoctors} required>
          <option value="">{loadingDoctors ? 'Loading doctors…' : departmentId ? 'Choose a doctor' : 'Choose a department first'}</option>
          {doctors.map((doctor) => <option key={doctor.doctorId} value={doctor.doctorId}>Dr. {doctor.firstName} {doctor.lastName} — {doctor.specialization} · ₹{doctor.consultationFee}</option>)}
        </select>
      </label>
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
function FeedbackForm({ onNotice }) { const [appointmentId, setAppointmentId] = useState(''); const [rating, setRating] = useState('5'); const [comments, setComments] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const submit = async (event) => { event.preventDefault(); setBusy(true); try { setError(''); await api('/api/feedback', { method: 'POST', body: JSON.stringify({ appointmentId: Number(appointmentId), rating: Number(rating), comments }) }); onNotice('Feedback saved. Thank you.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } }; return <section className="panel"><h2>Share feedback</h2><form className="grid" onSubmit={submit}><label>Completed appointment ID<input value={appointmentId} onChange={(event) => setAppointmentId(event.target.value)} required /></label><label>Rating<select value={rating} onChange={(event) => setRating(event.target.value)}>{[5, 4, 3, 2, 1].map((value) => <option key={value} value={value}>{value}</option>)}</select></label><label className="wide">Comments<textarea value={comments} onChange={(event) => setComments(event.target.value)} /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Submitting…' : 'Submit feedback'}</button></form></section>; }
function TreatmentForm({ onNotice }) { const [appointmentId, setAppointmentId] = useState(''); const [diagnosis, setDiagnosis] = useState(''); const [prescription, setPrescription] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const submit = async (event) => { event.preventDefault(); setBusy(true); try { setError(''); await api(`/api/appointments/${appointmentId}/treatment`, { method: 'POST', body: JSON.stringify({ diagnosis, prescription }) }); onNotice('Treatment recorded.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } }; return <section className="panel"><h2>Record treatment</h2><form className="grid" onSubmit={submit}><label>Accepted appointment ID<input value={appointmentId} onChange={(event) => setAppointmentId(event.target.value)} required /></label><label>Diagnosis<input value={diagnosis} onChange={(event) => setDiagnosis(event.target.value)} /></label><label className="wide">Prescription<textarea value={prescription} onChange={(event) => setPrescription(event.target.value)} /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Saving…' : 'Save treatment'}</button></form></section>; }
function BillingForm({ onNotice }) { const [appointmentId, setAppointmentId] = useState(''); const [amount, setAmount] = useState(''); const [description, setDescription] = useState(''); const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const submit = async (event) => { event.preventDefault(); setBusy(true); try { setError(''); await api(`/api/appointments/${appointmentId}/bill`, { method: 'POST', body: JSON.stringify({ amount: Number(amount), description }) }); onNotice('Bill generated.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } }; return <section className="panel"><h2>Generate bill</h2><form className="grid" onSubmit={submit}><label>Completed appointment ID<input value={appointmentId} onChange={(event) => setAppointmentId(event.target.value)} required /></label><label>Amount<input type="number" min="0.01" step="0.01" value={amount} onChange={(event) => setAmount(event.target.value)} required /></label><label className="wide">Description<textarea value={description} onChange={(event) => setDescription(event.target.value)} /></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Generating…' : 'Generate bill'}</button></form></section>; }
function AccountStatusForm({ onNotice }) { const [userId, setUserId] = useState(''); const [isActive, setIsActive] = useState(true); const [error, setError] = useState(''); const [busy, setBusy] = useState(false); const submit = async (event) => { event.preventDefault(); setBusy(true); try { setError(''); await api(`/api/admin/accounts/${userId}/status`, { method: 'PATCH', body: JSON.stringify({ isActive }) }); onNotice('Account status updated.'); } catch (failure) { setError(failure.message); } finally { setBusy(false); } }; return <section className="panel"><h2>Manage staff account</h2><form className="grid" onSubmit={submit}><label>User ID<input value={userId} onChange={(event) => setUserId(event.target.value)} required /></label><label>Account status<select value={String(isActive)} onChange={(event) => setIsActive(event.target.value === 'true')}><option value="true">Active</option><option value="false">Inactive</option></select></label>{error && <p className="error">{error}</p>}<button className="primary" disabled={busy}>{busy ? 'Updating…' : 'Update account'}</button></form></section>; }
function Card({ label, value }) { return <article className="card"><p>{label}</p><strong>{value}</strong></article>; }
const appContainer = document.getElementById('root');
const appRoot = appContainer.__mediCoreRoot ?? createRoot(appContainer);
appContainer.__mediCoreRoot = appRoot;
appRoot.render(<StrictMode><App /></StrictMode>);
