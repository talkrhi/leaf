import { api, errorText } from "../api.js";
import { h } from "../dom.js";
import { field, formatDate, setStatus } from "./ui.js";

interface User {
  id: number;
  username: string;
  role: string;
  createdAt: string;
}

const roleSelect = (current: string) =>
  h("select", {}, ["admin", "user", "guest"].map((role) => h("option", { value: role, selected: role === current }, role)));

export async function showUsers(view: HTMLElement) {
  const users = await api<User[]>("GET", "/api/users");

  async function change(action: () => Promise<unknown>) {
    try {
      await action();
    } catch (e) {
      alert(errorText(e));
    }
    await showUsers(view);
  }

  async function removeGuests() {
    const { removed } = await api<{ removed: number }>("POST", "/api/users/remove-guests");
    alert(`Removed ${removed} guest account(s) that never commented.`);
  }

  const rows = users.map((user) => {
    const role = roleSelect(user.role);
    role.addEventListener("change", () => change(() => api("PUT", `/api/users/${user.id}`, { role: role.value })));

    const resetPassword = () => {
      const password = prompt(`New password for ${user.username} (8+ characters):`);
      if (password) change(() => api("PUT", `/api/users/${user.id}`, { password }));
    };
    const remove = () => {
      if (confirm(`Delete ${user.username} and all their comments?`)) change(() => api("DELETE", `/api/users/${user.id}`));
    };

    return h("tr", {},
      h("td", {}, user.username),
      h("td", {}, role),
      h("td", {}, formatDate(user.createdAt)),
      h("td", { class: "actions" },
        h("button", { class: "link", onclick: resetPassword }, "Reset password"),
        h("button", { class: "link danger", onclick: remove }, "Delete"),
      ),
    );
  });

  const username = h("input", { required: true, autocomplete: "off" });
  const password = h("input", { type: "password", required: true, minlength: 8, autocomplete: "new-password" });
  const role = roleSelect("user");
  const status = h("p");

  async function create(event: Event) {
    event.preventDefault();
    try {
      await api("POST", "/api/users", { username: username.value, password: password.value, role: role.value });
      await showUsers(view);
    } catch (e) {
      setStatus(status, errorText(e));
    }
  }

  view.replaceChildren(
    h("div", { class: "toolbar" },
      h("h1", {}, "Users"),
      h("button", { class: "link", onclick: () => change(removeGuests) }, "Remove unused guests"),
    ),
    h("table", { class: "table" },
      h("thead", {}, h("tr", {}, h("th", {}, "Username"), h("th", {}, "Role"), h("th", {}, "Joined"), h("th"))),
      h("tbody", {}, rows),
    ),
    h("h2", {}, "Create an account"),
    h("form", { class: "leaf-form inline", onsubmit: create },
      field("Username", username),
      field("Password", password),
      field("Role", role),
      h("button", {}, "Create"),
    ),
    status,
  );
}
