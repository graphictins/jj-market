function send(action) {
    window.chrome.webview.postMessage({ action: action });
}

function showResult(text) {
    document.getElementById('result').textContent = text;
}

/* ===== JJCOIN GRAPH ===== */
var jjcoinBars = [];
var jjcoinChart = null;

function pushJJCoinBar(open, high, low, close) {
    if (jjcoinChart === null) {
        jjcoinChart = initJJCoinChart();
    }
    jjcoinBars.push({ open: open, high: high, low: low, close: close });
    if (jjcoinBars.length > 300) jjcoinBars.shift();
    jjcoinChart.draw();
}

function initJJCoinChart() {
    var canvas = document.getElementById('jjcoin-canvas');
    if (!canvas) return null;
    var ctx = canvas.getContext('2d');

    function resize() {
        var rect = canvas.getBoundingClientRect();
        var dpr = window.devicePixelRatio || 1;
        canvas.width = Math.round(rect.width * dpr);
        canvas.height = Math.round(rect.height * dpr);
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }
    window.addEventListener('resize', function () {
        resize();
        draw();
    });

    function draw() {
        resize();
        var w = canvas.clientWidth;
        var h = canvas.clientHeight;
        ctx.clearRect(0, 0, w, h);

        if (jjcoinBars.length === 0) {
            drawEmpty(w, h);
            return;
        }

        var padding = { top: 12, right: 10, bottom: 22, left: 46 };
        var pw = w - padding.left - padding.right;
        var ph = h - padding.top - padding.bottom;

        var min = Infinity, max = -Infinity;
        jjcoinBars.forEach(function (b) {
            if (b.low < min) min = b.low;
            if (b.high > max) max = b.high;
        });
        var range = (max - min) || 1;
        max += range * 0.08;
        min -= range * 0.08;
        range = max - min;

        var n = jjcoinBars.length;
        var step = pw / n;
        var candleW = Math.max(2, step * 0.6);

        function yFor(v) { return padding.top + ph - ((v - min) / range) * ph; }
        function xFor(i) { return padding.left + i * step + step / 2; }

        drawGrid(ctx, w, h, padding, pw, ph, min, max, range);
        drawLastLine(ctx, w, padding, h);
        drawCandles(ctx, xFor, yFor, candleW);
        drawAxisLabels(ctx, w, h, padding, min, max, xFor, n);
    }

    function drawEmpty(w, h) {
        ctx.fillStyle = 'rgba(60,60,60,0.4)';
        ctx.font = '13px Roboto Mono, monospace';
        ctx.textAlign = 'center';
        ctx.fillText('waiting for JJCoin data...', w / 2, h / 2);
    }

    function drawGrid(ctx, w, h, padding, pw, ph, min, max, range) {
        ctx.strokeStyle = 'rgba(60,60,60,0.18)';
        ctx.lineWidth = 1;
        ctx.font = '11px Roboto Mono, monospace';
        ctx.textAlign = 'right';
        var lines = 6;
        for (var i = 0; i <= lines; i++) {
            var v = min + (range * i) / lines;
            var y = padding.top + ph - (ph * i) / lines;
            ctx.beginPath();
            ctx.moveTo(padding.left, y);
            ctx.lineTo(padding.left + pw, y);
            ctx.stroke();
            ctx.fillStyle = 'rgba(60,60,60,0.7)';
            ctx.fillText(v.toFixed(2), padding.left - 6, y + 4);
        }
    }

    function drawLastLine(ctx, w, padding, h) {
        var last = jjcoinBars[jjcoinBars.length - 1];
        if (!last) return;
        ctx.strokeStyle = last.close >= last.open ? 'rgba(0,180,0,0.5)' : 'rgba(220,40,40,0.5)';
        ctx.setLineDash([4, 4]);
        ctx.beginPath();
        ctx.moveTo(padding.left, h - padding.bottom);
        ctx.lineTo(w - padding.right, h - padding.bottom);
        ctx.stroke();
        ctx.setLineDash([]);
    }

    function drawCandles(ctx, xFor, yFor, candleW) {
        jjcoinBars.forEach(function (b, i) {
            var x = xFor(i);
            var up = b.close >= b.open;
            var color = up ? 'rgba(34,180,34,0.95)' : 'rgba(220,40,40,0.95)';
            ctx.strokeStyle = color;
            ctx.fillStyle = color;
            ctx.lineWidth = 1;

            ctx.beginPath();
            ctx.moveTo(x, yFor(b.high));
            ctx.lineTo(x, yFor(b.low));
            ctx.stroke();

            var top = yFor(Math.max(b.open, b.close));
            var bottom = yFor(Math.min(b.open, b.close));
            var bodyH = Math.max(1, bottom - top);
            ctx.fillRect(x - candleW / 2, top, candleW, bodyH);
        });
    }

    function drawAxisLabels(ctx, w, h, padding, min, max, xFor, n) {
        ctx.fillStyle = 'rgba(60,60,60,0.7)';
        ctx.textAlign = 'center';
        ctx.font = '11px Roboto Mono, monospace';
        var labels = 6;
        if (n > 1) {
            for (var j = 0; j < labels; j++) {
                var idx = Math.round((j * (n - 1)) / (labels - 1));
                ctx.fillText((idx + 1).toString(), xFor(idx), h - 6);
            }
        }
    }

    resize();
    draw();
    return { draw: draw };
}

initJJCoinChart();
/* ===== /JJCOIN GRAPH ===== */