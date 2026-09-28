"use strict";

const $ = (id) => document.getElementById(id);
const POLL_MS = 3000;
const COLORS = ["#1a73e8", "#d93025", "#188038", "#e37400", "#a142f4", "#12b5cb", "#e52592", "#5f6368"];

const state = { messages: [], query: "", openId: null, tab: "html", detail: null };

const svg = (path) => {
  const el = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  el.setAttribute("viewBox", "0 0 24 24");
  const p = document.createElementNS("http://www.w3.org/2000/svg", "path");
  p.setAttribute("d", path);
  el.append(p);
  return el;
};
const CLIP = "M16.5 6v11.5a4 4 0 0 1-8 0V5a2.5 2.5 0 0 1 5 0v10.5a1 1 0 0 1-2 0V6H10v9.5a2.5 2.5 0 0 0 5 0V5a4 4 0 0 0-8 0v12.5a5.5 5.5 0 0 0 11 0V6h-1.5z";

const api = async (path, options) => {
  const response = await fetch(path, options);
  if (!response.ok && response.status !== 404) throw new Error(`${path}: ${response.status}`);
  return response;
};

/** Builds an element from a tag, class name and children; text is always set as text, never as markup. */
const h = (tag, className, ...children) => {
  const el = document.createElement(tag);
  if (className) el.className = className;
  el.append(...children);
  return el;
};

const senderOf = (m) => m.fromName || m.fromAddress || "(unknown sender)";
const colorOf = (text) => COLORS[[...text].reduce((sum, c) => sum + c.charCodeAt(0), 0) % COLORS.length];

function avatar(m) {
  const name = senderOf(m);
  const el = h("span", "avatar", name.trim().charAt(0) || "?");
  el.style.background = colorOf(name);
  return el;
}

function shortTime(iso) {
  const date = new Date(iso);
  const now = new Date();
  if (date.toDateString() === now.toDateString()) return date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
  const options = date.getFullYear() === now.getFullYear() ? { month: "short", day: "numeric" } : { year: "numeric", month: "short", day: "numeric" };
  return date.toLocaleDateString([], options);
}

const longTime = (iso) => new Date(iso).toLocaleString([], { dateStyle: "medium", timeStyle: "short" });

/* ---------- inbox ---------- */

async function loadList() {
  const response = await api(`/api/messages?q=${encodeURIComponent(state.query)}`);
  state.messages = await response.json();
  renderList();
}

function renderList() {
  const unread = state.messages.filter((m) => !m.read).length;
  const count = $("unread-count");
  count.hidden = unread === 0 || state.query !== "";
  count.textContent = unread;
  document.title = unread > 0 ? `(${unread}) Nexo Mail` : "Nexo Mail";
  $("list-info").textContent = state.messages.length === 1 ? "1 message" : `${state.messages.length} messages`;

  $("empty").hidden = state.messages.length > 0;
  $("empty-title").textContent = state.query ? "No messages match your search" : "Your inbox is empty";
  $("empty-text").textContent = state.query ? "Try different words." : "Emails sent by Nexo (or any SMTP client) show up here.";

  $("rows").replaceChildren(...state.messages.map(row));
}

function row(m) {
  const li = h(
    "li",
    `row${m.read ? "" : " unread"}`,
    avatar(m),
    h("span", "from", senderOf(m)),
    h("span", "preview", m.subject, h("span", "snippet", ` – ${m.snippet}`)),
    ...(m.hasAttachments ? [Object.assign(svg(CLIP), { className: "clip" })] : []),
    Object.assign(h("time", null, shortTime(m.receivedAt)), { title: longTime(m.receivedAt) }),
  );
  li.tabIndex = 0;
  li.setAttribute("role", "button");
  li.addEventListener("click", () => openMessage(m.id));
  li.addEventListener("keydown", (e) => e.key === "Enter" && openMessage(m.id));
  return li;
}

/* ---------- reader ---------- */

async function openMessage(id) {
  const response = await api(`/api/messages/${id}`);
  if (response.status === 404) return showList();
  const detail = await response.json();
  state.openId = id;
  state.detail = detail;
  state.tab = detail.html ? "html" : "text";
  await api(`/api/messages/${id}/read`, { method: "POST" });
  $("list-view").hidden = true;
  $("reader").hidden = false;
  renderReader();
  loadList();
}

function showList() {
  state.openId = null;
  state.detail = null;
  $("reader").hidden = true;
  $("list-view").hidden = false;
  loadList();
}

