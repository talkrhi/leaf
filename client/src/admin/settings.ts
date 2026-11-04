import { api, errorText } from "../api.js";
import { h } from "../dom.js";
import { field, setStatus } from "./ui.js";

interface Settings {
  title: string;
  description: string;
  allowRegistration: boolean;
  allowGuests: boolean;
  allowComments: boolean;
}

export async function showSettings(view: HTMLElement) {
  const settings = await api<Settings>("GET", "/api/settings");

  const title = h("input", { value: settings.title, required: true });
  const description = h("input", { value: settings.description });
  const allowRegistration = h("input", { type: "checkbox", checked: settings.allowRegistration });
  const allowGuests = h("input", { type: "checkbox", checked: settings.allowGuests });
  const allowComments = h("input", { type: "checkbox", checked: settings.allowComments });
  const status = h("p");

  async function save(event: Event) {
    event.preventDefault();
    try {
      await api("PUT", "/api/settings", {
        title: title.value,
        description: description.value,
        allowRegistration: allowRegistration.checked,
        allowGuests: allowGuests.checked,
        allowComments: allowComments.checked,
      } satisfies Settings);
      setStatus(status, "Saved", true);
    } catch (e) {
      setStatus(status, errorText(e));
    }
  }

  view.replaceChildren(
    h("h1", {}, "Settings"),
    h("form", { class: "leaf-form", onsubmit: save },
      field("Blog title", title),
      field("Description", description),
      h("label", { class: "check" }, allowRegistration, "Anyone can create an account"),
      h("label", { class: "check" }, allowGuests, "Visitors can continue as a guest"),
      h("label", { class: "check" }, allowComments, "Comments are turned on"),
      status,
      h("button", {}, "Save settings"),
    ),
  );
}
