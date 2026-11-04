import { api, errorText } from "../api.js";
import { h } from "../dom.js";
import { loadEmojis } from "../mfm/emojis.js";
import { renderMfm } from "../mfm/render.js";
import { field, formatDate, saveShortcut, setStatus } from "./ui.js";

interface Post {
  id: number;
  slug: string;
  title: string;
  body: string;
  tags: string;
  published: boolean;
  authorName: string;
  createdAt: string;
}

const TOOLBAR: [string, string, string][] = [
  ["Bold", "**", "**"],
  ["Italic", "<i>", "</i>"],
  ["Strike", "~~", "~~"],
  ["Code", "`", "`"],
  ["Link", "[", "](https://)"],
  ["Center", "<center>", "</center>"],
  ["Small", "<small>", "</small>"],
  ["Color", "$[fg.color=e8590c ", "]"],
  ["Highlight", "$[bg.color=ffec99 ", "]"],
  ["Big", "$[x2 ", "]"],
  ["Tada", "$[tada ", "]"],
  ["Jelly", "$[jelly ", "]"],
  ["Spin", "$[spin ", "]"],
  ["Bounce", "$[bounce ", "]"],
  ["Rainbow", "$[rainbow ", "]"],
  ["Sparkle", "$[sparkle ", "]"],
  ["Blur", "$[blur ", "]"],
];

export async function showPosts(view: HTMLElement, id?: string) {
  if (id === "new") return showEditor(view, null);
  if (id) return showEditor(view, await api<Post>("GET", `/api/posts/${id}`));

  const posts = await api<Post[]>("GET", "/api/posts");

  async function remove(post: Post) {
    if (!confirm(`Delete "${post.title}"? This can't be undone.`)) return;
    await api("DELETE", `/api/posts/${post.id}`);
    await showPosts(view);
  }

  const rows = posts.map((post) =>
    h("tr", {},
      h("td", {}, h("a", { href: `#posts/${post.id}` }, post.title)),
      h("td", {}, post.published ? "Published" : h("span", { class: "badge" }, "Draft")),
      h("td", {}, formatDate(post.createdAt)),
      h("td", { class: "actions" },
        h("a", { href: `/post/${post.slug}`, target: "_blank" }, "View"),
        h("button", { class: "link danger", onclick: () => remove(post) }, "Delete"),
      ),
    ),
  );

  view.replaceChildren(
    h("div", { class: "toolbar" }, h("h1", {}, "Posts"), h("a", { class: "button", href: "#posts/new" }, "New post")),
    posts.length === 0
      ? h("p", { class: "muted" }, "No posts yet. Write your first one!")
      : h("table", { class: "table" },
          h("thead", {}, h("tr", {}, h("th", {}, "Title"), h("th", {}, "Status"), h("th", {}, "Created"), h("th"))),
          h("tbody", {}, rows),
        ),
  );
}

async function showEditor(view: HTMLElement, post: Post | null) {
  const emojis = await loadEmojis();
  const title = h("input", { value: post?.title ?? "", placeholder: "A great title", required: true });
  const slug = h("input", { value: post?.slug ?? "", placeholder: "made from the title" });
  const tags = h("input", { value: post?.tags ?? "", placeholder: "news, life" });
  const published = h("input", { type: "checkbox", checked: post?.published ?? false });
  const body = h("textarea", { placeholder: "Write your post with MFM..." }, post?.body ?? "");
  const preview = h("div", { class: "mfm preview" });
  const status = h("p");

  const update = () => preview.replaceChildren(renderMfm(body.value, { emojis }));
  body.addEventListener("input", update);
  update();

  function wrap(before: string, after: string) {
    const { selectionStart: start, selectionEnd: end } = body;
    body.setRangeText(before + body.value.slice(start, end) + after, start, end);
    body.setSelectionRange(start + before.length, end + before.length);
    body.focus();
    update();
  }

  function insert(text: string) {
    body.setRangeText(text, body.selectionStart, body.selectionEnd, "end");
    body.focus();
    update();
  }

  async function save(event: Event) {
    event.preventDefault();
    const input = {
      title: title.value,
      slug: slug.value,
      tags: tags.value,
      body: body.value,
      published: published.checked,
    };
    try {
      post = post
        ? await api<Post>("PUT", `/api/posts/${post.id}`, input)
        : await api<Post>("POST", "/api/posts", input);
      slug.value = post.slug;
      history.replaceState(null, "", `#posts/${post.id}`);
      setStatus(status, `Saved ${post.published ? "(published)" : "(draft)"}`, true);
    } catch (e) {
      setStatus(status, errorText(e));
    }
  }

  const form = h("form", { class: "leaf-form editor", onsubmit: save },
    h("div", { class: "toolbar" },
      h("h1", {}, post ? "Edit post" : "New post"),
      h("a", { href: "#posts" }, "← All posts"),
    ),
    field("Title", title),
    h("div", { class: "row" }, field("Slug", slug), field("Tags", tags)),
    h("div", { class: "mfm-toolbar" },
      TOOLBAR.map(([label, before, after]) => h("button", { type: "button", onclick: () => wrap(before, after) }, label)),
    ),
    emojis.size > 0 && h("div", { class: "mfm-toolbar emoji-picker" },
      [...emojis].map(([name, url]) =>
        h("button", { type: "button", title: `:${name}:`, onclick: () => insert(`:${name}:`) },
          h("img", { class: "mfm-emoji mfm-emoji-custom", src: url, alt: `:${name}:` }),
        ),
      ),
    ),
    h("div", { class: "split" }, body, preview),
    h("label", { class: "check" }, published, "Published (visible to everyone)"),
    status,
    h("button", {}, "Save"),
  );
  saveShortcut(form);
  view.replaceChildren(form);
}
