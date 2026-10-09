// ── Authentication ──
function showLogin() {
    document.getElementById('login-container').style.display = '';
    document.getElementById('app-main').style.display = 'none';
    document.getElementById('login-error').textContent = '';
    document.getElementById('login-form').reset();
}

function showApp() {
    document.getElementById('login-container').style.display = 'none';
    document.getElementById('app-main').style.display = '';
}

function handleLogin(e) {
    e.preventDefault();
    const company = document.getElementById('login-company').value.trim();
    const username = document.getElementById('login-username').value.trim();
    const password = document.getElementById('login-password').value;
    login(company, username, password);
    return false;
}

async function login(companyCode, username, password) {
    try {
        const result = await apiPost('/auth/login', { companyCode, username, password });
        // Store session (simple localStorage for now)
        localStorage.setItem('crm_token', result.token || '');
        localStorage.setItem('crm_company', companyCode);
        localStorage.setItem('crm_username', username);
        showApp();
        loadDashboard();
    } catch (err) {
        document.getElementById('login-error').textContent = 'Login failed: ' + (err.message || 'Invalid credentials');
    }
}

function logout() {
    localStorage.removeItem('crm_token');
    localStorage.removeItem('crm_company');
    localStorage.removeItem('crm_username');
    showLogin();
}

function isLoggedIn() {
    return !!localStorage.getItem('crm_token');
}

// On page load, show login or app
window.addEventListener('DOMContentLoaded', () => {
    if (isLoggedIn()) {
        showApp();
        loadDashboard();
    } else {
        showLogin();
    }
});

// ── Navigation ──
document.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', e => {
        e.preventDefault();
        const page = link.dataset.page;
        document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
        link.classList.add('active');
        document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
        document.getElementById(`page-${page}`).classList.add('active');
        if (page === 'dashboard') loadDashboard();
        if (page === 'contacts') loadContacts();
        if (page === 'properties') loadProperties();
        if (page === 'leads') loadLeads();
        if (page === 'tasks') loadTasks();
        if (page === 'interactions') loadInteractions();
        if (page === 'brokerages') loadBrokerages();
        if (page === 'calculator') loadCalculatorPage();
        if (page === 'sitevisits') loadSiteVisits();
    });
});

// ── Toast ──
function showToast(message, isError = false) {
    const toast = document.getElementById('toast');
    toast.textContent = message;
    toast.className = isError ? 'toast error show' : 'toast show';
    setTimeout(() => toast.className = 'toast', 3000);
}

// ── Modal ──
function openModal(title, html) {
    document.getElementById('modal-title').textContent = title;
    document.getElementById('modal-body').innerHTML = html;
    document.getElementById('modal-overlay').classList.add('active');
}

function closeModal() {
    document.getElementById('modal-overlay').classList.remove('active');
}

