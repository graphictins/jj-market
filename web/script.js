/* ==========================================================
   JJMarket terminal — client logic
   Talks to VB.NET via window.chrome.webview.postMessage
   ========================================================== */

const send = (o) => window.chrome.webview.postMessage(o);

window.__errs = [];
window.addEventListener('error', (e) => window.__errs.push(String(e.message || e.error) + ' @' + (e.lineno || '?') + ':' + (e.colno || '?')));
window.addEventListener('unhandledrejection', (e) => window.__errs.push('promise: ' + String(e.reason)));

const $ = (id) => document.getElementById(id);
const fmt = (n, d = 2) => n == null || isNaN(n) ? '–' : Number(n).toLocaleString('en-US', { minimumFractionDigits: d, maximumFractionDigits: d });
const cmp = (n) => isNaN(n) ? '' : (n >= 0 ? 'plus' : 'minus');

/* ----------------------------------------------------------
   State
   ---------------------------------------------------------- */
const state = {
    price: null,
    first: null,
    high: 0,
    low: Infinity,
    volume: 0,
    cash: 0,
    holding: 0,
    holdingValue: 0,
    avgCost: 0,
    trades: [],
    orders: [],
    history: [],      // price stream
    tf: 0,            // timeframe index
    ct: 'candle',     // chart type
    otype: 'market',  // order type
    resultTimer: null,
};

/* ==========================================================
   Chart
   ========================================================== */
const BAR_TICKS = [5, 15, 30, 60];

let chart = null, candleSeries = null, lineSeries = null, areaSeries = null;

function initChart() {
    const host = $('chart');
    const opts = {
        autoSize: true,
        layout: {
            background: { type: LightweightCharts.ColorType.Solid, color: '#131722' },
            textColor: '#8b93a7',
            fontFamily: '"Roboto Mono", Consolas, monospace',
            fontSize: 11,
        },
        grid: {
            vertLines: { color: '#1d2433' },
            horzLines: { color: '#1d2433' },
        },
        rightPriceScale: { borderColor: '#2a3242' },
        timeScale: { borderColor: '#2a3242', timeVisible: true, secondsVisible: true, rightOffset: 4, barSpacing: 9, minBarSpacing: 2 },
        crosshair: {
            vertLine: { color: '#3a4356', style: LightweightCharts.LineStyle.Dotted },
            horzLine: { color: '#3a4356', style: LightweightCharts.LineStyle.Dotted },
        },
    };

    chart = LightweightCharts.createChart(host, opts);

    candleSeries = chart.addSeries(LightweightCharts.CandlestickSeries, {
        upColor: '#26a69a', downColor: '#ef5350',
        wickUpColor: '#26a69a', wickDownColor: '#ef5350',
        borderVisible: false,
        priceLineVisible: false,
    });

    lineSeries = chart.addSeries(LightweightCharts.LineSeries, {
        color: '#3b82f6', lineWidth: 2, priceLineVisible: false,
    });

    areaSeries = chart.addSeries(LightweightCharts.AreaSeries, {
        lineColor: '#3b82f6', topColor: 'rgba(59,130,246,0.28)', bottomColor: 'rgba(59,130,246,0.02)',
        lineWidth: 2, priceLineVisible: false,
    });
}

function buildCandles() {
    const n = state.history.length;
    if (n < 2) return [];
    const ticks = BAR_TICKS[state.tf];
    const out = [];
    let open = state.history[0], high = state.history[0], low = state.history[0], close = state.history[0];
    let bucket = 1;

    for (let i = 1; i < n; i++) {
        const v = state.history[i];
        high = Math.max(high, v); low = Math.min(low, v); close = v;
        bucket++;
        if (bucket >= ticks || i === n - 1) {
            out.push({ open, high, low, close });
            open = high = low = close = v; bucket = 1;
        }
    }

    const step = ticks * 0.5;
    const start = Math.floor(Date.now() / 1000) - out.length * step;
    out.forEach((c, i) => { c.time = start + i * step; });
    return out;
}

