"use strict";

(function(){
    const container = document.getElementById('players-list');
    if (!container) return;

    const paginationEl = document.getElementById('players-pagination');
    const searchEl = document.getElementById('players-search');
    const pageSizeEl = document.getElementById('players-pageSize');

    let allPlayers = [];
    let filtered = [];
    let currentPage = 1;
    let pageSize = parseInt(pageSizeEl?.value || '10', 10);

    // connection selection: use global playersPresenceConnection if present, else build local connection
    let connection = window.playersPresenceConnection;
    let localStarted = false;

    function escapeHtml(s){
        return s ? s.replace(/[&<>"]+/g, function(m){return ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'})[m];}) : '';
    }

    function paginate(array, page, size){
        const start = (page - 1) * size;
        return array.slice(start, start + size);
    }

    function render(list){
        container.innerHTML = '';
        if (!list || list.length === 0){
            container.innerHTML = '<div class="text-muted">Aucun joueur trouvé.</div>';
            return;
        }

        list.forEach(p => {
            const row = document.createElement('div');
            row.className = 'd-flex align-items-center justify-content-between p-2 border-bottom';
            const left = document.createElement('div');
            left.innerHTML = `<strong>${escapeHtml(p.displayName || p.displayName)}</strong>`;
            const right = document.createElement('div');
            const badge = document.createElement('span');
            badge.className = 'badge ' + (p.isOnline ? 'bg-success' : 'bg-secondary');
            badge.textContent = p.isOnline ? 'Online' : 'Offline';
            right.appendChild(badge);
            if (p.status) {
                const s = document.createElement('small'); s.className='text-muted ms-2'; s.textContent = p.status; right.appendChild(s);
            }
            row.appendChild(left);
            row.appendChild(right);
            container.appendChild(row);
        });
    }

    function updatePaginationFromTotal(total){
        if (!paginationEl) return;
        paginationEl.innerHTML = '';
        const pages = Math.max(1, Math.ceil(total / pageSize));

        function mkLi(label, page, disabled){
            const li = document.createElement('li'); li.className = 'page-item' + (disabled ? ' disabled' : '');
            const a = document.createElement('a'); a.className='page-link'; a.href='#'; a.textContent = label;
            a.addEventListener('click', (e)=>{ e.preventDefault(); if (!disabled) { currentPage = page; fetchPage(); } });
            li.appendChild(a); return li;
        }

        paginationEl.appendChild(mkLi('«', 1, currentPage === 1));
        paginationEl.appendChild(mkLi('‹', Math.max(1, currentPage - 1), currentPage === 1));

        const windowSize = 5;
        const start = Math.max(1, currentPage - Math.floor(windowSize/2));
        const end = Math.min(pages, start + windowSize - 1);
        for (let p = start; p <= end; p++){
            const li = document.createElement('li'); li.className = 'page-item' + (p === currentPage ? ' active' : '');
            const a = document.createElement('a'); a.className='page-link'; a.href='#'; a.textContent = String(p);
            a.addEventListener('click', (e)=>{ e.preventDefault(); currentPage = p; fetchPage(); });
            li.appendChild(a); paginationEl.appendChild(li);
        }

        paginationEl.appendChild(mkLi('›', Math.min(pages, currentPage + 1), currentPage === pages));
        paginationEl.appendChild(mkLi('»', pages, currentPage === pages));
    }

    async function ensureConnection(){
        // if there's a global promise, await it and use the global connection
        if (window.playersPresenceConnectionPromise) {
            try{
                connection = await window.playersPresenceConnectionPromise;
            }catch(e){
                console.warn('[players-presence] global connection promise rejected', e);
                // fall back to local connection
                connection = null;
            }
        }

        if (!connection){
            connection = new signalR.HubConnectionBuilder().withUrl('/hubs/players').withAutomaticReconnect().build();
            connection.on('PlayersUpdated', () => fetchPage());
            try{
                await connection.start();
                localStarted = true;
                console.debug('[players-presence] local connection started');
            }catch(e){
                console.error('[players-presence] failed to start local connection', e);
            }
        } else {
            // subscribe to event on existing connection
            connection.on('PlayersUpdated', () => fetchPage());
        }

        // initial fetch
        fetchPage();
    }

    // debounce search
    let debounceTimer = 0;
    function onSearchChange(){
        window.clearTimeout(debounceTimer);
        debounceTimer = window.setTimeout(()=>{ currentPage = 1; fetchPage(); }, 250);
    }

    // hookup controls
    searchEl?.addEventListener('input', onSearchChange);
    pageSizeEl?.addEventListener('change', ()=>{ currentPage = 1; fetchPage(); });

    // ensure signalR connection (global or local) and start behavior
    ensureConnection();

    async function fetchPage(){
        const search = (searchEl?.value || '').trim();
        pageSize = parseInt(pageSizeEl?.value || '10', 10);
        const url = `/Players/GetPlayers?page=${currentPage}&pageSize=${pageSize}&search=${encodeURIComponent(search)}`;
        try{
            console.debug('[players-presence] fetchPage url=', url);
            const res = await fetch(url);
            if (!res.ok) {
                console.error('[players-presence] fetch failed', res.status, res.statusText);
                container.innerHTML = `<div class="text-danger">Erreur lors du chargement des joueurs (${res.status})</div>`;
                return;
            }
            const json = await res.json();
            console.debug('[players-presence] fetch result total=', json.total, 'items=', (json.items || []).length);
            filtered = json.items || [];
            // total used for pagination
            const total = json.total || 0;
            // render current page items (server already paged)
            render(filtered);
            // update pagination UI based on total
            updatePaginationFromTotal(total);
        }catch(err){
            console.error('[players-presence] fetch error', err);
            container.innerHTML = `<div class="text-danger">Erreur réseau lors du chargement des joueurs</div>`;
        }
    }

})();
