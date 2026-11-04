import { api, errorText } from "../api.js";
import { h } from "../dom.js";
import type { Emoji } from "../mfm/emojis.js";
import { field, setStatus } from "./ui.js";

export async function showEmojis(view: HTMLElement) {
  const emojis = await api<Emoji[]>("GET", "/api/emojis");

  const name = h("input", { name: "name", required: true, autocomplete: "off", placeholder: "party_leaf" });
  const file = h("input", { name: "file", type: "file", required: true, accept: "image/png,image/gif,image/webp,image/jpeg" });
  const status = h("p");

  file.addEventListener("change", () => {
    const picked = file.files?.[0];
    if (picked && !name.value) {
      name.value = picked.name.replace(/\.[^.]+$/, "").toLowerCase().replace(/[^a-z0-9_-]+/g, "_").slice(0, 32);
    }
  });

  async function upload(event: Event) {
    event.preventDefault();
    try {
      await api("POST", "/api/emojis", new FormData(form));
      await showEmojis(view);
    } catch (e) {
      setStatus(status, errorText(e));
    }
  }

  async function remove(emoji: Emoji) {
    if (!confirm(`Delete :${emoji.name}:?`)) return;
    try {
      await api("DELETE", `/api/emojis/${emoji.name}`);
      await showEmojis(view);
    } catch (e) {
      alert(errorText(e));
    }
  }

  const rows = emojis.map((emoji) =>
    h("tr", {},
      h("td", {}, h("img", { class: "emoji-preview", src: emoji.url, alt: `:${emoji.name}:` })),
      h("td", {}, h("code", {}, `:${emoji.name}:`)),
      h("td", { class: "actions" }, h("button", { class: "link danger", onclick: () => remove(emoji) }, "Delete")),
    ),
  );

  const form = h("form", { class: "leaf-form", onsubmit: upload },
    field("Name", name),
    field("Image (PNG, GIF, WebP or JPEG, up to 1 MB)", file),
    status,
    h("button", {}, "Upload"),
  );

  view.replaceChildren(
    h("h1", {}, "Emojis"),
    h("p", { class: "muted" }, "Write :name: in a post or comment to use a custom emoji."),
    emojis.length === 0
      ? h("p", { class: "muted" }, "No custom emojis yet.")
      : h("table", { class: "table" },
          h("thead", {}, h("tr", {}, h("th", {}, "Emoji"), h("th", {}, "Name"), h("th"))),
          h("tbody", {}, rows),
        ),
    h("h2", {}, "Add an emoji"),
    form,
  );
}