// ── Formatting Helpers ──
function formatDate(dateStr) {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatPrice(price) {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(price);
}

function badgeClass(value) {
    return 'badge badge-' + (value || '').toLowerCase().replace(/\s+/g, '');
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ── Dashboard ──
async function loadDashboard() {
    try {
        const [contacts, properties, interactions, leads, overdueTasks] = await Promise.all([
            apiGet('/contacts'),
            apiGet('/properties'),
            apiGet('/interactions'),
            apiGet('/leads'),
            apiGet('/tasks/overdue')
        ]);
        document.getElementById('stat-contacts').textContent = contacts.length;
        document.getElementById('stat-properties').textContent = properties.length;
        document.getElementById('stat-available').textContent = properties.filter(p => p.status === 'Available').length;
        document.getElementById('stat-leads').textContent = leads.filter(l => !['Won','Lost'].includes(l.stage)).length;
        document.getElementById('stat-overdue').textContent = overdueTasks.length;
        document.getElementById('stat-interactions').textContent = interactions.length;

        // Overdue tasks
        const overdueContainer = document.getElementById('overdue-tasks-list');
        if (overdueTasks.length === 0) {
            overdueContainer.innerHTML = '<p class="empty-state">No overdue tasks. You\'re on track!</p>';
        } else {
            overdueContainer.innerHTML = `<table>
                <thead><tr><th>Task</th><th>Due Date</th><th>Priority</th><th>Contact</th></tr></thead>
                <tbody>${overdueTasks.map(t => `<tr class="task-overdue">
                    <td><strong>${escapeHtml(t.title)}</strong></td>
                    <td class="text-danger">${formatDate(t.dueDate)}</td>
                    <td><span class="${badgeClass(t.priority)}">${escapeHtml(t.priority)}</span></td>
                    <td>${t.contact ? escapeHtml(t.contact.firstName + ' ' + t.contact.lastName) : '-'}</td>
                </tr>`).join('')}</tbody>
            </table>`;
        }

        // Recent interactions
        const recent = interactions.slice(0, 5);
        const container = document.getElementById('recent-interactions-list');
        if (recent.length === 0) {
            container.innerHTML = '<p class="empty-state">No interactions yet.</p>';
        } else {
            container.innerHTML = `<table>
                <thead><tr><th>Date</th><th>Type</th><th>Contact</th><th>Description</th></tr></thead>
                <tbody>${recent.map(i => `<tr>
                    <td>${formatDate(i.date)}</td>
                    <td><span class="${badgeClass(i.type)}">${escapeHtml(i.type)}</span></td>
                    <td>${i.contact ? escapeHtml(i.contact.firstName + ' ' + i.contact.lastName) : '-'}</td>
                    <td>${escapeHtml(i.description ? i.description.substring(0, 80) : '')}</td>
                </tr>`).join('')}</tbody>
            </table>`;
        }
    } catch (err) {
        showToast('Failed to load dashboard: ' + err.message, true);
    }
}

// ── Contacts ──
let allContacts = [];
let searchContactTimer;

async function loadContacts() {
    try {
        allContacts = await apiGet('/contacts');
        renderContacts();
    } catch (err) {
        showToast('Failed to load contacts: ' + err.message, true);
    }
}

function searchContacts() {
    clearTimeout(searchContactTimer);
    searchContactTimer = setTimeout(async () => {
        try {
            const q = document.getElementById('contact-search').value.trim();
            const type = document.getElementById('contact-type-filter').value;
            const params = new URLSearchParams();
            if (q) params.set('q', q);
            if (type) params.set('type', type);
            allContacts = await apiGet(`/contacts/search?${params.toString()}`);
            renderContacts();
        } catch (err) {
            showToast('Search failed: ' + err.message, true);
        }
    }, 300);
}

function renderContacts() {
    const container = document.getElementById('contacts-list');
    if (allContacts.length === 0) {
        container.innerHTML = '<p class="empty-state">No contacts yet. Add your first contact!</p>';
        return;
    }
    container.innerHTML = `<table>
        <thead><tr><th>Name</th><th>Email</th><th>Phone</th><th>Type</th><th>Created</th><th>Actions</th></tr></thead>
        <tbody>${allContacts.map(c => `<tr>
            <td><strong>${escapeHtml(c.firstName)} ${escapeHtml(c.lastName)}</strong></td>
            <td>${escapeHtml(c.email) || '-'}</td>
            <td>${escapeHtml(c.phone) || '-'}</td>
            <td><span class="${badgeClass(c.type)}">${escapeHtml(c.type)}</span></td>
            <td>${formatDate(c.createdAt)}</td>
            <td class="actions">
                <button class="btn btn-sm btn-edit" data-onclick="editContact(${c.id})">Edit</button>
                <button class="btn btn-sm btn-danger" data-onclick="deleteContact(${c.id})">Delete</button>
            </td>
        </tr>`).join('')}</tbody>
    </table>`;
}

function contactFormHtml(c = null) {
    return `
    <form id="contact-form" data-onsubmit="saveContact(event, ${c ? c.id : 'null'})">
        <div class="form-row">
            <div class="form-group">
                <label>First Name *</label>
                <input name="firstName" required maxlength="100" value="${c ? escapeHtml(c.firstName) : ''}">
            </div>
            <div class="form-group">
                <label>Last Name *</label>
                <input name="lastName" required maxlength="100" value="${c ? escapeHtml(c.lastName) : ''}">
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Email</label>
                <input name="email" type="email" maxlength="200" value="${c ? escapeHtml(c.email || '') : ''}">
            </div>
            <div class="form-group">
                <label>Phone</label>
                <input name="phone" maxlength="20" value="${c ? escapeHtml(c.phone || '') : ''}">
            </div>
        </div>
        <div class="form-group">
            <label>Type *</label>
            <select name="type" required>
                ${['Buyer','Seller','Tenant','Landlord','Agent'].map(t =>
                    `<option value="${t}" ${c && c.type === t ? 'selected' : ''}>${t}</option>`
                ).join('')}
            </select>
        </div>
        <div class="form-group">
            <label>Notes</label>
            <textarea name="notes" maxlength="500">${c ? escapeHtml(c.notes || '') : ''}</textarea>
        </div>
        <div class="form-actions">
            <button type="button" class="btn btn-secondary" data-onclick="closeModal()">Cancel</button>
            <button type="submit" class="btn btn-primary">${c ? 'Update' : 'Create'}</button>
        </div>
    </form>`;
}

function showContactForm() {
    openModal('Add Contact', contactFormHtml());
}

async function editContact(id) {
    try {
        const contact = await apiGet(`/contacts/${id}`);
        openModal('Edit Contact', contactFormHtml(contact));
    } catch (err) {
        showToast('Failed to load contact: ' + err.message, true);
    }
}

async function saveContact(e, id) {
    e.preventDefault();
    const form = e.target;
    const data = {
        firstName: form.firstName.value.trim(),
        lastName: form.lastName.value.trim(),
        email: form.email.value.trim() || null,
        phone: form.phone.value.trim() || null,
        type: form.type.value,
        notes: form.notes.value.trim() || null
    };
    try {
        if (id) {
            await apiPut(`/contacts/${id}`, data);
            showToast('Contact updated!');
        } else {
            await apiPost('/contacts', data);
            showToast('Contact created!');
        }
        closeModal();
        loadContacts();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

async function deleteContact(id) {
    if (!confirm('Delete this contact?')) return;
    try {
        await apiDelete(`/contacts/${id}`);
        showToast('Contact deleted');
        loadContacts();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

// ── Properties ──
let allProperties = [];
let searchPropertyTimer;

async function loadProperties() {
    searchProperties();
}

function searchProperties() {
    clearTimeout(searchPropertyTimer);
    searchPropertyTimer = setTimeout(async () => {
        try {
            const q = document.getElementById('property-search').value.trim();
            const status = document.getElementById('property-status-filter').value;
            const type = document.getElementById('property-type-filter').value;
            const minPrice = document.getElementById('property-min-price').value;
            const maxPrice = document.getElementById('property-max-price').value;
            const minBeds = document.getElementById('property-min-beds').value;
            const params = new URLSearchParams();
            if (q) params.set('q', q);
            if (status) params.set('status', status);
            if (type) params.set('type', type);
            if (minPrice) params.set('minPrice', minPrice);
            if (maxPrice) params.set('maxPrice', maxPrice);
            if (minBeds) params.set('minBeds', minBeds);
            allProperties = await apiGet(`/properties/search?${params.toString()}`);
            renderProperties();
        } catch (err) {
            showToast('Search failed: ' + err.message, true);
        }
    }, 300);
}

function renderProperties() {
    const container = document.getElementById('properties-list');
    if (allProperties.length === 0) {
        container.innerHTML = '<p class="empty-state">No properties yet. Add your first listing!</p>';
        return;
    }
    container.innerHTML = `<table>
        <thead><tr><th>Address</th><th>City</th><th>Type</th><th>Status</th><th>Price</th><th>Bed/Bath</th><th>Owner</th><th>Actions</th></tr></thead>
        <tbody>${allProperties.map(p => `<tr>
            <td><strong>${escapeHtml(p.address)}</strong></td>
            <td>${escapeHtml(p.city)}, ${escapeHtml(p.state)}</td>
            <td>${escapeHtml(p.type)}</td>
            <td><span class="${badgeClass(p.status)}">${escapeHtml(p.status)}</span></td>
            <td class="price">${formatPrice(p.price)}</td>
            <td>${p.bedrooms}/${p.bathrooms}</td>
            <td>${p.owner ? escapeHtml(p.owner.firstName + ' ' + p.owner.lastName) : '-'}</td>
            <td class="actions">
                <button class="btn btn-sm btn-edit" data-onclick="editProperty(${p.id})">Edit</button>
                <button class="btn btn-sm btn-danger" data-onclick="deleteProperty(${p.id})">Delete</button>
            </td>
        </tr>`).join('')}</tbody>
    </table>`;
}

function propertyFormHtml(p = null) {
    return `
    <form id="property-form" data-onsubmit="saveProperty(event, ${p ? p.id : 'null'})">
        <div class="form-group">
            <label>Address *</label>
            <input name="address" required maxlength="300" value="${p ? escapeHtml(p.address) : ''}">
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>City *</label>
                <input name="city" required maxlength="100" value="${p ? escapeHtml(p.city) : ''}">
            </div>
            <div class="form-group">
                <label>State *</label>
                <input name="state" required maxlength="50" value="${p ? escapeHtml(p.state) : ''}">
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Zip Code</label>
                <input name="zipCode" maxlength="10" value="${p ? escapeHtml(p.zipCode || '') : ''}">
            </div>
            <div class="form-group">
                <label>Price *</label>
                <input name="price" type="number" min="0" step="0.01" required value="${p ? p.price : ''}">
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Type *</label>
                <select name="type" required>
                    ${['House','Apartment','Condo','Townhouse','Land','Commercial'].map(t =>
                        `<option value="${t}" ${p && p.type === t ? 'selected' : ''}>${t}</option>`
                    ).join('')}
                </select>
            </div>
            <div class="form-group">
                <label>Status *</label>
                <select name="status" required>
                    ${['Available','UnderContract','Sold','Rented','OffMarket'].map(s =>
                        `<option value="${s}" ${p && p.status === s ? 'selected' : ''}>${s}</option>`
                    ).join('')}
                </select>
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Bedrooms</label>
                <input name="bedrooms" type="number" min="0" value="${p ? p.bedrooms : 0}">
            </div>
            <div class="form-group">
                <label>Bathrooms</label>
                <input name="bathrooms" type="number" min="0" value="${p ? p.bathrooms : 0}">
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Square Feet</label>
                <input name="squareFeet" type="number" min="0" value="${p ? p.squareFeet : 0}">
            </div>
            <div class="form-group">
                <label>Owner</label>
                <select name="ownerId" id="owner-select">
                    <option value="">-- None --</option>
                </select>
            </div>
        </div>
        <div class="form-group">
            <label>Description</label>
            <textarea name="description" maxlength="1000">${p ? escapeHtml(p.description || '') : ''}</textarea>
        </div>
        <div class="form-actions">
            <button type="button" class="btn btn-secondary" data-onclick="closeModal()">Cancel</button>
            <button type="submit" class="btn btn-primary">${p ? 'Update' : 'Create'}</button>
        </div>
    </form>`;
}

async function populateOwnerSelect(selectedId) {
    try {
        const contacts = await apiGet('/contacts');
        const select = document.getElementById('owner-select');
        contacts.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = `${c.firstName} ${c.lastName} (${c.type})`;
            if (selectedId && c.id === selectedId) opt.selected = true;
            select.appendChild(opt);
        });
    } catch (_) { /* ignore */ }
}

function showPropertyForm() {
    openModal('Add Property', propertyFormHtml());
    populateOwnerSelect(null);
}

async function editProperty(id) {
    try {
        const prop = await apiGet(`/properties/${id}`);
        openModal('Edit Property', propertyFormHtml(prop));
        populateOwnerSelect(prop.ownerId);
    } catch (err) {
        showToast('Failed to load property: ' + err.message, true);
    }
}

async function saveProperty(e, id) {
    e.preventDefault();
    const form = e.target;
    const data = {
        address: form.address.value.trim(),
        city: form.city.value.trim(),
        state: form.state.value.trim(),
        zipCode: form.zipCode.value.trim() || null,
        type: form.type.value,
        status: form.status.value,
        price: parseFloat(form.price.value),
        bedrooms: parseInt(form.bedrooms.value) || 0,
        bathrooms: parseInt(form.bathrooms.value) || 0,
        squareFeet: parseFloat(form.squareFeet.value) || 0,
        description: form.description.value.trim() || null,
        ownerId: form.ownerId.value ? parseInt(form.ownerId.value) : null
    };
    try {
        if (id) {
            await apiPut(`/properties/${id}`, data);
            showToast('Property updated!');
        } else {
            await apiPost('/properties', data);
            showToast('Property created!');
        }
        closeModal();
        loadProperties();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

async function deleteProperty(id) {
    if (!confirm('Delete this property?')) return;
    try {
        await apiDelete(`/properties/${id}`);
        showToast('Property deleted');
        loadProperties();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

// ── Interactions ──
async function loadInteractions() {
    try {
        const interactions = await apiGet('/interactions');
        renderInteractions(interactions);
    } catch (err) {
        showToast('Failed to load interactions: ' + err.message, true);
    }
}

function renderInteractions(interactions) {
    const container = document.getElementById('interactions-list');
    if (interactions.length === 0) {
        container.innerHTML = '<p class="empty-state">No interactions yet. Log your first interaction!</p>';
        return;
    }
    container.innerHTML = `<table>
        <thead><tr><th>Date</th><th>Type</th><th>Contact</th><th>Property</th><th>Description</th><th>Actions</th></tr></thead>
        <tbody>${interactions.map(i => `<tr>
            <td>${formatDate(i.date)}</td>
            <td><span class="${badgeClass(i.type)}">${escapeHtml(i.type)}</span></td>
            <td>${i.contact ? escapeHtml(i.contact.firstName + ' ' + i.contact.lastName) : '-'}</td>
            <td>${i.property ? escapeHtml(i.property.address) : '-'}</td>
            <td>${escapeHtml(i.description ? i.description.substring(0, 100) : '')}</td>
            <td class="actions">
                <button class="btn btn-sm btn-danger" data-onclick="deleteInteraction(${i.id})">Delete</button>
            </td>
        </tr>`).join('')}</tbody>
    </table>`;
}

function interactionFormHtml() {
    return `
    <form id="interaction-form" data-onsubmit="saveInteraction(event)">
        <div class="form-row">
            <div class="form-group">
                <label>Contact *</label>
                <select name="contactId" id="int-contact-select" required>
                    <option value="">-- Select --</option>
                </select>
            </div>
            <div class="form-group">
                <label>Property</label>
                <select name="propertyId" id="int-property-select">
                    <option value="">-- None --</option>
                </select>
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Type *</label>
                <select name="type" required>
                    ${['Call','Email','Meeting','Showing','Offer','Other'].map(t =>
                        `<option value="${t}">${t}</option>`
                    ).join('')}
                </select>
            </div>
            <div class="form-group">
                <label>Date</label>
                <input name="date" type="datetime-local">
            </div>
        </div>
        <div class="form-group">
            <label>Description *</label>
            <textarea name="description" required maxlength="2000"></textarea>
        </div>
        <div class="form-actions">
            <button type="button" class="btn btn-secondary" data-onclick="closeModal()">Cancel</button>
            <button type="submit" class="btn btn-primary">Log Interaction</button>
        </div>
    </form>`;
}

async function populateInteractionSelects() {
    try {
        const [contacts, properties] = await Promise.all([
            apiGet('/contacts'),
            apiGet('/properties')
        ]);
        const cs = document.getElementById('int-contact-select');
        contacts.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = `${c.firstName} ${c.lastName}`;
            cs.appendChild(opt);
        });
        const ps = document.getElementById('int-property-select');
        properties.forEach(p => {
            const opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = p.address;
            ps.appendChild(opt);
        });
    } catch (_) { /* ignore */ }
}

function showInteractionForm() {
    openModal('Log Interaction', interactionFormHtml());
    populateInteractionSelects();
}

async function saveInteraction(e) {
    e.preventDefault();
    const form = e.target;
    const data = {
        contactId: parseInt(form.contactId.value),
        propertyId: form.propertyId.value ? parseInt(form.propertyId.value) : null,
        type: form.type.value,
        description: form.description.value.trim(),
        date: form.date.value ? new Date(form.date.value).toISOString() : null
    };
    try {
        await apiPost('/interactions', data);
        showToast('Interaction logged!');
        closeModal();
        loadInteractions();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

async function deleteInteraction(id) {
    if (!confirm('Delete this interaction?')) return;
    try {
        await apiDelete(`/interactions/${id}`);
        showToast('Interaction deleted');
        loadInteractions();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

// ── Leads Pipeline ──
const LEAD_STAGES = ['New', 'Contacted', 'Qualified', 'Negotiation', 'Won', 'Lost'];
const LEAD_SOURCES = ['Website', 'Referral', 'Advertisement', 'WalkIn', 'SocialMedia', 'ColdCall', 'Other'];

async function loadLeads() {
    try {
        const filter = document.getElementById('lead-stage-filter').value;
        const endpoint = filter ? `/leads?stage=${filter}` : '/leads';
        const leads = await apiGet(endpoint);
        renderLeadsPipeline(leads);
        renderLeadsTable(leads);
    } catch (err) {
        showToast('Failed to load leads: ' + err.message, true);
    }
}

function renderLeadsPipeline(leads) {
    const container = document.getElementById('leads-pipeline');
    const filter = document.getElementById('lead-stage-filter').value;
    const stages = filter ? [filter] : LEAD_STAGES;

    container.innerHTML = stages.map(stage => {
        const stageLeads = leads.filter(l => l.stage === stage);
        return `<div class="pipeline-column stage-${stage.toLowerCase()}">
            <h3>${stage} <span class="pipeline-count">${stageLeads.length}</span></h3>
            ${stageLeads.length === 0 ? '<p style="font-size:12px;color:#94a3b8;text-align:center;">No leads</p>' : ''}
            ${stageLeads.map(l => `<div class="pipeline-card" data-onclick="editLead(${l.id})" style="cursor:pointer">
                <div class="card-name">${l.contact ? escapeHtml(l.contact.firstName + ' ' + l.contact.lastName) : 'Unknown'}</div>
                ${l.estimatedValue ? `<div class="card-value">${formatPrice(l.estimatedValue)}</div>` : ''}
                <div class="card-source">${escapeHtml(l.source)}</div>
            </div>`).join('')}
        </div>`;
    }).join('');
}

function renderLeadsTable(leads) {
    const container = document.getElementById('leads-list');
    if (leads.length === 0) {
        container.innerHTML = '<p class="empty-state">No leads yet. Start building your pipeline!</p>';
        return;
    }
    container.innerHTML = `<table>
        <thead><tr><th>Contact</th><th>Property</th><th>Stage</th><th>Source</th><th>Value</th><th>Updated</th><th>Actions</th></tr></thead>
        <tbody>${leads.map(l => `<tr>
            <td><strong>${l.contact ? escapeHtml(l.contact.firstName + ' ' + l.contact.lastName) : '-'}</strong></td>
            <td>${l.property ? escapeHtml(l.property.address) : '-'}</td>
            <td><span class="${badgeClass(l.stage)}">${escapeHtml(l.stage)}</span></td>
            <td>${escapeHtml(l.source)}</td>
            <td class="price">${l.estimatedValue ? formatPrice(l.estimatedValue) : '-'}</td>
            <td>${formatDate(l.updatedAt)}</td>
            <td class="actions">
                <button class="btn btn-sm btn-edit" data-onclick="editLead(${l.id})">Edit</button>
                <button class="btn btn-sm btn-danger" data-onclick="deleteLead(${l.id})">Delete</button>
            </td>
        </tr>`).join('')}</tbody>
    </table>`;
}

function leadFormHtml(l = null) {
    return `
    <form id="lead-form" data-onsubmit="saveLead(event, ${l ? l.id : 'null'})">
        <div class="form-row">
            <div class="form-group">
                <label>Contact *</label>
                <select name="contactId" id="lead-contact-select" required>
                    <option value="">-- Select --</option>
                </select>
            </div>
            <div class="form-group">
                <label>Property</label>
                <select name="propertyId" id="lead-property-select">
                    <option value="">-- None --</option>
                </select>
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Stage *</label>
                <select name="stage" required>
                    ${LEAD_STAGES.map(s =>
                        `<option value="${s}" ${l && l.stage === s ? 'selected' : ''}>${s}</option>`
                    ).join('')}
                </select>
            </div>
            <div class="form-group">
                <label>Source *</label>
                <select name="source" required>
                    ${LEAD_SOURCES.map(s =>
                        `<option value="${s}" ${l && l.source === s ? 'selected' : ''}>${s}</option>`
                    ).join('')}
                </select>
            </div>
        </div>
        <div class="form-group">
            <label>Estimated Value</label>
            <input name="estimatedValue" type="number" min="0" step="0.01" value="${l && l.estimatedValue ? l.estimatedValue : ''}">
        </div>
        <div class="form-group">
            <label>Notes</label>
            <textarea name="notes" maxlength="1000">${l ? escapeHtml(l.notes || '') : ''}</textarea>
        </div>
        <div class="form-actions">
            <button type="button" class="btn btn-secondary" data-onclick="closeModal()">Cancel</button>
            <button type="submit" class="btn btn-primary">${l ? 'Update' : 'Create'}</button>
        </div>
    </form>`;
}

async function populateLeadSelects(contactId, propertyId) {
    try {
        const [contacts, properties] = await Promise.all([
            apiGet('/contacts'),
            apiGet('/properties')
        ]);
        const cs = document.getElementById('lead-contact-select');
        contacts.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = `${c.firstName} ${c.lastName} (${c.type})`;
            if (contactId && c.id === contactId) opt.selected = true;
            cs.appendChild(opt);
        });
        const ps = document.getElementById('lead-property-select');
        properties.forEach(p => {
            const opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = p.address;
            if (propertyId && p.id === propertyId) opt.selected = true;
            ps.appendChild(opt);
        });
    } catch (_) { /* ignore */ }
}

function showLeadForm() {
    openModal('Add Lead', leadFormHtml());
    populateLeadSelects(null, null);
}

async function editLead(id) {
    try {
        const [lead, timeline] = await Promise.all([
            apiGet(`/leads/${id}`),
            apiGet(`/leads/${id}/timeline`)
        ]);
        openModal('Edit Lead', leadFormHtml(lead) + timelineHtml(timeline));
        populateLeadSelects(lead.contactId, lead.propertyId);
    } catch (err) {
        showToast('Failed to load lead: ' + err.message, true);
    }
function timelineHtml(timeline) {
    if (!timeline || timeline.length === 0) {
        return `<div class="timeline-section"><h3>Timeline</h3><p class="empty-state">No activities yet.</p></div>`;
    }
    return `<div class="timeline-section"><h3>Timeline</h3><ul class="timeline-list">${timeline.map(e => `
        <li class="timeline-item">
            <div class="timeline-date">${formatDate(e.date)}</div>
            <div class="timeline-type"><span class="badge badge-info">${escapeHtml(e.type)}</span></div>
            <div class="timeline-summary"><strong>${escapeHtml(e.summary)}</strong></div>
            <div class="timeline-details">${escapeHtml(e.details)}</div>
        </li>`).join('')}</ul></div>`;
}
}

async function saveLead(e, id) {
    e.preventDefault();
    const form = e.target;
    const data = {
        contactId: parseInt(form.contactId.value),
        propertyId: form.propertyId.value ? parseInt(form.propertyId.value) : null,
        stage: form.stage.value,
        source: form.source.value,
        estimatedValue: form.estimatedValue.value ? parseFloat(form.estimatedValue.value) : null,
        notes: form.notes.value.trim() || null
    };
    try {
        if (id) {
            await apiPut(`/leads/${id}`, data);
            showToast('Lead updated!');
        } else {
            await apiPost('/leads', data);
            showToast('Lead created!');
        }
        closeModal();
        loadLeads();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

async function deleteLead(id) {
    if (!confirm('Delete this lead?')) return;
    try {
        await apiDelete(`/leads/${id}`);
        showToast('Lead deleted');
        loadLeads();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

// ── Tasks & Reminders ──
const TASK_PRIORITIES = ['Low', 'Medium', 'High', 'Urgent'];
const TASK_STATUSES = ['Pending', 'InProgress', 'Completed', 'Cancelled'];

async function loadTasks() {
    try {
        const filter = document.getElementById('task-status-filter').value;
        const endpoint = filter ? `/tasks?status=${filter}` : '/tasks';
        const tasks = await apiGet(endpoint);
        renderTasks(tasks);
    } catch (err) {
        showToast('Failed to load tasks: ' + err.message, true);
    }
}

function renderTasks(tasks) {
    const container = document.getElementById('tasks-list');
    if (tasks.length === 0) {
        container.innerHTML = '<p class="empty-state">No tasks. Create a reminder to stay organized!</p>';
        return;
    }
    const now = new Date();
    container.innerHTML = `<table>
        <thead><tr><th>Title</th><th>Due Date</th><th>Priority</th><th>Status</th><th>Contact</th><th>Actions</th></tr></thead>
        <tbody>${tasks.map(t => {
            const isOverdue = new Date(t.dueDate) < now && t.status !== 'Completed' && t.status !== 'Cancelled';
            return `<tr class="${isOverdue ? 'task-overdue' : ''}">
                <td><strong>${escapeHtml(t.title)}</strong>${t.description ? `<br><small style="color:#64748b">${escapeHtml(t.description.substring(0, 60))}</small>` : ''}</td>
                <td class="${isOverdue ? 'text-danger' : ''}">${formatDate(t.dueDate)}</td>
                <td><span class="${badgeClass(t.priority)}">${escapeHtml(t.priority)}</span></td>
                <td><span class="${badgeClass(t.status)}">${escapeHtml(t.status)}</span></td>
                <td>${t.contact ? escapeHtml(t.contact.firstName + ' ' + t.contact.lastName) : '-'}</td>
                <td class="actions">
                    ${t.status === 'Pending' ? `<button class="btn btn-sm btn-primary" data-onclick="markTaskStatus(${t.id}, 'InProgress')">Start</button>` : ''}
                    ${t.status === 'InProgress' ? `<button class="btn btn-sm btn-primary" data-onclick="markTaskStatus(${t.id}, 'Completed')">Done</button>` : ''}
                    <button class="btn btn-sm btn-edit" data-onclick="editTask(${t.id})">Edit</button>
                    <button class="btn btn-sm btn-danger" data-onclick="deleteTask(${t.id})">Delete</button>
                </td>
            </tr>`;
        }).join('')}</tbody>
    </table>`;
}

function taskFormHtml(t = null) {
    return `
    <form id="task-form" data-onsubmit="saveTask(event, ${t ? t.id : 'null'})">
        <div class="form-group">
            <label>Title *</label>
            <input name="title" required maxlength="200" value="${t ? escapeHtml(t.title) : ''}">
        </div>
        <div class="form-group">
            <label>Description</label>
            <textarea name="description" maxlength="1000">${t ? escapeHtml(t.description || '') : ''}</textarea>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Due Date *</label>
                <input name="dueDate" type="datetime-local" required value="${t ? toLocalDatetime(t.dueDate) : ''}">
            </div>
            <div class="form-group">
                <label>Priority *</label>
                <select name="priority" required>
                    ${TASK_PRIORITIES.map(p =>
                        `<option value="${p}" ${t && t.priority === p ? 'selected' : ''}>${p}</option>`
                    ).join('')}
                </select>
            </div>
        </div>
        ${t ? `<div class="form-group">
            <label>Status</label>
            <select name="status">
                ${TASK_STATUSES.map(s =>
                    `<option value="${s}" ${t.status === s ? 'selected' : ''}>${s}</option>`
                ).join('')}
            </select>
        </div>` : ''}
        <div class="form-row">
            <div class="form-group">
                <label>Contact</label>
                <select name="contactId" id="task-contact-select">
                    <option value="">-- None --</option>
                </select>
            </div>
            <div class="form-group">
                <label>Property</label>
                <select name="propertyId" id="task-property-select">
                    <option value="">-- None --</option>
                </select>
            </div>
        </div>
        <div class="form-group">
            <label>Lead</label>
            <select name="leadId" id="task-lead-select">
                <option value="">-- None --</option>
            </select>
        </div>
        <div class="form-actions">
            <button type="button" class="btn btn-secondary" data-onclick="closeModal()">Cancel</button>
            <button type="submit" class="btn btn-primary">${t ? 'Update' : 'Create'}</button>
        </div>
    </form>`;
}

function toLocalDatetime(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.getFullYear() + '-' +
        String(d.getMonth() + 1).padStart(2, '0') + '-' +
        String(d.getDate()).padStart(2, '0') + 'T' +
        String(d.getHours()).padStart(2, '0') + ':' +
        String(d.getMinutes()).padStart(2, '0');
}

async function populateTaskSelects(contactId, propertyId, leadId) {
    try {
        const [contacts, properties, leads] = await Promise.all([
            apiGet('/contacts'),
            apiGet('/properties'),
            apiGet('/leads')
        ]);
        const cs = document.getElementById('task-contact-select');
        contacts.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = `${c.firstName} ${c.lastName}`;
            if (contactId && c.id === contactId) opt.selected = true;
            cs.appendChild(opt);
        });
        const ps = document.getElementById('task-property-select');
        properties.forEach(p => {
            const opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = p.address;
            if (propertyId && p.id === propertyId) opt.selected = true;
            ps.appendChild(opt);
        });
        const ls = document.getElementById('task-lead-select');
        leads.forEach(l => {
            const opt = document.createElement('option');
            opt.value = l.id;
            opt.textContent = l.contact ? `${l.contact.firstName} ${l.contact.lastName} - ${l.stage}` : `Lead #${l.id}`;
            if (leadId && l.id === leadId) opt.selected = true;
            ls.appendChild(opt);
        });
    } catch (_) { /* ignore */ }
}

function showTaskForm() {
    openModal('Add Task', taskFormHtml());
    populateTaskSelects(null, null, null);
}

async function editTask(id) {
    try {
        const task = await apiGet(`/tasks/${id}`);
        openModal('Edit Task', taskFormHtml(task));
        populateTaskSelects(task.contactId, task.propertyId, task.leadId);
    } catch (err) {
        showToast('Failed to load task: ' + err.message, true);
    }
}

async function saveTask(e, id) {
    e.preventDefault();
    const form = e.target;
    const data = {
        title: form.title.value.trim(),
        description: form.description.value.trim() || null,
        dueDate: new Date(form.dueDate.value).toISOString(),
        priority: form.priority.value,
        contactId: form.contactId.value ? parseInt(form.contactId.value) : null,
        propertyId: form.propertyId.value ? parseInt(form.propertyId.value) : null,
        leadId: form.leadId.value ? parseInt(form.leadId.value) : null
    };
    if (id) {
        data.status = form.status.value;
    }
    try {
        if (id) {
            await apiPut(`/tasks/${id}`, data);
            showToast('Task updated!');
        } else {
            await apiPost('/tasks', data);
            showToast('Task created!');
        }
        closeModal();
        loadTasks();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

async function markTaskStatus(id, newStatus) {
    try {
        const task = await apiGet(`/tasks/${id}`);
        const data = {
            title: task.title,
            description: task.description,
            dueDate: task.dueDate,
            priority: task.priority,
            status: newStatus,
            contactId: task.contactId,
            propertyId: task.propertyId,
            leadId: task.leadId
        };
        await apiPut(`/tasks/${id}`, data);
        showToast(`Task marked as ${newStatus}!`);
        loadTasks();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}

async function deleteTask(id) {
    if (!confirm('Delete this task?')) return;
    try {
        await apiDelete(`/tasks/${id}`);
        showToast('Task deleted');
        loadTasks();
    } catch (err) {
        showToast('Error: ' + err.message, true);
    }
}


// Optionally, add a logout button somewhere in the UI for users to log out.

// ══════════════════════════════════════════
// ── Brokerage Tracking ──
// ══════════════════════════════════════════
const BROKERAGE_STATUSES = ['Pending', 'PartiallyPaid', 'Paid'];

async function loadBrokerages() {
    try {
        const filter = document.getElementById('brokerage-status-filter').value;
        const endpoint = filter ? `/brokerages?status=${filter}` : '/brokerages';
        const brokerages = await apiGet(endpoint);
        renderBrokerageSummary(brokerages);
        renderBrokerages(brokerages);
    } catch (err) {
        showToast('Failed to load brokerages: ' + err.message, true);
    }
}

function renderBrokerageSummary(brokerages) {
    const totalDeal = brokerages.reduce((s, b) => s + b.dealValue, 0);
    const totalCommission = brokerages.reduce((s, b) => s + b.commissionAmount, 0);
    const totalGst = brokerages.reduce((s, b) => s + b.gstAmount, 0);
    const pendingAmount = brokerages.filter(b => b.paymentStatus !== 'Paid').reduce((s, b) => s + b.totalPayable, 0);

    document.getElementById('brokerage-summary').innerHTML = `
        <div class="stat-card"><div class="stat-number">${formatPrice(totalDeal)}</div><div class="stat-label">Total Deal Value</div></div>
        <div class="stat-card"><div class="stat-number">${formatPrice(totalCommission)}</div><div class="stat-label">Total Commission</div></div>
        <div class="stat-card"><div class="stat-number">${formatPrice(totalGst)}</div><div class="stat-label">Total GST (18%)</div></div>
        <div class="stat-card"><div class="stat-number">${formatPrice(pendingAmount)}</div><div class="stat-label">Pending Payment</div></div>
    `;
}

function renderBrokerages(brokerages) {
    const container = document.getElementById('brokerages-list');
    if (brokerages.length === 0) {
        container.innerHTML = '<p class="empty-state">No brokerage records yet.</p>';
        return;
    }
    container.innerHTML = `<table>
        <thead><tr><th>Deal Value</th><th>Commission %</th><th>Commission</th><th>GST (18%)</th><th>Total Payable</th><th>Sub-Broker</th><th>Status</th><th>Actions</th></tr></thead>
        <tbody>${brokerages.map(b => `<tr>
            <td class="price">${formatPrice(b.dealValue)}</td>
            <td>${b.commissionPercent}%</td>
            <td>${formatPrice(b.commissionAmount)}</td>
            <td>${formatPrice(b.gstAmount)}</td>
            <td><strong>${formatPrice(b.totalPayable)}</strong></td>
            <td>${b.subBrokerName ? `${escapeHtml(b.subBrokerName)} (${b.subBrokerSplitPercent}% = ${formatPrice(b.subBrokerAmount || 0)})` : '-'}</td>
            <td><span class="${badgeClass(b.paymentStatus)}">${escapeHtml(b.paymentStatus)}</span></td>
            <td class="actions">
                <button class="btn btn-sm btn-edit" data-onclick="editBrokerage(${b.id})">Edit</button>
                <button class="btn btn-sm btn-danger" data-onclick="deleteBrokerage(${b.id})">Delete</button>
            </td>
        </tr>`).join('')}</tbody>
    </table>`;
}

function brokerageFormHtml(b = null) {
    return `
    <form id="brokerage-form" data-onsubmit="saveBrokerage(event, ${b ? b.id : 'null'})">
        <div class="form-row">
            <div class="form-group">
                <label>Lead</label>
                <select name="leadId" id="brk-lead-select"><option value="">-- None --</option></select>
            </div>
            <div class="form-group">
                <label>Property</label>
                <select name="propertyId" id="brk-property-select"><option value="">-- None --</option></select>
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Deal Value (₹) *</label>
                <input name="dealValue" type="number" min="1" step="1" required value="${b ? b.dealValue : ''}">
            </div>
            <div class="form-group">
                <label>Commission % *</label>
                <input name="commissionPercent" type="number" min="0" max="100" step="0.01" required value="${b ? b.commissionPercent : ''}">
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Sub-Broker Name</label>
                <input name="subBrokerName" maxlength="200" value="${b ? escapeHtml(b.subBrokerName || '') : ''}">
            </div>
            <div class="form-group">
                <label>Sub-Broker Split %</label>
                <input name="subBrokerSplitPercent" type="number" min="0" max="100" step="0.01" value="${b && b.subBrokerSplitPercent ? b.subBrokerSplitPercent : ''}">
            </div>
        </div>
        ${b ? `<div class="form-group">
            <label>Payment Status</label>
            <select name="paymentStatus">
                ${BROKERAGE_STATUSES.map(s => `<option value="${s}" ${b.paymentStatus === s ? 'selected' : ''}>${s === 'PartiallyPaid' ? 'Partially Paid' : s}</option>`).join('')}
            </select>
        </div>` : ''}
        <div class="form-group">
            <label>Notes</label>
            <textarea name="notes" maxlength="1000">${b ? escapeHtml(b.notes || '') : ''}</textarea>
        </div>
        <div class="form-actions">
            <button type="button" class="btn btn-secondary" data-onclick="closeModal()">Cancel</button>
            <button type="submit" class="btn btn-primary">${b ? 'Update' : 'Create'}</button>
        </div>
    </form>`;
}

async function populateBrokerageSelects(leadId, propertyId) {
    try {
        const [leads, properties] = await Promise.all([apiGet('/leads'), apiGet('/properties')]);
        const ls = document.getElementById('brk-lead-select');
        leads.forEach(l => {
            const opt = document.createElement('option');
            opt.value = l.id;
            opt.textContent = l.contact ? `${l.contact.firstName} ${l.contact.lastName} - ${l.stage}` : `Lead #${l.id}`;
            if (leadId && l.id === leadId) opt.selected = true;
            ls.appendChild(opt);
        });
        const ps = document.getElementById('brk-property-select');
        properties.forEach(p => {
            const opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = p.address;
            if (propertyId && p.id === propertyId) opt.selected = true;
            ps.appendChild(opt);
        });
    } catch (_) { /* ignore */ }
}

function showBrokerageForm() {
    openModal('Add Brokerage', brokerageFormHtml());
    populateBrokerageSelects(null, null);
}

async function editBrokerage(id) {
    try {
        const b = await apiGet(`/brokerages/${id}`);
        openModal('Edit Brokerage', brokerageFormHtml(b));
        populateBrokerageSelects(b.leadId, b.propertyId);
    } catch (err) { showToast('Failed to load brokerage: ' + err.message, true); }
}

async function saveBrokerage(e, id) {
    e.preventDefault();
    const form = e.target;
    const data = {
        leadId: form.leadId.value ? parseInt(form.leadId.value) : null,
        propertyId: form.propertyId.value ? parseInt(form.propertyId.value) : null,
        dealValue: parseFloat(form.dealValue.value),
        commissionPercent: parseFloat(form.commissionPercent.value),
        subBrokerName: form.subBrokerName.value.trim() || null,
        subBrokerSplitPercent: form.subBrokerSplitPercent.value ? parseFloat(form.subBrokerSplitPercent.value) : null,
        notes: form.notes.value.trim() || null
    };
    if (id) data.paymentStatus = form.paymentStatus.value;
    try {
        if (id) { await apiPut(`/brokerages/${id}`, data); showToast('Brokerage updated!'); }
        else { await apiPost('/brokerages', data); showToast('Brokerage created!'); }
        closeModal(); loadBrokerages();
    } catch (err) { showToast('Error: ' + err.message, true); }
}

async function deleteBrokerage(id) {
    if (!confirm('Delete this brokerage record?')) return;
    try { await apiDelete(`/brokerages/${id}`); showToast('Brokerage deleted'); loadBrokerages(); }
    catch (err) { showToast('Error: ' + err.message, true); }
}

// ══════════════════════════════════════════
// ── EMI & Stamp Duty Calculator ──
// ══════════════════════════════════════════
async function loadCalculatorPage() {
    try {
        const states = await apiGet('/calculator/states');
        const select = document.getElementById('stampduty-state-select');
        select.innerHTML = '<option value="">-- Select State --</option>';
        states.forEach(s => {
            const opt = document.createElement('option');
            opt.value = s;
            opt.textContent = s;
            select.appendChild(opt);
        });
    } catch (err) { showToast('Failed to load states: ' + err.message, true); }
}

async function calculateEmi(e) {
    e.preventDefault();
    const form = e.target;
    const data = {
        loanAmount: parseFloat(form.loanAmount.value),
        annualInterestRate: parseFloat(form.annualInterestRate.value),
        tenureMonths: parseInt(form.tenureMonths.value)
    };
    try {
        const result = await apiPost('/calculator/emi', data);
        const div = document.getElementById('emi-result');
        div.style.display = 'block';
        div.innerHTML = `
            <div class="result-row"><span class="label">Loan Amount</span><span>${formatPrice(result.loanAmount)}</span></div>
            <div class="result-row"><span class="label">Interest Rate</span><span>${result.annualInterestRate}% p.a.</span></div>
            <div class="result-row"><span class="label">Tenure</span><span>${result.tenureMonths} months (${(result.tenureMonths/12).toFixed(1)} yrs)</span></div>
            <div class="result-row total"><span class="label">Monthly EMI</span><span>${formatPrice(result.monthlyEmi)}</span></div>
            <div class="result-row"><span class="label">Total Interest</span><span>${formatPrice(result.totalInterest)}</span></div>
            <div class="result-row"><span class="label">Total Payment</span><span>${formatPrice(result.totalPayment)}</span></div>
        `;
    } catch (err) { showToast('Error: ' + err.message, true); }
}

async function calculateStampDuty(e) {
    e.preventDefault();
    const form = e.target;
    const data = {
        propertyValue: parseFloat(form.propertyValue.value),
        state: form.state.value
    };
    try {
        const result = await apiPost('/calculator/stampduty', data);
        const div = document.getElementById('stampduty-result');
        div.style.display = 'block';
        div.innerHTML = `
            <div class="result-row"><span class="label">Property Value</span><span>${formatPrice(result.propertyValue)}</span></div>
            <div class="result-row"><span class="label">State</span><span>${escapeHtml(result.state)}</span></div>
            <div class="result-row"><span class="label">Stamp Duty (${result.stampDutyPercent}%)</span><span>${formatPrice(result.stampDutyAmount)}</span></div>
            <div class="result-row"><span class="label">Registration (${result.registrationPercent}%)</span><span>${formatPrice(result.registrationAmount)}</span></div>
            <div class="result-row total"><span class="label">Total Cost</span><span>${formatPrice(result.totalCost)}</span></div>
        `;
    } catch (err) { showToast('Error: ' + err.message, true); }
}

// ══════════════════════════════════════════
// ── Site Visit Scheduling ──
// ══════════════════════════════════════════
const SITEVISIT_STATUSES = ['Scheduled', 'Confirmed', 'Completed', 'Cancelled', 'NoShow'];

async function loadSiteVisits() {
    try {
        const filter = document.getElementById('sitevisit-status-filter').value;
        const endpoint = filter ? `/sitevisits?status=${filter}` : '/sitevisits';
        const visits = await apiGet(endpoint);
        renderSiteVisits(visits);
    } catch (err) { showToast('Failed to load site visits: ' + err.message, true); }
}

function renderSiteVisits(visits) {
    const container = document.getElementById('sitevisits-list');
    if (visits.length === 0) {
        container.innerHTML = '<p class="empty-state">No site visits scheduled. Book one now!</p>';
        return;
    }
    const now = new Date();
    container.innerHTML = `<table>
        <thead><tr><th>Contact</th><th>Property</th><th>Scheduled Date</th><th>Status</th><th>Pickup</th><th>Feedback</th><th>Actions</th></tr></thead>
        <tbody>${visits.map(v => {
            const isPast = new Date(v.scheduledDate) < now && v.status === 'Scheduled';
            return `<tr class="${isPast ? 'task-overdue' : ''}">
                <td><strong>${v.contact ? escapeHtml(v.contact.firstName + ' ' + v.contact.lastName) : '-'}</strong></td>
                <td>${v.property ? escapeHtml(v.property.address) : '-'}</td>
                <td class="${isPast ? 'text-danger' : ''}">${formatDate(v.scheduledDate)}</td>
                <td><span class="${badgeClass(v.status)}">${v.status === 'NoShow' ? 'No Show' : escapeHtml(v.status)}</span></td>
                <td>${escapeHtml(v.pickupLocation) || '-'}</td>
                <td>${escapeHtml(v.feedback ? v.feedback.substring(0, 60) : '') || '-'}</td>
                <td class="actions">
                    ${v.status === 'Scheduled' ? `<button class="btn btn-sm btn-primary" data-onclick="updateSiteVisitStatus(${v.id}, 'Confirmed')">Confirm</button>` : ''}
                    ${v.status === 'Confirmed' ? `<button class="btn btn-sm btn-primary" data-onclick="updateSiteVisitStatus(${v.id}, 'Completed')">Done</button>` : ''}
                    <button class="btn btn-sm btn-edit" data-onclick="editSiteVisit(${v.id})">Edit</button>
                    <button class="btn btn-sm btn-danger" data-onclick="deleteSiteVisit(${v.id})">Delete</button>
                </td>
            </tr>`;
        }).join('')}</tbody>
    </table>`;
}

function siteVisitFormHtml(v = null) {
    return `
    <form id="sitevisit-form" data-onsubmit="saveSiteVisit(event, ${v ? v.id : 'null'})">
        <div class="form-row">
            <div class="form-group">
                <label>Contact *</label>
                <select name="contactId" id="sv-contact-select" required><option value="">-- Select --</option></select>
            </div>
            <div class="form-group">
                <label>Property *</label>
                <select name="propertyId" id="sv-property-select" required><option value="">-- Select --</option></select>
            </div>
        </div>
        <div class="form-row">
            <div class="form-group">
                <label>Scheduled Date *</label>
                <input name="scheduledDate" type="datetime-local" required value="${v ? toLocalDatetime(v.scheduledDate) : ''}">
            </div>
            ${v ? `<div class="form-group">
                <label>Status</label>
                <select name="status">
                    ${SITEVISIT_STATUSES.map(s => `<option value="${s}" ${v.status === s ? 'selected' : ''}>${s === 'NoShow' ? 'No Show' : s}</option>`).join('')}
                </select>
            </div>` : ''}
        </div>
        <div class="form-group">
            <label>Pickup Location</label>
            <input name="pickupLocation" maxlength="200" value="${v ? escapeHtml(v.pickupLocation || '') : ''}" placeholder="e.g. Metro Station, Office, etc.">
        </div>
        ${v ? `<div class="form-group">
            <label>Feedback</label>
            <textarea name="feedback" maxlength="500">${escapeHtml(v.feedback || '')}</textarea>
        </div>` : ''}
        <div class="form-group">
            <label>Notes</label>
            <textarea name="notes" maxlength="1000">${v ? escapeHtml(v.notes || '') : ''}</textarea>
        </div>
        <div class="form-actions">
            <button type="button" class="btn btn-secondary" data-onclick="closeModal()">Cancel</button>
            <button type="submit" class="btn btn-primary">${v ? 'Update' : 'Schedule'}</button>
        </div>
    </form>`;
}

async function populateSiteVisitSelects(contactId, propertyId) {
    try {
        const [contacts, properties] = await Promise.all([apiGet('/contacts'), apiGet('/properties')]);
        const cs = document.getElementById('sv-contact-select');
        contacts.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = `${c.firstName} ${c.lastName}`;
            if (contactId && c.id === contactId) opt.selected = true;
            cs.appendChild(opt);
        });
        const ps = document.getElementById('sv-property-select');
        properties.forEach(p => {
            const opt = document.createElement('option');
            opt.value = p.id;
            opt.textContent = p.address;
            if (propertyId && p.id === propertyId) opt.selected = true;
            ps.appendChild(opt);
        });
    } catch (_) { /* ignore */ }
}

function showSiteVisitForm() {
    openModal('Schedule Site Visit', siteVisitFormHtml());
    populateSiteVisitSelects(null, null);
}

async function editSiteVisit(id) {
    try {
        const v = await apiGet(`/sitevisits/${id}`);
        openModal('Edit Site Visit', siteVisitFormHtml(v));
        populateSiteVisitSelects(v.contactId, v.propertyId);
    } catch (err) { showToast('Failed to load site visit: ' + err.message, true); }
}

async function saveSiteVisit(e, id) {
    e.preventDefault();
    const form = e.target;
    const data = {
        contactId: parseInt(form.contactId.value),
        propertyId: parseInt(form.propertyId.value),
        scheduledDate: new Date(form.scheduledDate.value).toISOString(),
        pickupLocation: form.pickupLocation.value.trim() || null,
        notes: form.notes.value.trim() || null
    };
    if (id) {
        data.status = form.status.value;
        data.feedback = form.feedback.value.trim() || null;
    }
    try {
        if (id) { await apiPut(`/sitevisits/${id}`, data); showToast('Site visit updated!'); }
        else { await apiPost('/sitevisits', data); showToast('Site visit scheduled!'); }
        closeModal(); loadSiteVisits();
    } catch (err) { showToast('Error: ' + err.message, true); }
}

async function updateSiteVisitStatus(id, newStatus) {
    try {
        const v = await apiGet(`/sitevisits/${id}`);
        const data = {
            contactId: v.contactId,
            propertyId: v.propertyId,
            scheduledDate: v.scheduledDate,
            status: newStatus,
            pickupLocation: v.pickupLocation,
            feedback: v.feedback,
            notes: v.notes
        };
        await apiPut(`/sitevisits/${id}`, data);
        showToast(`Visit marked as ${newStatus}!`);
        loadSiteVisits();
    } catch (err) { showToast('Error: ' + err.message, true); }
}

async function deleteSiteVisit(id) {
    if (!confirm('Delete this site visit?')) return;
    try { await apiDelete(`/sitevisits/${id}`); showToast('Site visit deleted'); loadSiteVisits(); }
    catch (err) { showToast('Error: ' + err.message, true); }
}
