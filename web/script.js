function send(action) {
    window.chrome.webview.postMessage({ action: action });
}

function showResult(text) {
    document.getElementById('result').textContent = text;
}