import { api, errorText } from "./api.js";
import { loadEmojis } from "./mfm/emojis.js";
import { renderMfm } from "./mfm/render.js";

async function renderBlocks() {
  const blocks = document.querySelectorAll<HTMLElement>("[data-mfm]");
  if (blocks.length === 0) return;

  const emojis = await loadEmojis();
  for (const el of blocks) {
    const media = el.dataset.mfm !== "limited";
    el.replaceChildren(renderMfm(el.textContent ?? "", { media, emojis }));
    el.removeAttribute("data-mfm");
  }
}

renderBlocks();

function returnUrl(fallback: string): string {
  const params = new URLSearchParams(location.search);
  const target = params.get("returnUrl") ?? params.get("ReturnUrl");
  return target && /^\/(?![/\\])/.test(target) ? target : fallback;
}

document.addEventListener("submit", async (event) => {
  const form = event.target as HTMLFormElement;
  const endpoint = form.dataset.leafForm;
  if (!endpoint) return;
  event.preventDefault();

  const error = form.querySelector<HTMLElement>("[data-error]");
  const button = form.querySelector("button");
  if (error) error.textContent = "";
  button?.setAttribute("disabled", "");

  try {
    await api("POST", endpoint, Object.fromEntries(new FormData(form)));
    if (form.dataset.redirect) location.href = returnUrl(form.dataset.redirect);
    else location.reload();
  } catch (e) {
    if (error) error.textContent = errorText(e);
    button?.removeAttribute("disabled");
  }
});

document.addEventListener("click", async (event) => {
  const button = (event.target as Element).closest<HTMLElement>("[data-leaf-action]");
  if (!button) return;

  try {
    switch (button.dataset.leafAction) {
      case "logout":
        await api("POST", "/api/auth/logout");
        location.href = "/";
        break;
      case "guest":
        await api("POST", "/api/auth/guest");
        location.href = returnUrl("/");
        break;
      case "delete-comment":
        if (!confirm("Delete this comment?")) return;
        await api("DELETE", `/api/comments/${button.dataset.id}`);
        button.closest(".comment")?.remove();
        break;
    }
  } catch (e) {
    alert(errorText(e));
  }
});
