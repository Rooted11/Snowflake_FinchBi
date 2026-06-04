/*
 * Sypher BI — data-source widget
 * Standalone module: polls the API's /health endpoint and renders a live badge
 * showing the active warehouse (Neon / Snowflake) and round-trip latency.
 *
 * Wire-up (already done in the dashboard):
 *   <span id="sourceBadge" class="pill"></span>
 *   <script>window.SYPHER_API = API;</script>   // API base, e.g. https://…/api/Analytics
 *   <script src="assets/sypher-source.js" defer></script>
 */
(function () {
  "use strict";

  var API =
    window.SYPHER_API ||
    "https://sypher-bi-backend.onrender.com/api/Analytics";
  var POLL_MS = 30000;

  var COLORS = { Snowflake: "#29B5E8", Neon: "#1D9E75", offline: "#A32D2D" };

  function el() {
    return document.getElementById("sourceBadge");
  }

  function render(provider, ms, ok) {
    var badge = el();
    if (!badge) return;
    var color = ok ? COLORS[provider] || COLORS.Neon : COLORS.offline;
    var label = ok ? provider + " · " + ms + "ms" : "offline";
    badge.innerHTML =
      '<span style="display:inline-block;width:7px;height:7px;border-radius:50%;' +
      "background:" + color + ";margin-right:6px;" +
      (ok ? "box-shadow:0 0 0 0 " + color + ";animation:livePulse 1.8s infinite;" : "") +
      '"></span>' + label;
    badge.title = ok
      ? "Serving analytics from " + provider
      : "API unreachable";
  }

  function poll() {
    var t0 = performance.now();
    fetch(API + "/health", { cache: "no-store" })
      .then(function (r) { return r.ok ? r.json() : Promise.reject(r.status); })
      .then(function (h) {
        var ms = Math.round(performance.now() - t0);
        render(h.provider || "Neon", ms, true);
      })
      .catch(function () { render(null, 0, false); });
  }

  function start() {
    if (!el()) return;
    poll();
    setInterval(poll, POLL_MS);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", start);
  } else {
    start();
  }
})();