function redrawChart(initial) {
    if (!chart) return;
    const candles = buildCandles();

    candleSeries.applyOptions({ visible: state.ct === 'candle' });
    lineSeries.applyOptions({ visible: state.ct === 'line' });
    areaSeries.applyOptions({ visible: state.ct === 'area' });

    if (state.ct === 'candle') {
        candleSeries.setData(candles.map(c => ({ time: c.time, open: c.open, high: c.high, low: c.low, close: c.close })));
    } else {
        const lineData = candles.map(c => ({ time: c.time, value: c.close }));
        lineSeries.setData(lineData);
        areaSeries.setData(lineData);
    }

    try {
        const n = candles.length;
        if (n > 0) {
            const bs = chart.timeScale().options().barSpacing || 9;
            const rightOff = chart.timeScale().options().rightOffset || 4;
            const visible = Math.max(1, Math.round(chart.timeScale().width() / bs) - rightOff);
            chart.timeScale().setVisibleLogicalRange({ from: Math.max(0, n - visible), to: n - 1 + rightOff });
        }
    } catch (_) { }
}

/* ==========================================================
   Order book (synthesized around current price)
   ========================================================== */
const BOOK_ROWS = 9;

function tickSize(p) { return Math.max(0.01, Math.round(p * 0.001 * 100) / 100); }

function pseudoQty(i, now) { return (30 + (i * 7919) % 70) * (0.82 + 0.35 * Math.abs(Math.sin(now / 23000 + i * 1.7))); }

function renderBook() {
    const p = state.price;
    if (p == null) return;
    const tick = tickSize(p);
    const now = Date.now();
    const asks = [], bids = [], cumB = [], cumA = [];
    let cumAval = 0, cumBval = 0;

    for (let i = 0; i < BOOK_ROWS; i++) {
        const aQty = pseudoQty(i, now), bQty = pseudoQty(i + BOOK_ROWS, now);
        asks.push({ price: p + (i + 1) * tick, qty: aQty, total: (p + (i + 1) * tick) * aQty });
        bids.push({ price: p - (i + 1) * tick, qty: bQty, total: (p - (i + 1) * tick) * bQty });
    }
    for (let i = 0; i < BOOK_ROWS; i++) {
        cumAval += asks[i].qty; cumA.push(cumAval);
        cumBval += bids[i].qty; cumB.push(cumBval);
    }
    const maxCum = Math.max(cumAval, cumBval);

    const row = (side, r, cum, i) => {
        const el = document.createElement('div');
        el.className = 'depth-row ' + side;
        el.innerHTML =
            `<span class="dep" style="width:${(cum / maxCum) * 100}%"></span>` +
            `<span class="price-c">${fmt(r.price)}</span>` +
            `<span class="qty-d">${fmt(r.qty, 0)}</span>` +
            `<span class="tot-d">${fmt(r.total, 0)}</span>`;
        return el;
    };

    const asksBox = $('book-asks');
    const bidsBox = $('book-bids');
    asksBox.textContent = '';
    bidsBox.textContent = '';
    for (i = BOOK_ROWS - 1; i >= 0; i--) asksBox.appendChild(row('ask', asks[i], cumA[i], i));
    for (i = 0; i < BOOK_ROWS; i++) bidsBox.appendChild(row('bid', bids[i], cumB[i], i));

    const spread = asks[0].price - bids[0].price;
    $('book-spread').textContent = 'spread ' + fmt(spread);

    const mid = $('book-mid');
    mid.textContent = '';
    const mp = document.createElement('span');
    mp.className = 'mid-price';
    mp.style.color = spread >= 0 ? 'var(--up)' : 'var(--down)';
    mp.textContent = fmt(p);
    const mq = document.createElement('span');
    mq.style.color = 'var(--text-dim)';
    mq.style.fontSize = '11px';
    mq.textContent = 'JJCOIN';
    mid.appendChild(mp); mid.appendChild(mq);
}

/* ==========================================================
   Ticker / stats
   ========================================================== */
function renderStats() {
    const p = state.price;
    if (p == null) { $('ticker-price').textContent = '–'; return; }

    state.high = Math.max(state.high, p);
    state.low = Math.min(state.low, p);

    const changePct = state.first ? (p - state.first) / state.first * 100 : 0;
    const pos = changePct >= 0;

    $('ticker-price').textContent = fmt(p);
    const ch = $('ticker-change');
    ch.textContent = (pos ? '+' : '') + fmt(changePct, 2) + '%';
    ch.classList.toggle('down', !pos);

    $('stat-high').textContent = fmt(state.high);
    $('stat-low').textContent = fmt(state.low);
    $('stat-vol').textContent = state.volume > 0 ? fmt(state.volume, 0) : '–';
    $('stat-range').textContent = fmt(state.high - state.low);

    $('trade-price').textContent = fmt(p);
    $('live-dot').style.cssText = '';
}

