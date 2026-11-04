import { api, errorText } from "../api.js";
import { h } from "../dom.js";
import { allowTabs, saveShortcut, setStatus } from "./ui.js";

interface PageInfo {
  name: string;
  routable: boolean;
}

const NEW_PAGE = `<leaf:page title="New page" nav="New page" order="50" />

<h1>New page</h1>

<leaf:mfm>
  Write something **nice** here.
</leaf:mfm>
`;

const CHEAT_SHEET = `<leaf:page title="About" nav="About" order="10" />   page title, menu label, menu position
<leaf:page layout="none" />                            don't wrap this page in _layout
<leaf:slot />                                          (in _layout) where the page goes
<leaf:include src="_footer" />                         paste in another .leaf file
<leaf:nav />                                           menu of every page with nav="..."
<leaf:posts limit="10" tag="news" />                   list of posts (tag is optional)
<leaf:post />  <leaf:comments />                       (in post.leaf) the post and its comments
<leaf:mfm> **MFM** $[tada here] </leaf:mfm>            write MFM right in a page
<leaf:set name="greeting" value="Hi {{user.name}}" />  make your own variable

<leaf:if test="signed-in"> ... <leaf:else /> ... </leaf:if>
   tests: signed-in  anon  member  admin  user  guest  registration  guests  comments  post
          any.variable (true when not empty), and a leading ! flips it: test="!admin"

{{site.title}}  {{site.description}}  {{page.title}}  {{page.name}}  {{user.name}}  {{user.role}}
{{post.id}}  {{post.title}}  {{post.slug}}  {{year}}  {{query.anything}}  {{your.own}}`;

export async function showPages(view: HTMLElement, name?: string) {
  const pages = await api<PageInfo[]>("GET", "/api/pages");
  const current = pages.find((page) => page.name === name) ?? pages.find((page) => page.name === "index") ?? pages[0];
  if (!current) return;

  const { source } = await api<{ source: string }>("GET", `/api/pages/${current.name}`);
  const editor = h("textarea", { class: "code", spellcheck: "false" }, source);
  const status = h("p");
  allowTabs(editor);

  async function save(event: Event) {
    event.preventDefault();
    try {
      await api("PUT", `/api/pages/${current!.name}`, { source: editor.value });
      setStatus(status, "Saved", true);
    } catch (e) {
      setStatus(status, errorText(e));
    }
  }

  async function create() {
    const newName = prompt("Page name (lowercase letters, numbers, dashes; start with _ for a partial):")?.trim().toLowerCase();
    if (!newName) return;
    if (!pages.some((page) => page.name === newName)) {
      try {
        await api("PUT", `/api/pages/${newName}`, { source: NEW_PAGE });
      } catch (e) {
        return alert(errorText(e));
      }
    }
    location.hash = `#pages/${newName}`;
  }

  async function remove() {
    if (!confirm(`Delete ${current!.name}.leaf?`)) return;
    try {
      await api("DELETE", `/api/pages/${current!.name}`);
      location.hash = "#pages";
    } catch (e) {
      setStatus(status, errorText(e));
    }
  }

  const form = h("form", { class: "leaf-form editor", onsubmit: save },
    h("div", { class: "toolbar" },
      h("h2", {}, `${current.name}.leaf`),
      current.routable && h("a", { href: current.name === "index" ? "/" : `/${current.name}`, target: "_blank" }, "View page →"),
    ),
    editor,
    status,
    h("div", { class: "toolbar" },
      h("button", {}, "Save"),
      h("button", { type: "button", class: "link danger", onclick: remove }, "Delete page"),
    ),
  );
  saveShortcut(form);

  view.replaceChildren(
    h("div", { class: "toolbar" }, h("h1", {}, "Pages"), h("button", { onclick: create }, "New page")),
    h("div", { class: "pages" },
      h("ul", { class: "page-list" },
        pages.map((page) =>
          h("li", {}, h("a", { href: `#pages/${page.name}`, "aria-current": page === current }, `${page.name}.leaf`)),
        ),
      ),
      form,
    ),
    h("details", { class: "cheatsheet" }, h("summary", {}, ".leaf cheat sheet"), h("pre", {}, CHEAT_SHEET)),
  );
}
