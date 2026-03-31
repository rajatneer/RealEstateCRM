const API_BASE = '/api';

async function apiGet(endpoint) {
    const response = await fetch(`${API_BASE}${endpoint}`);
    if (!response.ok) throw new Error(`GET ${endpoint} failed: ${response.status}`);
    return response.json();
}

async function apiPost(endpoint, data) {
    const response = await fetch(`${API_BASE}${endpoint}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || `POST ${endpoint} failed: ${response.status}`);
    }
    return response.json();
}

async function apiPut(endpoint, data) {
    const response = await fetch(`${API_BASE}${endpoint}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    if (!response.ok) {
        const text = await response.text();
        throw new Error(text || `PUT ${endpoint} failed: ${response.status}`);
    }
    return response.json();
}

async function apiDelete(endpoint) {
    const response = await fetch(`${API_BASE}${endpoint}`, { method: 'DELETE' });
    if (!response.ok) throw new Error(`DELETE ${endpoint} failed: ${response.status}`);
    return true;
}