/* ==========================================================
   Order entry UI
   ========================================================== */
function effPrice() { return state.otype === 'limit' ? parseFloat($('limit-price').value) : state.price; }

function updTradeTotals() {
    const qty = parseFloat($('qty').value) || 0;
    const pr = effPrice();
    const tot = qty * pr;
    $('trade-total').textContent = tot && !isNaN(tot) ? fmt(tot) : '0.00';
    $('trade-total-lev').textContent = tot && !isNaN(tot) ? fmt(tot / 10) : '0.00';
}

function placeOrder(isBuy) {
    const qty = parseFloat($('qty').value);
    if (!qty || qty <= 0) { showResult('ERR: Enter a quantity greater than 0.'); return; }

    if (state.otype === 'limit') {
        const lim = parseFloat($('limit-price').value);
        if (!lim || lim <= 0) { showResult('ERR: Enter a valid limit price.'); return; }
        send(isBuy ? { action: 'buy', qty, limit: lim } : { action: 'sell', qty, limit: lim });
        showResult('OK: Limit order placed @ ' + fmt(lim));
    } else {
        send(isBuy ? { action: 'buy', qty } : { action: 'sell', qty });
        showResult('OK: ' + (isBuy ? 'Buy' : 'Sell') + ' order sent');
    }
    updTradeTotals();
}

function showResult(t) {
    const el = $('result');
    clearTimeout(state.resultTimer);
    if (t.startsWith('ERR')) el.className = 'result-line mon err';
    else if (t.startsWith('OK')) el.className = 'result-line mon ok';
    else el.className = 'result-line mon';
    el.textContent = t;
    if (t.startsWith('OK') || t.startsWith('ERR')) state.resultTimer = setTimeout(() => { el.textContent = ''; }, 4000);
}

/* ==========================================================
   Portfolio / history / orders
   ========================================================== */
function renderPortfolio() {
    const equity = state.cash + state.holdingValue;
    $('pf-equity').textContent = fmt(equity);
    $('pf-cash').textContent = fmt(state.cash);
    $('pf-holding').textContent = fmt(state.holding, 4);
    $('pf-holding-worth').textContent = fmt(state.holdingValue);

    const avg = state.avgCost || 0;
    $('pf-avgcost').textContent = fmt(avg);
    $('avail-qty').textContent = fmt(state.holding, 4);
    $('avail-cash').textContent = fmt(state.cash);

    let pnl = 0;
    if (avg > 0 && state.price != null) pnl = (state.price - avg) * state.holding;
    const pnlEl = $('pf-pnl');
    pnlEl.textContent = (pnl >= 0 ? '+' : '') + fmt(pnl);
    pnlEl.className = 'pf-value mon ' + cmp(pnl);
}

function renderHistory() {
    const box = $('history-list');
    box.textContent = '';
    if (!state.trades.length) { box.innerHTML = '<div class="empty-row">No trades yet</div>'; return; }

    // average cost basis from buys
    let buyQty = 0, buyCost = 0;
    for (const t of state.trades) if (t.side === 'buy') { buyQty += t.qty; buyCost += t.total; }
    state.avgCost = buyQty > 0 ? buyCost / buyQty : 0;

    const frag = document.createDocumentFragment();
    state.trades.slice().reverse().slice(0, 40).forEach(t => {
        const row = document.createElement('div');
        row.className = 'hx-row';
        const side = t.side === 'buy' ? 'BUY' : 'SELL';
        row.innerHTML =
            `<span class="hx-type ${t.side}">${side}</span>` +
            `<span class="hx-main">
                <span class="hx-line1">${fmt(t.qty, 4)} @ ${fmt(t.price)}</span>
                <span class="hx-line2">${t.time}</span>
             </span>` +
            `<span class="hx-total"><b>${fmt(t.total)}</b></span>`;
        frag.appendChild(row);
    });
    box.appendChild(frag);
}

function renderOrders() {
    const box = $('orders-list');
    box.textContent = '';
    if (!state.orders.length) { box.innerHTML = '<div class="empty-row">No open orders</div>'; return; }
    const frag = document.createDocumentFragment();
    state.orders.forEach(o => {
        const row = document.createElement('div');
        row.className = 'ord-row';
        row.innerHTML =
            `<span class="hx-type ${o.side}">${o.side === 'buy' ? 'BUY' : 'SELL'}</span>` +
            `<span class="hx-main">
                <span class="hx-line1">${fmt(o.qty, 4)} @ ${fmt(o.limit)}</span>
                <span class="hx-line2">limit · pending</span>
             </span>` +
            `<button class="ord-cancel" title="Cancel">✕</button>`;
        row.querySelector('.ord-cancel').addEventListener('click', () => send({ action: 'cancel', id: o.id }));
        frag.appendChild(row);
    });
    box.appendChild(frag);
}

