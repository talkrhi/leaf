import { errorText } from "../api.js";
import { h } from "../dom.js";
import { showEmojis } from "./emojis.js";
import { showPages } from "./pages.js";
import { showPosts } from "./posts.js";
import { showSettings } from "./settings.js";
import { showUsers } from "./users.js";

type View = (view: HTMLElement, arg?: string) => Promise<void>;

const views: Record<string, View> = {
  posts: showPosts,
  pages: showPages,
  users: showUsers,
  emojis: showEmojis,
  settings: showSettings,
};

async function route() {
  const [hashName, arg] = location.hash.slice(1).split("/");
  const name = Object.hasOwn(views, hashName) ? hashName : "posts";
  const show = views[name];
  const view = document.getElementById("view")!;

  for (const tab of document.querySelectorAll<HTMLAnchorElement>("#tabs a")) {
    tab.toggleAttribute("aria-current", tab.hash === `#${name}`);
  }

  view.replaceChildren(h("p", { class: "muted" }, "Loading..."));
  try {
    await show(view, arg && decodeURIComponent(arg));
  } catch (e) {
    view.replaceChildren(h("p", { class: "form-error" }, errorText(e)));
  }
}

window.addEventListener("hashchange", route);
route();
