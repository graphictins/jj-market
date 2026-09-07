const c = document.getElementById('chart');
const x = c.getContext('2d');
let last = [];

function send(a) { window.chrome.webview.postMessage({ action: a }); }
function showResult(t) { document.getElementById('result').textContent = t; }

function drawData(p) {
    last = p;
    c.width = c.clientWidth; c.height = c.clientHeight;
    x.clearRect(0, 0, c.width, c.height);
    x.beginPath();
    for (let i = 0; i < p.length; i++) {
        const px = i * (c.width * .75) / (p.length - 1);
        const py = c.height - (p[i] - 80) / 40 * c.height;
        i ? x.lineTo(px, py) : x.moveTo(px, py);
    }
    x.stroke();
}

addEventListener('resize', () => drawData(last));