const c = document.getElementById('chart');
const x = c.getContext('2d');
let buf = [];
let seeded = false;

c.width = 800; c.height = 480;

function send(a) { window.chrome.webview.postMessage({ action: a }); }
function showResult(t) { document.getElementById('result').textContent = t; }
function setNews(list) {
    const box = document.getElementById('news-list');
    box.textContent = '';
    list.forEach(t => {
        const p = document.createElement('p');
        p.className = 'roboto-mono';
        p.textContent = t;
        box.appendChild(p);
    });
}
function setPrice(v) { document.getElementById('jjcoin-price').textContent = Number(v).toFixed(2); }
function setPortfolio(cash, jjcoin, jjcoinAmount) {
    document.getElementById('cash').textContent = cash;
    document.getElementById('jjcoin').textContent = jjcoin;
    document.getElementById('jjcoin-amount').textContent = jjcoinAmount;
}

function drawData(p) {
    if (p.length) {
        if (!seeded) {
            buf = p.slice(-60);
            seeded = true;
        } else {
            buf.push(p[p.length - 1]);
            if (buf.length > 60) buf.shift();
        }
        render();
    }
}

function render() {
    x.clearRect(0, 0, c.width, c.height);
    const n = buf.length;
    if (n < 2) return;

    const MIN = 80, MAX = 120;
    const px = i => i * (c.width * .75) / (n - 1);
    const py = v => c.height - (v - MIN) / (MAX - MIN) * c.height;

    x.strokeStyle = 'black';
    x.lineWidth = 3;
    x.beginPath();
    for (let i = 0; i < n; i++) {
        const X = px(i), Y = py(buf[i]);
        i ? x.lineTo(X, Y) : x.moveTo(X, Y);
    }
    x.stroke();
}