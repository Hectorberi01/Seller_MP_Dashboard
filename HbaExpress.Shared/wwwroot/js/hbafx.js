/* ============================================================
   hbafx — petite couche « fun » du dashboard HbaExpress Pro.
   Autonome (aucune dépendance CDN). Expose window.hbafx :
     - hbafx.toast(message, type)      → notification flottante
     - hbafx.celebrate(options)        → pluie de confettis
     - hbafx.countUp(el|selector)      → compteur animé
   + auto : compteurs [data-countup] et apparitions au défilement.
   Respecte prefers-reduced-motion.
   ============================================================ */
(function () {
  "use strict";

  var REDUCED = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  var easeOut = function (t) { return 1 - Math.pow(1 - t, 3); };

  // ---------------------------------------------------------
  //  Toasts
  // ---------------------------------------------------------
  function toastContainer() {
    var c = document.getElementById("hbafx-toasts");
    if (!c) {
      c = document.createElement("div");
      c.id = "hbafx-toasts";
      c.className = "fx-toasts";
      document.body.appendChild(c);
    }
    return c;
  }

  var TOAST_ICONS = {
    success: "M20 6 9 17l-5-5",
    error: "M18 6 6 18M6 6l12 12",
    info: "M12 8h.01M11 12h1v4h1"
  };

  function toast(message, type) {
    if (!message) return;
    type = type || "info";
    var el = document.createElement("div");
    el.className = "fx-toast fx-toast-" + type;
    el.setAttribute("role", "status");
    el.innerHTML =
      '<span class="fx-toast-ico" aria-hidden="true"><svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="' +
      (TOAST_ICONS[type] || TOAST_ICONS.info) +
      '"/></svg></span><span class="fx-toast-msg"></span>';
    el.querySelector(".fx-toast-msg").textContent = message;
    toastContainer().appendChild(el);
    requestAnimationFrame(function () { el.classList.add("fx-in"); });
    var remove = function () {
      el.classList.remove("fx-in");
      el.classList.add("fx-out");
      setTimeout(function () { el.remove(); }, 250);
    };
    setTimeout(remove, 3600);
    el.addEventListener("click", remove);
  }

  // ---------------------------------------------------------
  //  Confettis (canvas éphémère, sans lib)
  // ---------------------------------------------------------
  function celebrate(options) {
    if (REDUCED) return;
    options = options || {};
    var colors = options.colors || ["#1F8A4C", "#34B36A", "#EF9F27", "#639922", "#185FA5"];
    var count = options.count || 120;

    var canvas = document.createElement("canvas");
    canvas.className = "fx-confetti";
    document.body.appendChild(canvas);
    var ctx = canvas.getContext("2d");
    var dpr = window.devicePixelRatio || 1;
    function size() {
      canvas.width = window.innerWidth * dpr;
      canvas.height = window.innerHeight * dpr;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }
    size();

    var W = window.innerWidth, H = window.innerHeight;
    var originX = options.x != null ? options.x : W / 2;
    var originY = options.y != null ? options.y : H * 0.35;
    var parts = [];
    for (var i = 0; i < count; i++) {
      var angle = Math.random() * Math.PI * 2;
      var speed = 4 + Math.random() * 7;
      parts.push({
        x: originX, y: originY,
        vx: Math.cos(angle) * speed,
        vy: Math.sin(angle) * speed - 4,
        g: 0.18 + Math.random() * 0.12,
        size: 5 + Math.random() * 6,
        rot: Math.random() * Math.PI,
        vr: (Math.random() - 0.5) * 0.3,
        color: colors[(Math.random() * colors.length) | 0],
        life: 0, ttl: 90 + Math.random() * 40
      });
    }

    var frame = 0;
    function tick() {
      ctx.clearRect(0, 0, W, H);
      var alive = false;
      for (var j = 0; j < parts.length; j++) {
        var p = parts[j];
        if (p.life > p.ttl) continue;
        alive = true;
        p.life++;
        p.vy += p.g;
        p.x += p.vx;
        p.y += p.vy;
        p.rot += p.vr;
        ctx.save();
        ctx.globalAlpha = Math.max(0, 1 - p.life / p.ttl);
        ctx.translate(p.x, p.y);
        ctx.rotate(p.rot);
        ctx.fillStyle = p.color;
        ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 0.6);
        ctx.restore();
      }
      frame++;
      if (alive && frame < 200) {
        requestAnimationFrame(tick);
      } else {
        canvas.remove();
      }
    }
    requestAnimationFrame(tick);
  }

  // ---------------------------------------------------------
  //  Compteurs animés
  // ---------------------------------------------------------
  var nf = new Intl.NumberFormat("fr-FR");
  function splitNumber(text) {
    var m = String(text).match(/^(\D*?)([\d\s.,]+)(\D*)$/);
    if (!m) return null;
    var core = m[2].replace(/\s/g, "").replace(/,/g, ".");
    var val = parseFloat(core);
    if (isNaN(val)) return null;
    var decimals = core.indexOf(".") >= 0 ? (core.split(".")[1] || "").length : 0;
    return { prefix: m[1], suffix: m[3], value: val, decimals: decimals };
  }
  function fmt(v, decimals) {
    if (decimals > 0) return v.toFixed(decimals).replace(".", ",");
    return nf.format(Math.round(v));
  }
  function countUp(target) {
    var el = typeof target === "string" ? document.querySelector(target) : target;
    if (!el || el.dataset.fxCounted === "1") return;
    var parts = splitNumber(el.textContent);
    if (!parts) { el.dataset.fxCounted = "1"; return; }
    el.dataset.fxCounted = "1";
    var original = el.textContent;
    if (REDUCED || parts.value === 0) return;
    var dur = 900, start = null;
    function step(ts) {
      if (start === null) start = ts;
      var t = Math.min(1, (ts - start) / dur);
      var cur = parts.value * easeOut(t);
      el.textContent = parts.prefix + fmt(cur, parts.decimals) + parts.suffix;
      if (t < 1) requestAnimationFrame(step);
      else el.textContent = original;
    }
    requestAnimationFrame(step);
  }

  // ---------------------------------------------------------
  //  Observers : apparition au défilement + auto-compteurs
  // ---------------------------------------------------------
  var io = "IntersectionObserver" in window
    ? new IntersectionObserver(function (entries) {
        entries.forEach(function (e) {
          if (!e.isIntersecting) return;
          var el = e.target;
          if (el.hasAttribute("data-countup")) countUp(el);
          el.classList.add("fx-in");
          io.unobserve(el);
        });
      }, { threshold: 0.15, rootMargin: "0px 0px -40px 0px" })
    : null;

  function enhance(root) {
    root = root || document;
    // Apparition douce des cartes.
    var cards = root.querySelectorAll ? root.querySelectorAll(".mp-card:not(.fx-reveal), [data-reveal]:not(.fx-reveal)") : [];
    cards.forEach(function (el) {
      el.classList.add("fx-reveal");
      if (io) io.observe(el); else el.classList.add("fx-in");
    });
    // Compteurs marqués.
    var nums = root.querySelectorAll ? root.querySelectorAll("[data-countup]:not([data-fx-counted])") : [];
    nums.forEach(function (el) {
      if (io) io.observe(el); else countUp(el);
    });
  }

  // Ré-analyse quand Blazor injecte/remplace du DOM.
  var mo = new MutationObserver(function (muts) {
    for (var i = 0; i < muts.length; i++) {
      var added = muts[i].addedNodes;
      for (var k = 0; k < added.length; k++) {
        if (added[k].nodeType === 1) enhance(added[k]);
      }
    }
  });

  function boot() {
    // Observer d'abord : si enhance échoue, le DOM injecté restera pris en charge.
    try { mo.observe(document.body, { childList: true, subtree: true }); } catch (e) { /* noop */ }
    try { enhance(document); } catch (e) { /* noop */ }
    // Filet de sécurité : révèle tout élément marqué mais resté masqué (au cas où
    // l'IntersectionObserver n'aurait pas déclenché), après un court délai.
    setTimeout(function () {
      document.querySelectorAll(".fx-reveal:not(.fx-in)").forEach(function (el) {
        var r = el.getBoundingClientRect();
        if (r.top < window.innerHeight && r.bottom > 0) el.classList.add("fx-in");
      });
    }, 1200);
  }
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }

  window.hbafx = { toast: toast, celebrate: celebrate, countUp: countUp, enhance: enhance };
})();