function renderReader() {
  const m = state.detail;
  $("subject").textContent = m.subject;
  const av = avatar(m);
  $("sender-avatar").replaceWith(Object.assign(av, { id: "sender-avatar" }));
  $("sender-name").textContent = senderOf(m);
  $("sender-address").textContent = m.fromAddress ? `<${m.fromAddress}>` : "";
  $("recipients").textContent = `to ${m.to.length ? m.to.join(", ") : "undisclosed recipients"}${m.cc.length ? ` · cc ${m.cc.join(", ")}` : ""}`;
  $("sent-at").textContent = longTime(m.sentAt ?? m.receivedAt);

  $("attachments").replaceChildren(
    ...m.attachments.map((a) => {
      const link = h("a", "chip", svg(CLIP), `${a.fileName} (${formatSize(a.size)})`);
      link.href = `/api/messages/${m.id}/attachments/${a.index}`;
      link.download = a.fileName;
      return link;
    }),
  );

  const tabs = [["html", "HTML", m.html], ["text", "Text", m.text], ["source", "Source", true]].filter(([, , has]) => has);
  $("tabs").replaceChildren(
    ...tabs.map(([key, label]) => {
      const tab = h("button", "tab", label);
      tab.setAttribute("role", "tab");
      tab.setAttribute("aria-selected", String(state.tab === key));
      tab.addEventListener("click", () => {
        state.tab = key;
        renderReader();
      });
      return tab;
    }),
  );
  renderBody();
}

async function renderBody() {
  const m = state.detail;
  const body = $("body");
  if (state.tab === "html") {
    const frame = document.createElement("iframe");
    // No scripts run in the sandbox; same-origin is only allowed so the frame can be sized to its content.
    frame.setAttribute("sandbox", "allow-same-origin allow-popups allow-popups-to-escape-sandbox");
    frame.title = "Message body";
    frame.srcdoc = `<base target="_blank"><style>body{margin:16px;font:14px/1.5 Arial,sans-serif;color:#202124}img{max-width:100%}</style>${m.html}`;
    frame.addEventListener("load", () => {
      frame.style.height = `${frame.contentDocument.documentElement.scrollHeight + 8}px`;
    });
    body.replaceChildren(frame);
  } else if (state.tab === "text") {
    body.replaceChildren(h("pre", null, m.text ?? ""));
  } else {
    const raw = await (await api(`/api/messages/${m.id}/raw`)).text();
    if (state.tab === "source") body.replaceChildren(h("pre", null, raw));
  }
}

const formatSize = (bytes) => (bytes < 1024 ? `${bytes} B` : bytes < 1048576 ? `${(bytes / 1024).toFixed(1)} KB` : `${(bytes / 1048576).toFixed(1)} MB`);

/* ---------- actions ---------- */

$("back").addEventListener("click", showList);
$("refresh").addEventListener("click", loadList);
$("delete").addEventListener("click", async () => {
  await api(`/api/messages/${state.openId}`, { method: "DELETE" });
  showList();
});
$("mark-unread").addEventListener("click", async () => {
  await api(`/api/messages/${state.openId}/read?read=false`, { method: "POST" });
  showList();
});
$("mark-all-read").addEventListener("click", async () => {
  await Promise.all(state.messages.filter((m) => !m.read).map((m) => api(`/api/messages/${m.id}/read`, { method: "POST" })));
  loadList();
});
$("delete-all").addEventListener("click", async () => {
  if (!confirm("Delete every message in this mailbox?")) return;
  await api("/api/messages", { method: "DELETE" });
  showList();
});
$("menu").addEventListener("click", () => $("layout").classList.toggle("collapsed"));
$("inbox-link").addEventListener("click", (e) => {
  e.preventDefault();
  showList();
});
$("search").addEventListener("submit", (e) => e.preventDefault());
$("q").addEventListener("input", (e) => {
  state.query = e.target.value.trim();
  if (state.openId) showList();
  else loadList();
});
document.addEventListener("keydown", (e) => {
  if (e.key === "Escape" && state.openId) showList();
  if (e.key === "/" && document.activeElement !== $("q")) {
    e.preventDefault();
    $("q").focus();
  }
});

api("/api/info")
  .then((r) => r.json())
  .then((info) => ($("smtp-address").textContent = `localhost:${info.smtpPort}`))
  .catch(() => {});
loadList();
setInterval(() => !state.openId && loadList().catch(() => {}), POLL_MS);
