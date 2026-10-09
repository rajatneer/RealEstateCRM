const API_BASE = '/api';

function authHeaders(extra = {}) {
    const token = localStorage.getItem('crm_token');
    return token ? { ...extra, Authorization: `Bearer ${token}` } : extra;
}

// Turns ProblemDetails / validation errors / plain text into one readable message.
async function readError(response, fallback) {
    let text = '';
    try { text = await response.text(); } catch { /* ignore */ }
    if (!text) return fallback;
    try {
        const body = JSON.parse(text);
        if (body.errors) {
            return Object.values(body.errors).flat().join(' ');
        }
        return body.detail || body.title || fallback;
    } catch {
        return text;
    }
}

// An expired or invalid token sends the user back to the login screen.
function handleUnauthorized(response, endpoint) {
    if (response.status === 401 && endpoint !== '/auth/login') {
        if (typeof logout === 'function') logout();
        throw new Error('Your session has expired. Please sign in again.');
    }
}

async function apiRequest(method, endpoint, data) {
    const options = { method, headers: authHeaders(data !== undefined ? { 'Content-Type': 'application/json' } : {}) };
    if (data !== undefined) options.body = JSON.stringify(data);

    const response = await fetch(`${API_BASE}${endpoint}`, options);
    handleUnauthorized(response, endpoint);
    if (!response.ok) {
        throw new Error(await readError(response, `${method} ${endpoint} failed: ${response.status}`));
    }
    if (response.status === 204) return true;
    const text = await response.text();
    return text ? JSON.parse(text) : true;
}

const apiGet = endpoint => apiRequest('GET', endpoint);
const apiPost = (endpoint, data) => apiRequest('POST', endpoint, data);
const apiPut = (endpoint, data) => apiRequest('PUT', endpoint, data);
const apiDelete = endpoint => apiRequest('DELETE', endpoint).then(() => true);