/* ==========================================================
   News
   ========================================================== */
function renderNews(list) {
    const track = $('news-track');
    track.textContent = '';
    if (!list.length) return;

    const inner = document.createElement('div');
    inner.className = 'news-inner';
    const frag = document.createDocumentFragment();
    const double = [...list, ...list];
    double.forEach(t => {
        const s = document.createElement('span');
        s.className = 'news-item ' + (/\b(buy|win|upgrade|backs|lists|rise|surge)\b/i.test(t) ? 'good' : /\b(investig|attack|seized|dump|postponed|hacker|fall|drop)\b/i.test(t) ? 'bad' : '');
        s.textContent = t + '  ·  ';
        frag.appendChild(s);
    });
    inner.appendChild(frag);
    track.appendChild(inner);
}

/* ==========================================================
   VB-facing API
   ========================================================== */
const HISTORY_MAX = 13200;

function onPrice(v) {
    state.price = v;
    redrawChart();
    renderBook();
    renderStats();
    renderPortfolio();
    updTradeTotals();
}

/* Full-history snapshot (sent once on bootstrap / seed) */
function drawData(ps) {
    if (!ps || !ps.length) return;
    state.history = ps.slice();
    if (state.history.length > HISTORY_MAX) state.history.splice(0, state.history.length - HISTORY_MAX);
    if (state.first == null) state.first = state.history[0];
    state.high = Math.max(...state.history);
    state.low = Math.min(...state.history);
    state.volume = 0;
    onPrice(ps[ps.length - 1]);
}

/* Incremental: one new price per tick */
function appendPrice(v) {
    if (state.history.length >= HISTORY_MAX) state.history.shift();
    state.history.push(v);
    state.volume += Math.abs(v - state.history[state.history.length - 2]);
    if (state.first == null) state.first = v;
    onPrice(v);
}

function setPrice(v) {
    state.price = Number(v);
    renderStats();
    renderBook();
    renderPortfolio();
    updTradeTotals();
}

function setPortfolio(cash, val, qty) {
    state.cash = parseFloat(String(cash).replace(/,/g, '')) || 0;
    state.holdingValue = parseFloat(String(val).replace(/,/g, '')) || 0;
    state.holding = parseFloat(String(qty).replace(/,/g, '')) || 0;
    renderPortfolio();
}

function setNews(list) {
    if (Array.isArray(list)) renderNews(list);
}

function setTrades(list) {
    if (!Array.isArray(list)) return;
    state.trades = list;
    renderHistory();
    renderPortfolio();
}

function setOrders(list) {
    if (!Array.isArray(list)) return;
    state.orders = list;
    renderOrders();
}

/* ==========================================================
   Bindings & init
   ========================================================== */
document.addEventListener('DOMContentLoaded', () => {
    initChart();

    document.querySelectorAll('.tf-btn').forEach(b => b.addEventListener('click', () => {
        document.querySelectorAll('.tf-btn').forEach(x => x.classList.remove('active'));
        b.classList.add('active');
        state.tf = parseInt(b.dataset.tf, 10);
        redrawChart(true);
    }));

    document.querySelectorAll('.ct-btn').forEach(b => b.addEventListener('click', () => {
        document.querySelectorAll('.ct-btn').forEach(x => x.classList.remove('active'));
        b.classList.add('active');
        state.ct = b.dataset.ct;
        redrawChart(false);
    }));

    document.querySelectorAll('.otype-btn').forEach(b => b.addEventListener('click', () => {
        document.querySelectorAll('.otype-btn').forEach(x => x.classList.remove('active'));
        b.classList.add('active');
        state.otype = b.dataset.otype;
        document.querySelector('.limit-row').classList.toggle('hidden', state.otype !== 'limit');
        updTradeTotals();
    }));

    $('btn-buy').addEventListener('click', () => placeOrder(true));
    $('btn-sell').addEventListener('click', () => placeOrder(false));
    $('qty').addEventListener('input', updTradeTotals);
    $('limit-price').addEventListener('input', updTradeTotals);

    updTradeTotals();

    send({ action: 'bootstrap' });
});